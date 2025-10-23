using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _service;

        public FeedbackController(IFeedbackService service)
        {
            _service = service;
        }

        private int? GetUserIdFromClaims()
        {
            try
            {
                var candidates = new[]
                {
                    "userId", "id", ClaimTypes.NameIdentifier, ClaimTypes.Sid, "sub"
                };

                foreach (var c in candidates)
                {
                    var claim = User.FindFirst(c);
                    if (claim != null && int.TryParse(claim.Value, out var id))
                        return id;
                }
            }
            catch { /* ignore */ }

            return null; // allow anonymous feedback if needed
        }

        // POST /api/feedback
        [HttpPost]
        [AllowAnonymous] // or [Authorize] if you want only logged-in users
        public ActionResult Submit([FromBody] FeedbackCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { message = "Message is required." });

            var uid = GetUserIdFromClaims();
            var id = _service.Submit(dto, uid);

            // Optionally: emit an admin notification (if you have a notification service)
            // try { _notifications.NotifyAdmins("New feedback", dto.Subject ?? "General", $"/admin/feedback/{id}"); } catch {}

            return CreatedAtAction(nameof(GetList), new { id }, new { feedbackId = id });
        }

        // GET /api/feedback?isRead=false&limit=50&offset=0
        // Admin-only listing
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult<List<FeedbackItem>> GetList([FromQuery] bool? isRead = null, [FromQuery] int? limit = 50, [FromQuery] int? offset = 0)
        {
            var list = _service.List(isRead, limit, offset);
            return Ok(list);
        }

        // PUT /api/feedback/{id}/read?isRead=true
        [HttpPut("{id:int}/read")]
        [Authorize(Roles = "Admin")]
        public IActionResult MarkRead([FromRoute] int id, [FromQuery] bool isRead = true)
        {
            _service.MarkRead(id, isRead);
            return NoContent();
        }
    }
}
