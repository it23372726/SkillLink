using SkillLink.API.Models;

namespace SkillLink.API.Services.Abstractions
{
    public interface IFeedbackService
    {
        int Submit(FeedbackCreateDto dto, int? userId);
        List<FeedbackItem> List(bool? isRead = null, int? limit = null, int? offset = null);
        void MarkRead(int feedbackId, bool isRead);
    }
}
