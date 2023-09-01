using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class MessagesController : BaseAPIController
    {
        private readonly IUnitOfWork _unit;
        private readonly IMapper _mapper;

        public MessagesController(IUnitOfWork unit, IMapper mapper)
        {
            _unit = unit;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO)
        {
            var username = User.GetUsername();

            if(username == createMessageDTO.RecipientUsername.ToLower())
            {
                return BadRequest("Вы не можете написать сообщение самому себе!!!");
            }
            var sender = await _unit.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await _unit.UserRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);

            if (recipient == null)
                return NotFound();

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDTO.Content
            };

            _unit.MessageRepository.AddMessage(message);

            if(await _unit.Complete()) return Ok(_mapper.Map<MessageDTO>(message));

            return BadRequest("Ошибка отправки сообщения!");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MessageDTO>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
        {
            messageParams.Username = User.GetUsername();

            var messages = await _unit.MessageRepository.GetMessageForUser(messageParams);

            Response.AddPaginationHeader(new PaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages));

            return messages;
        }



        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUsername();

            var message = await _unit.MessageRepository.GetMessage(id);

            if (message.SenderUsername != username && message.RecipientUsername != username)
            {
                return Unauthorized();
            }

            if (message.SenderUsername == username) { message.SenderDeleted = true; }
            if (message.RecipientUsername == username) { message.RecipientDeleted = true; }

            if(message.SenderDeleted && message.RecipientDeleted)
            {
                _unit.MessageRepository.RemoveMessage(message);
            }

            if (await _unit.Complete()) return Ok();

            return BadRequest("Не удалено!!!");
        }
    }
}
