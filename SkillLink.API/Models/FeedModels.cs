namespace SkillLink.API.Models
{
    public class FeedItemDto
    {
        public string PostType { get; set; } = ""; // LESSON | REQUEST
        public int PostId { get; set; }

        // author
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = "";
        public string AuthorEmail { get; set; } = "";
        public string AuthorPic { get; set;} = "";

        // content
        public string Title { get; set; } = "";          // Lesson: Title | Request: SkillName
        public string Subtitle { get; set; } = "";       // Lesson: "" | Request: Topic
        public string Body { get; set; } = "";           // Lesson: Description | Request: Description
        public string? ImageUrl { get; set; }            // Only for LESSON
        public DateTime CreatedAt { get; set; }

        // status/extra
        public string Status { get; set; } = "";         // request status or lesson status
        public DateTime? ScheduledAt { get; set; }       // lessons only

        // counts & my reaction
        public int Likes { get; set; }
        public int Dislikes { get; set; }
        public int CommentCount { get; set; }
        public string? MyReaction { get; set; }          // LIKE/DISLIKE/null
    }

    public class CreateCommentDto
    {
        public string Content { get; set; } = "";
    }
}
