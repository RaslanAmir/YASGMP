using System;

namespace YasGMP.AppCore.Services.Ai
{
    /// <summary>
    /// Strongly-typed options for configuring the OpenAI/ChatGPT integration.
    /// Read from configuration (Ai:OpenAI:*) with environment variable overrides.
    /// </summary>
    public sealed class OpenAiOptions
    {
        public string? ApiKey { get; set; }
        public string? Organization { get; set; }
        public string? Project { get; set; }
        public string? BaseUrl { get; set; }
        public string ChatModel { get; set; } = "gpt-4o-mini";
        public string EmbeddingModel { get; set; } = "text-embedding-3-small";
        public string ModerationModel { get; set; } = "omni-moderation-latest";
        public float Temperature { get; set; } = 0.2f;
        public int MaxOutputTokens { get; set; } = 1024;

        /// <summary>Applies environment variable overrides for common deployment setups.</summary>
        public void ApplyEnvironmentOverrides()
        {
            ApiKey = GetEnvOr(ApiKey, "OPENAI_API_KEY", "YASGMP_OPENAI_API_KEY");
            Organization = GetEnvOr(Organization, "OPENAI_ORG", "YASGMP_OPENAI_ORG");
            Project = GetEnvOr(Project, "OPENAI_PROJECT", "YASGMP_OPENAI_PROJECT");
            BaseUrl = GetEnvOr(BaseUrl, "OPENAI_BASE_URL", "YASGMP_OPENAI_BASE_URL");
            ChatModel = GetEnvOr(ChatModel, "YASGMP_OPENAI_CHAT_MODEL") ?? ChatModel;
            EmbeddingModel = GetEnvOr(EmbeddingModel, "YASGMP_OPENAI_EMBED_MODEL") ?? EmbeddingModel;
            ModerationModel = GetEnvOr(ModerationModel, "YASGMP_OPENAI_MOD_MODEL") ?? ModerationModel;
            Temperature = TryParseFloat(Temperature, "YASGMP_OPENAI_TEMPERATURE");
            MaxOutputTokens = TryParseInt(MaxOutputTokens, "YASGMP_OPENAI_MAX_TOKENS");
        }

        private static string? GetEnvOr(string? current, params string[] keys)
        {
            foreach (var k in keys)
            {
                var v = Environment.GetEnvironmentVariable(k);
                if (!string.IsNullOrWhiteSpace(v)) return v!;
            }
            return current;
        }

        private static float TryParseFloat(float current, string key)
        {
            var v = Environment.GetEnvironmentVariable(key);
            return (v != null && float.TryParse(v, out var f)) ? f : current;
        }

        private static int TryParseInt(int current, string key)
        {
            var v = Environment.GetEnvironmentVariable(key);
            return (v != null && int.TryParse(v, out var n)) ? n : current;
        }
    }
}
