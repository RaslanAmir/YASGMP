using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.DTO
{
    /// <summary>
    /// <b>ApiAuditEntryDto</b> – Strongly typed projection of <c>api_audit_log</c> rows enriched
    /// with API key metadata and user information so the WPF shell can surface forensic details
    /// without dynamic dictionaries.
    /// <para>
    /// Includes masked API key display helpers, owner/user context, timestamps, IP address, and
    /// request/response payloads for downstream filtering and inspector views.
    /// </para>
    /// </summary>
    public class ApiAuditEntryDto
    {
        /// <summary>Primary key of the <c>api_audit_log</c> record.</summary>
        public int Id { get; set; }

        /// <summary>Foreign key referencing <c>api_keys.id</c>.</summary>
        public int? ApiKeyId { get; set; }

        /// <summary>Raw API key value (never display directly – use <see cref="MaskedApiKey"/>).</summary>
        [Display(Name = "API Key (raw)")]
        public string? ApiKeyValue { get; set; }

        /// <summary>Description or label attached to the API key.</summary>
        [Display(Name = "Key Description")]
        public string? ApiKeyDescription { get; set; }

        /// <summary>Whether the linked API key is active.</summary>
        [Display(Name = "Key Active")]
        public bool? ApiKeyIsActive { get; set; }

        /// <summary>UTC timestamp the API key was created.</summary>
        [Display(Name = "Key Created")]
        public DateTime? ApiKeyCreatedAt { get; set; }

        /// <summary>UTC timestamp the API key record was last updated.</summary>
        [Display(Name = "Key Updated")]
        public DateTime? ApiKeyUpdatedAt { get; set; }

        /// <summary>UTC timestamp the API key was last used.</summary>
        [Display(Name = "Key Last Used")]
        public DateTime? ApiKeyLastUsedAt { get; set; }

        /// <summary>Owner id from <c>api_keys.owner_id</c> when populated.</summary>
        [Display(Name = "Key Owner Id")]
        public int? ApiKeyOwnerId { get; set; }

        /// <summary>Username of the API key owner, when resolvable.</summary>
        [Display(Name = "Key Owner Username")]
        public string? ApiKeyOwnerUsername { get; set; }

        /// <summary>Full name of the API key owner, when resolvable.</summary>
        [Display(Name = "Key Owner Name")]
        public string? ApiKeyOwnerFullName { get; set; }

        /// <summary>User id recorded on the audit entry.</summary>
        [Display(Name = "User Id")]
        public int? UserId { get; set; }

        /// <summary>Username associated with the audit entry.</summary>
        [Display(Name = "Username")]
        public string? Username { get; set; }

        /// <summary>Full name for the user associated with the audit entry.</summary>
        [Display(Name = "User Full Name")]
        public string? UserFullName { get; set; }

        /// <summary>Action/endpoint captured by the audit entry (e.g., HTTP verb + resource).</summary>
        [Display(Name = "Action")]
        public string? Action { get; set; }

        /// <summary>Primary timestamp for the audit entry (UTC).</summary>
        [Display(Name = "Timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>Creation timestamp recorded by the table (UTC).</summary>
        [Display(Name = "Created At")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Update timestamp recorded by the table (UTC).</summary>
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>IP address captured for the request.</summary>
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        /// <summary>Serialized request payload or metadata (JSON, query string, etc.).</summary>
        [Display(Name = "Request Details")]
        public string? RequestDetails { get; set; }

        /// <summary>Additional detail payload (response summary, exception, etc.).</summary>
        [Display(Name = "Details")]
        public string? Details { get; set; }

        /// <summary>Optional extended metadata (headers, correlation ids, etc.).</summary>
        [Display(Name = "Extra Metadata")]
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>Derived helper exposing the masked API key (all but last four hidden).</summary>
        [Display(Name = "API Key")]
        public string MaskedApiKey => MaskSensitiveValue(ApiKeyValue);

        /// <summary>Last four alphanumeric characters of the key, when available.</summary>
        public string? ApiKeyLastFour
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ApiKeyValue))
                {
                    return null;
                }

                var trimmed = ApiKeyValue.Trim();
                var buffer = new List<char>();
                for (int i = trimmed.Length - 1; i >= 0 && buffer.Count < 4; i--)
                {
                    if (char.IsLetterOrDigit(trimmed[i]))
                    {
                        buffer.Insert(0, trimmed[i]);
                    }
                }

                return buffer.Count == 0 ? null : new string(buffer.ToArray());
            }
        }

        /// <summary>Display label combining masked key, description, and identifier.</summary>
        public string ApiKeyDisplayLabel
        {
            get
            {
                var segments = new List<string>();
                var masked = MaskedApiKey;
                if (!string.IsNullOrWhiteSpace(masked))
                {
                    segments.Add(masked);
                }

                if (!string.IsNullOrWhiteSpace(ApiKeyDescription))
                {
                    segments.Add(ApiKeyDescription!.Trim());
                }

                if (ApiKeyId.HasValue)
                {
                    segments.Add($"#{ApiKeyId.Value}");
                }

                return segments.Count == 0 ? "Unknown API Key" : string.Join(" · ", segments);
            }
        }

        /// <summary>Display helper for the associated user (username + id when available).</summary>
        public string UserDisplay
        {
            get
            {
                var name = !string.IsNullOrWhiteSpace(Username)
                    ? Username!
                    : UserId?.ToString() ?? "-";

                if (UserId.HasValue && !string.IsNullOrWhiteSpace(Username))
                {
                    return $"{Username} (#{UserId.Value})";
                }

                if (!string.IsNullOrWhiteSpace(UserFullName))
                {
                    return $"{UserFullName} [{name}]";
                }

                return name;
            }
        }

        /// <summary>Owner display helper combining username, name, and id.</summary>
        public string OwnerDisplay
        {
            get
            {
                if (!ApiKeyOwnerId.HasValue && string.IsNullOrWhiteSpace(ApiKeyOwnerUsername) && string.IsNullOrWhiteSpace(ApiKeyOwnerFullName))
                {
                    return "-";
                }

                var label = ApiKeyOwnerUsername;
                if (!string.IsNullOrWhiteSpace(ApiKeyOwnerFullName))
                {
                    label = string.IsNullOrWhiteSpace(ApiKeyOwnerUsername)
                        ? ApiKeyOwnerFullName
                        : $"{ApiKeyOwnerFullName} [{ApiKeyOwnerUsername}]";
                }

                if (ApiKeyOwnerId.HasValue)
                {
                    label = string.IsNullOrWhiteSpace(label)
                        ? $"#{ApiKeyOwnerId.Value}"
                        : $"{label} (#{ApiKeyOwnerId.Value})";
                }

                return label ?? "-";
            }
        }

        /// <summary>Best-effort timestamp for display (prefers <see cref="Timestamp"/> then fallbacks).</summary>
        public DateTime? EffectiveTimestamp => Timestamp ?? CreatedAt ?? UpdatedAt;

        private static string MaskSensitiveValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            var chars = trimmed.ToCharArray();
            var alphanumericCount = 0;

            for (int i = chars.Length - 1; i >= 0; i--)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                {
                    continue;
                }

                alphanumericCount++;
                if (alphanumericCount > 4)
                {
                    chars[i] = '•';
                }
            }

            var masked = new string(chars);
            if (alphanumericCount <= 4)
            {
                var prefix = new string('•', Math.Max(0, 4 - alphanumericCount));
                return prefix + masked;
            }

            return masked;
        }
    }
}

