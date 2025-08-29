using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>PreventiveMaintenancePlan</b> – Ultra robust, inspector-ready model for Preventive Maintenance Plans (PPM).
    /// <para>
    /// ✅ GMP/CSV/Annex 11/21 CFR Part 11 compliant fields<br/>
    /// ✅ Versioning, e-signature, auditability, and analytics fields (risk/anomaly/AI)<br/>
    /// ✅ Backward-compatibility aliases for common UI/ViewModel expectations (<c>Title</c>, <c>DueDate</c>)<br/>
    /// ✅ Designed for schema tolerance when mapping from MySQL rows
    /// </para>
    /// </summary>
    public class PreventiveMaintenancePlan
    {
        /// <summary>Jedinstveni ID plana (PK).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Interna šifra plana (npr. "PPM-001").</summary>
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>Naziv plana (npr. "PPM za kompresor").</summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// UI/ViewModel-friendly alias for <see cref="Name"/>.  
        /// Setting <c>Title</c> updates <see cref="Name"/>; reading returns <see cref="Name"/>.
        /// </summary>
        [MaxLength(100)]
        public string Title
        {
            get => Name;
            set => Name = value ?? string.Empty;
        }

        /// <summary>Detaljan opis plana (svi koraci, odgovornosti, reference, regulatory links).</summary>
        [MaxLength(1500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>FK – Stroj na koji se plan odnosi.</summary>
        public int? MachineId { get; set; }

        /// <summary> Navigacijska referenca na stroj (ako se koristi u višim slojevima). </summary>
        public Machine? Machine { get; set; }

        /// <summary>FK – Komponenta (opcionalno, za specifične PPM).</summary>
        public int? ComponentId { get; set; }

        /// <summary> Navigacijska referenca na komponentu (ako se koristi u višim slojevima). </summary>
        public Component? Component { get; set; }

        /// <summary>Frekvencija održavanja (npr. "mjesečno", "90 dana", cron, interval+unit).</summary>
        [MaxLength(60)]
        public string? Frequency { get; set; }

        /// <summary>Putanja do checklist datoteke (PDF/Excel, SOP, OCR-parsable, cloud URI).</summary>
        [MaxLength(255)]
        public string? ChecklistFile { get; set; }

        /// <summary>FK – ID odgovorne osobe.</summary>
        public int? ResponsibleUserId { get; set; }

        /// <summary>Navigacijska referenca na odgovornu osobu (ako se koristi u višim slojevima).</summary>
        public User? ResponsibleUser { get; set; }

        /// <summary>Povijest izvršavanja (datum, user, rezultat, signature, IP, forensics).</summary>
        public List<MaintenanceExecutionLog> ExecutionHistory { get; set; } = new();

        /// <summary>Datum zadnjeg izvršenja (UTC).</summary>
        public DateTime? LastExecuted { get; set; }

        /// <summary>Datum sljedeće planirane aktivnosti (UTC).</summary>
        public DateTime? NextDue { get; set; }

        /// <summary>
        /// UI/ViewModel-friendly alias for <see cref="NextDue"/>.  
        /// Setting <c>DueDate</c> updates <see cref="NextDue"/>; reading returns <see cref="NextDue"/>.
        /// </summary>
        public DateTime? DueDate
        {
            get => NextDue;
            set => NextDue = value;
        }

        /// <summary>Status plana (aktivan, pauziran, arhiviran, expired...)</summary>
        [MaxLength(40)]
        public string? Status { get; set; }

        /// <summary>Risk score (AI/ML readiness).</summary>
        public double? RiskScore { get; set; }

        /// <summary>AI/analytics recommendation (auto-extension, predictive trigger…).</summary>
        [MaxLength(2048)]
        public string? AiRecommendation { get; set; }

        /// <summary>Digitalni potpis zadnje izmjene (hash/e-sign/HSM).</summary>
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>Vrijeme zadnje izmjene (UTC).</summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>ID korisnika koji je zadnji izmijenio.</summary>
        public int? LastModifiedById { get; set; }

        /// <summary>Navigacijska referenca na korisnika koji je zadnji mijenjao.</summary>
        public User? LastModifiedBy { get; set; }

        /// <summary>IP adresa uređaja s kojeg je mijenjano.</summary>
        [MaxLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>Session ID / device fingerprint (Part 11 traceability).</summary>
        [MaxLength(80)]
        public string? SessionId { get; set; }

        /// <summary>Geolokacija zadnje izmjene (city/country/coords; GDPR aware).</summary>
        [MaxLength(100)]
        public string? GeoLocation { get; set; }

        /// <summary>Popis priloga (fotografije, izvještaji, SOP, certifikati).</summary>
        public List<string> Attachments { get; set; } = new();

        /// <summary>Logička verzija zapisa (za rollback/review).</summary>
        public int? Version { get; set; }

        /// <summary>FK na prethodnu verziju (ako se vodi verzioniranje).</summary>
        public int? PreviousVersionId { get; set; }

        /// <summary>Navigacijska referenca na prethodnu verziju.</summary>
        public PreventiveMaintenancePlan? PreviousVersion { get; set; }

        /// <summary>Označava je li ovo aktivna verzija plana.</summary>
        public bool IsActiveVersion { get; set; } = true;

        /// <summary>Povezani radni nalozi nastali iz plana.</summary>
        public List<WorkOrder> LinkedWorkOrders { get; set; } = new();

        /// <summary>Je li plan generiran automatizmom (AI/scheduler) ili ručno.</summary>
        public bool IsAutomated { get; set; }

        /// <summary>Zahtijeva li plan notifikaciju ili eskalaciju pri kašnjenju.</summary>
        public bool RequiresNotification { get; set; }

        /// <summary>AI/ML anomaly/predictive score.</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>Slobodna napomena (inspektor/auditor/system).</summary>
        [MaxLength(512)]
        public string? Note { get; set; }

        /// <summary>Vraća <c>true</c> ako je plan dospio i nije završen.</summary>
        public bool IsOverdue => NextDue.HasValue && DateTime.UtcNow > NextDue.Value &&
                                 !string.Equals(Status ?? string.Empty, "zatvoren", StringComparison.OrdinalIgnoreCase);

        /// <summary>Sažetak za log/inspekciju.</summary>
        public override string ToString()
            => $"[{Code}] {Name} (Status: {Status ?? "-"}, NextDue: {NextDue:u})";

        /// <summary>
        /// Duboka kopija objekta (za rollback/inspektorski export/verzioniranje).
        /// </summary>
        public PreventiveMaintenancePlan DeepCopy()
        {
            return new PreventiveMaintenancePlan
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                MachineId = MachineId,
                Machine = Machine,
                ComponentId = ComponentId,
                Component = Component,
                Frequency = Frequency,
                ChecklistFile = ChecklistFile,
                ResponsibleUserId = ResponsibleUserId,
                ResponsibleUser = ResponsibleUser,
                ExecutionHistory = new List<MaintenanceExecutionLog>(ExecutionHistory ?? new()),
                LastExecuted = LastExecuted,
                NextDue = NextDue,
                Status = Status,
                RiskScore = RiskScore,
                AiRecommendation = AiRecommendation,
                DigitalSignature = DigitalSignature,
                LastModified = LastModified,
                LastModifiedById = LastModifiedById,
                LastModifiedBy = LastModifiedBy,
                SourceIp = SourceIp,
                SessionId = SessionId,
                GeoLocation = GeoLocation,
                Attachments = new List<string>(Attachments ?? new()),
                Version = Version,
                PreviousVersionId = PreviousVersionId,
                PreviousVersion = PreviousVersion,
                IsActiveVersion = IsActiveVersion,
                LinkedWorkOrders = new List<WorkOrder>(LinkedWorkOrders ?? new()),
                IsAutomated = IsAutomated,
                RequiresNotification = RequiresNotification,
                AnomalyScore = AnomalyScore,
                Note = Note
            };
        }

        /// <summary>
        /// Backward-compatibility helper for UIs/ViewModels expecting <c>Clone()</c>.
        /// Returns <see cref="DeepCopy"/>.
        /// </summary>
        public PreventiveMaintenancePlan Clone() => DeepCopy();
    }
}
