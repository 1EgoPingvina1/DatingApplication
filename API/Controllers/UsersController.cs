using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    public class UsersController : BaseAPIController
    {
        private readonly IUnitOfWork _unit;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UsersController(IUnitOfWork unit, IMapper mapper,IPhotoService photoService)
        {
            _unit = unit;
            _mapper = mapper;
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDTO>>> GetUsers([FromQuery] UserParams userParams)
        {
            var gender = await _unit.UserRepository.GetUserGender(User.GetUsername());
            userParams.CurrentUsername = User.GetUsername();

            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = gender == "male" ? "female" : "male";
            }

            var users = await _unit.UserRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));
            return Ok(users);
        }


        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDTO>> GetUser(string username)
        {
            return await _unit.UserRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
        {
            //var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //If i not be use ? returns an exception
            var username = User.GetUsername();
            var user = await _unit.UserRepository.GetUserByUsernameAsync(username);
            if(user == null) {
                return NotFound();
            }
            _mapper.Map(memberUpdateDTO, user);
            if(await _unit.Complete()) return NoContent();

            return BadRequest("Ошибка изменения профиля");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
        {
            var user = await _unit.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            if(user == null)
            {
                return NotFound();
            }
            var result = await _photoService.AddPhotoAsync(file);
            if(result.Error != null)
            {
                return BadRequest(result.Error.Message);
            }
            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId,
            };

            if(user.Photos.Count == 0) 
            {
                photo.IsMain = true;
            }
            user.Photos.Add(photo);
            if(await _unit.Complete())
            {
                return CreatedAtAction(nameof(GetUser),
                    new { user.UserName}, _mapper.Map<PhotoDTO>(photo));
            }

            return BadRequest("Ошибка добавления фото");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await _unit.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            if(user == null) { return NotFound(); }

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if(photo == null) { return NotFound(); }
            if(photo.IsMain) { return BadRequest("Это фото уже является основным");}

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;

            if (await _unit.Complete()) return NoContent();

            return BadRequest("Проблема установки основого изображения");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _unit.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x =>x.Id == photoId);
            if(photo == null) { return NotFound();};
            if(photo.IsMain)
            {
                return BadRequest("Вы не можете удалить главное фото!");
            }

            if(photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if(result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if(await _unit.Complete()) { return NoContent(); }
            return BadRequest("Ошибка удаление фото");
        }
    }
}
