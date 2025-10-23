using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _repo;

        public FeedbackService(IFeedbackRepository repo)
        {
            _repo = repo;
        }

        public int Submit(FeedbackCreateDto dto, int? userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new ArgumentException("Feedback message is required", nameof(dto.Message));

            // basic trimming to keep storage clean
            dto.Subject = dto.Subject?.Trim();
            dto.Message = dto.Message.Trim();
            dto.Page = dto.Page?.Trim();
            dto.UserAgent = dto.UserAgent?.Trim();

            return _repo.Insert(dto, userId);
        }

        public List<FeedbackItem> List(bool? isRead = null, int? limit = null, int? offset = null)
            => _repo.List(isRead, limit, offset);

        public void MarkRead(int feedbackId, bool isRead)
            => _repo.MarkRead(feedbackId, isRead);
    }
}
