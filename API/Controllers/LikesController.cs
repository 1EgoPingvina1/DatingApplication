
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    public class LikesController : BaseAPIController
    {
        private readonly IUnitOfWork _unit;

        public LikesController(IUnitOfWork unit)
        {
            _unit = unit;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username)
        {
            var sourceUserId = User.GetUserId();
            var likedUser = await _unit.UserRepository.GetUserByUsernameAsync(username);
            var sourceUser = await _unit.LikeRepository.GetUserWithLikes(sourceUserId);

            if(likedUser == null) { return NotFound(); }

            if (sourceUser.UserName == username) return BadRequest("Вы не можете ставить лайк себе!!!");

            var userLike = await _unit.LikeRepository.GetUserLike(sourceUserId, likedUser.Id);

            if (userLike != null) return BadRequest("Вы уже оценили данного пользователя");

            userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                TargerUserId = likedUser.Id,
            };

            sourceUser.LikedUsers.Add(userLike);
            if(await _unit.Complete()) { return NoContent(); }

            return BadRequest("Ошибка оценки пользователя");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<LikeDTO>>> GetUserLikes([FromQuery] LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();
            var users = await _unit.LikeRepository.GetUserLikes(likesParams);

            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));

            return Ok(users);
        }

    }
}
