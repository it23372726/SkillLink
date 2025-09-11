using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

// Suppress obsolete FormatterServices warning in this test file only
#pragma warning disable SYSLIB0050

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class RequestsControllerUnitTests
    {
        private RequestsController Create(ClaimsPrincipal? user = null)
        {
            // Create controller with uninitialized concrete services. We only call actions
            // that short-circuit before hitting the services.
            var rs = (RequestService)FormatterServices.GetUninitializedObject(typeof(RequestService));
            var ars = (AcceptedRequestService)FormatterServices.GetUninitializedObject(typeof(AcceptedRequestService));
            var ctrl = new RequestsController(rs, ars);
            var http = new DefaultHttpContext();
            http.User = user ?? new ClaimsPrincipal(new ClaimsIdentity());
            ctrl.ControllerContext = new ControllerContext { HttpContext = http };
            return ctrl;
        }

        [Test]
        public void Search_ShouldReturnBadRequest_WhenQueryMissing()
        {
            var ctrl = Create();
            var res = ctrl.Search("");
            res.Should().BeOfType<BadRequestObjectResult>();
            var body = (res as BadRequestObjectResult)!.Value!;
            var msg = body.GetType().GetProperty("message")!.GetValue(body) as string;
            msg.Should().Be("Search query is required");
        }

        // The remaining CRUD tests would require invoking RequestService, which is tightly
        // coupled to a database. Those are better covered by integration tests.

        [Test]
        public void AcceptRequest_ShouldReturnBadRequest_WhenUnauthenticated()
        {
            var ctrl = Create();
            var res = ctrl.AcceptRequest(1);
            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void GetAcceptedRequests_ShouldReturnBadRequest_WhenUnauthenticated()
        {
            var ctrl = Create();
            var res = ctrl.GetAcceptedRequests();
            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void GetAcceptedStatus_ShouldReturnBadRequest_WhenUnauthenticated()
        {
            var ctrl = Create();
            var res = ctrl.GetAcceptedStatus(5);
            res.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
#pragma warning restore SYSLIB0050
