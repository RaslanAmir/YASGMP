using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>PartService</b> – GMP/CSV compliant servis za upravljanje rezervnim dijelovima (Parts).
    /// <para>
    /// ✅ CRUD + upravljanje zalihama, digitalni potpisi, potpuna sljedivost preko AuditService-a.<br/>
    /// ✅ Svi unosi su forenzički potpisani i spremni za inspekciju (Annex 11 / 21 CFR Part 11).
    /// </para>
    /// </summary>
    public class PartService
    {
        private readonly DatabaseService _db;
        private readonly AuditService _audit;

        /// <summary>Inicijalizira <see cref="PartService"/> s potrebnim servisima.</summary>
        public PartService(DatabaseService databaseService, AuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region === CRUD OPERATIONS ===

        /// <summary>Dohvaća sve rezervne dijelove iz baze.</summary>
        public async Task<List<Part>> GetAllAsync() => await _db.GetAllPartsAsync();

        /// <summary>
        /// Dohvaća rezervni dio po ID-u. Baca iznimku ako nije pronađen (eliminira CS8603).
        /// </summary>
        public async Task<Part> GetByIdAsync(int id)
        {
            var p = await _db.GetPartByIdAsync(id);
            if (p == null) throw new KeyNotFoundException($"Dio #{id} nije pronađen.");
            return p;
        }

        /// <summary>Kreira novi rezervni dio i bilježi audit događaj.</summary>
        public async Task CreateAsync(Part part, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            ValidatePart(part);

            ApplySignatureMetadata(part, signatureMetadata, () => ComputeLegacyDigitalSignature(part));
            await _db.InsertOrUpdatePartAsync(part, update: false, signatureMetadata: signatureMetadata);

            await _audit.LogSystemEventAsync("PART_CREATE", $"Dodani novi dio ID={part.Id}, Name={part.Name}, Stock={part.Stock}");
        }

        /// <summary>Ažurira postojeći rezervni dio i bilježi audit događaj.</summary>
        public async Task UpdateAsync(Part part, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            ValidatePart(part);

            ApplySignatureMetadata(part, signatureMetadata, () => ComputeLegacyDigitalSignature(part));
            await _db.InsertOrUpdatePartAsync(part, update: true, signatureMetadata: signatureMetadata);

            await _audit.LogSystemEventAsync("PART_UPDATE", $"Ažuriran dio ID={part.Id}, Name={part.Name}, Stock={part.Stock}");
        }

        /// <summary>Briše rezervni dio po ID-u i bilježi operaciju u audit log.</summary>
        public async Task DeleteAsync(int partId, int userId)
        {
            await _db.DeletePartAsync(partId);
            await _audit.LogSystemEventAsync("PART_DELETE", $"Obrisan dio ID={partId}");
        }

        #endregion

        #region === STOCK MANAGEMENT ===

        public async Task IncreaseStockAsync(int partId, int amount, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            var part = await GetByIdAsync(partId);
            part.Stock += amount;
            ApplySignatureMetadata(part, signatureMetadata, () => ComputeLegacyDigitalSignature(part));

            await _db.InsertOrUpdatePartAsync(part, update: true, signatureMetadata: signatureMetadata);
            await _audit.LogSystemEventAsync("PART_STOCK_INCREASE", $"Povećana zaliha za dio ID={part.Id} za {amount}, nova zaliha={part.Stock}");
        }

        public async Task DecreaseStockAsync(int partId, int amount, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            var part = await GetByIdAsync(partId);
            if (part.Stock - amount < 0)
                throw new InvalidOperationException("Zaliha ne može biti negativna.");

            part.Stock -= amount;
            ApplySignatureMetadata(part, signatureMetadata, () => ComputeLegacyDigitalSignature(part));

            await _db.InsertOrUpdatePartAsync(part, update: true, signatureMetadata: signatureMetadata);
            await _audit.LogSystemEventAsync("PART_STOCK_DECREASE", $"Smanjena zaliha za dio ID={part.Id} za {amount}, nova zaliha={part.Stock}");
        }

        /// <summary>Jednostavna provjera dostupnosti na skladištu.</summary>
        public bool IsInStock(Part? part) => part != null && part.Stock > 0;

        #endregion

        #region === VALIDATION ===

        private void ValidatePart(Part part)
        {
            if (string.IsNullOrWhiteSpace(part.Name))
                throw new InvalidOperationException("Naziv dijela je obavezan.");

            if (string.IsNullOrWhiteSpace(part.Code))
                throw new InvalidOperationException("Kod dijela je obavezan.");

            bool hasDefaultSupplier = part.DefaultSupplierId.HasValue && part.DefaultSupplierId.Value > 0;
            bool hasAnySupplierPrice = part.SupplierPrices != null && part.SupplierPrices.Count > 0;

            if (!hasDefaultSupplier && !hasAnySupplierPrice)
                throw new InvalidOperationException("Dobavljač je obavezan. Postavite DefaultSupplierId ili dodajte barem jedan zapis u SupplierPrices.");
        }

        #endregion

        #region === DIGITAL SIGNATURE ===

        private static void ApplySignatureMetadata(Part part, SignatureMetadataDto? metadata, Func<string> legacyFactory)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (legacyFactory == null) throw new ArgumentNullException(nameof(legacyFactory));

            string hash = metadata?.Hash ?? part.DigitalSignature ?? legacyFactory();
            part.DigitalSignature = hash;

            if (metadata?.Id.HasValue == true)
            {
                part.DigitalSignatureId = metadata.Id;
            }

            if (!string.IsNullOrWhiteSpace(metadata?.IpAddress))
            {
                part.SourceIp = metadata.IpAddress!;
            }
        }

        [Obsolete("Signature metadata should provide the hash; this fallback will be removed once legacy flows are upgraded.")]
        private string ComputeLegacyDigitalSignature(Part part)
        {
            string supplierName = GetSupplierNameForSignature(part);
            string raw = $"{part.Id}|{part.Code}|{part.Name}|{supplierName}|{DateTime.UtcNow:O}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        private static string GetSupplierNameForSignature(Part? part)
        {
            if (part == null) return "N/A";

            if (!string.IsNullOrWhiteSpace(part.DefaultSupplier?.Name))
                return part.DefaultSupplier.Name;

            if (part.SupplierPrices != null && part.SupplierPrices.Count > 0)
                return part.SupplierPrices[0]?.SupplierName
                       ?? part.SupplierPrices[0]?.Supplier?.Name
                       ?? "N/A";

            return "N/A";
        }

        #endregion
    }
}
