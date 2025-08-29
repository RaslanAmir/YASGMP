// YasGMP/Helpers/DigitalSignatureHelper.cs

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using YasGMP.Models;

namespace YasGMP.Helpers
{
    /// <summary>
    /// <b>DigitalSignatureHelper</b> – Utilities for creating deterministic SHA-256 based
    /// signature hashes for GMP/Annex 11/21 CFR Part 11 workflows (audit trails, e-signatures, exports).
    /// <para>
    /// Production note: replace simple SHA-256 hashing with proper asymmetric signing (RSA/ECDSA)
    /// and a managed certificate/PKI for fully compliant e-signatures.
    /// </para>
    /// </summary>
    public static class DigitalSignatureHelper
    {
        // ---------------------------------------------------------------------
        //  GENERIC STRING + FILE
        // ---------------------------------------------------------------------

        /// <summary>
        /// Generates a base64-encoded SHA-256 digital signature from an arbitrary canonical string.
        /// </summary>
        /// <param name="dataToSign">Canonical string to sign.</param>
        /// <returns>Base64-encoded SHA-256 hash.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataToSign"/> is null.</exception>
        public static string GenerateSignatureHash(string dataToSign)
        {
            if (dataToSign == null)
                throw new ArgumentNullException(nameof(dataToSign));

            using SHA256 sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(dataToSign);
            byte[] hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Generates a base64-encoded SHA-256 hash for the contents of a file.
        /// </summary>
        /// <param name="filePath">Full path to file on disk.</param>
        /// <returns>Base64-encoded SHA-256 content hash.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="filePath"/> is null/empty.</exception>
        /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
        public static string GenerateFileHash(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            return Convert.ToBase64String(sha.ComputeHash(stream));
        }

        // ---------------------------------------------------------------------
        //  DOMAIN OVERLOADS (kept intentionally generous to eliminate CS1503)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Signature for an <see cref="Asset"/> record (generic asset abstraction).
        /// </summary>
        public static string GenerateSignatureHash(Asset asset, string sessionId, string deviceInfo)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
            if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

            string dataToSign = $"{asset.Id}|{asset.AssetName}|{sessionId}|{deviceInfo}";
            return GenerateSignatureHash(dataToSign);
        }

        /// <summary>
        /// Signature for a <see cref="Machine"/> (legacy screens often pass a Machine directly).
        /// </summary>
        public static string GenerateSignatureHash(Machine machine, string sessionId, string deviceInfo)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
            if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

            string dataToSign =
                $"{machine.Id}|{machine.Code}|{machine.Name}|{machine.SerialNumber}|{sessionId}|{deviceInfo}";
            return GenerateSignatureHash(dataToSign);
        }

        /// <summary>
        /// Signature for a <see cref="Part"/>.
        /// </summary>
        public static string GenerateSignatureHash(Part part, string sessionId, string deviceInfo)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
            if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

            string dataToSign = $"{part.Id}|{part.Code}|{part.Name}|{sessionId}|{deviceInfo}";
            return GenerateSignatureHash(dataToSign);
        }

        /// <summary>
        /// Signature for a <see cref="Qualification"/>.
        /// </summary>
        public static string GenerateSignatureHash(Qualification qualification, string sessionId, string deviceInfo)
        {
            if (qualification == null) throw new ArgumentNullException(nameof(qualification));
            if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
            if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

            string equip = qualification.Machine?.Name ?? qualification.Component?.Name ?? qualification.Supplier?.Name ?? "N/A";
            string dataToSign = $"{qualification.Id}|{qualification.Code}|{qualification.Type}|{equip}|{sessionId}|{deviceInfo}";
            return GenerateSignatureHash(dataToSign);
        }

        /// <summary>
        /// Signature for a <see cref="Supplier"/> (some older pages hash suppliers directly).
        /// </summary>
        public static string GenerateSignatureHash(Supplier supplier, string sessionId, string deviceInfo)
        {
            if (supplier == null) throw new ArgumentNullException(nameof(supplier));
            if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
            if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

            string dataToSign = $"{supplier.Id}|{supplier.Name}|{supplier.VatNumber}|{supplier.Status}|{sessionId}|{deviceInfo}";
            return GenerateSignatureHash(dataToSign);
        }

        /// <summary>
        /// Signature for a <see cref="ContractorIntervention"/>.
        /// </summary>
        public static string GenerateSignatureHash(ContractorIntervention intervention, string sessionId, string deviceInfo)
        {
            if (intervention == null)
                throw new ArgumentNullException(nameof(intervention), "ContractorIntervention cannot be null for digital signature generation.");
            if (sessionId == null)
                throw new ArgumentNullException(nameof(sessionId), "SessionId cannot be null for digital signature generation.");
            if (deviceInfo == null)
                throw new ArgumentNullException(nameof(deviceInfo), "DeviceInfo cannot be null for digital signature generation.");

            string dataToSign =
                $"{intervention.Id}|{intervention.AssetName}|{intervention.ContractorName}|{intervention.InterventionType}|{intervention.Status}|{intervention.StartDate:O}|{intervention.EndDate:O}|{intervention.Notes}|{sessionId}|{deviceInfo}";
            return GenerateSignatureHash(dataToSign);
        }

        /// <summary>
        /// Signature for a <see cref="CapaCase"/>.
        /// </summary>
        public static string GenerateSignatureHash(CapaCase capaCase, string sessionId, string deviceInfo)
        {
            if (capaCase == null) throw new ArgumentNullException(nameof(capaCase));
            if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
            if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

            string dataToSign = $"{capaCase.Id}|{capaCase.CapaCode}|{capaCase.Title}|{capaCase.Status}|{capaCase.RiskRating}|{sessionId}|{deviceInfo}";
            return GenerateSignatureHash(dataToSign);
        }

        // ---------------------------------------------------------------------
        //  OPTIONAL: Example asymmetric signing placeholder (commented)
        // ---------------------------------------------------------------------
        /*
        public static string GenerateAsymmetricSignature(string dataToSign, RSAParameters privateKey)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(privateKey);
            var dataBytes = Encoding.UTF8.GetBytes(dataToSign);
            var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }
        */
    }
}
