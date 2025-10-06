using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IRequestRepository
    {
        RequestWithUser? GetByIdWithUser(int requestId);
        List<RequestWithUser> GetByLearnerIdWithUser(int learnerId);
        List<RequestWithUser> GetAllWithUser();
        void Insert(Request req);
        void Update(int requestId, Request req);
        void UpdateStatus(int requestId, string status);
        void Delete(int requestId);
        List<RequestWithUser> SearchWithUser(string query);
    }
}
