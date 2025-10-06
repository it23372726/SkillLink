using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IFeedRepository
    {
        /// <summary>
        /// Returns merged feed rows (LESSON/TutorPosts + REQUEST/Requests) paginated and optionally filtered by q.
        /// Returns items WITHOUT counts/reactions (services can augment).
        /// </summary>
        List<FeedItemDto> GetFeedPage(int page, int pageSize, string? q);
    }
}
