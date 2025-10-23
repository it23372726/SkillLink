namespace SkillLink.API.Models
{
    public class Rating
    {
        public int RatingId { get; set; }
        public int AcceptedRequestId { get; set; }
        public int TutorId { get; set; }
        public int LearnerId { get; set; }
        public int Score { get; set; }            // 1..5
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
        public class AcceptedMeta
    {
        public int AcceptedRequestId { get; set; }
        public int RequestId { get; set; }
        public int RequesterId { get; set; } // learner who created the request
        public int AcceptorId { get; set; }  // tutor who accepted
        public string Status { get; set; } = "";
    }
}
