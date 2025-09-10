using System.Net;
using System.Net.Mail;

namespace SkillLink.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var host = _config["Smtp:Host"];
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"];
            var from = _config["Smtp:From"];
            var useSsl = bool.Parse(_config["Smtp:UseSSL"] ?? "false");

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = useSsl,
                Credentials = new NetworkCredential(user, pass)
            };

            using var msg = new MailMessage(from!, to, subject, htmlBody)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(msg);
        }
    }
}
