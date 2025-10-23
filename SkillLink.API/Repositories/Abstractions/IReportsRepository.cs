using SkillLink.API.Models.Reports;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IReportsRepository
    {
        List<SkillDemandRow> GetTopRequestedSkills(DateTime? from, DateTime? to, int limit);
    }
}
