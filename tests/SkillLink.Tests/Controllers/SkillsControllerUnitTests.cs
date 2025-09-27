using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class SkillsControllerUnitTests
    {
        private SkillsController Create(out Mock<ISkillService> mock)
        {
            mock = new Mock<ISkillService>(MockBehavior.Strict);
            return new SkillsController(mock.Object);
        }

        [Test]
        public void AddSkill_ShouldReturnOk()
        {
            var ctrl = Create(out var mock);
            var req = new AddSkillRequest { UserId = 7, SkillName = "React", Level = "Intermediate" };

            mock.Setup(s => s.AddSkill(req));

            var res = ctrl.AddSkill(req);
            res.Should().BeOfType<OkObjectResult>();

            mock.Verify(s => s.AddSkill(req), Times.Once);
        }

        [Test]
        public void DeleteSkill_ShouldReturnOk()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.DeleteUserSkill(7, 11));

            var res = ctrl.DeleteSkill(7, 11);
            res.Should().BeOfType<OkObjectResult>();

            mock.Verify(s => s.DeleteUserSkill(7, 11), Times.Once);
        }

        [Test]
        public void GetUserSkills_ShouldReturnOk_WithList()
        {
            var ctrl = Create(out var mock);
            var expected = new List<UserSkill>
            {
                new UserSkill { UserSkillId = 1, UserId = 7, SkillId = 10, Level = "Beginner", 
                    Skill = new Skill { SkillId = 10, Name = "C#", IsPredefined = true } }
            };

            mock.Setup(s => s.GetUserSkills(7)).Returns(expected);

            var res = ctrl.GetUserSkills(7);
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.Verify(s => s.GetUserSkills(7), Times.Once);
        }

        [Test]
        public void Suggest_ShouldReturnOk_WithList()
        {
            var ctrl = Create(out var mock);
            var expected = new List<Skill> { new Skill { SkillId = 2, Name = "React", IsPredefined = false } };

            mock.Setup(s => s.SuggestSkills("Re")).Returns(expected);

            var res = ctrl.Suggest("Re");
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.Verify(s => s.SuggestSkills("Re"), Times.Once);
        }

        [Test]
        public void FilterUsers_ShouldReturnOk_WithList()
        {
            var ctrl = Create(out var mock);
            var expected = new List<User>
            {
                new User { UserId = 3, FullName = "Jane", Email = "jane@example.com" }
            };

            mock.Setup(s => s.GetUsersBySkill("React")).Returns(expected);

            var res = ctrl.FilterUsers("React");
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.Verify(s => s.GetUsersBySkill("React"), Times.Once);
        }
    }
}
