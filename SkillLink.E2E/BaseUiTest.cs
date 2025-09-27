using System;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using OpenQA.Selenium.Support.UI;

namespace SkillLink.E2E
{
    public abstract class BaseUiTest : IDisposable
    {
        protected IWebDriver Driver = null!;
        protected string ApiBaseUrl = Environment.GetEnvironmentVariable("E2E_API_URL") ?? "http://localhost:5159";
        protected string FrontendUrl = Environment.GetEnvironmentVariable("E2E_WEB_URL") ?? "http://localhost:3000";

        private bool _disposed;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            new DriverManager().SetUpDriver(new ChromeConfig());
        }

        [SetUp]
        public void Setup()
        {
            var opts = new ChromeOptions();
            var headless = (Environment.GetEnvironmentVariable("HEADLESS") ?? "1") != "0";

            if (headless) 
                // opts.AddArgument("--headless=new");
            opts.AddArgument("--window-size=1536,960");
            opts.AddArgument("--disable-gpu");
            opts.AddArgument("--no-sandbox");
            opts.AddArgument("--disable-dev-shm-usage");
            opts.AddArgument("--remote-allow-origins=*");
            opts.AddArgument("--lang=en-US");

            Driver = new ChromeDriver(opts);
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        }

        [TearDown]
        public void Teardown()
        {
            try { Driver?.Quit(); } catch { /* ignore */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            try { Driver?.Dispose(); } catch { /* ignore */ }
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        // ---------- Helpers to avoid click intercepted ----------
        protected void JsScrollCenter(IWebElement el)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].scrollIntoView({behavior:'instant',block:'center',inline:'center'});", el);
        }

        protected void JsClick(IWebElement el)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", el);
        }

        protected void SafeClick(IWebElement el)
        {
            try
            {
                ((IJavaScriptExecutor)Driver)
                    .ExecuteScript("arguments[0].scrollIntoView({block:'center', inline:'center'});", el);
                el.Click();
            }
            catch (WebDriverException)
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", el);
            }
        }

    }
}
