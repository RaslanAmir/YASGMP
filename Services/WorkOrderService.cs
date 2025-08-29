using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>WorkOrderService</b> – Ultra-robustan GMP-compliant servis za upravljanje radnim nalozima.
    /// <para>
    /// ✅ Validacija, digitalni potpis, audit trail putem <see cref="WorkOrderAuditService"/>.<br/>
    /// ✅ Usklađeno s 21 CFR Part 11, EU GMP Annex 11 i ISO 13485.
    /// </para>
    /// </summary>
    public class WorkOrderService
    {
        private readonly DatabaseService _db;
        private readonly WorkOrderAuditService _audit;

        /// <summary>
        /// Inicijalizira servis sa slojevima baze i audita.
        /// </summary>
        /// <param name="databaseService">Apstrakcija nad pristupom bazi podataka.</param>
        /// <param name="auditService">Servis za GMP audit zapise.</param>
        /// <exception cref="ArgumentNullException">Ako je bilo koji ovisni servis <c>null</c>.</exception>
        public WorkOrderService(DatabaseService databaseService, WorkOrderAuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region === CRUD OPERATIONS ===

        /// <summary>
        /// Dohvaća sve radne naloge iz baze.
        /// </summary>
        /// <remarks>
        /// Metoda je asinhrona i koristi <c>ConfigureAwait(false)</c> radi optimalnog schedulinga u MAUI okruženju.
        /// </remarks>
        /// <returns>Lista svih <see cref="WorkOrder"/> entiteta.</returns>
        public async Task<List<WorkOrder>> GetAllAsync() =>
            await _db.GetAllWorkOrdersAsync().ConfigureAwait(false);

        /// <summary>
        /// Dohvaća radni nalog po primarnom ključu.
        /// </summary>
        /// <param name="id">Jedinstveni identifikator radnog naloga.</param>
        /// <returns>Pronadjeni <see cref="WorkOrder"/> entitet.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Baca se ako radni nalog s navedenim <paramref name="id"/> ne postoji (rješava CS8603).
        /// </exception>
        public async Task<WorkOrder> GetByIdAsync(int id)
        {
            var order = await _db.GetWorkOrderByIdAsync(id).ConfigureAwait(false);
            if (order is null)
                throw new KeyNotFoundException($"Radni nalog (ID={id}) nije pronađen.");
            return order;
        }

        /// <summary>
        /// Kreira novi radni nalog, validira podatke, dodaje digitalni potpis i zapisuje u audit trail.
        /// </summary>
        /// <param name="order">Model radnog naloga za spremanje.</param>
        /// <param name="userId">Korisnik koji izvodi radnju (za audit).</param>
        /// <returns>Asinhroni zadatak.</returns>
        /// <exception cref="ArgumentNullException">Ako je <paramref name="order"/> <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Ako validacija ne prođe.</exception>
        public async Task CreateAsync(WorkOrder order, int userId)
        {
            ValidateOrder(order);
            order.Status ??= "OPEN";
            order.DigitalSignature = GenerateDigitalSignature(order);

            // DatabaseService API (InsertOrUpdate...): (workorder, update, actorUserId, ip, device)
            await _db.InsertOrUpdateWorkOrderAsync(order, update: false, actorUserId: userId, ip: "system", device: Environment.MachineName).ConfigureAwait(false);
            await LogAudit(order.Id, userId, WorkOrderActionType.Create, $"Kreiran radni nalog {order.Type} za stroj {order.MachineId}").ConfigureAwait(false);
        }

        /// <summary>
        /// Ažurira postojeći radni nalog i bilježi promjene u GMP audit trail.
        /// </summary>
        /// <param name="order">Model radnog naloga sa željenim izmjenama.</param>
        /// <param name="userId">Korisnik koji izvodi radnju.</param>
        /// <returns>Asinhroni zadatak.</returns>
        /// <exception cref="ArgumentNullException">Ako je <paramref name="order"/> <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Ako validacija ne prođe.</exception>
        public async Task UpdateAsync(WorkOrder order, int userId)
        {
            ValidateOrder(order);
            order.DigitalSignature = GenerateDigitalSignature(order);

            await _db.InsertOrUpdateWorkOrderAsync(order, update: true, actorUserId: userId, ip: "system", device: Environment.MachineName).ConfigureAwait(false);
            await LogAudit(order.Id, userId, WorkOrderActionType.Update, $"Ažuriran radni nalog ID={order.Id}").ConfigureAwait(false);
        }

        /// <summary>
        /// Zatvara radni nalog (ako je otvoren) i bilježi akciju u audit trail.
        /// </summary>
        /// <param name="workOrderId">ID radnog naloga koji se zatvara.</param>
        /// <param name="userId">Korisnik koji izvodi radnju.</param>
        /// <param name="resultNote">Završna napomena/rezultat.</param>
        /// <returns>Asinhroni zadatak.</returns>
        /// <exception cref="InvalidOperationException">Ako nalog ne postoji ili je već zatvoren.</exception>
        public async Task CloseWorkOrderAsync(int workOrderId, int userId, string resultNote)
        {
            var order = await _db.GetWorkOrderByIdAsync(workOrderId).ConfigureAwait(false);
            if (order == null) throw new InvalidOperationException("Radni nalog ne postoji.");
            if (IsClosed(order)) throw new InvalidOperationException("Radni nalog je već zatvoren.");

            order.Status = "CLOSED";
            order.Result = resultNote ?? string.Empty;
            order.DigitalSignature = GenerateDigitalSignature(order);

            await _db.InsertOrUpdateWorkOrderAsync(order, update: true, actorUserId: userId, ip: "system", device: Environment.MachineName).ConfigureAwait(false);
            await LogAudit(order.Id, userId, WorkOrderActionType.Closed, $"Zatvoren nalog ID={order.Id} | Rezultat: {resultNote}").ConfigureAwait(false);
        }

        /// <summary>
        /// Briše radni nalog i bilježi GMP audit zapis o brisanju.
        /// </summary>
        /// <param name="workOrderId">ID radnog naloga za brisanje.</param>
        /// <param name="userId">Korisnik koji izvodi radnju.</param>
        /// <returns>Asinhroni zadatak.</returns>
        public async Task DeleteAsync(int workOrderId, int userId)
        {
            await _db.DeleteWorkOrderAsync(workOrderId, userId, ip: "system", device: Environment.MachineName).ConfigureAwait(false);
            await LogAudit(workOrderId, userId, WorkOrderActionType.Delete, $"Obrisan radni nalog ID={workOrderId}").ConfigureAwait(false);
        }

        #endregion

        #region === VALIDATION & STATUS ===

        /// <summary>
        /// Validira obavezna polja radnog naloga (MachineId, Type, CreatedById).
        /// </summary>
        /// <param name="order">Radni nalog koji se validira.</param>
        /// <exception cref="ArgumentNullException">Ako je <paramref name="order"/> <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Ako bilo koje od obaveznih polja nije postavljeno.</exception>
        private static void ValidateOrder(WorkOrder order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.MachineId <= 0) throw new InvalidOperationException("Radni nalog mora imati valjani ID stroja.");
            if (string.IsNullOrWhiteSpace(order.Type)) throw new InvalidOperationException("Vrsta radnog naloga je obavezna.");
            if (order.CreatedById <= 0) throw new InvalidOperationException("Radni nalog mora imati korisnika koji ga je kreirao.");
        }

        /// <summary>
        /// Provjerava je li nalog otvoren.
        /// </summary>
        /// <param name="order">Radni nalog.</param>
        /// <returns><c>true</c> ako je status OPEN/otvoren; inače <c>false</c>.</returns>
        public static bool IsOpen(WorkOrder order) =>
            order?.Status?.Equals("OPEN", StringComparison.OrdinalIgnoreCase) == true
            || order?.Status?.Equals("otvoren", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Provjerava je li nalog zatvoren.
        /// </summary>
        /// <param name="order">Radni nalog.</param>
        /// <returns><c>true</c> ako je status CLOSED/zavrsen; inače <c>false</c>.</returns>
        public static bool IsClosed(WorkOrder order) =>
            order?.Status?.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) == true
            || order?.Status?.Equals("zavrsen", StringComparison.OrdinalIgnoreCase) == true;

        #endregion

        #region === DIGITAL SIGNATURES ===

        /// <summary>
        /// Generira jedinstveni digitalni potpis (SHA256 + GUID salt) za radni nalog.
        /// </summary>
        /// <param name="order">Radni nalog.</param>
        /// <returns>Base64 SHA-256 hash.</returns>
        private static string GenerateDigitalSignature(WorkOrder order)
        {
            string raw = $"{order.Id}|{order.MachineId}|{order.ComponentId}|{order.Status}|{DateTime.UtcNow:O}|{Guid.NewGuid()}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        /// <summary>
        /// Overload za kreiranje potpisa iz proizvoljnog tekstualnog payload-a (npr. za audit događaje).
        /// </summary>
        /// <param name="data">Ulazni tekst za potpisivanje.</param>
        /// <returns>Base64 SHA-256 hash.</returns>
        private static string GenerateDigitalSignature(string data)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes($"{data}|{Guid.NewGuid()}")));
        }

        #endregion

        #region === AUDIT INTEGRATION ===

        /// <summary>
        /// Centralizirano bilježi promjene nad radnim nalozima u GMP audit log.
        /// </summary>
        /// <param name="workOrderId">ID radnog naloga.</param>
        /// <param name="userId">ID korisnika.</param>
        /// <param name="action">Tip akcije.</param>
        /// <param name="details">Detaljan opis akcije.</param>
        /// <returns>Asinhroni zadatak.</returns>
        private async Task LogAudit(int workOrderId, int userId, WorkOrderActionType action, string details)
        {
            await _audit.CreateAsync(new WorkOrderAudit
            {
                WorkOrderId = workOrderId,
                UserId = userId,
                Action = action,
                Note = details,
                ChangedAt = DateTime.UtcNow,
                DigitalSignature = GenerateDigitalSignature(details),
                SourceIp = "system",
                DeviceInfo = Environment.MachineName
            }).ConfigureAwait(false);
        }

        #endregion

        #region === FUTURE HOOKS ===

        /// <summary>
        /// 🔥 Hook za AI predikciju kvarova ili IoT integraciju sa senzorima strojeva.
        /// </summary>
        /// <param name="workOrderId">ID radnog naloga.</param>
        /// <returns>Simuliran prikaz IoT statusa za zadani nalog.</returns>
        public Task<string> GetIoTSensorStatusAsync(int workOrderId) =>
            Task.FromResult($"IoT Data: WorkOrder {workOrderId} – All sensors nominal.");

        #endregion
    }
}
