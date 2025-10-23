using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IFeedbackRepository
    {
        int Insert(FeedbackCreateDto dto, int? userId);
        List<FeedbackItem> List(bool? isRead = null, int? limit = null, int? offset = null);
        void MarkRead(int feedbackId, bool isRead);
    }
}
