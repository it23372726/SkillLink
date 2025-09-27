using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;
using System.Collections.Generic;
using SkillLink.API.Dtos.Admin;

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class AdminControllerUnitTests
    {
        private AdminController Create(out Mock<IAdminService> mock)
        {
            mock = new Mock<IAdminService>(MockBehavior.Strict);
            return new AdminController(mock.Object);
        }

        [Test]
        public void GetUsers_ShouldReturnOk()
        {
            var ctrl = Create(out var mock);
            var expected = new List<User> { new User { UserId = 1, FullName = "A" } };

            mock.Setup(s => s.GetUsers("bob")).Returns(expected);

            var res = ctrl.GetUsers("bob");

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.VerifyAll();
        }

        [Test]
        public void SetActive_ShouldReturnOk_WhenSuccess()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.SetUserActive(5, true)).Returns(true);

            var res = ctrl.SetActive(5, new AdminUpdateActiveRequest { IsActive = true });

            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void SetActive_ShouldReturnBadRequest_WhenFail()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.SetUserActive(5, false)).Returns(false);

            var res = ctrl.SetActive(5, new AdminUpdateActiveRequest { IsActive = false });

            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void SetRole_ShouldRejectInvalidRole()
        {
            var ctrl = Create(out var mock);

            var res = ctrl.SetRole(5, new AdminUpdateRoleRequest { Role = "Hack" });

            res.Should().BeOfType<BadRequestObjectResult>();
            mock.Invocations.Should().BeEmpty();
        }

        [Test]
        public void SetRole_ShouldReturnOk_WhenSuccess()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.SetUserRole(5, "Tutor")).Returns(true);

            var res = ctrl.SetRole(5, new AdminUpdateRoleRequest { Role = "Tutor" });

            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void SetRole_ShouldReturnBadRequest_WhenFail()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.SetUserRole(5, "Tutor")).Returns(false);

            var res = ctrl.SetRole(5, new AdminUpdateRoleRequest { Role = "Tutor" });

            res.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
