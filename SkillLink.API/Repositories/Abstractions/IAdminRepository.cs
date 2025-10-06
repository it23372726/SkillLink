using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IAdminRepository
    {
        List<User> GetUsers(string? search = null);
        bool SetUserActive(int userId, bool isActive);
        bool SetUserRole(int userId, string role);
    }
}
