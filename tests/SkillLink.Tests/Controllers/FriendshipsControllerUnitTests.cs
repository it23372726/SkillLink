using System.Collections.Generic;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Services;
using SkillLink.API.Services.Abstractions;
using SkillLink.API.Models;

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class FriendshipsControllerUnitTests
    {
        private static ClaimsPrincipal FakeUser(int userId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "USER"),
            }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private FriendshipsController Create(out Mock<IFriendshipService> mockService, int currentUserId = 42)
        {
            mockService = new Mock<IFriendshipService>(MockBehavior.Strict);
            var ctrl = new FriendshipsController(mockService.Object);
            var http = new DefaultHttpContext { User = FakeUser(currentUserId) };
            ctrl.ControllerContext = new ControllerContext { HttpContext = http };
            return ctrl;
        }

        [Test]
        public void GetFollowers_ShouldReturnOk_WithList()
        {
            var me = 42;
            var ctrl = Create(out var mock, me);

            var expected = new List<User>
            {
                new User { UserId = 1, FullName = "Alice", Email = "alice@example.com" },
                new User { UserId = 2, FullName = "Bob", Email = "bob@example.com" }
            };

            mock.Setup(s => s.GetFollowers(me)).Returns(expected);

            var res = ctrl.GetFollowers();
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.Verify(s => s.GetFollowers(me), Times.Once);
        }

        [Test]
        public void GetMyFriends_ShouldReturnOk_WithList()
        {
            var me = 42;
            var ctrl = Create(out var mock, me);

            var expected = new List<User> {
                new User { UserId = 7, FullName = "Drew", Email = "drew@example.com" }
            };

            mock.Setup(s => s.GetMyFriends(me)).Returns(expected);

            var res = ctrl.GetMyFriends();
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.Verify(s => s.GetMyFriends(me), Times.Once);
        }

        [Test]
        public void Follow_ShouldReturnOk_OnSuccess()
        {
            var me = 42;
            var ctrl = Create(out var mock, me);

            mock.Setup(s => s.Follow(me, 99));

            var res = ctrl.Follow(99);
            res.Should().BeOfType<OkObjectResult>();

            mock.Verify(s => s.Follow(me, 99), Times.Once);
        }

        [Test]
        public void Follow_ShouldReturnBadRequest_OnDuplicate()
        {
            var me = 42;
            var ctrl = Create(out var mock, me);

            mock.Setup(s => s.Follow(me, 99))
                .Throws(new System.InvalidOperationException("Already following"));

            var res = ctrl.Follow(99);
            res.Should().BeOfType<BadRequestObjectResult>();
            var body = (res as BadRequestObjectResult)!.Value!;
            var err = body.GetType().GetProperty("error")!.GetValue(body) as string;
            err.Should().Be("Already following");

            mock.Verify(s => s.Follow(me, 99), Times.Once);
        }

        [Test]
        public void Unfollow_ShouldReturnOk()
        {
            var me = 42;
            var ctrl = Create(out var mock, me);

            mock.Setup(s => s.Unfollow(me, 99));

            var res = ctrl.Unfollow(99);
            res.Should().BeOfType<OkObjectResult>();

            mock.Verify(s => s.Unfollow(me, 99), Times.Once);
        }

        [Test]
        public void Search_ShouldReturnOk_WithResults()
        {
            var me = 42;
            var ctrl = Create(out var mock, me);

            var expected = new List<User> {
                new User { UserId = 3, FullName = "Jane", Email = "jane@example.com" }
            };

            mock.Setup(s => s.SearchUsers("ja", me)).Returns(expected);

            var res = ctrl.Search("ja");
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.Verify(s => s.SearchUsers("ja", me), Times.Once);
        }
    }
}
