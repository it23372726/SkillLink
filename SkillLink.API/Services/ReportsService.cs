using SkillLink.API.Models.Reports;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class ReportsService : IReportsService
    {
        private readonly IReportsRepository _repo;
        public ReportsService(IReportsRepository repo) => _repo = repo;

        public List<SkillDemandRow> GetTopRequestedSkills(DateTime? from, DateTime? to, int limit)
            => _repo.GetTopRequestedSkills(from, to, limit <= 0 ? 10 : limit);
    }
}
