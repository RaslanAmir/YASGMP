using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.AppCore.Services.Ai
{
    /// <summary>
    /// High-level AI assistant abstraction used by both WPF and MAUI hosts.
    /// </summary>
    public interface IAiAssistantService
    {
        /// <summary>Simple chat completion with optional system prompt and context.</summary>
        Task<string> ChatAsync(string userMessage,
                               string? systemPrompt = null,
                               IReadOnlyList<(string role, string content)>? additionalMessages = null,
                               CancellationToken cancellationToken = default);

        /// <summary>Moderates content and returns a simple verdict string.</summary>
        Task<string> ModerateAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>Computes embedding vector for the given text.</summary>
        Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default);
    }
}

