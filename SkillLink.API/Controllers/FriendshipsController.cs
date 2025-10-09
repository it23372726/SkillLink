using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/friends")]
    [Authorize]
    public class FriendshipsController : ControllerBase
    {
        private readonly IFriendshipService _service;

        public FriendshipsController(IFriendshipService service) => _service = service;

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);


        [HttpGet("followers")]
        public IActionResult GetFollowers()
        {
            var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var list = _service.GetFollowers(me);
            return Ok(list);
        }


        [HttpPost("{userId}/follow")]
        public IActionResult Follow(int userId)
        {
            try
            {
                _service.Follow(CurrentUserId, userId);
                return Ok(new { message = "Followed successfully" });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("{userId}/unfollow")]
        public IActionResult Unfollow(int userId)
        {
            _service.Unfollow(CurrentUserId, userId);
            return Ok(new { message = "Unfollowed successfully" });
        }

        [HttpGet("my")]
        public IActionResult GetMyFriends() => Ok(_service.GetMyFriends(CurrentUserId));

        [Authorize]
        [HttpGet("search")]
        public IActionResult Search([FromQuery] string q)
        {
            var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var results = _service.SearchUsers(q ?? "", me);  // exclude me in service
            return Ok(results);
        }

        // GET /api/friends/{id}/followers
        [HttpGet("{id:int}/followers")]
        public async Task<IActionResult> GetFollowers([FromRoute] int id)
        {
            var list = await _service.GetFollowersAsync(id);
            return Ok(list);
        }

        // GET /api/friends/{id}/following
        [HttpGet("{id:int}/following")]
        public async Task<IActionResult> GetFollowing([FromRoute] int id)
        {
            var list = await _service.GetFollowingAsync(id);
            return Ok(list);
        }

    }
}
