using SkillLink.API.Models;
using System.Collections.Generic;

namespace SkillLink.API.Services.Abstractions
{
    public interface IAdminService
    {
        List<User> GetUsers(string? search = null);
        bool SetUserActive(int userId, bool isActive);
        bool SetUserRole(int userId, string role);
    }
}
