using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface ISessionRepository
    {
        bool RequestExists(int requestId);
        bool UserExists(int userId);
        void Insert(Session session);
        List<Session> GetAll();
        Session? GetById(int id);
        List<Session> GetByTutorId(int tutorId);
        void UpdateStatus(int sessionId, string status);
        void Delete(int sessionId);
    }
}
