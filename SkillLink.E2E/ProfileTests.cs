// using NUnit.Framework;
// using OpenQA.Selenium;
// using FluentAssertions;
// using OpenQA.Selenium.Support.UI;
// using System;
// using System.IO;
// using System.Linq;
// using System.Threading;

// namespace SkillLink.E2E
// {
//     public class ProfileTests : BaseUiTest
//     {
//         private readonly string _email = Environment.GetEnvironmentVariable("E2E_USER_EMAIL") ?? "learner@skilllink.local";
//         private readonly string _pass  = Environment.GetEnvironmentVariable("E2E_USER_PASS")  ?? "Learner@123";

//         private void LoginLearner()
//         {
//             Driver.Navigate().GoToUrl($"{FrontendUrl}/login");
//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
//             wait.Until(d => d.FindElement(By.CssSelector("input[name='email']")));
//             Driver.FindElement(By.CssSelector("input[name='email']")).SendKeys(_email);
//             Driver.FindElement(By.CssSelector("input[name='password']")).SendKeys(_pass);
//             Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

//             wait.Until(d => d.Url.Contains("/dashboard") || d.Url.Contains("/admin-dashboard"));
//             if (Driver.Url.Contains("/admin-dashboard"))
//             {
//                 Driver.Navigate().GoToUrl($"{FrontendUrl}/dashboard");
//                 wait.Until(d => d.Url.Contains("/dashboard"));
//             }
//         }
//         private void EnterEditModeStrict()
//         {
//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(25));

//             var editBtn = wait.Until(d => d.FindElement(By.CssSelector("[data-testid='profile-edit-btn']")));
//             SafeClick(editBtn);
//             Console.WriteLine("editbtnnnnn stric>>>>>>>",editBtn);

//             // Wait for the specific edit form container
//             wait.Until(d => d.FindElements(By.CssSelector("[data-testid='profile-edit-form']")).Any())
//                 .Should().BeTrue("Profile edit form should be visible");
//         }

//         private void EnterEditMode()
//         {
//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));  // Increased to 45s for slow render

//             var editBtn =
//                 Driver.FindElements(By.CssSelector("[data-testid='edit-profile']")).FirstOrDefault() ??
//                 Driver.FindElements(By.CssSelector("#edit-profile-btn")).FirstOrDefault() ??
//                 Driver.FindElements(By.XPath("//button[normalize-space()='Edit Profile']")).FirstOrDefault() ??
//                 Driver.FindElements(By.XPath("//button[normalize-space()='Edit']")).FirstOrDefault();

//             editBtn.Should().NotBeNull("Expected an Edit Profile button");
//             SafeClick(editBtn!);

//             Thread.Sleep(2000);  // Increased sleep for animation/ re-render

//             wait.Until(d => 
//             {
//                 var formEl = d.FindElements(By.CssSelector("[data-testid='profile-edit-form'], input[name='fullName']")).FirstOrDefault();
//                 return formEl != null && formEl.Displayed && formEl.Enabled;
//             });
//             Console.WriteLine("Edit form is visible and enabled.");
//         }

//         [Test]
//         public void Learner_Can_Update_Profile_Name()
//         {
//             LoginLearner();
//             Driver.Navigate().GoToUrl($"{FrontendUrl}/profile");

//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));  // Increased
//             wait.Until(d => d.FindElements(By.CssSelector("[data-testid='edit-profile']")));
//             Console.WriteLine("Profile page loaded; edit button found.");

//             EnterEditModeStrict();

//             var nameBox = wait.Until(d =>
//                 d.FindElements(By.CssSelector("input[name='fullName']")).FirstOrDefault()
//                 ?? d.FindElements(By.CssSelector("#fullName")).FirstOrDefault()
//             );
//             nameBox.Should().NotBeNull();
//             nameBox!.Clear();
//             nameBox.SendKeys("Updated Name Test");

//             var saveBtn = Driver.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault()
//                        ?? Driver.FindElements(By.XPath("//button[contains(.,'Save')]")).FirstOrDefault();
//             saveBtn.Should().NotBeNull();
//             SafeClick(saveBtn!);

//             wait.Until(d =>
//                 d.PageSource.IndexOf("updated", StringComparison.OrdinalIgnoreCase) >= 0 ||
//                 d.FindElements(By.CssSelector(".ring-emerald-300, .text-emerald-500, .text-green-600")).Count > 0
//             );
//         }

//         [Test]
//         public void Learner_Invalid_Photo_Shows_Error()
//         {
//             LoginLearner();
//             Driver.Navigate().GoToUrl($"{FrontendUrl}/profile");
//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));

//             // Ensure we’re on the profile page
//             wait.Until(d => d.FindElements(By.CssSelector("[data-testid='profile-edit-btn']")).Any());
//             Console.WriteLine("Profile page loaded; edit button found.");

//             EnterEditModeStrict();

//             // Now upload invalid file
//             var fileInput = wait.Until(d => d.FindElement(By.CssSelector("#profilePic")));
//             ((IJavaScriptExecutor)Driver).ExecuteScript(
//                 "arguments[0].classList.remove('hidden'); arguments[0].style.display='block';", fileInput);

//             var tmp = Path.Combine(Path.GetTempPath(), "not_img.txt");
//             File.WriteAllText(tmp, "this is not an image");
//             fileInput.SendKeys(tmp);

//             // FE shows: "Please upload an image (JPG/PNG)."
//             wait.Until(d =>
//                 d.PageSource.IndexOf("please upload an image", StringComparison.OrdinalIgnoreCase) >= 0
//             ).Should().BeTrue("Validation error should appear for invalid image type");
//         }
//     }
// }