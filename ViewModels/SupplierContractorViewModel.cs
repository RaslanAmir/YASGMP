using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za dobavljače i vanjske izvođače (servisere/contractore) s GMP podrškom.
    /// Omogućuje prikaz, CRUD operacije, filtriranje, povezivanje s dijelovima, povijest intervencija i export.
    /// </summary>
    public class SupplierContractorViewModel
    {
        /// <summary>Kolekcija dobavljača iz baze.</summary>
        public ObservableCollection<Supplier> Suppliers { get; set; } = new ObservableCollection<Supplier>();

        /// <summary>Kolekcija vanjskih izvođača (servisera/contractora) iz baze.</summary>
        public ObservableCollection<ContractorIntervention> ContractorInterventions { get; set; } = new ObservableCollection<ContractorIntervention>();

        /// <summary>Selektirani dobavljač (može biti null kada ništa nije odabrano).</summary>
        public Supplier? SelectedSupplier { get; set; }

        /// <summary>Selektirana intervencija/vanjski izvođač (može biti null).</summary>
        public ContractorIntervention? SelectedContractorIntervention { get; set; }

        private readonly DatabaseService _db;

        /// <summary>Inicijalizira novi ViewModel i dohvaća podatke iz baze.</summary>
        /// <param name="db">Instanca <see cref="DatabaseService"/>. Ne smije biti null.</param>
        /// <exception cref="ArgumentNullException">Baca se ako je <paramref name="db"/> null.</exception>
        public SupplierContractorViewModel(DatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _ = LoadAllAsync();
        }

        /// <summary>Dohvaća sve dobavljače i izvođače iz baze.</summary>
        public async Task LoadAllAsync()
        {
            Suppliers.Clear();
            foreach (var sup in await _db.GetAllSuppliersAsync().ConfigureAwait(false))
                Suppliers.Add(sup);

            ContractorInterventions.Clear();
            foreach (var interv in await _db.GetAllContractorInterventionsAsync().ConfigureAwait(false))
                ContractorInterventions.Add(interv);
        }

        /// <summary>Dodaje ili ažurira dobavljača (Supplier).</summary>
        public async Task SaveSupplierAsync(Supplier supplier, bool update)
        {
            await _db.InsertOrUpdateSupplierAsync(supplier, update).ConfigureAwait(false);
            await LoadAllAsync().ConfigureAwait(false);
        }

        /// <summary>Briše dobavljača po ID-u.</summary>
        public async Task DeleteSupplierAsync(int id)
        {
            await _db.DeleteSupplierAsync(id).ConfigureAwait(false);
            await LoadAllAsync().ConfigureAwait(false);
        }

        /// <summary>Dodaje ili ažurira vanjsku intervenciju (ContractorIntervention).</summary>
        public async Task SaveContractorInterventionAsync(ContractorIntervention ci, bool update)
        {
            await _db.InsertOrUpdateContractorInterventionAsync(ci, update).ConfigureAwait(false);
            await LoadAllAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Briše intervenciju po ID-u (poziva eksplicitno instancni overload bez dvosmislenosti).
        /// </summary>
        public async Task DeleteContractorInterventionAsync(int id)
        {
            // Pozovi overload: (int id, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
            await _db.DeleteContractorInterventionAsync(id, 1, "system", "ui", default).ConfigureAwait(false);
            await LoadAllAsync().ConfigureAwait(false);
        }
    }
}
