// using NUnit.Framework;
// using OpenQA.Selenium;
// using FluentAssertions;
// using OpenQA.Selenium.Support.UI;
// using System;
// using System.Linq;
// using System.Threading;

// namespace SkillLink.E2E
// {
//     public class RequestTests : BaseUiTest
//     {
//         private readonly string _email = Environment.GetEnvironmentVariable("E2E_USER_EMAIL") ?? "learner@skilllink.local";
//         private readonly string _pass  = Environment.GetEnvironmentVariable("E2E_USER_PASS")  ?? "Learner@123";

//         private void LoginLearner()
//         {
//             Driver.Navigate().GoToUrl($"{FrontendUrl}/login");
//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
//             wait.Until(d => d.FindElement(By.CssSelector("input[name='email']")));
//             Driver.FindElement(By.CssSelector("input[name='email']")).Clear();
//             Driver.FindElement(By.CssSelector("input[name='email']")).SendKeys(_email);
//             Driver.FindElement(By.CssSelector("input[name='password']")).Clear();
//             Driver.FindElement(By.CssSelector("input[name='password']")).SendKeys(_pass);
//             Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

//             wait.Until(d => d.Url.Contains("/dashboard") || d.Url.Contains("/admin-dashboard"));
//             if (Driver.Url.Contains("/admin-dashboard"))
//             {
//                 Driver.Navigate().GoToUrl($"{FrontendUrl}/dashboard");
//                 wait.Until(d => d.Url.Contains("/dashboard"));
//             }
//         }

//         [Test]
//         public void Learner_Can_Create_Request_And_See_It()
//         {
//             LoginLearner();
//             Driver.Navigate().GoToUrl($"{FrontendUrl}/request");
//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(25));
//             // Prefer the stable testid if present; fallback to id
//             var createBtn = wait.Until(d =>
//                 d.FindElements(By.CssSelector("[data-testid='open-create-request']")).FirstOrDefault()
//                 ?? d.FindElements(By.CssSelector("#open-create-request-id")).FirstOrDefault()
//             );
//             Console.WriteLine("createbutton>>>>>",createBtn );
//             createBtn.Should().NotBeNull("Expected Create Request button");
//             SafeClick(createBtn!);

//             // Modal should be visible
//             wait.Until(d => d.FindElements(By.CssSelector("[data-testid='create-request-modal']")).Any())
//                 .Should().BeTrue("Create request modal should appear");

//             // Fill fields
//             var skill = wait.Until(d => d.FindElement(By.CssSelector("input[name='skillName']")));
//             skill.Clear();
//             skill.SendKeys("Selenium Testing");

//             var topic = Driver.FindElement(By.CssSelector("input[name='topic']"));
//             topic.Clear();
//             topic.SendKeys("Basic automation test");

//             var desc = Driver.FindElement(By.CssSelector("textarea[name='description']"));
//             desc.Clear();
//             desc.SendKeys("E2E request created from test.");

//             // Submit
//             var submit = wait.Until(d => d.FindElement(By.CssSelector("[data-testid='create-request-submit']")));
//             SafeClick(submit);

//             // Wait for either success toast/text or the card to appear
//             var successWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
//             successWait.Until(d =>
//                 d.PageSource.IndexOf("request created successfully", StringComparison.OrdinalIgnoreCase) >= 0 ||
//                 d.FindElements(By.XPath(
//                     "//*[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'selenium testing')]"
//                 )).Any()
//             ).Should().BeTrue("Should see success or the new request card");

//             // Finally assert the card exists
//             var cardFound = Driver.FindElements(By.XPath(
//                 "//*[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'selenium testing')]"
//             )).Any();
//             cardFound.Should().BeTrue("Newly created request should be visible");
//         }

//     }
// }