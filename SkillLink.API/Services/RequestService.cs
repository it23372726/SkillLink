using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

public class RequestService : IRequestService
{
    private readonly IRequestRepository _repo;

    public RequestService(IRequestRepository repo)
    {
        _repo = repo;
    }

    public RequestWithUser? GetById(int requestId) => _repo.GetByIdWithUser(requestId);

    public List<RequestWithUser> GetByLearnerId(int learnerId) => _repo.GetByLearnerIdWithUser(learnerId);

    public List<RequestWithUser> GetAllRequests() => _repo.GetAllWithUser();

    public void AddRequest(Request req) => _repo.Insert(req);

    public void UpdateRequest(int requestId, Request req) => _repo.Update(requestId, req);

    public void UpdateStatus(int requestId, string status) => _repo.UpdateStatus(requestId, status);

    public void DeleteRequest(int requestId) => _repo.Delete(requestId);

    public List<RequestWithUser> SearchRequests(string query) => _repo.SearchWithUser(query);
}
