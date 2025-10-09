using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IRequestRepository
    {
        RequestWithUser? GetByIdWithUser(int requestId);
        List<RequestWithUser> GetByLearnerIdWithUser(int learnerId);
        List<RequestWithUser> GetAllWithUser();
        List<RequestWithUser> GetAllVisibleWithUser(int? viewerUserId);
        void Insert(Request req);
        void Update(int requestId, Request req);
        void UpdateStatus(int requestId, string status);
        void Delete(int requestId);
        List<RequestWithUser> SearchVisibleWithUser(string query, int? viewerUserId);
        void RemovePreferredTutor(int requestId);
        void CancelDirected(int requestId);

    }
}
