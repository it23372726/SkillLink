// using NUnit.Framework;
// using SkillLink.API.Models;
// using System;

// namespace SkillLink.Tests.Models
// {
//     [TestFixture]
//     public class SessionModelUnitTests
//     {
//         [Test]
//         public void Session_DefaultValues_ShouldBeCorrect()
//         {
//             var s = new Session();

//             Assert.That(s.SessionId, Is.EqualTo(0));
//             Assert.That(s.RequestId, Is.EqualTo(0));
//             Assert.That(s.TutorId, Is.EqualTo(0));
//             Assert.That(s.ScheduledAt, Is.Null);             // nullable default
//             Assert.That(s.Status, Is.EqualTo("PENDING"));    // default value
//             Assert.That(s.CreatedAt, Is.EqualTo(default(DateTime)));
//         }

//         [Test]
//         public void RoomName_ShouldIncludeSessionId()
//         {
//             var s = new Session { SessionId = 42 };
//             Assert.That(s.RoomName, Is.EqualTo("SkillLinkSession_42"));
//         }

//         [Test]
//         public void ScheduledAt_CanBeAssigned()
//         {
//             var now = DateTime.UtcNow;
//             var s = new Session { ScheduledAt = now };

//             Assert.That(s.ScheduledAt.HasValue, Is.True);
//             Assert.That(s.ScheduledAt!.Value, Is.EqualTo(now));
//         }

//         [Test]
//         public void Status_CanBeChanged()
//         {
//             var s = new Session();
//             s.Status = "COMPLETED";
//             Assert.That(s.Status, Is.EqualTo("COMPLETED"));
//         }

//         [Test]
//         public void CreatedAt_CanBeAssigned()
//         {
//             var time = DateTime.UtcNow;
//             var s = new Session { CreatedAt = time };

//             Assert.That(s.CreatedAt, Is.EqualTo(time));
//         }
//     }
// }
