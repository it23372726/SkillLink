using System.Runtime.Serialization;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SkillLink.API.Controllers;
using SkillLink.API.Models;
using SkillLink.API.Services;

#pragma warning disable SYSLIB0050

namespace SkillLink.Tests.Controllers
{
    [TestFixture]
    public class SessionsControllerUnitTests
    {
        private SessionsController Create()
        {
            var service = (SessionService)FormatterServices.GetUninitializedObject(typeof(SessionService));
            var ctrl = new SessionsController(service);
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return ctrl;
        }

        [Test]
        public void GetAll_ShouldReturnOk_WithList()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void GetById_ShouldReturnNotFound_WhenMissing()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void GetByTutorId_ShouldReturnNotFound_WhenEmpty()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void Create_ShouldReturnOk()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void UpdateStatus_ShouldReturnOk()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void Delete_ShouldReturnOk()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }
    }
}
#pragma warning restore SYSLIB0050
