using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Models.Reports;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class AdminReportsControllerUnitTests
    {
        [Test]
        public void SkillDemand_ShouldReturnOkList()
        {
            var svc = new Mock<IReportsService>(MockBehavior.Strict);
            var data = new List<SkillDemandRow> { new SkillDemandRow { SkillName = "C#", TotalRequests = 5 } };
            svc.Setup(s => s.GetTopRequestedSkills(null, null, 10)).Returns(data);

            var ctrl = new AdminReportsController(svc.Object);
            var res = ctrl.SkillDemand(null, null, 10);
            res.Result.Should().BeOfType<OkObjectResult>();
            ((OkObjectResult)res.Result!).Value.Should().BeSameAs(data);
        }
    }
}
