using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace SkillLink.E2E.Pages
{
    public class LoginPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        }

        public void GoTo(string baseUrl)
        {
            _driver.Navigate().GoToUrl($"{baseUrl}/login");
            _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("input[type='email']")));
        }

        public void Login(string email, string password)
        {
            var emailInput = _driver.FindElement(By.CssSelector("input[type='email']"));
            var passInput  = _driver.FindElement(By.CssSelector("input[type='password']"));
            var submitBtn  = _driver.FindElement(By.CssSelector("button[type='submit']"));

            emailInput.Clear();
            emailInput.SendKeys(email);

            passInput.Clear();
            passInput.SendKeys(password);

            submitBtn.Click();
        }
    }
}
