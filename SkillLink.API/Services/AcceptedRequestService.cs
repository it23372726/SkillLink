using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class AcceptedRequestService : IAcceptedRequestService
    {
        private readonly IAcceptedRequestRepository _repo;

        public AcceptedRequestService(IAcceptedRequestRepository repo)
        {
            _repo = repo;
        }

        public void AcceptRequest(int requestId, int acceptorId)
        {
            if (_repo.HasUserAcceptedRequest(acceptorId, requestId))
            {
                throw new InvalidOperationException("Request already accepted");
            }
            _repo.InsertAcceptance(requestId, acceptorId);
        }

        public List<AcceptedRequestWithDetails> GetAcceptedRequestsByUser(int userId)
            => _repo.GetAcceptedRequestsByUser(userId);

        public void UpdateAcceptanceStatus(int acceptedRequestId, string status)
            => _repo.UpdateAcceptanceStatus(acceptedRequestId, status);

        public bool HasUserAcceptedRequest(int userId, int requestId)
            => _repo.HasUserAcceptedRequest(userId, requestId);

        public void ScheduleMeeting(int acceptedRequestId, DateTime scheduleDate, string meetingType, string meetingLink)
            => _repo.ScheduleMeeting(acceptedRequestId, scheduleDate, meetingType, meetingLink);

        public List<AcceptedRequestWithDetails> GetRequestsIAskedFor(int userId)
            => _repo.GetRequestsIAskedFor(userId);
        public AcceptedRequestWithDetails? GetAcceptedMeta(int acceptedRequestId)
            => _repo.GetAcceptedMeta(acceptedRequestId);

        public void Complete(int acceptedRequestId)
            => _repo.Complete(acceptedRequestId);
    }
}
