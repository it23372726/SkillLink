using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class ReactionService : IReactionService
    {
        private readonly IReactionRepository _repo;

        public ReactionService(IReactionRepository repo)
        {
            _repo = repo;
        }

        public void UpsertReaction(int userId, string postType, int postId, string reaction)
        {
            // optional: validate reaction value here
            var normalized = reaction?.Trim().ToUpperInvariant();
            if (normalized != "LIKE" && normalized != "DISLIKE")
            {
                throw new ArgumentException("Reaction must be LIKE or DISLIKE");
            }

            _repo.Upsert(userId, postType, postId, normalized);
        }

        public void RemoveReaction(int userId, string postType, int postId)
        {
            _repo.Remove(userId, postType, postId);
        }

        public (int likes, int dislikes, string? my) GetReactionSummary(int userId, string postType, int postId)
        {
            var (likes, dislikes) = _repo.GetCounts(postType, postId);
            var my = _repo.GetMyReaction(userId, postType, postId);
            return (likes, dislikes, my);
        }
    }
}
