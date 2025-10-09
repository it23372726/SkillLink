using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface INotificationRepository
    {
        void Insert(Notification n);
        List<Notification> ListForUser(int userId);
        void MarkRead(int userId, int notificationId);
        void MarkAllRead(int userId);
    }
}