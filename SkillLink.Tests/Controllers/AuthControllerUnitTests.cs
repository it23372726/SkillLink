using System;
using System.Runtime.Serialization;
// Suppress obsolete FormatterServices warning in this test file only
#pragma warning disable SYSLIB0050
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Models;
using SkillLink.API.Services;
using System.Security.Claims;

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerUnitTests
    {
        private AuthController CreateControllerWithAuthService(AuthService? service = null, ClaimsPrincipal? user = null)
        {
            // Create an AuthService instance without running its constructor (avoids DB/config deps)
            service ??= (AuthService)FormatterServices.GetUninitializedObject(typeof(AuthService));
            var controller = new AuthController(service);

            // Attach an HttpContext so controller.User is available
            var httpContext = new DefaultHttpContext();
            httpContext.User = user ?? new ClaimsPrincipal(new ClaimsIdentity()); // unauthenticated by default

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            return controller;
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void VerifyEmail_ShouldReturnBadRequest_WhenTokenMissing(string? token)
        {
            var controller = CreateControllerWithAuthService();

            var result = controller.VerifyEmail(token!);

            result.Should().BeOfType<BadRequestObjectResult>();
            var body = (result as BadRequestObjectResult)!.Value!;
            var messageProp = body.GetType().GetProperty("message");
            messageProp.Should().NotBeNull();
            var message = (string?)messageProp!.GetValue(body);
            message.Should().Be("Missing token");
        }

        [Test]
        public void GetCurrentUser_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            var controller = CreateControllerWithAuthService();

            var result = controller.GetCurrentUser();

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var body = (result as UnauthorizedObjectResult)!.Value!;
            var messageProp = body.GetType().GetProperty("message");
            messageProp.Should().NotBeNull();
            var message = (string?)messageProp!.GetValue(body);
            message.Should().Be("Invalid token or user not logged in");
        }

        [Test]
        public void GetUserProfile_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            var controller = CreateControllerWithAuthService();

            var result = controller.GetUserProfile();

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var body = (result as UnauthorizedObjectResult)!.Value!;
            var messageProp = body.GetType().GetProperty("message");
            messageProp.Should().NotBeNull();
            var message = (string?)messageProp!.GetValue(body);
            message.Should().Be("Invalid token or user not logged in");
        }

        [Test]
        public void UpdateUserProfile_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            var controller = CreateControllerWithAuthService();
            var req = new UpdateProfileRequest { FullName = "", Bio = null, Location = null };

            var result = controller.UpdateUserProfile(req);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var body = (result as UnauthorizedObjectResult)!.Value!;
            var messageProp = body.GetType().GetProperty("message");
            messageProp.Should().NotBeNull();
            var message = (string?)messageProp!.GetValue(body);
            message.Should().Be("Invalid token or user not logged in");
        }

        [Test]
        public void UpdateTeachMode_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            var controller = CreateControllerWithAuthService();
            var req = new AuthController.UpdateTeachModeRequest { ReadyToTeach = true };

            var result = controller.UpdateTeachMode(req);

            // This action returns bare Unauthorized()
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Test]
        public void UpdateActive_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            var controller = CreateControllerWithAuthService();
            var req = new AuthController.UpdateActiveRequest { IsActive = true };

            var result = controller.UpdateActive(req);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var body = (result as UnauthorizedObjectResult)!.Value!;
            var messageProp = body.GetType().GetProperty("message");
            messageProp.Should().NotBeNull();
            var message = (string?)messageProp!.GetValue(body);
            message.Should().Be("Invalid token");
        }

        [Test]
        public void Register_ShouldReturnBadRequest_WhenRequiredFieldsMissing()
        {
            var controller = CreateControllerWithAuthService();
            var invalidReq = new RegisterRequest
            {
                FullName = "", // missing
                Email = "",     // missing
                Password = ""   // missing
            };

            var result = controller.Register(invalidReq).GetAwaiter().GetResult();

            result.Should().BeOfType<BadRequestObjectResult>();
            var body = (result as BadRequestObjectResult)!.Value!;
            var messageProp = body.GetType().GetProperty("message");
            messageProp.Should().NotBeNull();
            var message = (string?)messageProp!.GetValue(body);
            message.Should().Be("Full name, email, and password are required.");
        }
    }
}
#pragma warning restore SYSLIB0050
