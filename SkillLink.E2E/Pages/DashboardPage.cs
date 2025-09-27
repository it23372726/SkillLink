using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace SkillLink.E2E.Pages
{
    public class DashboardPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public DashboardPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        }

        public bool IsLoaded()
        {
            // Adjust selectors to match your actual dashboard layout/text
            try
            {
                _wait.Until(drv =>
                    drv.Url.Contains("/dashboard") ||
                    drv.Url.Contains("/admin-dashboard") ||
                    drv.PageSource.Contains("Welcome") ||
                    drv.PageSource.Contains("Dashboard") ||
                    drv.PageSource.Contains("Home"));

                return true;
            }
            catch { return false; }
        }

        public void LogoutIfVisible()
        {
            try
            {
                var logoutBtn = _driver.FindElement(By.XPath("//button[contains(., 'Logout') or contains(., 'Sign out')]"));
                logoutBtn.Click();
            }
            catch { /* ignore if not present */ }
        }
    }
}
