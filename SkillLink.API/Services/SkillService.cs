using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class SkillService : ISkillService
    {
        private readonly ISkillRepository _repo;
        private readonly IAuthService _auth;

        public SkillService(ISkillRepository repo, IAuthService authService)
        {
            _repo = repo;
            _auth = authService;
        }

        public void AddSkill(AddSkillRequest req)
        {
            // 1) Ensure skill exists
            var skillId = _repo.GetSkillIdByName(req.SkillName)
                        ?? _repo.InsertSkill(req.SkillName, isPredefined: false);

            // 2) Map to user (upsert)
            _repo.UpsertUserSkill(req.UserId, skillId, req.Level);
        }

        public void DeleteUserSkill(int userId, int skillId) =>
            _repo.DeleteUserSkill(userId, skillId);

        public List<UserSkill> GetUserSkills(int userId) =>
            _repo.GetUserSkillsWithSkill(userId);

        public List<Skill> SuggestSkills(string query) =>
            _repo.SuggestSkillsByPrefix(query);

        public List<User> GetUsersBySkill(string query)
        {
            var userIds = _repo.GetUserIdsBySkillPrefix(query);
            var users = new List<User>();
            foreach (var id in userIds)
            {
                var u = _auth.GetUserById(id);
                if (u != null) users.Add(u);
            }
            return users;
        }
    }
}
