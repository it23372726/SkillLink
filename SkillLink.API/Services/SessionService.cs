using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _repo;

        public SessionService(ISessionRepository repo)
        {
            _repo = repo;
        }

        public void AddSession(Session session)
        {
            if (session.RequestId <= 0) throw new ArgumentException("RequestId required");
            if (session.TutorId <= 0) throw new ArgumentException("TutorId required");

            if (!_repo.RequestExists(session.RequestId))
                throw new KeyNotFoundException("Request not found");

            if (!_repo.UserExists(session.TutorId))
                throw new KeyNotFoundException("Tutor not found");

            _repo.Insert(session);
        }

        public List<Session> GetAllSessions() => _repo.GetAll();

        public Session? GetById(int id) => _repo.GetById(id);

        public List<Session>? GetByTutorId(int id) => _repo.GetByTutorId(id);

        public void UpdateStatus(int sessionId, string status) =>
            _repo.UpdateStatus(sessionId, status);

        public void Delete(int sessionId) => _repo.Delete(sessionId);
    }
}
