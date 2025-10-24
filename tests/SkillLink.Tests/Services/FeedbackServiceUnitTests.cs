using FluentAssertions;
using Moq;
using NUnit.Framework;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class FeedbackServiceUnitTests
    {
        [Test]
        public void Submit_Should_Trim_And_Call_Insert()
        {
            var repo = new Mock<IFeedbackRepository>(MockBehavior.Strict);
            repo.Setup(r => r.Insert(It.Is<FeedbackCreateDto>(d =>
                    d.Subject == "Hello" &&
                    d.Message == "World" &&
                    d.Page == "/home" &&
                    d.UserAgent == "UA"
                ), 7)).Returns(42);

            var svc = new FeedbackService(repo.Object);
            var dto = new FeedbackCreateDto { Subject = "  Hello  ", Message = "  World  ", Page = "  /home  ", UserAgent = " UA " };
            var id = svc.Submit(dto, 7);

            id.Should().Be(42);
            repo.VerifyAll();
        }

        [Test]
        public void Submit_Should_Throw_When_Message_Missing()
        {
            var repo = new Mock<IFeedbackRepository>(MockBehavior.Strict);
            var svc = new FeedbackService(repo.Object);
            Assert.Throws<ArgumentException>(() => svc.Submit(new FeedbackCreateDto { Message = "  " }, null));
        }

        [Test]
        public void List_And_MarkRead_Should_Delegate()
        {
            var repo = new Mock<IFeedbackRepository>(MockBehavior.Strict);
            repo.Setup(r => r.List(null, 10, 0)).Returns(new List<FeedbackItem>());
            repo.Setup(r => r.MarkRead(5, true));

            var svc = new FeedbackService(repo.Object);
            svc.List(null, 10, 0).Should().NotBeNull();
            svc.MarkRead(5, true);

            repo.VerifyAll();
        }
    }
}
