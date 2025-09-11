using NUnit.Framework;
using SkillLink.API.Models;
using System;

namespace SkillLink.Tests.Models
{
    [TestFixture]
    public class RequestModelUnitTests
    {
        [Test]
        public void Request_DefaultValues_ShouldBeCorrect()
        {
            var r = new Request();
            Assert.That(r.RequestId, Is.EqualTo(0));
            Assert.That(r.LearnerId, Is.EqualTo(0));
            Assert.That(r.SkillName, Is.Null); // not initialized
            Assert.That(r.Topic, Is.Null);
            Assert.That(r.Status, Is.EqualTo("OPEN"));
            Assert.That(r.CreatedAt, Is.EqualTo(default(DateTime)));
            Assert.That(r.Description, Is.Null);
        }

        [Test]
        public void RequestWithUser_ShouldInheritAndAddFields()
        {
            var r = new RequestWithUser
            {
                RequestId = 1,
                LearnerId = 2,
                SkillName = "React",
                FullName = "Alice",
                Email = "alice@uni.edu"
            };

            Assert.That(r.RequestId, Is.EqualTo(1));
            Assert.That(r.SkillName, Is.EqualTo("React"));
            Assert.That(r.FullName, Is.EqualTo("Alice"));
            Assert.That(r.Email, Is.EqualTo("alice@uni.edu"));
        }

        [Test]
        public void AcceptedRequest_DefaultValues_ShouldBeCorrect()
        {
            var a = new AcceptedRequest();
            Assert.That(a.AcceptedRequestId, Is.EqualTo(0));
            Assert.That(a.RequestId, Is.EqualTo(0));
            Assert.That(a.AcceptorId, Is.EqualTo(0));
            Assert.That(a.Status, Is.EqualTo("PENDING"));   // default
            Assert.That(a.ScheduleDate, Is.Null);
            Assert.That(a.MeetingType, Is.EqualTo(string.Empty));
            Assert.That(a.MeetingLink, Is.EqualTo(string.Empty));
        }

        [Test]
        public void AcceptedRequestWithDetails_ShouldInheritAndAddFields()
        {
            var a = new AcceptedRequestWithDetails
            {
                AcceptedRequestId = 5,
                RequestId = 2,
                AcceptorId = 10,
                SkillName = "Python",
                Topic = "Loops",
                Description = "For/While",
                RequesterName = "Bob",
                RequesterEmail = "bob@uni.edu",
                RequesterId = 22
            };

            Assert.That(a.SkillName, Is.EqualTo("Python"));
            Assert.That(a.Topic, Is.EqualTo("Loops"));
            Assert.That(a.RequesterEmail, Is.EqualTo("bob@uni.edu"));
            Assert.That(a.RequesterId, Is.EqualTo(22));
        }

        [Test]
        public void ScheduleMeetingRequest_DefaultsAndAssignment_ShouldWork()
        {
            var sm = new ScheduleMeetingRequest
            {
                ScheduleDate = DateTime.UtcNow,
                MeetingType = "Zoom",
                MeetingLink = "http://zoom.link"
            };

            Assert.That(sm.MeetingType, Is.EqualTo("Zoom"));
            Assert.That(sm.MeetingLink, Is.EqualTo("http://zoom.link"));
            Assert.That(sm.ScheduleDate, Is.Not.EqualTo(default(DateTime)));
        }
    }
}
