﻿using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography.Xml;

namespace API.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        private readonly IUnitOfWork _unit;
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _presenceHub;

        public MessageHub(IUnitOfWork unit,
            IMapper mapper,
            IHubContext<PresenceHub> presenceHub)
        {
            _unit = unit;
            _mapper = mapper;
            _presenceHub = presenceHub;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"];
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);

            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _unit.MessageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);

            if (_unit.HasChanges()) await _unit.Complete();

            await Clients.Caller.SendAsync("RecieveMessageThread",messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDTO createMessageDTO)
        {
            var username = Context.User.GetUsername();

            if (username == createMessageDTO.RecipientUsername.ToLower())
            {
                throw new HubException("Вы не можете отправить сообщение себе же");
            }
            var sender = await _unit.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await _unit.UserRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);

            if (recipient == null) throw new HubException("Пользователь не найден...");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDTO.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);

            var group = await _unit.MessageRepository.GetMessageGroup(groupName);

            if(group.Connections.Any(x => x.Username == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await PresenceTracker.GetConnectionUser(recipient.UserName);
                if(connections != null)
                {
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }

            _unit.MessageRepository.AddMessage(message);

            if (await _unit.Complete())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDTO>(message));
            }
        }

        private string GetGroupName(string caller,string other)
        {
            var stringCompare = string.CompareOrdinal(caller,other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await _unit.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            if(group == null)
            {
                group = new Group(groupName);
                _unit.MessageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);

            if (await _unit.Complete()) return group;

            throw new HubException("Ошибка добавления группы");
        }

        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await _unit.MessageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _unit.MessageRepository.RemoveConnection(connection);

            if (await _unit.Complete()) return group;
            throw new HubException("Ошибка отчистки группы");
        }
    }
}
