using System.Runtime.Serialization;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Services;

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class AdminControllerUnitTests
    {
        private AdminController Create()
        {
            var svc = (AdminService)FormatterServices.GetUninitializedObject(typeof(AdminService));
            return new AdminController(svc);
        }

        [Test]
        public void SetRole_ShouldValidateRole_AndReturnBadRequest_ForInvalid()
        {
            var ctrl = Create();

            var res = ctrl.SetRole(1, new AdminController.UpdateRoleRequest { Role = "Hacker" });

            res.Should().BeOfType<BadRequestObjectResult>();
            var body = (res as BadRequestObjectResult)!.Value!;
            var msg = body.GetType().GetProperty("message")!.GetValue(body) as string;
            msg.Should().Be("Invalid role");
        }

        [Test]
        public void Controller_ShouldBeAdminAuthorized()
        {
            var attr = typeof(AdminController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .OfType<AuthorizeAttribute>()
                .FirstOrDefault();
            attr.Should().NotBeNull();
            attr!.Roles.Should().Be("Admin");
        }
    }
}
