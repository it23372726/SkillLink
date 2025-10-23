using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly IRequestService _service;
        private readonly IAcceptedRequestService _acceptedRequestService;
        private readonly INotificationService _notificationService;

        public RequestsController(
            IRequestService service,
            IAcceptedRequestService acceptedRequestService,
            INotificationService notificationService)
        {
            _service = service;
            _acceptedRequestService = acceptedRequestService;
            _notificationService = notificationService;
        }

        private int? TryGetUserId()
        {
            var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(s, out var id) ? id : (int?)null;
        }

        private int RequireUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        /* --------------------- Public/semipublic GETs --------------------- */

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            var uid = TryGetUserId();
            return Ok(_service.GetAllRequests(uid));
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public IActionResult Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query is required" });

            var uid = TryGetUserId();
            return Ok(_service.SearchRequests(q, uid));
        }

        [HttpGet("by-requestId/{id}")]
        [AllowAnonymous]
        public IActionResult GetById(int id)
        {
            var req = _service.GetById(id);
            if (req == null) return NotFound(new { message = "Request not found" });

            var uid = TryGetUserId();
            if (req.IsPrivate && uid != req.LearnerId && uid != req.PreferredTutorId)
                return StatusCode(403, new { message = "This request is private." });

            return Ok(req);
        }

        [HttpGet("by-learnerId/{id}")]
        [AllowAnonymous]
        public IActionResult GetByLearnerId(int id)
        {
            var req = _service.GetByLearnerId(id);
            if (req == null || req.Count == 0)
                return NotFound(new { message = "No requests found for this user" });

            return Ok(req);
        }

        /* --------------------- Mutations (require auth) --------------------- */

        [HttpPost]
        [Authorize]
        public IActionResult Create([FromBody] Request req)
        {
            // learner must be the JWT user
            req.LearnerId = RequireUserId();
            req.Status = "PENDING";
            if (req.PreferredTutorId.HasValue && req.PreferredTutorId.Value > 0)
                req.IsPrivate = true;

            _service.AddRequest(req);

            // If direct/private, notify the target user
            if (req.IsPrivate && req.PreferredTutorId is int targetId)
            {
                _notificationService.Send(new Notification
                {
                    UserId = targetId,
                    Type = "PRIVATE_REQUEST_CREATED",
                    Title = "New Private Request",
                    Body = $"{User.Identity?.Name ?? "Someone"} requested: {req.SkillName}" +
                           (string.IsNullOrWhiteSpace(req.Topic) ? "" : $" • {req.Topic}"),
                    Link = "/request"
                });
            }

            return Ok(new { message = "Request Created" });
        }

        [HttpPut("{id}")]
        [Authorize]
        public IActionResult UpdateRequest(int id, [FromBody] Request req)
        {
            var existingRequest = _service.GetById(id);
            if (existingRequest == null)
                return NotFound(new { message = "Request not found" });

            // (Optional) authorization: only owner can edit while pending
            // if (existingRequest.LearnerId != RequireUserId() || existingRequest.Status != "PENDING") { ... }

            _service.UpdateRequest(id, req);
            return Ok(new { message = "Request updated" });
        }

        [HttpPatch("{id}")]
        [Authorize]
        public IActionResult UpdateStatus(int id, [FromBody] string status)
        {
            var existingRequest = _service.GetById(id);
            if (existingRequest == null)
                return NotFound(new { message = "Request not found" });

            _service.UpdateStatus(id, status);
            return Ok(new { message = "Status updated" });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var existingRequest = _service.GetById(id);
            if (existingRequest == null)
                return NotFound(new { message = "Request not found" });

            _service.DeleteRequest(id);
            return Ok(new { message = "Request deleted" });
        }

        /* --------------------- Accept / Decline / Status --------------------- */

        [HttpPost("{id}/accept")]
        [Authorize]
        public IActionResult AcceptRequest(int id)
        {
            try
            {
                var userId = RequireUserId();
                var req = _service.GetById(id);
                if (req == null) return NotFound(new { message = "Request not found" });

                if (req.IsPrivate && req.PreferredTutorId != userId)
                    return StatusCode(403, new { message = "This private request can only be accepted by the targeted user." });

                _acceptedRequestService.AcceptRequest(id, userId);

                _notificationService.Send(new Notification
                {
                    UserId = req.LearnerId,
                    Type = "REQUEST_ACCEPTED",
                    Title = "Your request was accepted",
                    Body = $"{req.SkillName}" + (string.IsNullOrWhiteSpace(req.Topic) ? "" : $" • {req.Topic}"),
                    Link = "/request"
                });

                return Ok(new { message = "Request accepted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("accepted")]
        [Authorize]
        public IActionResult GetAcceptedRequests()
        {
            try
            {
                var userId = RequireUserId();
                var acceptedRequests = _acceptedRequestService.GetAcceptedRequestsByUser(userId);
                return Ok(acceptedRequests);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/accepted-status")]
        [Authorize]
        public IActionResult GetAcceptedStatus(int id)
        {
            try
            {
                var userId = RequireUserId();
                var hasAccepted = _acceptedRequestService.HasUserAcceptedRequest(userId, id);
                return Ok(new { hasAccepted });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("accepted/{id}/schedule")]
        [Authorize]
        public IActionResult ScheduleMeeting(int id, [FromBody] ScheduleMeetingRequest request)
        {
            try
            {
                var userId = RequireUserId();

                // Only the accepting tutor can schedule
                var meta = _acceptedRequestService.GetAcceptedMeta(id);
                if (meta == null) return NotFound(new { message = "Accepted record not found" });
                if (meta.AcceptorId != userId)
                    return StatusCode(403, new { message = "Only the accepting tutor can schedule this session." });

                _acceptedRequestService.ScheduleMeeting(id, request.ScheduleDate, request.MeetingType, request.MeetingLink);
                return Ok(new { message = "Meeting scheduled successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("accepted/requester")]
        [Authorize]
        public IActionResult GetRequestsIAskedFor()
        {
            try
            {
                var userId = RequireUserId();
                var requested = _acceptedRequestService.GetRequestsIAskedFor(userId);
                return Ok(requested);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/decline-directed")]
        [Authorize]
        public IActionResult DeclineDirected(int id)
        {
            var userId = RequireUserId();
            var req = _service.GetById(id);
            if (req == null) return NotFound(new { message = "Request not found" });

            // Only the targeted tutor can decline a private (directed) request
            if (!req.IsPrivate || req.PreferredTutorId != userId)
                return StatusCode(403, new { message = "Only the targeted tutor can decline this private request." });

            // Atomically cancel and clear targeting
            _service.CancelDirected(id);

            // Notify the requester
            _notificationService.Send(new Notification
            {
                UserId = req.LearnerId,
                Type = "PRIVATE_REQUEST_DECLINED",
                Title = "Private request declined",
                Body = $"{User.Identity?.Name ?? "Tutor"} declined your private request for {req.SkillName}" +
                       (string.IsNullOrWhiteSpace(req.Topic) ? "" : $" • {req.Topic}"),
                Link = "/request"
            });

            return Ok(new { message = "Directed request declined; request has been cancelled." });
        }

        [HttpPost("{id}/decline")]
        [Authorize]
        public IActionResult Decline(int id) => DeclineDirected(id);

        [HttpPost("accepted/{id}/complete")]
        [Authorize]
        public IActionResult CompleteAccepted(int id)
        {
            try
            {
                var userId = RequireUserId();

                // Ownership check via slim meta (tutor-only completion)
                var meta = _acceptedRequestService.GetAcceptedMeta(id);
                if (meta == null) return NotFound(new { message = "Accepted record not found" });
                if (meta.AcceptorId != userId)
                    return StatusCode(403, new { message = "Only the accepting tutor can complete this session." });

                // Timing check needs schedule info => use detailed query
                var details = _acceptedRequestService.GetAcceptedDetails(id);
                if (details == null)
                    return NotFound(new { message = "Accepted record not found" });

                if (details.ScheduleDate == null)
                    return BadRequest(new { message = "Cannot complete: no schedule found." });

                if (details.ScheduleDate.Value.ToUniversalTime() > DateTime.UtcNow)
                    return BadRequest(new { message = "Cannot complete before the scheduled time." });

                _acceptedRequestService.Complete(id);

                // Notify requester
                _notificationService.Send(new Notification
                {
                    UserId = details.RequesterId,
                    Type = "REQUEST_COMPLETED",
                    Title = "Lesson completed",
                    Body = "Your scheduled lesson has been marked completed.",
                    Link = "/request"
                });

                return Ok(new { message = "Marked as completed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
