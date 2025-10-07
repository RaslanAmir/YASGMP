using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.AppCore.Models.Signatures;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>MachineService</b> – GMP compliant servis za upravljanje strojevima/opremom.
    /// <para>
    /// • Pruža CRUD, validaciju i digitalne potpise<br/>
    /// • Svaka akcija se bilježi u <see cref="AuditService"/> (Annex 11 / 21 CFR Part 11)<br/>
    /// • Sadrži kanonsku normalizaciju statusa (vidi <see cref="NormalizeStatus(string?)"/>)
    /// </para>
    /// </summary>
    public class MachineService
    {
        private readonly DatabaseService _db;
        private readonly AuditService _audit;

        /// <summary>DI konstruktor.</summary>
        public MachineService(DatabaseService databaseService, AuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region === CRUD OPERATIONS ===========================================================

        /// <summary>
        /// Dohvati sve strojeve iz baze.
        /// </summary>
        public async Task<List<Machine>> GetAllAsync() => await _db.GetAllMachinesAsync();

        /// <summary>
        /// Dohvati stroj po ID-u ili baci <see cref="InvalidOperationException"/> ako ne postoji.
        /// </summary>
        public async Task<Machine> GetByIdAsync(int id)
            => await _db.GetMachineByIdAsync(id) ?? throw new InvalidOperationException("Machine not found.");

        /// <summary>
        /// Kreira novi stroj (potpis + audit). Status se normalizira na kanonsku vrijednost.
        /// </summary>
        public async Task CreateAsync(
            Machine machine,
            int userId,
            string ip = "system",
            string deviceInfo = "server",
            string? sessionId = null,
            SignatureMetadataDto? signatureMetadata = null)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            ValidateMachine(machine);
            machine.Status = NormalizeStatus(machine.Status);
            ApplySignatureMetadata(machine, signatureMetadata, () => GenerateLegacyDigitalSignature(machine));

            string effectiveDevice = signatureMetadata?.Device ?? deviceInfo;
            string? effectiveSession = signatureMetadata?.Session ?? sessionId;

            await _db.InsertOrUpdateMachineAsync(
                machine,
                update: false,
                actorUserId: userId,
                ip: ip,
                deviceInfo: effectiveDevice,
                sessionId: effectiveSession,
                signatureMetadata: signatureMetadata);

            await _audit.LogSystemEventAsync("MACHINE_CREATE",
                $"✅ Kreiran novi stroj: ID={machine.Id}, Name={machine.Name}, Location={machine.Location}");
        }

        /// <summary>
        /// Ažurira postojeći stroj (novi potpis + audit). Status se normalizira.
        /// </summary>
        public async Task UpdateAsync(
            Machine machine,
            int userId,
            string ip = "system",
            string deviceInfo = "server",
            string? sessionId = null,
            SignatureMetadataDto? signatureMetadata = null)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            ValidateMachine(machine);
            machine.Status = NormalizeStatus(machine.Status);
            ApplySignatureMetadata(machine, signatureMetadata, () => GenerateLegacyDigitalSignature(machine));

            string effectiveDevice = signatureMetadata?.Device ?? deviceInfo;
            string? effectiveSession = signatureMetadata?.Session ?? sessionId;

            await _db.InsertOrUpdateMachineAsync(
                machine,
                update: true,
                actorUserId: userId,
                ip: ip,
                deviceInfo: effectiveDevice,
                sessionId: effectiveSession,
                signatureMetadata: signatureMetadata);

            await _audit.LogSystemEventAsync("MACHINE_UPDATE",
                $"♻️ Ažuriran stroj: ID={machine.Id}, Name={machine.Name}, Location={machine.Location}");
        }

        /// <summary>
        /// Briše stroj po ID-u (audit trail).
        /// </summary>
        public async Task DeleteAsync(int machineId, int userId, string ip = "system", string deviceInfo = "server", string? sessionId = null)
        {
            await _db.DeleteMachineAsync(machineId, actorUserId: userId, ip: ip, deviceInfo: deviceInfo, sessionId: sessionId);

            await _audit.LogSystemEventAsync("MACHINE_DELETE",
                $"🗑️ Obrisani stroj ID={machineId}");
        }

        #endregion

        #region === STATUS & VALIDATION =======================================================

        /// <summary>Vraća <c>true</c> ako je stroj aktivan.</summary>
        public bool IsActive(Machine machine) =>
            machine != null &&
            !string.IsNullOrEmpty(machine.Status) &&
            NormalizeStatus(machine.Status).Equals("active", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Poslovna validacija minimalnih GMP polja.
        /// </summary>
        public void ValidateMachine(Machine machine)
        {
            if (string.IsNullOrWhiteSpace(machine.Name))
                throw new InvalidOperationException("❌ Naziv stroja je obavezan.");
            if (string.IsNullOrWhiteSpace(machine.Code))
                throw new InvalidOperationException("❌ Kod stroja je obavezan.");
            if (string.IsNullOrWhiteSpace(machine.Location))
                throw new InvalidOperationException("❌ Lokacija stroja mora biti definirana.");
            if (string.IsNullOrWhiteSpace(machine.Manufacturer))
                throw new InvalidOperationException("❌ Proizvođač stroja je obavezan.");
            if (string.IsNullOrWhiteSpace(machine.UrsDoc))
                throw new InvalidOperationException("⚠️ URS dokument (User Requirement Specification) je obavezan za GMP compliance.");
            if (machine.InstallDate > DateTime.UtcNow)
                throw new InvalidOperationException("❌ Datum instalacije ne može biti u budućnosti.");
        }

        /// <summary>
        /// Kanonska normalizacija statusa na skup:
        /// <c>active</c>, <c>maintenance</c>, <c>decommissioned</c>, <c>reserved</c>, <c>scrapped</c>.
        /// Sadrži i uobičajene lokalizirane sinonime (npr. <i>u pogonu</i>, <i>van pogona</i>).
        /// </summary>
        public static string NormalizeStatus(string? raw)
        {
            string s = (raw ?? string.Empty).Trim().ToLowerInvariant();

            return s switch
            {
                // canonical English
                "active" => "active",
                "maintenance" or "maint" or "service" => "maintenance",
                "decommissioned" or "decom" or "retired" => "decommissioned",
                "reserved" => "reserved",
                "scrapped" or "scrap" => "scrapped",

                // common localized synonyms
                "u pogonu" or "operativan" or "operational" => "active",
                "van pogona" or "neispravan" or "kvar" or "servis" => "maintenance",
                "otpisan" or "rashodovan" => "scrapped",
                "rezerviran" => "reserved",
                "dekomisioniran" => "decommissioned",

                // fallback
                _ => "active"
            };
        }

        #endregion

        #region === DIGITAL SIGNATURE =========================================================

        /// <summary>Applies captured signature metadata (or generates a legacy signature when absent).</summary>
        private static void ApplySignatureMetadata(Machine machine, SignatureMetadataDto? metadata, Func<string> legacyFactory)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            if (legacyFactory == null) throw new ArgumentNullException(nameof(legacyFactory));

            string hash = metadata?.Hash ?? machine.DigitalSignature ?? legacyFactory();
            machine.DigitalSignature = hash;
        }

        /// <summary>Legacy deterministic signature generator retained for backward compatibility.</summary>
        [Obsolete("Signature metadata should provide the hash; this fallback will be removed once legacy flows are upgraded.")]
        private string GenerateLegacyDigitalSignature(Machine machine)
        {
            string status = NormalizeStatus(machine.Status);
            string raw = $"{machine.Id}|{machine.Name}|{machine.Location}|{machine.UrsDoc}|{status}|{DateTime.UtcNow:O}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw)));
        }

        #endregion

        #region === EXTENSIBILITY HOOKS =======================================================
        /// <summary>
        /// Executes the link to ppm plan async operation.
        /// </summary>

        public async Task LinkToPpmPlanAsync(int machineId, int ppmPlanId)
            => await _audit.LogSystemEventAsync("MACHINE_PPM_LINK", $"🔗 Povezan PPM Plan ID={ppmPlanId} sa strojem ID={machineId}");
        /// <summary>
        /// Executes the trigger initial calibration async operation.
        /// </summary>

        public async Task TriggerInitialCalibrationAsync(int machineId)
            => await _audit.LogSystemEventAsync("MACHINE_CALIBRATION_TRIGGER", $"📌 Automatski trigger kalibracije za stroj ID={machineId}");

        #endregion
    }
}
