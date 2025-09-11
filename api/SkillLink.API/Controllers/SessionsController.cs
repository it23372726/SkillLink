using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Models;
using SkillLink.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly SessionService _service;

        public SessionsController(SessionService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAllSessions());
        }

        [HttpGet("by-sessionId/{id}")]
        public IActionResult GetById(int id)
        {
            var session = _service.GetById(id);
            if (session == null) return NotFound(new { message = "Session not found" });
            return Ok(session);
        }

        [HttpGet("by-tutorId/{id}")]
        public IActionResult GetByTutorId(int id)
        {
            var sessions = _service.GetByTutorId(id);

            if (sessions == null || sessions.Count == 0)
                return NotFound(new { message = "No sessions found for this tutor" });

            return Ok(sessions);
        }

        // [Authorize(Roles = "Tutor")]
        [HttpPost]
        public IActionResult Create(Session session)
        {
            _service.AddSession(session);
            return Ok(new { message = "Session created successfully" });
        }

        [HttpPatch("{id}")]
        public IActionResult UpdateStatus(int id, [FromBody] string status)
        {
            _service.UpdateStatus(id, status);
            return Ok(new { message = "Session status updated" });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return Ok(new { message = "Session deleted" });
        }
    }
}
