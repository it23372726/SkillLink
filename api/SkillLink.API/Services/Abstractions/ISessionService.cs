namespace SkillLink.API.Services.Abstractions
{
    using System.Collections.Generic;
    using SkillLink.API.Models;

    public interface ISessionService
    {
        List<Session> GetAllSessions();
        Session? GetById(int id);
        List<Session> GetByTutorId(int tutorId);
        void AddSession(Session session);
        void UpdateStatus(int id, string status);
        void Delete(int id);
    }
}
