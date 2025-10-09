namespace SkillLink.API.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = "PRIVATE_REQUEST_CREATED";
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public string? Link { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
