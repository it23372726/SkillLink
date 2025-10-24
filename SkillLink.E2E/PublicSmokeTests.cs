using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using FluentAssertions;
using System;

namespace SkillLink.E2E
{
    // Lightweight, API-independent smoke checks for public pages
    public class PublicSmokeTests : BaseUiTest
    {
        [Test]
        public void Landing_Page_Should_Load()
        {
            Driver.Navigate().GoToUrl(FrontendUrl);

            // Wait for document ready
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");

            // Basic sanity: body exists and has some content
            var body = Driver.FindElement(By.TagName("body"));
            body.Should().NotBeNull();
            (body.Text?.Length ?? 0).Should().BeGreaterThan(0, "Landing page should render some text");
        }

        [Test]
        public void Login_Page_Should_Render_Inputs()
        {
            Driver.Navigate().GoToUrl($"{FrontendUrl}/login");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElements(By.CssSelector("input[name='email']")).Count > 0);

            var email = Driver.FindElement(By.CssSelector("input[name='email']"));
            var password = Driver.FindElement(By.CssSelector("input[name='password']"));
            var submit = Driver.FindElement(By.CssSelector("button[type='submit']"));

            email.Displayed.Should().BeTrue();
            password.Displayed.Should().BeTrue();
            submit.Displayed.Should().BeTrue();
        }
    }
}
