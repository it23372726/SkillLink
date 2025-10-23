namespace SkillLink.API.Models
{
    public class FeedbackCreateDto
    {
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Page { get; set; }
        public string? UserAgent { get; set; }
    }

    public class FeedbackItem
    {
        public int FeedbackId { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Page { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
