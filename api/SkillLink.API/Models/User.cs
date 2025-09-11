namespace SkillLink.API.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Learner";
        public DateTime CreatedAt { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? ProfilePicture { get; set; }
        public bool ReadyToTeach { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationExpires { get; set; }
    }
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Location { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Learner";
        public IFormFile? ProfilePicture { get; set; }
            // For DB storage after saving file
        public string? ProfilePicturePath { get; set; }

        
    }
}
