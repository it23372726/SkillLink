using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Dtos.Admin;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _admin;

        public AdminController(IAdminService admin)
        {
            _admin = admin;
        }

        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] string? q)
        {
            var list = _admin.GetUsers(q);
            return Ok(list);
        }

        [HttpPut("users/{id}/active")]
        public IActionResult SetActive(int id, [FromBody] AdminUpdateActiveRequest req)
        {
            var ok = _admin.SetUserActive(id, req.IsActive);
            if (!ok) return BadRequest(new { message = "Failed to update" });
            return Ok(new { message = "Updated" });
        }

        // If you created AdminUpdateRoleRequest DTO, use it here:
        [HttpPut("users/{id}/role")]
        public IActionResult SetRole(int id, [FromBody] AdminUpdateRoleRequest req)
        {
            var allowed = new[] { "Learner", "Tutor", "Admin" };
            if (!allowed.Contains(req.Role))
                return BadRequest(new { message = "Invalid role" });

            var ok = _admin.SetUserRole(id, req.Role);
            if (!ok) return BadRequest(new { message = "Failed to update" });
            return Ok(new { message = "Updated" });
        }
    }
}
