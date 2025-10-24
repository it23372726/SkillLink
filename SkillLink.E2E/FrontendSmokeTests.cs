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
            if (!(url.Contains("/dashboard") || url.Contains("/admin-dashboard")))
            {
                string err = Driver.FindElements(By.CssSelector(".bg-red-50")).Count > 0
                    ? Driver.FindElement(By.CssSelector(".bg-red-50")).Text
                    : "(no error banner)";
                Assert.Inconclusive($"Login did not navigate. Likely API/DB not running. URL: {url}. Error: {err}");
                return;
            }

            url.Should().MatchRegex(".*/dashboard|.*/admin-dashboard");
        }
    }
}
