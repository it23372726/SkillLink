namespace SkillLink.API.Models
{
    public class CreateTutorPostDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxParticipants { get; set; }
    }

    public class UpdateTutorPostDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxParticipants { get; set; }
        public string? Status { get; set; } // optional: Open | Closed | Scheduled
    }

    public class ScheduleTutorPostDto
    {
        public DateTime ScheduledAt { get; set; }
    }
}
