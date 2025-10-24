using System;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using OpenQA.Selenium.Support.UI;
using System.Net.Http;
using System.Threading.Tasks;

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
                opts.AddArgument("--headless=new");
            opts.AddArgument("--window-size=1536,960");
            opts.AddArgument("--disable-gpu");
            opts.AddArgument("--no-sandbox");
            opts.AddArgument("--disable-dev-shm-usage");
            opts.AddArgument("--remote-allow-origins=*");
            opts.AddArgument("--lang=en-US");

            Driver = new ChromeDriver(opts);
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);

            // Ensure endpoints are reachable; if not, mark tests inconclusive instead of hard-failing
            try
            {
                EnsureEndpointsReachableOrSkipAsync().GetAwaiter().GetResult();
            }
            catch (InconclusiveException)
            {
                throw; // bubble up as inconclusive
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"E2E endpoints not reachable. Set E2E_WEB_URL/E2E_API_URL or start servers. Details: {ex.Message}");
            }
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

        private async Task EnsureEndpointsReachableOrSkipAsync()
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

            // Basic check for frontend
            var web = FrontendUrl?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(web))
                Assert.Inconclusive("Frontend URL is not configured.");

            try
            {
                using var resp = await http.GetAsync(web!);
                if (!resp.IsSuccessStatusCode && (int)resp.StatusCode < 400)
                {
                    // non-success but reachable is fine
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot reach frontend at {web}. {ex.Message}");
            }

            // Optional: API check (don’t fail if API is down, but prefer to warn)
            var api = ApiBaseUrl?.TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(api))
            {
                try { using var _ = await http.GetAsync(api!); }
                catch (Exception ex) { TestContext.Progress.WriteLine($"[WARN] API not reachable at {api}: {ex.Message}"); }
            }
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
