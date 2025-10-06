using SkillLink.API.Models;

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
    }
}
