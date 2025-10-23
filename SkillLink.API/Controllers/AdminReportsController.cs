using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Models.Reports;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : ControllerBase
    {
        private readonly IReportsService _svc;
        public AdminReportsController(IReportsService svc) => _svc = svc;

        // GET /api/admin/reports/skill-demand?from=2025-01-01&to=2026-01-01&limit=10
        [HttpGet("skill-demand")]
        public ActionResult<List<SkillDemandRow>> SkillDemand([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int limit = 10)
        {
            var data = _svc.GetTopRequestedSkills(from, to, limit);
            return Ok(data);
        }
    }
}
