using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

// RequestService.cs
public class RequestService : IRequestService
{
    private readonly IRequestRepository _repo;

    public RequestService(IRequestRepository repo) => _repo = repo;

    public RequestWithUser? GetById(int id) => _repo.GetByIdWithUser(id);
    public List<RequestWithUser> GetByLearnerId(int learnerId) => _repo.GetByLearnerIdWithUser(learnerId);

    public List<RequestWithUser> GetAllRequests(int? viewerUserId) =>
        _repo.GetAllVisibleWithUser(viewerUserId);

    public void AddRequest(Request req) => _repo.Insert(req);
    public void UpdateRequest(int id, Request req) => _repo.Update(id, req);
    public void UpdateStatus(int id, string status) => _repo.UpdateStatus(id, status);
    public void DeleteRequest(int id) => _repo.Delete(id);

    public List<RequestWithUser> SearchRequests(string q, int? viewerUserId) =>
        _repo.SearchVisibleWithUser(q, viewerUserId);
    public void RemovePreferredTutor(int id) => _repo.RemovePreferredTutor(id);
    public void CancelDirected(int requestId) => _repo.CancelDirected(requestId);

}

