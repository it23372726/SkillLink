using System;
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

[TestFixture]
public class RequestsControllerUnitTests
{
    private static ClaimsPrincipal FakeUser(int userId) =>
        new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "TestAuth"));

    private RequestsController Create(out Mock<IRequestService> reqMock,
                                     out Mock<IAcceptedRequestService> accMock,
                                     int currentUserId = 77)
    {
        reqMock = new Mock<IRequestService>(MockBehavior.Strict);
        accMock = new Mock<IAcceptedRequestService>(MockBehavior.Strict);
        var notif = new Mock<INotificationService>(MockBehavior.Loose);
        var ctrl = new RequestsController(reqMock.Object, accMock.Object, notif.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = FakeUser(currentUserId) }
        };
        return ctrl;
    }

    [Test]
    public void Search_ShouldReturnBadRequest_WhenQueryMissing()
    {
        var ctrl = Create(out var req, out var acc);
        var res = ctrl.Search("");
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public void Search_ShouldReturnOk_WithResults()
    {
        var ctrl = Create(out var req, out var acc);
        var expected = new List<RequestWithUser> {
            new RequestWithUser { RequestId = 1, SkillName = "Math", FullName = "Alice" }
        };

    req.Setup(s => s.SearchRequests("ma", It.IsAny<int?>())).Returns(expected);
        var res = ctrl.Search("ma");
        res.Should().BeOfType<OkObjectResult>();
        (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);
    req.Verify(s => s.SearchRequests("ma", It.IsAny<int?>()), Times.Once);
    }

    [Test]
    public void GetById_ShouldReturnNotFound_WhenMissing()
    {
        var ctrl = Create(out var req, out var acc);
        req.Setup(s => s.GetById(5)).Returns((RequestWithUser?)null);
        var res = ctrl.GetById(5);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public void GetById_ShouldReturnOk_WhenFound()
    {
        var ctrl = Create(out var req, out var acc);
        var r = new RequestWithUser { RequestId = 10, FullName = "Zee", SkillName = "C#" };
        req.Setup(s => s.GetById(10)).Returns(r);
        var res = ctrl.GetById(10);
        res.Should().BeOfType<OkObjectResult>();
        (res as OkObjectResult)!.Value.Should().BeEquivalentTo(r);
    }

    [Test]
    public void GetByLearnerId_ShouldReturnNotFound_WhenEmpty()
    {
        var ctrl = Create(out var req, out var acc);
        req.Setup(s => s.GetByLearnerId(22)).Returns(new List<RequestWithUser>());
        var res = ctrl.GetByLearnerId(22);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public void Create_ShouldReturnOk()
    {
        var ctrl = Create(out var req, out var acc);
        var payload = new Request { LearnerId = 1, SkillName = "React" };
        req.Setup(s => s.AddRequest(payload));

        var res = ctrl.Create(payload);
        res.Should().BeOfType<OkObjectResult>();

        req.Verify(s => s.AddRequest(payload), Times.Once);
    }

    [Test]
    public void UpdateRequest_ShouldReturnNotFound_WhenMissing()
    {
        var ctrl = Create(out var req, out var acc);
        req.Setup(s => s.GetById(99)).Returns((RequestWithUser?)null);

        var res = ctrl.UpdateRequest(99, new Request { SkillName = "X" });
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public void UpdateRequest_ShouldReturnOk_WhenExists()
    {
        var ctrl = Create(out var req, out var acc);
        var existing = new RequestWithUser { RequestId = 7, SkillName = "Old" };
        req.Setup(s => s.GetById(7)).Returns(existing);
        req.Setup(s => s.UpdateRequest(7, It.IsAny<Request>()));

        var res = ctrl.UpdateRequest(7, new Request { SkillName = "New" });
        res.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public void UpdateStatus_ShouldReturnNotFound_WhenMissing()
    {
        var ctrl = Create(out var req, out var acc);
        req.Setup(s => s.GetById(101)).Returns((RequestWithUser?)null);
        var res = ctrl.UpdateStatus(101, "CLOSED");
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public void UpdateStatus_ShouldReturnOk()
    {
        var ctrl = Create(out var req, out var acc);
        var existing = new RequestWithUser { RequestId = 5, SkillName = "Node" };
        req.Setup(s => s.GetById(5)).Returns(existing);
        req.Setup(s => s.UpdateStatus(5, "CLOSED"));

        var res = ctrl.UpdateStatus(5, "CLOSED");
        res.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public void Delete_ShouldReturnNotFound_WhenMissing()
    {
        var ctrl = Create(out var req, out var acc);
        req.Setup(s => s.GetById(70)).Returns((RequestWithUser?)null);
        var res = ctrl.Delete(70);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public void Delete_ShouldReturnOk()
    {
        var ctrl = Create(out var req, out var acc);
        var existing = new RequestWithUser { RequestId = 12 };
        req.Setup(s => s.GetById(12)).Returns(existing);
        req.Setup(s => s.DeleteRequest(12));

        var res = ctrl.Delete(12);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public void AcceptRequest_ShouldReturnOk()
    {
        var me = 77;
        var ctrl = Create(out var req, out var acc, me);
        req.Setup(s => s.GetById(15)).Returns(new RequestWithUser{ RequestId = 15, LearnerId = 5, SkillName = "X", IsPrivate = false });
        acc.Setup(s => s.AcceptRequest(15, me));
        var res = ctrl.AcceptRequest(15);
        res.Should().BeOfType<OkObjectResult>();
        acc.Verify(s => s.AcceptRequest(15, me), Times.Once);
    }

    [Test]
    public void AcceptRequest_ShouldReturnBadRequest_OnException()
    {
        var me = 77;
        var ctrl = Create(out var req, out var acc, me);
        req.Setup(s => s.GetById(15)).Returns(new RequestWithUser{ RequestId = 15, LearnerId = 5, SkillName = "X", IsPrivate = false });
        acc.Setup(s => s.AcceptRequest(15, me)).Throws(new Exception("Already accepted"));
        var res = ctrl.AcceptRequest(15);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public void GetAcceptedRequests_ShouldReturnOk()
    {
        var me = 77;
        var ctrl = Create(out var req, out var acc, me);
        var expected = new List<AcceptedRequestWithDetails>();
        acc.Setup(s => s.GetAcceptedRequestsByUser(me)).Returns(expected);

        var res = ctrl.GetAcceptedRequests();
        res.Should().BeOfType<OkObjectResult>();
        (res as OkObjectResult)!.Value.Should().BeSameAs(expected);
    }

    [Test]
    public void GetAcceptedStatus_ShouldReturnOk()
    {
        var me = 77;
        var ctrl = Create(out var req, out var acc, me);
        acc.Setup(s => s.HasUserAcceptedRequest(me, 100)).Returns(true);

        var res = ctrl.GetAcceptedStatus(100);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public void ScheduleMeeting_ShouldReturnOk()
    {
        var me = 77;
        var ctrl = Create(out var req, out var acc, me);
        acc.Setup(s => s.GetAcceptedMeta(10)).Returns(new AcceptedMeta { AcceptedRequestId = 10, AcceptorId = me, RequesterId = 5, Status = "ACCEPTED" });
        acc.Setup(s => s.ScheduleMeeting(10, It.IsAny<DateTime>(), "Zoom", "https://zoom.us/xyz"));

        var payload = new ScheduleMeetingRequest {
            ScheduleDate = DateTime.UtcNow.AddDays(1),
            MeetingType = "Zoom",
            MeetingLink = "https://zoom.us/xyz"
        };

        var res = ctrl.ScheduleMeeting(10, payload);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public void GetRequestsIAskedFor_ShouldReturnOk()
    {
        var me = 77;
        var ctrl = Create(out var req, out var acc, me);
        var expected = new List<AcceptedRequestWithDetails>();
        acc.Setup(s => s.GetRequestsIAskedFor(me)).Returns(expected);

        var res = ctrl.GetRequestsIAskedFor();
        res.Should().BeOfType<OkObjectResult>();
        (res as OkObjectResult)!.Value.Should().BeSameAs(expected);
    }
}
