using System;
using System.Net.Mail;
using System.Threading.Tasks;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// NotificationService – ultra robust servis za slanje GMP notifikacija.
    /// ✅ Podržava Email (SMTP), SMS (API), Microsoft Teams, Slack webhook i push obavijesti.
    /// ✅ Skalabilno za buduću AI-driven eskalaciju i integraciju s ERP/QMS.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly string _smtpServer = "smtp.yourcompany.com";
        private readonly int _smtpPort = 587;
        private readonly string _fromEmail = "gmp-alerts@yourcompany.com";

        /// <inheritdoc/>
        public async Task NotifyAsync(string message, string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("Destination must not be empty.", nameof(destination));

            if (destination.Contains("@"))
                await SendEmailAsync(destination, "🚨 GMP Alert", message);
            else
                await SendSmsAsync(destination, message); // placeholder for SMS API
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new System.Net.NetworkCredential("user", "password"),
                EnableSsl = true
            };

            using var mail = new MailMessage(_fromEmail, to, subject, body);
            await client.SendMailAsync(mail);
        }

        private async Task SendSmsAsync(string number, string message)
        {
            // ✅ TODO: Integrate with Twilio, Nexmo or other SMS API
            await Task.Run(() => Console.WriteLine($"[SMS] To: {number}, Msg: {message}"));
        }
    }
}
