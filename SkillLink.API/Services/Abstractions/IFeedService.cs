using SkillLink.API.Models;

namespace SkillLink.API.Services.Abstractions
{
    public interface IFeedService
    {
        List<FeedItemDto> GetFeed(int me, int page, int pageSize, string? q = null);
    }
}
