using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using SkillLink.API.Services;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class EmailServiceMoreUnitTests
    {
        private IConfiguration BuildConfig(IDictionary<string, string?> overrides)
        {
            var defaults = new Dictionary<string, string?>
            {
                { "Smtp:Host", "localhost" },
                { "Smtp:Port", "2525" },
                { "Smtp:User", "" },
                { "Smtp:Pass", "" },
                { "Smtp:From", "noreply@skilllink.com" },
                { "Smtp:UseSSL", "false" }
            };

            foreach (var kv in overrides)
            {
                defaults[kv.Key] = kv.Value;
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(defaults)
                .Build();
        }

        [Test]
        public void SendAsync_ShouldThrow_FormatException_WhenPortIsInvalid()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Smtp:Port", "not-a-number" }
            });

            var svc = new EmailService(config);
            Assert.ThrowsAsync<FormatException>(async () =>
                await svc.SendAsync("user@example.com", "Subject", "<b>Body</b>")
            );
        }

        [Test]
        public void SendAsync_ShouldThrow_ArgumentNullException_WhenFromMissing()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Smtp:From", null }
            });

            var svc = new EmailService(config);
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await svc.SendAsync("user@example.com", "Subject", "<b>Body</b>")
            );
        }

        [Test]
        public void SendAsync_ShouldThrow_FormatException_WhenToEmailInvalid()
        {
            var config = BuildConfig(new Dictionary<string, string?>());
            var svc = new EmailService(config);

            Assert.ThrowsAsync<FormatException>(async () =>
                await svc.SendAsync("not-an-email", "Subject", "<b>Body</b>")
            );
        }

        [Test]
        public void SendAsync_ShouldThrow_SmtpException_WhenHostMissing()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Smtp:Host", null }
            });

            var svc = new EmailService(config);
            Assert.ThrowsAsync<System.Net.Mail.SmtpException>(async () =>
                await svc.SendAsync("user@example.com", "Subject", "<b>Body</b>")
            );
        }
    }
}
