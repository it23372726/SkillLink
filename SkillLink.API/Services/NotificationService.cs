using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;
using SkillLink.API.Models;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    public NotificationService(INotificationRepository repo) => _repo = repo;

    public void Send(Notification n) => _repo.Insert(n);
    public List<Notification> ListForUser(int userId) => _repo.ListForUser(userId);
    public void MarkRead(int userId, int notificationId) => _repo.MarkRead(userId, notificationId);
    public void MarkAllRead(int userId) => _repo.MarkAllRead(userId);
}
