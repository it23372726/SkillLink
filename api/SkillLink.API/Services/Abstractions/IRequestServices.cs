namespace SkillLink.API.Services.Abstractions
{
    public interface IRequestService
    {
        RequestWithUser? GetById(int requestId);
        List<RequestWithUser> GetByLearnerId(int learnerId);
        List<RequestWithUser> GetAllRequests();
        void AddRequest(Request req);
        void UpdateRequest(int requestId, Request req);
        void UpdateStatus(int requestId, string status);
        void DeleteRequest(int requestId);
        List<RequestWithUser> SearchRequests(string query);
    }
}
