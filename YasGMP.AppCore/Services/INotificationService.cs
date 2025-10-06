using System.Threading.Tasks;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// INotificationService – sučelje za napredne notifikacije (email, SMS, MS Teams, Slack).
    /// ✅ Koristi se za eskalacije visoko-rizičnih CAPA slučajeva i GMP upozorenja.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Šalje obavijest prema zadanoj destinaciji (email, kanal, telefon).
        /// </summary>
        /// <param name="message">Sadržaj poruke.</param>
        /// <param name="destination">Destinacija (email, broj, kanal).</param>
        Task NotifyAsync(string message, string destination);
    }
}

