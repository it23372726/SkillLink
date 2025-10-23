using SkillLink.API.Models;
using System.Collections.Generic;

namespace SkillLink.API.Services.Abstractions
{
    public interface IRatingService
    {
        void Create(int learnerId, int tutorId, int acceptedRequestId, int rating, string comment);
        bool ExistsForAccepted(int acceptedRequestId, int learnerId);
        RatingSummaryDto SummaryForTutor(int tutorId);

        List<RatingViewDto> ListReceived(int tutorId, int limit);
        List<RatingViewDto> ListGiven(int learnerId, int limit);
    }
}
