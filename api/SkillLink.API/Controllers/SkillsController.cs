using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Services;
using SkillLink.API.Models;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly SkillService _service;

        public SkillsController(SkillService service)
        {
            _service = service;
        }

        [HttpPost("add")]
        public IActionResult AddSkill(AddSkillRequest req)
        {
            _service.AddSkill(req);
            return Ok(new { message = "Skill added successfully" });
        }

        [HttpDelete("{userId}/{skillId}")]
        public IActionResult DeleteSkill(int userId, int skillId)
        {
            _service.DeleteUserSkill(userId, skillId);
            return Ok(new { message = "Skill deleted successfully" });
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetUserSkills(int userId)
        {
            var skills = _service.GetUserSkills(userId);
            return Ok(skills);
        }

        [HttpGet("suggest")]
        public IActionResult Suggest([FromQuery] string q)
        {
            var list = _service.SuggestSkills(q);
            return Ok(list);
        }

        [HttpGet("filter")]
        public IActionResult FilterUsers([FromQuery] string skill)
        {
            var users = _service.GetUsersBySkill(skill);
            return Ok(users);
        }
    }
}
