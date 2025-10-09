using SkillLink.API.Models;
using System.Security.Claims;
using SkillLink.API.Dtos.Auth;


namespace SkillLink.API.Services.Abstractions
{
    public interface IAuthService
    {
        User? CurrentUser(ClaimsPrincipal user);
        User? GetUserById(int id);
        void Register(RegisterRequest req);
        bool VerifyEmailByToken(string token);
        string? Login(LoginRequest req);
        User? GetUserProfile(int userId);
        bool UpdateUserProfile(int userId, UpdateProfileRequest request);
        bool UpdateTeachMode(int userId, bool readyToTeach);
        bool SetActive(int userId, bool isActive);
        void DeleteUserFromDB(int id);
        bool UpdateProfilePicture(int userId, string? path);
        Task<PublicUserDto?> GetPublicUserAsync(int userId);
    }
}
