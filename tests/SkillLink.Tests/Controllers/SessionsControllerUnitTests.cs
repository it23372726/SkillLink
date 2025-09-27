using System;
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
    public class SessionsControllerUnitTests
    {
        private SessionsController Create(out Mock<ISessionService> mock)
        {
            mock = new Mock<ISessionService>(MockBehavior.Strict);
            return new SessionsController(mock.Object);
        }

        [Test]
        public void GetAll_ShouldReturnOk_WithList()
        {
            var ctrl = Create(out var mock);
            var expected = new List<Session> {
                new Session { SessionId = 1, TutorId = 9, RequestId = 3, Status = "PENDING", CreatedAt = DateTime.UtcNow }
            };
            mock.Setup(s => s.GetAllSessions()).Returns(expected);

            var res = ctrl.GetAll();
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.Verify(s => s.GetAllSessions(), Times.Once);
        }

        [Test]
        public void GetById_ShouldReturnNotFound_WhenMissing()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.GetById(99)).Returns((Session?)null);

            var res = ctrl.GetById(99);
            res.Should().BeOfType<NotFoundObjectResult>();
            mock.Verify(s => s.GetById(99), Times.Once);
        }

        [Test]
        public void GetById_ShouldReturnOk_WhenFound()
        {
            var ctrl = Create(out var mock);
            var item = new Session { SessionId = 7, TutorId = 2, RequestId = 1, Status = "PENDING", CreatedAt = DateTime.UtcNow };
            mock.Setup(s => s.GetById(7)).Returns(item);

            var res = ctrl.GetById(7);
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(item);

            mock.Verify(s => s.GetById(7), Times.Once);
        }

        [Test]
        public void GetByTutorId_ShouldReturnNotFound_WhenEmpty()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.GetByTutorId(5)).Returns(new List<Session>());

            var res = ctrl.GetByTutorId(5);
            res.Should().BeOfType<NotFoundObjectResult>();

            mock.Verify(s => s.GetByTutorId(5), Times.Once);
        }

        [Test]
        public void GetByTutorId_ShouldReturnOk_WhenFound()
        {
            var ctrl = Create(out var mock);
            var expected = new List<Session> {
                new Session { SessionId = 2, TutorId = 5, RequestId = 1, Status = "PENDING", CreatedAt = DateTime.UtcNow }
            };
            mock.Setup(s => s.GetByTutorId(5)).Returns(expected);

            var res = ctrl.GetByTutorId(5);
            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            mock.Verify(s => s.GetByTutorId(5), Times.Once);
        }

        [Test]
        public void Create_ShouldReturnOk()
        {
            var ctrl = Create(out var mock);
            var s = new Session { TutorId = 2, RequestId = 10, Status = "PENDING" };

            mock.Setup(svc => svc.AddSession(s));

            var res = ctrl.Create(s);
            res.Should().BeOfType<OkObjectResult>();

            mock.Verify(svc => svc.AddSession(s), Times.Once);
        }

        [Test]
        public void UpdateStatus_ShouldReturnOk()
        {
            var ctrl = Create(out var mock);

            mock.Setup(s => s.UpdateStatus(5, "SCHEDULED"));

            var res = ctrl.UpdateStatus(5, "SCHEDULED");
            res.Should().BeOfType<OkObjectResult>();

            mock.Verify(s => s.UpdateStatus(5, "SCHEDULED"), Times.Once);
        }

        [Test]
        public void Delete_ShouldReturnOk()
        {
            var ctrl = Create(out var mock);

            mock.Setup(s => s.Delete(8));

            var res = ctrl.Delete(8);
            res.Should().BeOfType<OkObjectResult>();

            mock.Verify(s => s.Delete(8), Times.Once);
        }
    }
}
