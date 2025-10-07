using System;
using System.Collections.Generic;
using System.Linq;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the redactor value.
    /// </summary>
    public static class Redactor
    {
        private static readonly string[] SecretKeys = new[]
        {
            "password","pwd","pass","token","apikey","api_key","secret","connectionstring","user_id","username","email"
        };
        /// <summary>
        /// Executes the object operation.
        /// </summary>

        public static IDictionary<string, object?>? Redact(IDictionary<string, object?>? data, bool enabled)
        {
            if (!enabled || data == null) return data;
            var copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in data)
            {
                if (IsSecretKey(kv.Key))
                {
                    copy[kv.Key] = kv.Value is null ? null : "****";
                }
                else if (kv.Value is string s && LooksSensitiveString(s))
                {
                    copy[kv.Key] = "****";
                }
                else
                {
                    copy[kv.Key] = kv.Value;
                }
            }
            return copy;
        }
        /// <summary>
        /// Executes the redact conn string operation.
        /// </summary>

        public static string RedactConnString(string conn)
        {
            if (string.IsNullOrWhiteSpace(conn)) return conn;
            var parts = conn.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var kv = parts[i].Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length == 2 && IsSecretKey(kv[0]))
                {
                    parts[i] = $"{kv[0]}=****";
                }
            }
            return string.Join(';', parts);
        }

        private static bool IsSecretKey(string key)
            => SecretKeys.Any(k => key.Equals(k, StringComparison.OrdinalIgnoreCase));

        private static bool LooksSensitiveString(string s)
        {
            if (s.Length >= 24 && s.Any(char.IsDigit) && s.Any(char.IsLetter)) return true; // likely token
            if (s.Contains("BEGIN PRIVATE KEY", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}

