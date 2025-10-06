using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface ISkillRepository
    {
        int? GetSkillIdByName(string name);
        int InsertSkill(string name, bool isPredefined = false);
        void UpsertUserSkill(int userId, int skillId, string level);
        void DeleteUserSkill(int userId, int skillId);
        List<UserSkill> GetUserSkillsWithSkill(int userId);
        List<Skill> SuggestSkillsByPrefix(string prefix);
        List<int> GetUserIdsBySkillPrefix(string prefix);
    }
}
