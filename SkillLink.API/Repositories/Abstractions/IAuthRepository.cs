using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IAuthRepository
    {
        // Users
        User? GetUserById(int id);
        User? GetUserByEmail(string email);
        bool EmailExists(string email);

        // Registration / Verification
        int CreateUser(RegisterRequest req, string passwordHash, string emailVerificationToken, DateTime expiresUtc);
        bool VerifyEmailByToken(string token, out int? userId);

        // Profile
        User? GetProfile(int userId);
        bool UpdateProfile(int userId, string fullName, string? bio, string? location);
        bool UpdateTeachMode(int userId, bool readyToTeach, string role);
        bool SetActive(int userId, bool isActive);
        bool UpdateProfilePicture(int userId, string? path);

        // Admin / Cleanup
        void DeleteUserWithRules(int userId);
    }
}
