using System;
using System.Collections.Generic;
using System.Linq;
using YasGMP.Models;

namespace YasGMP.Data
{
    /// <summary>
    /// Simple seeding helper that ensures existing attachments have a retention
    /// policy record so the new attachment pipeline can operate consistently.
    /// </summary>
    public static class AttachmentSeedData
    {
        /// <summary>
        /// Executes the ensure seeded operation.
        /// </summary>
        public static void EnsureSeeded(YasGmpDbContext? context)
        {
            if (context == null)
            {
                return;
            }

            var attachmentIds = context.Attachments
                .Select(a => a.Id)
                .ToList();

            if (attachmentIds.Count == 0)
            {
                return;
            }

            var existing = new HashSet<int>(context.RetentionPolicies.Select(r => r.AttachmentId));
            var missing = attachmentIds.Where(id => !existing.Contains(id)).ToList();
            if (missing.Count == 0)
            {
                return;
            }

            foreach (var id in missing)
            {
                context.RetentionPolicies.Add(new RetentionPolicy
                {
                    AttachmentId = id,
                    PolicyName = "legacy-default",
                    CreatedAt = DateTime.UtcNow,
                    DeleteMode = "soft",
                    LegalHold = false,
                    ReviewRequired = false
                });
            }

            context.SaveChanges();
        }
    }
}
