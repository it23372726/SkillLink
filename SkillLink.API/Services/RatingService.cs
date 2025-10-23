using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;
using System.Collections.Generic;

namespace SkillLink.API.Services
{
    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _repo;

        public RatingService(IRatingRepository repo)
        {
            _repo = repo;
        }

        public void Create(int learnerId, int tutorId, int acceptedRequestId, int rating, string comment)
        {
            if (_repo.ExistsForAccepted(acceptedRequestId, learnerId))
                throw new InvalidOperationException("You already rated this session.");

            var model = new Rating
            {
                AcceptedRequestId = acceptedRequestId,
                TutorId           = tutorId,
                LearnerId         = learnerId,
                Score             = rating,
                Comment           = comment?.Trim() ?? "",
                CreatedAt         = DateTime.UtcNow
            };

            _repo.Create(model);
        }

        public bool ExistsForAccepted(int acceptedRequestId, int learnerId) =>
            _repo.ExistsForAccepted(acceptedRequestId, learnerId);

        public RatingSummaryDto SummaryForTutor(int tutorId) =>
            _repo.SummaryForTutor(tutorId) ?? new RatingSummaryDto { TutorId = tutorId, Count = 0, Average = 0 };

        public List<RatingViewDto> ListReceived(int tutorId, int limit) =>
            _repo.ListReceived(tutorId, limit);

        public List<RatingViewDto> ListGiven(int learnerId, int limit) =>
            _repo.ListGiven(learnerId, limit);
    }
}
