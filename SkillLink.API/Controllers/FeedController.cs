using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;
using System.Security.Claims;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/feed")]
    [Authorize]
    public class FeedController : ControllerBase
    {
        private readonly IFeedService _feed;
        private readonly IReactionService _reactions;
        private readonly ICommentService _comments;
        private int RequireUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);


        public FeedController(IFeedService feed, IReactionService reactions, ICommentService comments)
        {
            _feed = feed;
            _reactions = reactions;
            _comments = comments;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? q = null)
        {
            var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(_feed.GetFeed(me, page, pageSize, q));
        }
        [HttpPost("sort")]
        public IActionResult Sort([FromBody] List<FeedItemDto> data)
        {
            if (data == null || !data.Any())
            {
                return Ok(new List<FeedItemDto>()); // Return empty list if input is empty
            }

            var userId = RequireUserId();
            var sortedData = _feed.GetSort(data, userId); // Pass the user ID to the service
            var sortedTitles = sortedData.Select(item => item.Title);
            Console.WriteLine("SORTED DATA Controller: " + string.Join(", ", sortedTitles));

            return Ok(sortedData);
        }

        [HttpPost("{postType}/{postId}/like")]
        public IActionResult Like(string postType, int postId)
        {
            var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            _reactions.UpsertReaction(me, postType.ToUpperInvariant(), postId, "LIKE");
            return Ok(new { message = "Liked" });
        }

        [HttpPost("{postType}/{postId}/dislike")]
        public IActionResult Dislike(string postType, int postId)
        {
            var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            _reactions.UpsertReaction(me, postType.ToUpperInvariant(), postId, "DISLIKE");
            return Ok(new { message = "Disliked" });
        }

        [HttpDelete("{postType}/{postId}/reaction")]
        public IActionResult RemoveReaction(string postType, int postId)
        {
            var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            _reactions.RemoveReaction(me, postType.ToUpperInvariant(), postId);
            return Ok(new { message = "Reaction removed" });
        }

        [HttpGet("{postType}/{postId}/comments")]
        public IActionResult GetComments(string postType, int postId)
        {
            return Ok(_comments.GetComments(postType.ToUpperInvariant(), postId));
        }

        [HttpPost("{postType}/{postId}/comments")]
        public IActionResult AddComment(string postType, int postId, [FromBody] CreateCommentDto body)
        {
            var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (string.IsNullOrWhiteSpace(body.Content)) return BadRequest(new { message = "Empty comment" });
            _comments.Add(postType.ToUpperInvariant(), postId, me, body.Content.Trim());
            return Ok(new { message = "Comment added" });
        }

        [HttpDelete("comments/{commentId}")]
        public IActionResult DeleteComment(int commentId)
        {
            var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "USER";
            var isAdmin = string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);

            _comments.Delete(commentId, me, isAdmin);
            return Ok(new { message = "Comment deleted" });
        }
    }
}
