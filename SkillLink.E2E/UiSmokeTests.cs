// using NUnit.Framework;
// using OpenQA.Selenium;
// using FluentAssertions;
// using OpenQA.Selenium.Support.UI;
// using System;
// using System.Linq;
// using System.Threading;

// namespace SkillLink.E2E
// {
//     public class UiSmokeTests : BaseUiTest
//     {
//         private bool TryLogin(string email, string password, out string error)
//         {
//             error = "";
//             try
//             {
//                 Driver.Navigate().GoToUrl($"{FrontendUrl}/login");
//                 Driver.FindElement(By.CssSelector("input[name='email']")).Clear();
//                 Driver.FindElement(By.CssSelector("input[name='email']")).SendKeys(email);
//                 Driver.FindElement(By.CssSelector("input[name='password']")).Clear();
//                 Driver.FindElement(By.CssSelector("input[name='password']")).SendKeys(password);
//                 Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

//                 new WebDriverWait(Driver, TimeSpan.FromSeconds(10))
//                     .Until(d => d.Url.Contains("/dashboard") || d.Url.Contains("/admin-dashboard"));

//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 var msg = Driver.FindElements(By.CssSelector(".text-red-600, .text-red-500"))
//                                 .FirstOrDefault()?.Text ?? ex.Message;
//                 error = msg;
//                 return false;
//             }
//         }

//         private void LoginPreferLearnerOrAdmin()
//         {
//             var learnerEmail = Environment.GetEnvironmentVariable("E2E_LEARNER_EMAIL") ?? "learner@skilllink.local";
//             var learnerPass  = Environment.GetEnvironmentVariable("E2E_LEARNER_PASSWORD") ?? "Learner@123";

//             if (TryLogin(learnerEmail, learnerPass, out var err)) return;

//             TestContext.Progress.WriteLine($"Learner login failed: {err}. Falling back to admin…");

//             var adminEmail = Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@skilllink.local";
//             var adminPass  = Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

//             if (!TryLogin(adminEmail, adminPass, out var adminErr))
//                 Assert.Fail($"Admin login also failed: {adminErr}");
//         }

//         private bool ThemeIsDark()
//         {
//             return (bool)((IJavaScriptExecutor)Driver).ExecuteScript(
//                 "return document.documentElement.classList.contains('dark');"
//             );
//         }

//         private IWebElement? FindIconButton(string iconClass)
//         {
//             return Driver.FindElements(By.XPath($"//button[.//i[contains(@class,'{iconClass}')]]")).FirstOrDefault();
//         }

//         [Test]
//         public void DarkMode_Toggle_Works_On_Dashboard_Or_AdminDashboard()
//         {
//             LoginPreferLearnerOrAdmin();

//             if (!Driver.Url.Contains("/dashboard") && !Driver.Url.Contains("/admin-dashboard"))
//                 Driver.Navigate().GoToUrl($"{FrontendUrl}/dashboard");

//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
//             // Use the explicit testid (much safer than icon search)
//             var toggle = wait.Until(d => d.FindElements(By.CssSelector("[data-testid='theme-toggle']")).FirstOrDefault());
//             toggle.Should().NotBeNull("Expected theme toggle button");
//             Console.WriteLine("Dashboard loaded; theme toggle found.");

//             bool wasDark = (bool)((IJavaScriptExecutor)Driver).ExecuteScript(
//                 "return document.documentElement.classList.contains('dark');"
//             );

//             SafeClick(toggle!);

//             // Wait until <html> toggles class
//             new WebDriverWait(Driver, TimeSpan.FromSeconds(30))
//                 .Until(d => (bool)((IJavaScriptExecutor)d).ExecuteScript(
//                     "return document.documentElement.classList.contains('dark');"
//                 ) != wasDark
//             ).Should().BeTrue("Dark mode class should toggle after pressing theme button");

//             // Optional toggle back
//             SafeClick(toggle!);
//             new WebDriverWait(Driver, TimeSpan.FromSeconds(30))
//                 .Until(d => (bool)((IJavaScriptExecutor)d).ExecuteScript(
//                     "return document.documentElement.classList.contains('dark');"
//                 ) == wasDark
//             );
//         }

//     }
// }