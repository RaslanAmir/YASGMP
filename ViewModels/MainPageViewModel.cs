using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za glavnu kontrolnu ploču (dashboard) MAUI aplikacije YasGMP.
    /// Sadrži komandne metode za otvaranje svih ključnih GMP modula.
    /// </summary>
    public class MainPageViewModel
    {
        /// <summary>
        /// Tekst za dobrodošlicu, prikazuje korisnika (pripremi za binding).
        /// </summary>
        public string WelcomeText { get; set; } = "Dobrodošli u YasGMP!";

        // Komande za sve module
        /// <summary>Komanda za otvaranje korisnika.</summary>
        public ICommand OpenUsersCommand { get; }
        /// <summary>Komanda za strojeve.</summary>
        public ICommand OpenMachinesCommand { get; }
        /// <summary>Komanda za komponente.</summary>
        public ICommand OpenComponentsCommand { get; }
        /// <summary>Komanda za dijelove (Parts).</summary>
        public ICommand OpenPartsCommand { get; }
        /// <summary>Komanda za dobavljače.</summary>
        public ICommand OpenSuppliersCommand { get; }
        /// <summary>Komanda za radne naloge (Work Orders).</summary>
        public ICommand OpenWorkOrdersCommand { get; }
        /// <summary>Komanda za kalibracije.</summary>
        public ICommand OpenCalibrationsCommand { get; }
        /// <summary>Komanda za validacije/kvalifikacije (IQ/OQ/PQ/URS).</summary>
        public ICommand OpenValidationCommand { get; }
        /// <summary>Komanda za CAPA slučajeve.</summary>
        public ICommand OpenCapaCommand { get; }
        /// <summary>Komanda za SOP/dokumentaciju.</summary>
        public ICommand OpenSopCommand { get; }
        /// <summary>Komanda za inspekcije.</summary>
        public ICommand OpenInspectionsCommand { get; }
        /// <summary>Komanda za izvještaje (reports/dashboard).</summary>
        public ICommand OpenReportsCommand { get; }
        /// <summary>Komanda za GMP/Audit log.</summary>
        public ICommand OpenAuditLogCommand { get; }

        /// <summary>
        /// Konstruktor – inicijalizira sve komande i priprema navigaciju.
        /// </summary>
        public MainPageViewModel()
        {
            OpenUsersCommand = new Command(OpenUsers);
            OpenMachinesCommand = new Command(OpenMachines);
            OpenComponentsCommand = new Command(OpenComponents);
            OpenPartsCommand = new Command(OpenParts);
            OpenSuppliersCommand = new Command(OpenSuppliers);
            OpenWorkOrdersCommand = new Command(OpenWorkOrders);
            OpenCalibrationsCommand = new Command(OpenCalibrations);
            OpenValidationCommand = new Command(OpenValidation);
            OpenCapaCommand = new Command(OpenCapa);
            OpenSopCommand = new Command(OpenSop);
            OpenInspectionsCommand = new Command(OpenInspections);
            OpenReportsCommand = new Command(OpenReports);
            OpenAuditLogCommand = new Command(OpenAuditLog);
        }

        /// <summary>
        /// Otvara ekran za korisnike.
        /// </summary>
        private async void OpenUsers() => await Shell.Current.GoToAsync("//UsersPage");

        /// <summary>
        /// Otvara ekran za strojeve.
        /// </summary>
        private async void OpenMachines() => await Shell.Current.GoToAsync("//MachinesPage");

        /// <summary>
        /// Otvara ekran za komponente.
        /// </summary>
        private async void OpenComponents() => await Shell.Current.GoToAsync("//ComponentsPage");

        /// <summary>
        /// Otvara ekran za dijelove (Parts).
        /// </summary>
        private async void OpenParts() => await Shell.Current.GoToAsync("//PartsPage");

        /// <summary>
        /// Otvara ekran za dobavljače.
        /// </summary>
        private async void OpenSuppliers() => await Shell.Current.GoToAsync("//SuppliersPage");

        /// <summary>
        /// Otvara ekran za radne naloge.
        /// </summary>
        private async void OpenWorkOrders() => await Shell.Current.GoToAsync("//WorkOrdersPage");

        /// <summary>
        /// Otvara ekran za kalibracije.
        /// </summary>
        private async void OpenCalibrations() => await Shell.Current.GoToAsync("//CalibrationsPage");

        /// <summary>
        /// Otvara ekran za validacije/kvalifikacije (IQ/OQ/PQ/URS).
        /// </summary>
        private async void OpenValidation() => await Shell.Current.GoToAsync("//ValidationPage");

        /// <summary>
        /// Otvara ekran za CAPA slučajeve.
        /// </summary>
        private async void OpenCapa() => await Shell.Current.GoToAsync("//CapaPage");

        /// <summary>
        /// Otvara ekran za SOP dokumente.
        /// </summary>
        private async void OpenSop() => await Shell.Current.GoToAsync("//SopPage");

        /// <summary>
        /// Otvara ekran za inspekcije.
        /// </summary>
        private async void OpenInspections() => await Shell.Current.GoToAsync("//InspectionsPage");

        /// <summary>
        /// Otvara ekran za izvještaje/dashboard.
        /// </summary>
        private async void OpenReports() => await Shell.Current.GoToAsync("//ReportsPage");

        /// <summary>
        /// Otvara ekran za audit/GMP log.
        /// </summary>
        private async void OpenAuditLog() => await Shell.Current.GoToAsync("//AuditLogPage");
    }
}
