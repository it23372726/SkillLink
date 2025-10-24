using FluentAssertions;
using Moq;
using NUnit.Framework;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class NotificationServiceUnitTests
    {
        [Test]
        public void Send_List_MarkRead_All_Should_Delegate()
        {
            var repo = new Mock<INotificationRepository>(MockBehavior.Strict);
            var n = new Notification { UserId = 9, Title = "Hi" };

            repo.Setup(r => r.Insert(n));
            repo.Setup(r => r.ListForUser(9)).Returns(new List<Notification>());
            repo.Setup(r => r.MarkRead(9, 3));
            repo.Setup(r => r.MarkAllRead(9));

            var svc = new NotificationService(repo.Object);
            svc.Send(n);
            svc.ListForUser(9).Should().NotBeNull();
            svc.MarkRead(9, 3);
            svc.MarkAllRead(9);

            repo.VerifyAll();
        }
    }
}
