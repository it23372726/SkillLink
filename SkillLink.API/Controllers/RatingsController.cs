using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Services.Abstractions;
using SkillLink.API.Models;
using System.Security.Claims;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingService _service;
        private readonly IAcceptedRequestService _accepted;

        public RatingsController(IRatingService service, IAcceptedRequestService accepted)
        {
            _service = service;
            _accepted = accepted;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public IActionResult Create([FromBody] CreateRatingDto dto)
        {
            if (dto is null) return BadRequest(new { message = "Invalid payload" });
            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { message = "Rating must be 1..5" });

            var meta = _accepted.GetAcceptedMeta(dto.AcceptedRequestId);
            if (meta == null) return NotFound(new { message = "Accepted record not found" });

            var userId = GetUserId();
            if (meta.RequesterId != userId)
                return StatusCode(403, new { message = "Only the requesting learner can rate this session." });

            var status = (meta.Status ?? "").ToUpperInvariant();
            if (status != "COMPLETED")
                return BadRequest(new { message = "You can only rate a completed session." });

            try
            {
                // Derive tutor from accepted meta; ignore client tutorId.
                _service.Create(userId, meta.AcceptorId, dto.AcceptedRequestId, dto.Rating, dto.Comment ?? "");
                return Ok(new { message = "Thanks for the rating!" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("exists/{acceptedRequestId:int}")]
        public IActionResult Exists(int acceptedRequestId)
        {
            var userId = GetUserId();
            var exists = _service.ExistsForAccepted(acceptedRequestId, userId);
            return Ok(new { exists });
        }

        // Tutors: what they RECEIVED
        [HttpGet("received")]
        public IActionResult ListReceived([FromQuery] int limit = 5)
        {
            var tutorId = GetUserId();
            var list = _service.ListReceived(tutorId, Math.Clamp(limit, 1, 50));
            return Ok(list);
        }

        // Learners: what they GAVE
        [HttpGet("given")]
        public IActionResult ListGiven([FromQuery] int limit = 5)
        {
            var learnerId = GetUserId();
            var list = _service.ListGiven(learnerId, Math.Clamp(limit, 1, 50));
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet("summary/{tutorId:int}")]
        public IActionResult Summary(int tutorId)
        {
            var s = _service.SummaryForTutor(tutorId);
            return Ok(s);
        }
    }
}
