using SkillLink.API.Models;

namespace SkillLink.API.Services.Abstractions{
    
    public interface INotificationService
    {
        void Send(Notification n);
        List<Notification> ListForUser(int userId);
        void MarkRead(int userId, int notificationId);
        void MarkAllRead(int userId);
    }

}

