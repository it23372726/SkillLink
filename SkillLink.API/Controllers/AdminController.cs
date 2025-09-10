using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // ← only Admins
    public class AdminController : ControllerBase
    {
        private readonly AdminService _admin;

        public AdminController(AdminService admin)
        {
            _admin = admin;
        }

        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] string? q)
        {
            var list = _admin.GetUsers(q);
            return Ok(list);
        }

        public class UpdateActiveRequest { public bool IsActive { get; set; } }

        [HttpPut("users/{id}/active")]
        public IActionResult SetActive(int id, UpdateActiveRequest req)
        {
            var ok = _admin.SetUserActive(id, req.IsActive);
            if (!ok) return BadRequest(new { message = "Failed to update" });
            return Ok(new { message = "Updated" });
        }

        public class UpdateRoleRequest { public string Role { get; set; } = "Learner"; }

        // Optional: allow changing roles (promote/demote)
        [HttpPut("users/{id}/role")]
        public IActionResult SetRole(int id, UpdateRoleRequest req)
        {
            var allowed = new[] { "Learner", "Tutor", "Admin" };
            if (!allowed.Contains(req.Role)) return BadRequest(new { message = "Invalid role" });

            var ok = _admin.SetUserRole(id, req.Role);
            if (!ok) return BadRequest(new { message = "Failed to update" });
            return Ok(new { message = "Updated" });
        }
    }
}
