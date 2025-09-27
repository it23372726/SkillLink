using System;
using FluentAssertions;
using NUnit.Framework;
using SkillLink.API.Models;

namespace SkillLink.Tests.Models
{
    [TestFixture]
    public class MoreModelUnitTests
    {
        /* ============================ FEED & COMMENTS ============================ */

        [Test]
        public void FeedItemDto_Defaults_ShouldBeCorrect()
        {
            var f = new FeedItemDto();
            f.PostType.Should().Be("");
            f.PostId.Should().Be(0);

            f.AuthorId.Should().Be(0);
            f.AuthorName.Should().Be("");
            f.AuthorEmail.Should().Be("");
            f.AuthorPic.Should().Be(""); // non-nullable default ""

            f.Title.Should().Be("");
            f.Subtitle.Should().Be("");
            f.Body.Should().Be("");
            f.ImageUrl.Should().BeNull();
            f.CreatedAt.Should().Be(default(DateTime));

            f.Status.Should().Be("");
            f.ScheduledAt.Should().BeNull();

            f.Likes.Should().Be(0);
            f.Dislikes.Should().Be(0);
            f.CommentCount.Should().Be(0);
            f.MyReaction.Should().BeNull();
        }

        [Test]
        public void FeedItemDto_Assignment_ShouldPersist()
        {
            var dt = DateTime.UtcNow.AddMinutes(-5);
            var f = new FeedItemDto
            {
                PostType = "LESSON",
                PostId = 10,
                AuthorId = 99,
                AuthorName = "Ada",
                AuthorEmail = "ada@uni.edu",
                AuthorPic = "/img/ada.png",
                Title = "Data Structures",
                Subtitle = "",
                Body = "Stacks & Queues",
                ImageUrl = "/uploads/p1.png",
                CreatedAt = dt,
                Status = "Open",
                ScheduledAt = null,
                Likes = 3,
                Dislikes = 1,
                CommentCount = 2,
                MyReaction = "LIKE"
            };

            f.PostType.Should().Be("LESSON");
            f.AuthorName.Should().Be("Ada");
            f.ImageUrl.Should().Be("/uploads/p1.png");
            f.MyReaction.Should().Be("LIKE");
            f.CreatedAt.Should().Be(dt);
        }

        [Test]
        public void CreateCommentDto_Default_ShouldBeEmptyString()
        {
            var c = new CreateCommentDto();
            c.Content.Should().Be("");
        }

        /* ============================ FRIENDSHIP ============================ */

        [Test]
        public void Friendship_Defaults_ShouldBeCorrect()
        {
            var before = DateTime.Now.AddSeconds(-2);
            var fr = new Friendship();
            var after = DateTime.Now.AddSeconds(2);

            fr.Id.Should().Be(0);
            fr.FollowerId.Should().Be(0);
            fr.FollowedId.Should().Be(0);
            fr.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        /* ============================ TUTOR POSTS ============================ */

        [Test]
        public void TutorPost_Defaults_ShouldBeCorrect()
        {
            var before = DateTime.Now.AddSeconds(-2);
            var tp = new TutorPost();
            var after = DateTime.Now.AddSeconds(2);

            tp.PostId.Should().Be(0);
            tp.TutorId.Should().Be(0);
            tp.Title.Should().Be("");
            tp.Description.Should().Be("");
            tp.MaxParticipants.Should().Be(0);
            tp.Status.Should().Be("Open");
            tp.ImageUrl.Should().BeNull();
            tp.CurrentParticipants.Should().Be(0);

            tp.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
            tp.ScheduledAt.Should().BeNull();
        }

        [Test]
        public void TutorPostWithUser_ShouldInherit_AndAddFields()
        {
            var tp = new TutorPostWithUser
            {
                PostId = 5,
                TutorId = 44,
                Title = "Intro to ML",
                TutorName = "Sarah",
                Email = "sarah@uni.edu"
            };

            tp.PostId.Should().Be(5);
            tp.TutorId.Should().Be(44);
            tp.TutorName.Should().Be("Sarah");
            tp.Email.Should().Be("sarah@uni.edu");
        }

        [Test]
        public void TutorPostParticipant_Defaults_ShouldBeCorrect()
        {
            var before = DateTime.Now.AddSeconds(-2);
            var p = new TutorPostParticipant();
            var after = DateTime.Now.AddSeconds(2);

            p.ParticipantId.Should().Be(0);
            p.PostId.Should().Be(0);
            p.UserId.Should().Be(0);
            p.AcceptedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [Test]
        public void CreateTutorPostDto_Defaults_ShouldBeCorrect()
        {
            var dto = new CreateTutorPostDto();
            dto.Title.Should().Be("");
            dto.Description.Should().BeNull();
            dto.MaxParticipants.Should().Be(0);
        }

        [Test]
        public void UpdateTutorPostDto_Defaults_ShouldBeCorrect()
        {
            var dto = new UpdateTutorPostDto();
            dto.Title.Should().Be("");
            dto.Description.Should().BeNull();
            dto.MaxParticipants.Should().Be(0);
            dto.Status.Should().BeNull();
        }

        [Test]
        public void ScheduleTutorPostDto_Assignment_ShouldPersist()
        {
            var dt = DateTime.UtcNow.AddDays(1);
            var dto = new ScheduleTutorPostDto { ScheduledAt = dt };
            dto.ScheduledAt.Should().Be(dt);
        }

        /* ============================ USERS & AUTH ============================ */

        [Test]
        public void User_Defaults_ShouldBeCorrect()
        {
            var u = new User();
            u.UserId.Should().Be(0);
            u.FullName.Should().Be("");
            u.Email.Should().Be("");
            u.PasswordHash.Should().Be("");
            u.Role.Should().Be("Learner");
            u.CreatedAt.Should().Be(default(DateTime));
            u.Bio.Should().BeNull();
            u.Location.Should().BeNull();
            u.ProfilePicture.Should().BeNull();
            u.ReadyToTeach.Should().BeFalse();
            u.IsActive.Should().BeFalse();
            u.EmailVerified.Should().BeFalse();
            u.EmailVerificationToken.Should().BeNull();
            u.EmailVerificationExpires.Should().BeNull();
        }

        [Test]
        public void UpdateProfileRequest_Defaults_ShouldBeCorrect()
        {
            var r = new UpdateProfileRequest();
            r.FullName.Should().Be("");
            r.Bio.Should().BeNull();
            r.Location.Should().BeNull();
        }

        [Test]
        public void LoginRequest_Defaults_ShouldBeCorrect()
        {
            var lr = new LoginRequest();
            lr.Email.Should().Be("");
            lr.Password.Should().Be("");
        }

        [Test]
        public void RegisterRequest_Defaults_ShouldBeCorrect()
        {
            var rr = new RegisterRequest();
            rr.FullName.Should().Be("");
            rr.Email.Should().Be("");
            rr.Password.Should().Be("");
            rr.Role.Should().Be("Learner");
            rr.ProfilePicture.Should().BeNull();
            rr.ProfilePicturePath.Should().BeNull();
        }

        /* ============================ SKILLS ============================ */

        [Test]
        public void Skill_Defaults_ShouldBeCorrect()
        {
            var s = new Skill();
            s.SkillId.Should().Be(0);
            s.Name.Should().Be("");
            s.IsPredefined.Should().BeFalse();
        }

        [Test]
        public void UserSkill_Defaults_ShouldBeCorrect()
        {
            var us = new UserSkill();
            us.UserSkillId.Should().Be(0);
            us.UserId.Should().Be(0);
            us.SkillId.Should().Be(0);
            us.Level.Should().Be("");
            us.Skill.Should().BeNull();
        }

        [Test]
        public void AddSkillRequest_Defaults_ShouldBeCorrect()
        {
            var req = new AddSkillRequest();
            req.UserId.Should().Be(0);
            req.SkillName.Should().Be("");
            req.Level.Should().Be("Beginner");
        }

        /* ============================ SESSION ============================ */

        [Test]
        public void Session_Defaults_ShouldBeCorrect()
        {
            var s = new Session();
            s.SessionId.Should().Be(0);
            s.RequestId.Should().Be(0);
            s.TutorId.Should().Be(0);
            s.ScheduledAt.Should().BeNull();
            s.Status.Should().Be("PENDING");
            s.CreatedAt.Should().Be(default(DateTime));
            s.RoomName.Should().Be("SkillLinkSession_0");
        }

        [Test]
        public void Session_Assignment_ShouldAffectRoomName()
        {
            var s = new Session { SessionId = 123 };
            s.RoomName.Should().Be("SkillLinkSession_123");
        }

        /* ============================ REQUESTS & ACCEPTED REQUESTS ============================ */

        [Test]
        public void Request_Defaults_ShouldBeCorrect()
        {
            var r = new Request();
            r.RequestId.Should().Be(0);
            r.LearnerId.Should().Be(0);
            r.SkillName.Should().BeNull(); // non-nullable type but uninitialized → runtime null
            r.Topic.Should().BeNull();
            r.Status.Should().Be("OPEN");
            r.CreatedAt.Should().Be(default(DateTime));
            r.Description.Should().BeNull();
        }

        [Test]
        public void RequestWithUser_ShouldInherit_AndAddFields()
        {
            var r = new RequestWithUser
            {
                RequestId = 9,
                LearnerId = 5,
                SkillName = "C#",
                FullName = "Nina",
                Email = "nina@uni.edu"
            };

            r.RequestId.Should().Be(9);
            r.SkillName.Should().Be("C#");
            r.FullName.Should().Be("Nina");
            r.Email.Should().Be("nina@uni.edu");
        }

        [Test]
        public void AcceptedRequest_Defaults_ShouldBeCorrect()
        {
            var a = new AcceptedRequest();
            a.AcceptedRequestId.Should().Be(0);
            a.RequestId.Should().Be(0);
            a.AcceptorId.Should().Be(0);
            a.AcceptedAt.Should().Be(default(DateTime)); // no default set in model
            a.Status.Should().Be("PENDING");
            a.ScheduleDate.Should().BeNull();
            a.MeetingType.Should().Be("");
            a.MeetingLink.Should().Be("");
        }

        [Test]
        public void AcceptedRequestWithDetails_ShouldInherit_AndAddFields()
        {
            var a = new AcceptedRequestWithDetails
            {
                AcceptedRequestId = 11,
                RequestId = 100,
                AcceptorId = 70,
                SkillName = "Python",
                Topic = "Functions",
                Description = "Scopes & closures",
                RequesterName = "Janet",
                RequesterEmail = "janet@uni.edu",
                RequesterId = 200
            };

            a.AcceptedRequestId.Should().Be(11);
            a.SkillName.Should().Be("Python");
            a.RequesterEmail.Should().Be("janet@uni.edu");
            a.RequesterId.Should().Be(200);
        }

        [Test]
        public void ScheduleMeetingRequest_Defaults_And_Assignment()
        {
            var def = new ScheduleMeetingRequest();
            def.ScheduleDate.Should().Be(default(DateTime));
            def.MeetingType.Should().Be("");
            def.MeetingLink.Should().Be("");

            var dt = DateTime.UtcNow.AddHours(3);
            var sm = new ScheduleMeetingRequest
            {
                ScheduleDate = dt,
                MeetingType = "Google Meet",
                MeetingLink = "https://meet.link/abc"
            };
            sm.ScheduleDate.Should().Be(dt);
            sm.MeetingType.Should().Be("Google Meet");
            sm.MeetingLink.Should().Be("https://meet.link/abc");
        }
    }
}
