using System.Collections.Generic;
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
    public class RatingsControllerUnitTests
    {
        private static ClaimsPrincipal FakeUser(int userId) =>
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "TestAuth"));

        private RatingsController Create(out Mock<IRatingService> rating,
                                         out Mock<IAcceptedRequestService> accepted,
                                         int uid = 88)
        {
            rating = new Mock<IRatingService>(MockBehavior.Strict);
            accepted = new Mock<IAcceptedRequestService>(MockBehavior.Strict);
            var ctrl = new RatingsController(rating.Object, accepted.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = FakeUser(uid) }
            };
            return ctrl;
        }

        [Test]
        public void Create_ShouldReturnBadRequest_WhenRatingOutOfRange()
        {
            var ctrl = Create(out var r, out var a);
            var res = ctrl.Create(new CreateRatingDto { AcceptedRequestId = 1, Rating = 6 });
            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void Create_ShouldReturnNotFound_WhenAcceptedMissing()
        {
            var ctrl = Create(out var r, out var a);
            a.Setup(s => s.GetAcceptedMeta(5)).Returns((AcceptedMeta?)null);
            var res = ctrl.Create(new CreateRatingDto { AcceptedRequestId = 5, Rating = 5 });
            res.Should().BeOfType<NotFoundObjectResult>();
        }

        [Test]
        public void Create_ShouldReturnForbidden_WhenNotRequester()
        {
            var uid = 88;
            var ctrl = Create(out var r, out var a, uid);
            a.Setup(s => s.GetAcceptedMeta(10)).Returns(new AcceptedMeta { AcceptedRequestId = 10, RequesterId = 77, AcceptorId = 9, Status = "COMPLETED" });
            var res = ctrl.Create(new CreateRatingDto { AcceptedRequestId = 10, Rating = 5 });
            (res as ObjectResult)!.StatusCode.Should().Be(403);
        }

        [Test]
        public void Create_ShouldReturnBadRequest_WhenNotCompleted()
        {
            var uid = 88;
            var ctrl = Create(out var r, out var a, uid);
            a.Setup(s => s.GetAcceptedMeta(10)).Returns(new AcceptedMeta { AcceptedRequestId = 10, RequesterId = uid, AcceptorId = 9, Status = "ACCEPTED" });
            var res = ctrl.Create(new CreateRatingDto { AcceptedRequestId = 10, Rating = 5 });
            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void Create_ShouldReturnOk_OnHappyPath()
        {
            var uid = 88;
            var ctrl = Create(out var r, out var a, uid);
            a.Setup(s => s.GetAcceptedMeta(10)).Returns(new AcceptedMeta { AcceptedRequestId = 10, RequesterId = uid, AcceptorId = 9, Status = "COMPLETED" });
            r.Setup(s => s.Create(uid, 9, 10, 5, ""));
            var res = ctrl.Create(new CreateRatingDto { AcceptedRequestId = 10, Rating = 5 });
            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void Exists_ShouldReturnOk()
        {
            var uid = 88;
            var ctrl = Create(out var r, out var a, uid);
            r.Setup(s => s.ExistsForAccepted(7, uid)).Returns(true);
            var res = ctrl.Exists(7);
            res.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void Summary_ShouldReturnOk()
        {
            var ctrl = Create(out var r, out var a);
            r.Setup(s => s.SummaryForTutor(44)).Returns(new RatingSummaryDto { TutorId = 44, Count = 3, Average = 4.3 });
            var res = ctrl.Summary(44);
            res.Should().BeOfType<OkObjectResult>();
        }
    }
}
