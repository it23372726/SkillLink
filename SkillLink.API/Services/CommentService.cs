using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _repo;
        public CommentService(ICommentRepository repo) => _repo = repo;

        public List<dynamic> GetComments(string postType, int postId)
        {
            var rows = _repo.GetComments(postType, postId);
            // Preserve your dynamic return shape
            return rows.Select(r => (dynamic)new
            {
                r.CommentId,
                r.Content,
                r.CreatedAt,
                r.UserId,
                r.FullName,
                r.ProfilePicture
            }).ToList();
        }

        public void Add(string postType, int postId, int userId, string content)
        {
            _repo.Insert(postType, postId, userId, content);
        }

        public void Delete(int commentId, int userId, bool isAdmin)
        {
            var meta = _repo.GetCommentMeta(commentId);
            if (meta == null) return;

            // Admin: delete any
            if (isAdmin)
            {
                _repo.DeleteById(commentId);
                return;
            }

            // Post owner? (TutorId for TutorPosts/LESSON; LearnerId for Requests)
            bool isPostOwner = false;
            var postOwnerId = _repo.GetPostOwnerId(meta.PostType, meta.PostId);
            if (postOwnerId != null && postOwnerId.Value == userId)
                isPostOwner = true;

            if (userId == meta.OwnerUserId || isPostOwner)
            {
                _repo.DeleteById(commentId);
            }
        }

        public int Count(string postType, int postId) => _repo.Count(postType, postId);
    }
}
