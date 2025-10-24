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
    public class FeedbackControllerUnitTests
    {
        [Test]
        public void Submit_ShouldReturnBadRequest_WhenMessageMissing()
        {
            var svc = new Mock<IFeedbackService>(MockBehavior.Strict);
            var ctrl = new FeedbackController(svc.Object);

            var res = ctrl.Submit(new FeedbackCreateDto { Message = "" });
            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void Submit_ShouldReturnCreated_WhenValid()
        {
            var svc = new Mock<IFeedbackService>(MockBehavior.Strict);
            svc.Setup(s => s.Submit(It.IsAny<FeedbackCreateDto>(), It.IsAny<int?>())).Returns(123);
            var ctrl = new FeedbackController(svc.Object);

            var payload = new FeedbackCreateDto { Message = "It works" };
            var res = ctrl.Submit(payload) as CreatedAtActionResult;

            res.Should().NotBeNull();
            res!.ActionName.Should().Be(nameof(FeedbackController.GetList));
        }

        [Test]
        public void GetList_ShouldReturnOk()
        {
            var svc = new Mock<IFeedbackService>(MockBehavior.Strict);
            svc.Setup(s => s.List(null, 50, 0)).Returns(new List<FeedbackItem>());
            var ctrl = new FeedbackController(svc.Object);

            var res = ctrl.GetList(null, 50, 0);
            res.Result.Should().BeOfType<OkObjectResult>();
        }

        [Test]
        public void MarkRead_ShouldReturnNoContent()
        {
            var svc = new Mock<IFeedbackService>(MockBehavior.Strict);
            svc.Setup(s => s.MarkRead(7, true));
            var ctrl = new FeedbackController(svc.Object);

            var res = ctrl.MarkRead(7, true);
            res.Should().BeOfType<NoContentResult>();
        }
    }
}
