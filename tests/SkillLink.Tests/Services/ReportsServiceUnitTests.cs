using FluentAssertions;
using Moq;
using NUnit.Framework;
using SkillLink.API.Models.Reports;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class ReportsServiceUnitTests
    {
        [Test]
        public void GetTopRequestedSkills_Should_Coerce_Limit()
        {
            var repo = new Mock<IReportsRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetTopRequestedSkills(null, null, 10)).Returns(new List<SkillDemandRow>());

            var svc = new ReportsService(repo.Object);
            var list = svc.GetTopRequestedSkills(null, null, 0);
            list.Should().NotBeNull();
            repo.VerifyAll();
        }
    }
}
