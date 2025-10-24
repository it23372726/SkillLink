using FluentAssertions;
using Moq;
using NUnit.Framework;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class RatingServiceUnitTests
    {
        [Test]
        public void Create_Should_Throw_When_AlreadyExists()
        {
            var repo = new Mock<IRatingRepository>(MockBehavior.Strict);
            repo.Setup(r => r.ExistsForAccepted(10, 7)).Returns(true);

            var svc = new RatingService(repo.Object);
            Assert.Throws<InvalidOperationException>(() => svc.Create(7, 9, 10, 5, "nice"));
        }

        [Test]
        public void Create_Should_Insert_With_Trimmed_Comment()
        {
            var repo = new Mock<IRatingRepository>(MockBehavior.Strict);
            repo.Setup(r => r.ExistsForAccepted(10, 7)).Returns(false);
            repo.Setup(r => r.Create(It.Is<Rating>(m =>
                m.AcceptedRequestId == 10 && m.LearnerId == 7 && m.TutorId == 9 && m.Score == 5 && m.Comment == "nice"
            )));

            var svc = new RatingService(repo.Object);
            svc.Create(7, 9, 10, 5, "  nice  ");
            repo.VerifyAll();
        }

        [Test]
        public void SummaryForTutor_Should_Default_When_Null()
        {
            var repo = new Mock<IRatingRepository>(MockBehavior.Strict);
            repo.Setup(r => r.SummaryForTutor(22)).Returns((RatingSummaryDto?)null);

            var svc = new RatingService(repo.Object);
            var s = svc.SummaryForTutor(22);
            s.TutorId.Should().Be(22);
            s.Count.Should().Be(0);
            s.Average.Should().Be(0);
        }

        [Test]
        public void List_Should_Delegate()
        {
            var repo = new Mock<IRatingRepository>(MockBehavior.Strict);
            repo.Setup(r => r.ListReceived(9, 5)).Returns(new List<RatingViewDto>());
            repo.Setup(r => r.ListGiven(7, 5)).Returns(new List<RatingViewDto>());

            var svc = new RatingService(repo.Object);
            svc.ListReceived(9, 5).Should().NotBeNull();
            svc.ListGiven(7, 5).Should().NotBeNull();
            repo.VerifyAll();
        }
    }
}
