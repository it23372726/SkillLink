using NUnit.Framework;
using OpenQA.Selenium;
using FluentAssertions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq;

namespace SkillLink.E2E
{
    public class AdminUserTests : BaseUiTest
    {
        private void LoginAsAdmin()
        {
            Driver.Navigate().GoToUrl($"{FrontendUrl}/login");
            Driver.FindElement(By.CssSelector("input[name='email']")).SendKeys("admin@skilllink.local");
            Driver.FindElement(By.CssSelector("input[name='password']")).SendKeys("Admin@123");
            Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Wait until redirected
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(8));
            wait.Until(d => d.Url.Contains("/admin-dashboard"));
            Driver.Url.Should().Contain("/admin-dashboard");
        }

        [Test]
        public void Admin_Can_Filter_And_Export()
        {
            LoginAsAdmin();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(8));
            wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count >= 0);

            // Click filter buttons
            var filters = new[] { "ALL", "ACTIVE", "INACTIVE", "TUTORS", "ADMINS" };
            foreach (var f in filters)
            {
                var btn = Driver.FindElements(By.CssSelector("button"))
                                .FirstOrDefault(b => (b.Text ?? "").Trim().Equals(
                                    f[0] + f[1..].ToLower(),
                                    StringComparison.OrdinalIgnoreCase));

                if (btn != null)
                {
                    ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", btn);
                    btn.Click();
                    System.Threading.Thread.Sleep(200);
                }
            }

            // CSV export
            var exportBtn = Driver.FindElements(By.CssSelector("button"))
                                  .FirstOrDefault(b => (b.Text ?? "").Contains("Export CSV", StringComparison.OrdinalIgnoreCase));
            exportBtn.Should().NotBeNull();
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", exportBtn);
            exportBtn!.Click();
        }

        [Test]
        public void Admin_Can_Update_Role_And_Activate_User()
        {
            LoginAsAdmin();
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(8));

            // Find first row (skip header)
            wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);
            var row = Driver.FindElements(By.CssSelector("table tbody tr")).First();

            // Role select
            var select = row.FindElements(By.CssSelector("select")).FirstOrDefault();
            select.Should().NotBeNull();

            // ✅ Scroll into view before clicking (prevents Dock overlap issue)
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", select);
            select!.Click();

            var options = select.FindElements(By.TagName("option"));
            options.Any().Should().BeTrue();
            options.First(o => o.Text.Contains("Tutor", StringComparison.OrdinalIgnoreCase)).Click(); // set to Tutor
            System.Threading.Thread.Sleep(500);

            // Activate/Deactivate button
            var actionBtn = row.FindElements(By.CssSelector("button"))
                .FirstOrDefault(b =>
                    (b.Text ?? "").Contains("Activate", StringComparison.OrdinalIgnoreCase) ||
                    (b.Text ?? "").Contains("Deactivate", StringComparison.OrdinalIgnoreCase));

            actionBtn.Should().NotBeNull();
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", actionBtn);
            actionBtn!.Click();
            System.Threading.Thread.Sleep(500);
        }

        [Test]
        public void Admin_Can_Delete_User_From_Drawer()
        {
            LoginAsAdmin();
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(8));

            wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);
            var row = Driver.FindElements(By.CssSelector("table tbody tr")).First();

            // Open drawer via "View"
            var viewBtn = row.FindElements(By.CssSelector("button"))
                             .FirstOrDefault(b => (b.Text ?? "").Contains("View", StringComparison.OrdinalIgnoreCase));
            viewBtn.Should().NotBeNull();
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", viewBtn);
            viewBtn!.Click();

            // Click delete inside drawer (auto confirm)
            var deleteBtn = wait.Until(d => d.FindElements(By.CssSelector("button"))
                                             .FirstOrDefault(b => (b.Text ?? "").Contains("Delete Permanently", StringComparison.OrdinalIgnoreCase)));
            deleteBtn.Should().NotBeNull();

            ((IJavaScriptExecutor)Driver).ExecuteScript(@"window.confirm = () => true;");
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", deleteBtn);
            deleteBtn!.Click();

            // Small wait for toast and refresh
            System.Threading.Thread.Sleep(800);
        }
    }
}
