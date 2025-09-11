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
    public class SkillsControllerUnitTests
    {
        private SkillsController Create()
        {
            var service = (SkillService)FormatterServices.GetUninitializedObject(typeof(SkillService));
            var ctrl = new SkillsController(service);
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return ctrl;
        }

        [Test]
        public void AddSkill_ShouldReturnOk()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void DeleteSkill_ShouldReturnOk()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void GetUserSkills_ShouldReturnOk_WithList()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void Suggest_ShouldReturnOk_WithList()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }

        [Test]
        public void FilterUsers_ShouldReturnOk_WithList()
        {
            Assert.Ignore("This endpoint calls service methods that require DB; covered by integration tests.");
        }
    }
}
#pragma warning restore SYSLIB0050
