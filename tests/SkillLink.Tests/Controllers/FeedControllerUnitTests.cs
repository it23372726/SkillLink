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
    public class FeedControllerUnitTests
    {
        private static ClaimsPrincipal FakeUser(int userId, string role = "USER")
        {
            var id = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
            }, "TestAuth");
            return new ClaimsPrincipal(id);
        }

        private FeedController Create(out Mock<IFeedService> feed,
                                      out Mock<IReactionService> react,
                                      out Mock<ICommentService> comments,
                                      int userId = 11,
                                      string role = "USER")
        {
            feed = new Mock<IFeedService>(MockBehavior.Strict);
            react = new Mock<IReactionService>(MockBehavior.Strict);
            comments = new Mock<ICommentService>(MockBehavior.Strict);

            var ctrl = new FeedController(feed.Object, react.Object, comments.Object);
            var http = new DefaultHttpContext { User = FakeUser(userId, role) };
            ctrl.ControllerContext = new ControllerContext { HttpContext = http };
            return ctrl;
        }

        [Test]
        public void Get_ShouldPassParameters_AndReturnOk()
        {
            var ctrl = Create(out var feed, out _, out _);
            var expected = new List<FeedItemDto> {
                new FeedItemDto { PostType = "LESSON", PostId = 1, Title = "C#" }
            };

            feed.Setup(s => s.GetFeed(11, 2, 20, "java")).Returns(expected);

            var res = ctrl.Get(page: 2, pageSize: 20, q: "java");

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);

            feed.VerifyAll();
        }

        [Test]
        public void Like_ShouldUppercaseType_AndReturnOk()
        {
            var ctrl = Create(out _, out var react, out _);

            react.Setup(s => s.UpsertReaction(11, "LESSON", 9, "LIKE"));

            var res = ctrl.Like("lesson", 9);

            res.Should().BeOfType<OkObjectResult>();
            react.VerifyAll();
        }

        [Test]
        public void Dislike_ShouldUppercaseType_AndReturnOk()
        {
            var ctrl = Create(out _, out var react, out _);

            react.Setup(s => s.UpsertReaction(11, "REQUEST", 7, "DISLIKE"));

            var res = ctrl.Dislike("request", 7);

            res.Should().BeOfType<OkObjectResult>();
            react.VerifyAll();
        }

        [Test]
        public void RemoveReaction_ShouldReturnOk()
        {
            var ctrl = Create(out _, out var react, out _);

            react.Setup(s => s.RemoveReaction(11, "LESSON", 5));

            var res = ctrl.RemoveReaction("lesson", 5);

            res.Should().BeOfType<OkObjectResult>();
            react.VerifyAll();
        }

        [Test]
        public void GetComments_ShouldReturnOk_WithList()
        {
            var ctrl = Create(out _, out _, out var comments);

            var expected = new List<dynamic> { new { CommentId = 1, Content = "hi" } };
            comments.Setup(s => s.GetComments("LESSON", 10)).Returns(expected);

            var res = ctrl.GetComments("lesson", 10);

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(expected);
            comments.VerifyAll();
        }

        [Test]
        public void AddComment_ShouldReturnBadRequest_WhenEmpty()
        {
            var ctrl = Create(out _, out _, out _);

            var res = ctrl.AddComment("lesson", 5, new CreateCommentDto { Content = " " });

            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void AddComment_ShouldAdd_AndReturnOk()
        {
            var ctrl = Create(out _, out _, out var comments);

            comments.Setup(s => s.Add("LESSON", 5, 11, "Hello"));

            var res = ctrl.AddComment("lesson", 5, new CreateCommentDto { Content = " Hello " });

            res.Should().BeOfType<OkObjectResult>();
            comments.VerifyAll();
        }

        [Test]
        public void DeleteComment_ShouldPassIsAdmin_False()
        {
            var ctrl = Create(out _, out _, out var comments, userId: 33, role: "USER");

            comments.Setup(s => s.Delete(123, 33, false));

            var res = ctrl.DeleteComment(123);

            res.Should().BeOfType<OkObjectResult>();
            comments.VerifyAll();
        }

        [Test]
        public void DeleteComment_ShouldPassIsAdmin_True()
        {
            var ctrl = Create(out _, out _, out var comments, userId: 33, role: "ADMIN");

            comments.Setup(s => s.Delete(123, 33, true));

            var res = ctrl.DeleteComment(123);

            res.Should().BeOfType<OkObjectResult>();
            comments.VerifyAll();
        }
    }
}
