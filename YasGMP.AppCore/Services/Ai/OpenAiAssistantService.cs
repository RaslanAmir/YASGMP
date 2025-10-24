using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.AppCore.Services.Ai
{
    /// <summary>
    /// Default assistant implementation backed by the OpenAI SDK where available,
    /// with an HttpClient fallback to preserve functionality when the SDK package
    /// isn't restored yet. The public API is intentionally small and robust.
    /// </summary>
    public sealed class OpenAiAssistantService : IAiAssistantService
    {
        private readonly OpenAiOptions _options;
        private readonly HttpClient _http;

        public OpenAiAssistantService(OpenAiOptions options, HttpClient? httpClient = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.ApplyEnvironmentOverrides();
            _http = httpClient ?? new HttpClient();
        }

        public async Task<string> ChatAsync(
            string userMessage,
            string? systemPrompt = null,
            IReadOnlyList<(string role, string content)>? additionalMessages = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return "AI is not configured (missing OPENAI_API_KEY).";
            }

            // HTTP fallback using Chat Completions
            var url = CombineBase(_options.BaseUrl) + "/v1/chat/completions";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            if (!string.IsNullOrWhiteSpace(_options.Organization))
            {
                req.Headers.TryAddWithoutValidation("OpenAI-Organization", _options.Organization);
            }
            if (!string.IsNullOrWhiteSpace(_options.Project))
            {
                req.Headers.TryAddWithoutValidation("OpenAI-Project", _options.Project);
            }

            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }
            if (additionalMessages != null)
            {
                foreach (var m in additionalMessages)
                {
                    messages.Add(new { role = m.role, content = m.content });
                }
            }
            messages.Add(new { role = "user", content = userMessage });

            var body = new
            {
                model = _options.ChatModel,
                temperature = _options.Temperature,
                max_tokens = _options.MaxOutputTokens,
                messages
            };

            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return $"AI error: {resp.StatusCode} — {json}";
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content").GetString();
                return content ?? string.Empty;
            }
            catch
            {
                return json; // raw fallback for inspection
            }
        }

        public async Task<string> ModerateAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return "AI moderation unavailable (missing OPENAI_API_KEY).";
            }

            var url = CombineBase(_options.BaseUrl) + "/v1/moderations";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            if (!string.IsNullOrWhiteSpace(_options.Organization))
            {
                req.Headers.TryAddWithoutValidation("OpenAI-Organization", _options.Organization);
            }
            if (!string.IsNullOrWhiteSpace(_options.Project))
            {
                req.Headers.TryAddWithoutValidation("OpenAI-Project", _options.Project);
            }

            var body = new { model = _options.ModerationModel, input = text };
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return $"Moderation error: {resp.StatusCode} — {json}";
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var result = doc.RootElement.GetProperty("results")[0];
                var flagged = result.GetProperty("flagged").GetBoolean();
                return flagged ? "flagged" : "clean";
            }
            catch
            {
                return json;
            }
        }

        public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return Array.Empty<float>();
            }

            var url = CombineBase(_options.BaseUrl) + "/v1/embeddings";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            if (!string.IsNullOrWhiteSpace(_options.Organization))
            {
                req.Headers.TryAddWithoutValidation("OpenAI-Organization", _options.Organization);
            }
            if (!string.IsNullOrWhiteSpace(_options.Project))
            {
                req.Headers.TryAddWithoutValidation("OpenAI-Project", _options.Project);
            }

            var body = new { model = _options.EmbeddingModel, input = text };
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return Array.Empty<float>();
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var arr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
                var vec = new List<float>(arr.GetArrayLength());
                foreach (var v in arr.EnumerateArray())
                {
                    vec.Add(v.GetSingle());
                }
                return vec;
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        private static string CombineBase(string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) return "https://api.openai.com";
            return baseUrl.TrimEnd('/');
        }
    }
}

