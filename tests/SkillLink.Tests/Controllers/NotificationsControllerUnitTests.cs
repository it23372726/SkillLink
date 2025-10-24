using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class NotificationsControllerUnitTests
    {
        private static ClaimsPrincipal FakeUser(int userId) =>
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "TestAuth"));

        private NotificationsController Create(out Mock<INotificationService> mock, int uid = 55)
        {
            mock = new Mock<INotificationService>(MockBehavior.Strict);
            var ctrl = new NotificationsController(mock.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = FakeUser(uid) }
            };
            return ctrl;
        }

        [Test]
        public void List_ShouldReturnOk_WithUserNotifications()
        {
            var ctrl = Create(out var mock, 55);
            mock.Setup(s => s.ListForUser(55)).Returns(new List<Notification>());
            var res = ctrl.List();
            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void MarkRead_ShouldReturnOk()
        {
            var ctrl = Create(out var mock, 55);
            mock.Setup(s => s.MarkRead(55, 9));
            var res = ctrl.MarkRead(9);
            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void MarkAllRead_ShouldReturnOk()
        {
            var ctrl = Create(out var mock, 55);
            mock.Setup(s => s.MarkAllRead(55));
            var res = ctrl.MarkAllRead();
            res.Should().BeOfType<OkObjectResult>();
        }
    }
}
