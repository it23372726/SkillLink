using System;

namespace SkillLink.API.Models
{
    public class TutorPost
    {
        public int PostId { get; set; }
        public int TutorId { get; set; }   // FK → Users
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public string Status { get; set; } = "Open"; // Open | Closed | Scheduled
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ScheduledAt { get; set; }
        public string MeetingLink { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }  // <— NEW

        // Computed on SELECT (COUNT from TutorPostParticipants)
        public int CurrentParticipants { get; set; } = 0;
    }

    public class TutorPostWithUser : TutorPost
    {
        public string TutorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class TutorPostParticipant
    {
        public int ParticipantId { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public DateTime AcceptedAt { get; set; } = DateTime.Now;
    }
}
