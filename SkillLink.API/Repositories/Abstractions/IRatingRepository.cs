using SkillLink.API.Models;
using System.Collections.Generic;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IRatingRepository
    {
        void Create(Rating rating);
        bool ExistsForAccepted(int acceptedRequestId, int learnerId);
        RatingSummaryDto? SummaryForTutor(int tutorId);

        List<RatingViewDto> ListReceived(int tutorId, int limit);
        List<RatingViewDto> ListGiven(int learnerId, int limit);
    }
}
