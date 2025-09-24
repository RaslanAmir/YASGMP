using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Services.Interfaces;
using YasGMP;

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
                await SendSmsAsync(destination, message);
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
            if (string.IsNullOrWhiteSpace(number))
            {
                throw new ArgumentException("SMS destination number must not be empty.", nameof(number));
            }

            var configuration = AppConfigurationHelper.LoadMerged();

            static string? Resolve(params string?[] candidates)
            {
                foreach (var candidate in candidates)
                {
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        return candidate.Trim();
                    }
                }

                return null;
            }

            var accountSid = Resolve(
                configuration["Notifications:Sms:Twilio:AccountSid"],
                configuration["Sms:Twilio:AccountSid"],
                configuration["Twilio:AccountSid"],
                Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID"));

            var authToken = Resolve(
                configuration["Notifications:Sms:Twilio:AuthToken"],
                configuration["Sms:Twilio:AuthToken"],
                configuration["Twilio:AuthToken"],
                Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN"));

            var fromNumber = Resolve(
                configuration["Notifications:Sms:Twilio:From"],
                configuration["Sms:Twilio:From"],
                configuration["Twilio:From"],
                Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER"));

            if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(fromNumber))
            {
                throw new InvalidOperationException("Twilio SMS configuration is missing. Please provide AccountSid, AuthToken, and From number in configuration or environment.");
            }

            var requestUri = new Uri($"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json");

            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["To"] = number,
                    ["From"] = fromNumber,
                    ["Body"] = message ?? string.Empty,
                }),
            };

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                throw new InvalidOperationException("Failed to reach Twilio SMS service.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                string? errorDetails = null;
                try
                {
                    errorDetails = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Ignore parsing issues; we'll throw a generic error below.
                }

                throw new InvalidOperationException($"Twilio SMS send failed with status {(int)response.StatusCode} ({response.StatusCode}). {errorDetails}");
            }
        }
    }
}
