using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;
using System.Linq;

namespace SkillLink.API.Services
{
    public class FeedService : IFeedService
    {
        private readonly IFeedRepository _feedRepo;
        private readonly IReactionService _reactions;
        private readonly ICommentService _comments;
        private readonly ISkillRepository _skillRepo; // This is the class field

        public FeedService(
            IFeedRepository feedRepo,
            IReactionService reactions,
            ICommentService comments,
            ISkillRepository skillRepo) // This is the parameter
        {
            _feedRepo = feedRepo;
            _reactions = reactions;
            _comments = comments;
            _skillRepo = skillRepo; // CORRECTED: Assign parameter to the class field _skillRepo
        }

        public List<FeedItemDto> GetFeed(int me, int page, int pageSize, string? q = null)
        {
            // ... (existing code is fine)
            var temp = _feedRepo.GetFeedPage(page, pageSize, q);
            var list = new List<FeedItemDto>(temp.Count);

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
        
        // This sorting logic is now correct for using profile skills
        public List<FeedItemDto> GetSort(List<FeedItemDto> data, int me)
        {
            // Step A: Get all skills from the user's profile.
            var myUserSkills = _skillRepo.GetUserSkillsWithSkill(me);
            

            // Step B: Create a HashSet of skill names for efficient lookups.
            var mySkillNames = myUserSkills
                .Select(userSkill => userSkill.Skill.Name)
                .ToHashSet();

            if (!mySkillNames.Any())
            {
                Console.WriteLine("ERROR" );
                // If the user has no skills, return the original list sorted by date.
                return data.OrderByDescending(item => item.CreatedAt).ToList();
            }

            // Step C: Sort the incoming data.
            var sortedList = data
                .OrderByDescending(item => mySkillNames.Contains(item.Title)) // Items matching a profile skill come first
                .ThenByDescending(item => item.CreatedAt) // Then sort by most recent
                .ToList();

            return sortedList;
        }
    }
}
