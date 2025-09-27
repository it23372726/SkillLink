// using System.Text.Json;
// using System.Text.Json.Serialization;
// using NUnit.Framework;
// using OpenQA.Selenium;
// using OpenQA.Selenium.Support.UI;
// using SeleniumExtras.WaitHelpers;
// using FluentAssertions;
// using System.Net.Http;
// using System.Net.Http.Json;
// using System.Threading.Tasks;
// using System;
// using System.Linq;
// using System.IO;

// namespace SkillLink.E2E
// {
//     public class SwaggerAuthTests : BaseUiTest
//     {
//         private sealed class TokenResponse
//         {
//             [JsonPropertyName("token")] public string Token { get; set; } = "";
//         }

//         private async Task<string> GetJwtAsync()
//         {
//             using var http = new HttpClient();
//             var payload = new { email = "sliitskilllink@gmail.com", password = "Skilllink@2003" };
//             var res = await http.PostAsJsonAsync($"{ApiBaseUrl}/api/auth/login", payload);
//             res.EnsureSuccessStatusCode();

//             var json = await res.Content.ReadFromJsonAsync<TokenResponse>();
//             if (json == null || string.IsNullOrWhiteSpace(json.Token))
//                 throw new InvalidOperationException("Login succeeded but token was missing");
//             return json.Token;
//         }

//         private async Task<string> GetExactMePathFromSwaggerAsync()
//         {
//             using var http = new HttpClient();
//             var json = await http.GetStringAsync($"{ApiBaseUrl}/swagger/v1/swagger.json");
//             using var doc = JsonDocument.Parse(json);
//             var root = doc.RootElement;

//             if (!root.TryGetProperty("paths", out var paths))
//                 throw new InvalidOperationException("Swagger JSON has no 'paths'");

//             string? exact = null;
//             foreach (var p in paths.EnumerateObject())
//             {
//                 var key = p.Name;
//                 if (key.Equals("/api/auth/me", StringComparison.OrdinalIgnoreCase) ||
//                     key.Equals("/api/Auth/me", StringComparison.OrdinalIgnoreCase))
//                 {
//                     exact = key; break;
//                 }
//             }

//             if (exact == null)
//             {
//                 foreach (var p in paths.EnumerateObject())
//                 {
//                     var key = p.Name;
//                     if (key.EndsWith("/auth/me", StringComparison.OrdinalIgnoreCase))
//                     {
//                         exact = key; break;
//                     }
//                 }
//             }

//             if (exact == null)
//             {
//                 var all = string.Join("\n", paths.EnumerateObject().Select(o => o.Name));
//                 throw new InvalidOperationException($"Could not find /api/auth/me in swagger paths.\nAvailable:\n{all}");
//             }

//             return exact;
//         }

//         private void WaitForPageReady(IWebDriver drv, int seconds = 20)
//         {
//             var wait = new WebDriverWait(drv, TimeSpan.FromSeconds(seconds));
//             wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");
//         }

//         [Test]
//         public async Task Authorize_Should_Accept_JWT_And_Call_Protected_Endpoint()
//         {
//             var token = await GetJwtAsync();
//             TestContext.Progress.WriteLine($"[DEBUG] token(first 30): {token[..Math.Min(30, token.Length)]}...");

//             var mePath = await GetExactMePathFromSwaggerAsync();
//             TestContext.Progress.WriteLine($"[DEBUG] swagger path: {mePath}");

//             Driver.Navigate().GoToUrl($"{ApiBaseUrl}/swagger");
//             WaitForPageReady(Driver);

//             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(25));

//             // 1) Make sure Swagger root & toolbar exists
//             wait.Until(drv => drv.FindElements(By.CssSelector("#swagger-ui")).Any());
//             wait.Until(drv => drv.FindElements(By.CssSelector("button.authorization__btn, button[aria-label='Authorize']")).Any());

//             // 2) Click Authorize
//             var authBtn = Driver.FindElements(By.CssSelector("button.authorization__btn, button[aria-label='Authorize']")).First();
//             ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", authBtn); // JS click for headless stability

//             // 3) Input field (input or textarea)
//             var authField = wait.Until(drv =>
//             {
//                 var el = drv.FindElements(By.CssSelector(".auth-container input, .auth-container textarea")).FirstOrDefault();
//                 return el;
//             });
//             authField.Should().NotBeNull("Authorize field should be present");

//             // 🔐 Try raw token first (Swagger commonly prepends Bearer)
//             authField!.Clear();
//             authField.SendKeys(token);

//             // Click "Authorize", then "Close/Done"
//             var authorizeBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
//                 By.CssSelector(".auth-btn-wrapper .btn.modal-btn.auth.authorize, .auth-container .authorize")
//             ));
//             ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", authorizeBtn);

//             var doneBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
//                 By.CssSelector(".btn.modal-btn.auth.btn-done, .auth-container .modal-btn.btn-done")
//             ));
//             ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", doneBtn);

//             // 4) Ensure operations are visible
//             wait.Until(drv => drv.FindElements(By.CssSelector(".opblock")).Count > 0);

//             // Expand tags if needed
//             foreach (var tag in Driver.FindElements(By.CssSelector(".opblock-tag")))
//             {
//                 var cls = tag.GetAttribute("class") ?? "";
//                 if (!cls.Contains("is-open", StringComparison.OrdinalIgnoreCase))
//                 {
//                     try { tag.Click(); } catch { /* ignore */ }
//                 }
//             }
//             wait.Until(drv => drv.FindElements(By.CssSelector(".opblock")).Count > 0);

//             // 5) Locate the /api/.../me op
//             IWebElement? meOp = null;

//             // Strategy A: exact text of path
//             try
//             {
//                 wait.Until(drv => drv.FindElements(By.CssSelector(".opblock .opblock-summary .opblock-summary-path")).Count > 0);
//                 var summaries = Driver.FindElements(By.CssSelector(".opblock .opblock-summary .opblock-summary-path"));
//                 var match = summaries.FirstOrDefault(s =>
//                     string.Equals((s.Text ?? "").Trim(), mePath, StringComparison.OrdinalIgnoreCase));
//                 if (match != null)
//                     meOp = match.FindElement(By.XPath("./ancestor::div[contains(@class,'opblock')]"));
//             }
//             catch { /* fallback below */ }

//             // Strategy B: contains-based search
//             if (meOp == null)
//             {
//                 var safe = mePath.ToLowerInvariant();
//                 var candidates = Driver.FindElements(By.XPath(
//                     "//div[contains(@class,'opblock')]" +
//                     "[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '" + safe + "')]" +
//                     "[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'get')]"
//                 ));
//                 if (candidates.Count > 0) meOp = candidates[0];
//             }

//             // Dump debug if missing
//             if (meOp == null)
//             {
//                 try
//                 {
//                     var ssPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "swagger_not_found.png");
//                     var htmlPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "swagger_dump.html");
//                     var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
//                     File.WriteAllBytes(ssPath, screenshot.AsByteArray);
//                     File.WriteAllText(htmlPath, Driver.PageSource);

//                     var visible = Driver.FindElements(By.CssSelector(".opblock .opblock-summary .opblock-summary-path"))
//                         .Select(e => e.Text?.Trim() ?? "")
//                         .Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();

//                     TestContext.Progress.WriteLine("[DEBUG] Visible Swagger paths:");
//                     foreach (var p in visible) TestContext.Progress.WriteLine(" - " + p);
//                     TestContext.Progress.WriteLine($"[DEBUG] Saved screenshot: {ssPath}");
//                     TestContext.Progress.WriteLine($"[DEBUG] Saved HTML dump: {htmlPath}");
//                 }
//                 catch { /* ignore */ }
//             }

//             meOp.Should().NotBeNull($"Swagger UI should display GET {mePath}");

//             // 6) Try it out + Execute
//             ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", meOp);

//             meOp!.FindElement(By.CssSelector(".opblock-summary")).Click();

//             var tryBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
//                 meOp.FindElement(By.CssSelector(".try-out__btn"))
//             ));
//             ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", tryBtn);

//             // NOTE: Swagger versions differ on execute button class
//             var execBtn = wait.Until(drv =>
//             {
//                 var a = meOp.FindElements(By.CssSelector(".opblock-control__btn.execute")).FirstOrDefault();
//                 if (a != null && a.Displayed && a.Enabled) return a;
//                 var b = meOp.FindElements(By.CssSelector(".execute-opblock")).FirstOrDefault();
//                 return (b != null && b.Displayed && b.Enabled) ? b : null;
//             });
//             ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", execBtn);

//             // Wait for numeric status
//             var statusEl = wait.Until(drv =>
//             {
//                 var els = meOp.FindElements(By.CssSelector(
//                     ".responses-wrapper .response .response-status, " +
//                     ".responses-wrapper .response-col_status, " +
//                     ".responses-wrapper td.response-col_status"
//                 ));
//                 var valid = els.FirstOrDefault(e => e.Text.Any(char.IsDigit));
//                 return valid ?? els.FirstOrDefault();
//             });

//             TestContext.Progress.WriteLine($"[DEBUG] Raw status text: {statusEl.Text}");

//             // If 401/400 happened, re-authorize with "Bearer " prefix and retry once
//             if (!statusEl.Text.Contains("200"))
//             {
//                 // Re-authorize with Bearer token explicitly
//                 var root = Driver.FindElement(By.Id("swagger-ui"));
//                 var authTrigger = root.FindElements(By.CssSelector("button.authorization__btn, button[aria-label='Authorize']")).First();
//                 ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", authTrigger);

//                 var field = wait.Until(drv =>
//                     drv.FindElements(By.CssSelector(".auth-container input, .auth-container textarea")).FirstOrDefault());
//                 field!.Clear();
//                 field.SendKeys($"Bearer {token}");

//                 var authBtn2 = wait.Until(ExpectedConditions.ElementToBeClickable(
//                     By.CssSelector(".auth-btn-wrapper .btn.modal-btn.auth.authorize, .auth-container .authorize")
//                 ));
//                 ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", authBtn2);

//                 var doneBtn2 = wait.Until(ExpectedConditions.ElementToBeClickable(
//                     By.CssSelector(".btn.modal-btn.auth.btn-done, .auth-container .modal-btn.btn-done")
//                 ));
//                 ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", doneBtn2);

//                 // Re-run execute
//                 meOp.FindElement(By.CssSelector(".opblock-summary")).Click();
//                 var tryBtn2 = wait.Until(ExpectedConditions.ElementToBeClickable(
//                     meOp.FindElement(By.CssSelector(".try-out__btn"))
//                 ));
//                 ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", tryBtn2);

//                 var execBtn2 = wait.Until(drv =>
//                 {
//                     var a = meOp.FindElements(By.CssSelector(".opblock-control__btn.execute")).FirstOrDefault();
//                     if (a != null && a.Displayed && a.Enabled) return a;
//                     var b = meOp.FindElements(By.CssSelector(".execute-opblock")).FirstOrDefault();
//                     return (b != null && b.Displayed && b.Enabled) ? b : null;
//                 });
//                 ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", execBtn2);

//                 statusEl = wait.Until(drv =>
//                 {
//                     var els = meOp.FindElements(By.CssSelector(
//                         ".responses-wrapper .response .response-status, " +
//                         ".responses-wrapper .response-col_status, " +
//                         ".responses-wrapper td.response-col_status"
//                     ));
//                     var valid = els.FirstOrDefault(e => e.Text.Any(char.IsDigit));
//                     return valid ?? els.FirstOrDefault();
//                 });

//                 TestContext.Progress.WriteLine($"[DEBUG] Retried status text: {statusEl.Text}");
//             }

//             statusEl.Text.Should().Contain("200", "Expected success response from /api/Auth/me");
//         }
//     }
// }
