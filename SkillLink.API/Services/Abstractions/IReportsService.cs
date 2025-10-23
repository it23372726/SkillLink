using SkillLink.API.Models.Reports;

namespace SkillLink.API.Services.Abstractions
{
    public interface IReportsService
    {
        List<SkillDemandRow> GetTopRequestedSkills(DateTime? from, DateTime? to, int limit);
    }
}
