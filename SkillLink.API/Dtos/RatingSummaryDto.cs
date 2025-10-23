namespace SkillLink.API.Models
{
    public class RatingSummaryDto
    {
        public int TutorId { get; set; }
        public int Count { get; set; }
        public double Average { get; set; }
    }
        public class RatingViewDto
    {
        public int RatingId { get; set; }
        public int AcceptedRequestId { get; set; }
        public int Rating { get; set; }          // Score in DB
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public string SkillName { get; set; } = "";
        public string FromUserName { get; set; } = "";  // learner
        public string ToUserName { get; set; } = "";    // tutor
    }
}
