namespace SkillLink.API.Models
{
    public class Session
    {
        public int SessionId { get; set; }
        public int RequestId { get; set; }
        public int TutorId { get; set; }
        public DateTime? ScheduledAt { get; set; }   // can be null until scheduled
        public string Status { get; set; } = "PENDING";
        public DateTime CreatedAt { get; set; }

        public string RoomName => $"SkillLinkSession_{SessionId}";
    }
}
