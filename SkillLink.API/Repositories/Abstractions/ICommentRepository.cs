namespace SkillLink.API.Repositories.Abstractions
{
    public class CommentRow
    {
        public int CommentId { get; set; }
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string ProfilePicture { get; set; } = "";
    }

    public class CommentMeta
    {
        public string PostType { get; set; } = "";
        public int PostId { get; set; }
        public int OwnerUserId { get; set; } // owner of the comment (author of comment)
    }

    public interface ICommentRepository
    {
        List<CommentRow> GetComments(string postType, int postId);
        void Insert(string postType, int postId, int userId, string content);
        void DeleteById(int commentId);
        /// <summary>
        /// Returns (postType, postId, commentOwnerUserId) for a comment or null if not found.
        /// </summary>
        CommentMeta? GetCommentMeta(int commentId);
        /// <summary>
        /// Returns post owner (TutorId for TutorPosts / Lesson; LearnerId for Requests).
        /// </summary>
        int? GetPostOwnerId(string postType, int postId);
        int Count(string postType, int postId);
    }
}
