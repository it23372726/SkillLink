using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;
using System.Security.Claims;

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerUnitTests
    {
        private static ClaimsPrincipal FakeUser(int id, string role = "Learner")
        {
            var iden = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuth");
            return new ClaimsPrincipal(iden);
        }

        private AuthController Create(out Mock<IAuthService> mock, ClaimsPrincipal? user = null)
        {
            mock = new Mock<IAuthService>(MockBehavior.Strict);
            var ctrl = new AuthController(mock.Object);
            var http = new DefaultHttpContext { User = user ?? new ClaimsPrincipal() };
            ctrl.ControllerContext = new ControllerContext { HttpContext = http };
            return ctrl;
        }

        [Test]
        public void VerifyEmail_ShouldBadRequest_WhenMissing()
        {
            var ctrl = Create(out _);
            var res = ctrl.VerifyEmail("");
            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void VerifyEmail_ShouldBadRequest_WhenInvalid()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.VerifyEmailByToken("x")).Returns(false);

            var res = ctrl.VerifyEmail("x");
            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void VerifyEmail_ShouldOk_WhenValid()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.VerifyEmailByToken("ok")).Returns(true);

            var res = ctrl.VerifyEmail("ok");
            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void GetUserProfile_ShouldUnauthorized_WhenNoUser()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.CurrentUser(It.IsAny<ClaimsPrincipal>())).Returns((User?)null);

            var res = ctrl.GetUserProfile();
            res.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Test]
        public void GetUserProfile_ShouldOk_WhenExists()
        {
            var ctrl = Create(out var mock, FakeUser(1));
            var me = new User { UserId = 1, FullName = "A" };
            mock.Setup(s => s.CurrentUser(It.IsAny<ClaimsPrincipal>())).Returns(me);
            mock.Setup(s => s.GetUserProfile(1)).Returns(me);

            var res = ctrl.GetUserProfile();
            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void Login_ShouldUnauthorized_WhenNullToken()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.Login(It.IsAny<LoginRequest>())).Returns((string?)null);

            var res = ctrl.Login(new LoginRequest { Email = "a", Password = "b" });
            res.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Test]
        public void Login_ShouldOk_WhenToken()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.Login(It.IsAny<LoginRequest>())).Returns("tok");

            var res = ctrl.Login(new LoginRequest { Email = "a", Password = "b" });
            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void UpdateTeachMode_ShouldReturnOk()
        {
            var ctrl = Create(out var mock, FakeUser(1));
            var me = new User { UserId = 1 };
            mock.Setup(s => s.CurrentUser(It.IsAny<ClaimsPrincipal>())).Returns(me);
            mock.Setup(s => s.UpdateTeachMode(1, true)).Returns(true);
            mock.Setup(s => s.GetUserProfile(1)).Returns(me);

            var res = ctrl.UpdateTeachMode(new AuthController.UpdateTeachModeRequest { ReadyToTeach = true });
            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void DeleteUser_ShouldPreventSelfDelete()
        {
            var ctrl = Create(out var mock, FakeUser(1, "Admin"));
            mock.Setup(s => s.CurrentUser(It.IsAny<ClaimsPrincipal>())).Returns(new User { UserId = 1 });

            var res = ctrl.DeleteUser(1);
            res.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
