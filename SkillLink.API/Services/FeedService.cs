using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class FeedService : IFeedService
    {
        private readonly IFeedRepository _feedRepo;
        private readonly IReactionService _reactions;
        private readonly ICommentService _comments;

        public FeedService(
            IFeedRepository feedRepo,
            IReactionService reactions,
            ICommentService comments)
        {
            _feedRepo = feedRepo;
            _reactions = reactions;
            _comments = comments;
        }

        public List<FeedItemDto> GetFeed(int me, int page, int pageSize, string? q = null)
        {
            var temp = _feedRepo.GetFeedPage(page, pageSize, q);
            var list = new List<FeedItemDto>(temp.Count);

            // augment with counts + my reaction
            foreach (var it in temp)
            {
                var (likes, dislikes, my) = _reactions.GetReactionSummary(me, it.PostType, it.PostId);
                it.Likes = likes;
                it.Dislikes = dislikes;
                it.MyReaction = my;
                it.CommentCount = _comments.Count(it.PostType, it.PostId);
                list.Add(it);
            }

            return list;
        }
    }
}
