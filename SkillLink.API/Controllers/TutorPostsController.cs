using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;
using System.Security.Claims;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/tutor-posts")]
    [Authorize] // Require JWT by default
    public class TutorPostsController : ControllerBase
    {
        private readonly ITutorPostService _service;

        public TutorPostsController(ITutorPostService service)
        {
            _service = service;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public IActionResult Create([FromBody] CreateTutorPostDto dto)
        {
            var post = new TutorPost
            {
                TutorId = GetUserId(),     // <— FIX: get from JWT, not DTO
                Title = dto.Title,
                Description = dto.Description ?? "",
                MaxParticipants = dto.MaxParticipants,
                Status = "Open"
            };
            var newId = _service.CreatePost(post);
            return Ok(new { message = "Post created!", postId = newId });
        }

        [HttpPost("{postId}/image")]
        [RequestSizeLimit(20_000_000)] // ~20MB
        public async Task<IActionResult> UploadImage(int postId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file" });

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lessons");
            Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(file.FileName);
            var fname = $"lesson_{postId}_{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(uploads, fname);

            using (var fs = new FileStream(full, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            var publicUrl = $"/uploads/lessons/{fname}".Replace("\\", "/");
            _service.SetImageUrl(postId, publicUrl);
            return Ok(new { url = publicUrl });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll() => Ok(_service.GetPosts());

        [AllowAnonymous]
        [HttpGet("{postId:int}")]
        public IActionResult GetById(int postId)
        {
            var post = _service.GetById(postId);
            if (post == null) return NotFound(new { message = "Not found" });
            return Ok(post);
        }

        [HttpGet("{postId:int}/accepted-status")]
        public IActionResult AcceptedStatus(int postId)
        {
            try
            {
                var userId = GetUserId();
                var hasAccepted = _service.HasUserAccepted(postId, userId);
                return Ok(new { hasAccepted });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Post not found" });
            }
        }
        [HttpGet("accepted-status")]
        public IActionResult AcceptedStatusMany([FromQuery] string ids)
        {
            if (string.IsNullOrWhiteSpace(ids)) return Ok(new Dictionary<int, bool>());

            var userId = GetUserId();
            var postIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
                            .Where(id => id.HasValue)
                            .Select(id => id!.Value)
                            .Distinct()
                            .ToList();

            if (postIds.Count == 0) return Ok(new Dictionary<int, bool>());

            var map = _service.GetAcceptedMapForUser(postIds, userId); // { postId: true/false }
            return Ok(map);
        }

        [HttpPost("{postId:int}/accept")]
        public IActionResult Accept(int postId)
        {
            try
            {
                var userId = GetUserId();
                _service.AcceptPost(postId, userId);
                return Ok(new { message = "Accepted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

       [HttpPut("{postId:int}/schedule")]
        public IActionResult Schedule(int postId, [FromBody] ScheduleTutorPostDto body)
        {
            _service.Schedule(postId, body);
            return Ok(new { message = "Scheduled successfully" });
        }

        [HttpPut("{postId:int}")]
        public IActionResult Update(int postId, [FromBody] UpdateTutorPostDto dto)
        {
            try
            {
                var tutorId = GetUserId();
                _service.UpdatePost(postId, tutorId, dto);
                return Ok(new { message = "Post updated" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{postId:int}")]
        public IActionResult Delete(int postId)
        {
            try
            {
                var tutorId = GetUserId();
                _service.DeletePost(postId, tutorId);
                return Ok(new { message = "Post deleted" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
