using NUnit.Framework;
using OpenQA.Selenium;
using FluentAssertions;

namespace SkillLink.E2E
{
    public class FrontendSmokeTests : BaseUiTest
    {
        [Test]
        public void Login_Should_Succeed_And_Navigate()
        {
            Driver.Navigate().GoToUrl($"{FrontendUrl}/login");

            Driver.FindElement(By.CssSelector("input[name='email']")).SendKeys("admin@skilllink.local");
            Driver.FindElement(By.CssSelector("input[name='password']")).SendKeys("Admin@123");
            Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Allow redirect
            System.Threading.Thread.Sleep(1000);

            var url = Driver.Url;
            url.Should().MatchRegex(".*/dashboard|.*/admin-dashboard");
        }
    }
}
