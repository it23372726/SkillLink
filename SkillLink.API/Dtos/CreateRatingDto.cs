namespace SkillLink.API.Models
{
    public class CreateRatingDto
    {
        public int AcceptedRequestId { get; set; }
        public int Rating { get; set; } // 1..5
        public string? Comment { get; set; }

        // NOTE: front-end may send tutorId; backend ignores it.
        public int? TutorId { get; set; }
    }
}
