namespace SkillLink.API.Dtos.Auth
{
    public class AuthUpdateActiveRequest
    {
        public bool IsActive { get; set; }
    }
    public class PublicUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? ProfilePicture { get; set; }
        public bool ReadyToTeach { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserSummaryDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string? ProfilePicture { get; set; }
        public string? Location { get; set; }
        public bool ReadyToTeach { get; set; }
    }
    
}
