namespace SkillLink.API.Services.Abstractions
{
    using System.Collections.Generic;
    using SkillLink.API.Models;

    public interface ISkillService
    {
        void AddSkill(AddSkillRequest req);
        void DeleteUserSkill(int userId, int skillId);
        List<UserSkill> GetUserSkills(int userId);
        List<Skill> SuggestSkills(string query);
        List<User> GetUsersBySkill(string query);
    }
}
