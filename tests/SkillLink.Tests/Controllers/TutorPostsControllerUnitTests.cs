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

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class TutorPostsControllerUnitTests
    {
        private static ClaimsPrincipal FakeUser(int userId, string role = "USER")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
            }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private TutorPostsController Create(out Mock<ITutorPostService> mock, int currentUserId = 99)
        {
            mock = new Mock<ITutorPostService>(MockBehavior.Strict);
            var ctrl = new TutorPostsController(mock.Object);
            var http = new DefaultHttpContext { User = FakeUser(currentUserId) };
            ctrl.ControllerContext = new ControllerContext { HttpContext = http };
            return ctrl;
        }

        [Test]
        public void Create_ShouldUseLoggedInUserId_AndReturnOkWithNewId()
        {
            var me = 99;
            var ctrl = Create(out var mock, me);
            var dto = new CreateTutorPostDto { Title = "C#", Description = "Basics", MaxParticipants = 3 };

            mock.Setup(s => s.CreatePost(It.Is<TutorPost>(p =>
                    p.TutorId == me &&
                    p.Title == "C#" &&
                    p.Description == "Basics" &&
                    p.MaxParticipants == 3 &&
                    p.Status == "Open"
                )))
                .Returns(123);

            var res = ctrl.Create(dto);

            res.Should().BeOfType<OkObjectResult>();
            var body = (res as OkObjectResult)!.Value!;
            var postId = (int)body.GetType().GetProperty("postId")!.GetValue(body)!;
            postId.Should().Be(123);

            mock.VerifyAll();
        }

        [Test]
        public void GetAll_ShouldReturnOk_WithList()
        {
            var ctrl = Create(out var mock);
            var expected = new List<TutorPostWithUser> {
                new TutorPostWithUser { PostId = 1, Title = "Java", TutorId = 1, TutorName = "Alice" }
            };
            mock.Setup(s => s.GetPosts()).Returns(expected);

            var res = ctrl.GetAll();

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);
            mock.VerifyAll();
        }

        [Test]
        public void GetById_ShouldReturnOk_WhenFound()
        {
            var ctrl = Create(out var mock);
            var post = new TutorPostWithUser { PostId = 5, Title = "SQL" };
            mock.Setup(s => s.GetById(5)).Returns(post);

            var res = ctrl.GetById(5);

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(post);
            mock.VerifyAll();
        }

        [Test]
        public void GetById_ShouldReturnNotFound_WhenMissing()
        {
            var ctrl = Create(out var mock);
            mock.Setup(s => s.GetById(88)).Returns((TutorPostWithUser?)null);

            var res = ctrl.GetById(88);

            res.Should().BeOfType<NotFoundObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Accept_ShouldReturnOk()
        {
            var me = 77;
            var ctrl = Create(out var mock, me);

            mock.Setup(s => s.AcceptPost(10, me));

            var res = ctrl.Accept(10);

            res.Should().BeOfType<OkObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Accept_ShouldReturnNotFound_WhenServiceThrowsKeyNotFound()
        {
            var me = 77;
            var ctrl = Create(out var mock, me);
            mock.Setup(s => s.AcceptPost(10, me)).Throws(new KeyNotFoundException("Post not found."));

            var res = ctrl.Accept(10);

            res.Should().BeOfType<NotFoundObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Accept_ShouldReturnBadRequest_WhenServiceThrowsInvalidOperation()
        {
            var me = 77;
            var ctrl = Create(out var mock, me);
            mock.Setup(s => s.AcceptPost(10, me)).Throws(new InvalidOperationException("Post is full."));

            var res = ctrl.Accept(10);

            res.Should().BeOfType<BadRequestObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Schedule_ShouldReturnOk()
        {
            var ctrl = Create(out var mock);
            var dto = new ScheduleTutorPostDto { ScheduledAt = DateTime.UtcNow.AddDays(1) };
            mock.Setup(s => s.Schedule(15, dto.ScheduledAt));

            var res = ctrl.Schedule(15, dto);

            res.Should().BeOfType<OkObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Update_ShouldReturnOk()
        {
            var me = 10;
            var ctrl = Create(out var mock, me);
            var dto = new UpdateTutorPostDto { Title = "Updated", MaxParticipants = 5, Description = "desc" };

            mock.Setup(s => s.UpdatePost(55, me, dto));

            var res = ctrl.Update(55, dto);

            res.Should().BeOfType<OkObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Update_ShouldReturnForbid_WhenUnauthorized()
        {
            var me = 10;
            var ctrl = Create(out var mock, me);
            var dto = new UpdateTutorPostDto { Title = "Updated", MaxParticipants = 5 };

            mock.Setup(s => s.UpdatePost(55, me, dto))
                .Throws(new UnauthorizedAccessException("Only owner can update."));

            var res = ctrl.Update(55, dto);

            res.Should().BeOfType<ForbidResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Update_ShouldReturnNotFound_WhenMissing()
        {
            var me = 10;
            var ctrl = Create(out var mock, me);
            var dto = new UpdateTutorPostDto { Title = "Updated", MaxParticipants = 5 };

            mock.Setup(s => s.UpdatePost(55, me, dto))
                .Throws(new KeyNotFoundException("Post not found."));

            var res = ctrl.Update(55, dto);

            res.Should().BeOfType<NotFoundObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Update_ShouldReturnBadRequest_WhenInvalid()
        {
            var me = 10;
            var ctrl = Create(out var mock, me);
            var dto = new UpdateTutorPostDto { Title = "Updated", MaxParticipants = 1 };

            mock.Setup(s => s.UpdatePost(55, me, dto))
                .Throws(new InvalidOperationException("MaxParticipants cannot be less than current participants."));

            var res = ctrl.Update(55, dto);

            res.Should().BeOfType<BadRequestObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Delete_ShouldReturnOk()
        {
            var me = 7;
            var ctrl = Create(out var mock, me);

            mock.Setup(s => s.DeletePost(66, me));

            var res = ctrl.Delete(66);

            res.Should().BeOfType<OkObjectResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Delete_ShouldReturnForbid_WhenUnauthorized()
        {
            var me = 7;
            var ctrl = Create(out var mock, me);

            mock.Setup(s => s.DeletePost(66, me))
                .Throws(new UnauthorizedAccessException("Only owner can delete."));

            var res = ctrl.Delete(66);

            res.Should().BeOfType<ForbidResult>();
            mock.VerifyAll();
        }

        [Test]
        public void Delete_ShouldReturnNotFound_WhenMissing()
        {
            var me = 7;
            var ctrl = Create(out var mock, me);

            mock.Setup(s => s.DeletePost(66, me))
                .Throws(new KeyNotFoundException("Post not found."));

            var res = ctrl.Delete(66);

            res.Should().BeOfType<NotFoundObjectResult>();
            mock.VerifyAll();
        }
    }
}
