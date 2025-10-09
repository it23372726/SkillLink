using SkillLink.API.Models;
namespace SkillLink.API.Services.Abstractions
{
    public interface IAcceptedRequestService
    {
        void AcceptRequest(int requestId, int acceptorId);
        List<AcceptedRequestWithDetails> GetAcceptedRequestsByUser(int userId);
        void UpdateAcceptanceStatus(int acceptedRequestId, string status);
        bool HasUserAcceptedRequest(int userId, int requestId);
        void ScheduleMeeting(int acceptedRequestId, DateTime scheduleDate, string meetingType, string meetingLink);
        List<AcceptedRequestWithDetails> GetRequestsIAskedFor(int userId);
        AcceptedRequestWithDetails? GetAcceptedMeta(int acceptedRequestId);
        void Complete(int acceptedRequestId);
    }
}
