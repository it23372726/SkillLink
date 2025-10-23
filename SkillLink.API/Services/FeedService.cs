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
        private readonly ISkillRepository _skillRepo;
        private readonly IRequestService _requestService;

        public FeedService(
            IFeedRepository feedRepo,
            IReactionService reactions,
            ICommentService comments,
            ISkillRepository skillRepo,
            IRequestService requestService) // This is the parameter
        {
            _feedRepo = feedRepo;
            _reactions = reactions;
            _comments = comments;
            _skillRepo = skillRepo; // CORRECTED: Assign parameter to the class field _skillRepo
            _requestService = requestService;
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
            if (data == null || data.Count == 0) return new List<FeedItemDto>();

            // Determine which type we are sorting (calls pass homogeneous lists)
            var firstType = (data.First().PostType ?? string.Empty).Trim().ToUpperInvariant();
            bool isLessons = string.Equals(firstType, "LESSON", StringComparison.OrdinalIgnoreCase);
            bool isRequests = string.Equals(firstType, "REQUEST", StringComparison.OrdinalIgnoreCase);

            // Helper: case-insensitive Contains
            static bool ContainsAny(string haystack, HashSet<string> needles)
            {
                if (string.IsNullOrWhiteSpace(haystack) || needles == null || needles.Count == 0) return false;
                foreach (var n in needles)
                {
                    if (string.IsNullOrWhiteSpace(n)) continue;
                    if (haystack.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
                return false;
            }

            if (isRequests)
            {
                // ✅ Existing behavior: prioritize items that match my profile skills
                var myUserSkills = _skillRepo.GetUserSkillsWithSkill(me) ?? Enumerable.Empty<dynamic>();
                var mySkillNames = myUserSkills
                    .Select(us => (string)us.Skill.Name)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!mySkillNames.Any())
                    return data.OrderByDescending(it => it.CreatedAt).ToList();

                return data
                    .OrderByDescending(it => ContainsAny(it.Title ?? string.Empty, mySkillNames))
                    .ThenByDescending(it => it.CreatedAt)
                    .ToList();
            }

            if (isLessons)
            {
                // ✅ NEW behavior: prioritize lessons similar to requests I SENT
                // (Use SkillName and Topic from my requests)
                var myRequests = _requestService.GetByLearnerId(me) ?? new List<RequestWithUser>();

                var requestTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var r in myRequests)
                {
                    if (!string.IsNullOrWhiteSpace(r.SkillName)) requestTerms.Add(r.SkillName.Trim());
                    if (!string.IsNullOrWhiteSpace(r.Topic)) requestTerms.Add(r.Topic.Trim());
                }

                if (!requestTerms.Any())
                    return data.OrderByDescending(it => it.CreatedAt).ToList();

                // You could make this smarter (tokenization, fuzzy match), but simple contains works well.
                return data
                    .OrderByDescending(it => ContainsAny(it.Title ?? string.Empty, requestTerms))
                    .ThenByDescending(it => it.CreatedAt)
                    .ToList();
            }

            // Fallback (mixed or unknown types): newest first
            return data.OrderByDescending(it => it.CreatedAt).ToList();
        }

    }
}