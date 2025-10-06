using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repo;

        public AdminService(IAdminRepository repo)
        {
            _repo = repo;
        }

        public List<User> GetUsers(string? search = null) => _repo.GetUsers(search);

        public bool SetUserActive(int userId, bool isActive) => _repo.SetUserActive(userId, isActive);

        public bool SetUserRole(int userId, string role) => _repo.SetUserRole(userId, role);
    }
}
