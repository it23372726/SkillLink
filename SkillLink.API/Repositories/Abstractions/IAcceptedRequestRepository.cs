using SkillLink.API.Models;
using System.Collections.Generic;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IAcceptedRequestRepository
    {
        bool HasUserAcceptedRequest(int userId, int requestId);
        void InsertAcceptance(int requestId, int acceptorId);

        List<AcceptedRequestWithDetails> GetAcceptedRequestsByUser(int userId);
        void UpdateAcceptanceStatus(int acceptedRequestId, string status);

        void ScheduleMeeting(int acceptedRequestId, DateTime scheduleDate, string meetingType, string meetingLink);

        List<AcceptedRequestWithDetails> GetRequestsIAskedFor(int userId);

        // Keep the rich details method if you use it elsewhere…
        AcceptedRequestWithDetails? GetAcceptedDetails(int acceptedRequestId);

        // …but expose a focused meta for cross-service validations.
        AcceptedMeta? GetAcceptedMeta(int acceptedRequestId);

        void Complete(int acceptedRequestId);
    }
}
