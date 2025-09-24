using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>TrainingLog</b> – Ultra-robust record of all user training, certification, and re-certification.
    /// Tracks training type, name, trainers, pass/fail, certification docs, expiration, re-training, audit trail, digital signatures, reminders, and forensics.
    /// Full GMP/CSV/21 CFR Part 11, HALMED, and ISO compliance for user competence tracking!
    /// </summary>
    public class TrainingLog
    {
        /// <summary>Jedinstveni ID zapisa o treningu (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>ID korisnika kojem je trening vezan (Foreign Key).</summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>Navigation to user.</summary>
        public User User { get; set; } = null!;

        /// <summary>Tip treninga (softver, stroj, SOP, BPM, QMS, sigurnost, CAPA, inspekcija...)</summary>
        [MaxLength(80)]
        public string TrainingType { get; set; } = string.Empty;

        /// <summary>Naziv ili tema treninga.</summary>
        [Required]
        [MaxLength(200)]
        public string TrainingName { get; set; } = string.Empty;

        /// <summary>Datum završetka/položenog treninga.</summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Informacija je li trening uspješno položen.</summary>
        public bool Passed { get; set; }

        /// <summary>Ime glavnog trenera ili voditelja treninga.</summary>
        [MaxLength(120)]
        public string Trainer { get; set; } = string.Empty;

        /// <summary>Popis dodatnih trenera ili sudionika (bonus, audit evidence).</summary>
        public List<string> AdditionalTrainers { get; set; } = new();

        /// <summary>Putanja do dokumentacije vezane uz trening (certifikat, lista, test, PDF, e-learning export).</summary>
        [MaxLength(512)]
        public string DocumentPath { get; set; } = string.Empty;

        /// <summary>Status certifikacije (validan, istekao, povučen, suspendiran, needs_recertification, ...).</summary>
        [MaxLength(32)]
        public string CertificationStatus { get; set; } = string.Empty;

        /// <summary>Datum isteka certifikata/treninga (bonus za ponovnu edukaciju i remindere).</summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>Datum kad je potrebno obnoviti trening/certifikat (reminder).</summary>
        public DateTime? RetrainingDue { get; set; }

        /// <summary>ID/putanja do digitalnog potpisa korisnika/trenera (hash, cert, audit chain).</summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>IP adresa/uređaj s kojeg je trening logiran (forenzička evidencija).</summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Bilo koji free-text komentar, napomena, CAPA ili inspektorska bilješka.</summary>
        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        /// <summary>Datum zadnje izmjene (audit trail).</summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>ID korisnika koji je zadnji mijenjao ovaj trening log.</summary>
        public int LastModifiedById { get; set; }

        /// <summary>Navigation to last modifier.</summary>
        public User LastModifiedBy { get; set; } = null!;

        /// <summary>Hash cijelog zapisa (for chain-of-custody, integrity, future blockchain anchor).</summary>
        [MaxLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>Status podsjetnika (pending, sent, overdue, completed).</summary>
        [MaxLength(24)]
        public string ReminderStatus { get; set; } = string.Empty;

        /// <summary>Označava je li korisnik prošao test (true/false, može biti null za samo-edukaciju).</summary>
        public bool? TestPassed { get; set; }

        /// <summary>Rezultat testa, bodovi ili ocjena (bonus).</summary>
        [MaxLength(30)]
        public string TestScore { get; set; } = string.Empty;

        /// <summary>Regulatorni zahtjev (HALMED, EMA, FDA, ISO, intern...)</summary>
        [MaxLength(100)]
        public string RegulatoryRequirement { get; set; } = string.Empty;
    }
}
