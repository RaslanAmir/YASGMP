using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// MachineService – GMP compliant servis za upravljanje strojevima.
    /// ✅ Pruža CRUD operacije, validaciju, digitalne potpise i integraciju s audit logovima.
    /// ✅ Svaka akcija se bilježi u <see cref="AuditService"/> za potpunu sljedivost.
    /// ✅ Usklađeno s EU GMP Annex 11 i 21 CFR Part 11.
    /// </summary>
    public class MachineService
    {
        private readonly DatabaseService _db;
        private readonly AuditService _audit;

        public MachineService(DatabaseService databaseService, AuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region === CRUD OPERATIONS ===

        /// <summary>Dohvati sve strojeve iz baze.</summary>
        public async Task<List<Machine>> GetAllAsync() => await _db.GetAllMachinesAsync();

        /// <summary>Dohvati stroj po ID-u.</summary>
        public async Task<Machine> GetByIdAsync(int id)
            => await _db.GetMachineByIdAsync(id) ?? throw new InvalidOperationException("Machine not found.");

        /// <summary>
        /// Kreira novi stroj i upisuje audit log.
        /// ✅ Generira digitalni potpis prije spremanja.
        /// </summary>
        public async Task CreateAsync(Machine machine, int userId, string ip = "system", string deviceInfo = "server", string? sessionId = null)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            ValidateMachine(machine);

            machine.DigitalSignature = GenerateDigitalSignature(machine);

            await _db.InsertOrUpdateMachineAsync(machine, update: false, actorUserId: userId, ip: ip, deviceInfo: deviceInfo, sessionId: sessionId);

            await _audit.LogSystemEventAsync("MACHINE_CREATE",
                $"✅ Kreiran novi stroj: ID={machine.Id}, Name={machine.Name}, Location={machine.Location}");
        }

        /// <summary>
        /// Ažurira postojeći stroj i bilježi promjene u audit log.
        /// ✅ Generira novi digitalni potpis nakon ažuriranja.
        /// </summary>
        public async Task UpdateAsync(Machine machine, int userId, string ip = "system", string deviceInfo = "server", string? sessionId = null)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            ValidateMachine(machine);

            machine.DigitalSignature = GenerateDigitalSignature(machine);

            await _db.InsertOrUpdateMachineAsync(machine, update: true, actorUserId: userId, ip: ip, deviceInfo: deviceInfo, sessionId: sessionId);

            await _audit.LogSystemEventAsync("MACHINE_UPDATE",
                $"♻️ Ažuriran stroj: ID={machine.Id}, Name={machine.Name}, Location={machine.Location}");
        }

        /// <summary>
        /// Briše stroj po ID-u (uz audit trail).
        /// ✅ GMP zahtijeva evidenciju brisanja u audit log.
        /// </summary>
        public async Task DeleteAsync(int machineId, int userId, string ip = "system", string deviceInfo = "server", string? sessionId = null)
        {
            await _db.DeleteMachineAsync(machineId, actorUserId: userId, ip: ip, deviceInfo: deviceInfo, sessionId: sessionId);

            await _audit.LogSystemEventAsync("MACHINE_DELETE",
                $"🗑️ Obrisani stroj ID={machineId}");
        }

        #endregion

        #region === STATUS & VALIDATION ===

        public bool IsActive(Machine machine) =>
            machine != null &&
            !string.IsNullOrEmpty(machine.Status) &&
            machine.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);

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

        #endregion

        #region === DIGITAL SIGNATURE ===

        private string GenerateDigitalSignature(Machine machine)
        {
            string raw = $"{machine.Id}|{machine.Name}|{machine.Location}|{machine.UrsDoc}|{DateTime.UtcNow:O}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        #endregion

        #region === FUTURE EXTENSIBILITY HOOKS ===

        public async Task LinkToPpmPlanAsync(int machineId, int ppmPlanId)
            => await _audit.LogSystemEventAsync("MACHINE_PPM_LINK", $"🔗 Povezan PPM Plan ID={ppmPlanId} sa strojem ID={machineId}");

        public async Task TriggerInitialCalibrationAsync(int machineId)
            => await _audit.LogSystemEventAsync("MACHINE_CALIBRATION_TRIGGER", $"📌 Automatski trigger kalibracije za stroj ID={machineId}");

        #endregion
    }
}
