using NUnit.Framework;
using OpenQA.Selenium;
using FluentAssertions;
using System.Threading;

namespace SkillLink.E2E
{
    public class AccessControlTests : BaseUiTest
    {
        [Test]
        public void Learner_Cannot_Access_Admin_Dashboard()
        {
            // Login as learner
            Driver.Navigate().GoToUrl($"{FrontendUrl}/login");
            Driver.FindElement(By.CssSelector("input[name='email']")).SendKeys("learner@skilllink.local");
            Driver.FindElement(By.CssSelector("input[name='password']")).SendKeys("Learner@123");
            Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            Thread.Sleep(600);
            // Try to access admin page
            Driver.Navigate().GoToUrl($"{FrontendUrl}/admin-dashboard");
            Thread.Sleep(600);

            // Expect to be redirected away or see no access
            Driver.Url.Should().NotContain("/admin-dashboard"); // or assert some "not authorized" text if you show it
        }
    }
}
