using Microsoft.Maui.Controls;
using System.Threading.Tasks;
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
            OpenReportsCommand = new Command(OpenReports);
            OpenAuditLogCommand = new Command(OpenAuditLog);
        }

        private static class Routes
        {
            public const string Dashboard    = "//root/home/dashboard";
            public const string Users        = "//root/admin/users";
            public const string Machines     = "//root/ops/machines";
            public const string Components   = "//root/ops/components";
            public const string Parts        = "//root/ops/parts";
            public const string Suppliers    = "//root/ops/suppliers";
            public const string WorkOrders   = "//root/ops/workorders";
            public const string Calibrations = "//root/ops/calibrations";
            public const string Validation   = "//root/quality/validation";
            public const string Capa         = "//root/quality/capa";
            public const string AuditLog     = "//root/quality/auditlog";
        }

        private static Task NavigateAsync(string route)
            => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync(route);

        /// <summary>
        /// Otvara ekran za korisnike.
        /// </summary>
        private async void OpenUsers() => await NavigateAsync(Routes.Users);

        /// <summary>
        /// Otvara ekran za strojeve.
        /// </summary>
        private async void OpenMachines() => await NavigateAsync(Routes.Machines);

        /// <summary>
        /// Otvara ekran za komponente.
        /// </summary>
        private async void OpenComponents() => await NavigateAsync(Routes.Components);

        /// <summary>
        /// Otvara ekran za dijelove (Parts).
        /// </summary>
        private async void OpenParts() => await NavigateAsync(Routes.Parts);

        /// <summary>
        /// Otvara ekran za dobavljače.
        /// </summary>
        private async void OpenSuppliers() => await NavigateAsync(Routes.Suppliers);

        /// <summary>
        /// Otvara ekran za radne naloge.
        /// </summary>
        private async void OpenWorkOrders() => await NavigateAsync(Routes.WorkOrders);

        /// <summary>
        /// Otvara ekran za kalibracije.
        /// </summary>
        private async void OpenCalibrations() => await NavigateAsync(Routes.Calibrations);

        /// <summary>
        /// Otvara ekran za validacije/kvalifikacije (IQ/OQ/PQ/URS).
        /// </summary>
        private async void OpenValidation() => await NavigateAsync(Routes.Validation);

        /// <summary>
        /// Otvara ekran za CAPA slučajeve.
        /// </summary>
        private async void OpenCapa() => await NavigateAsync(Routes.Capa);

        /// <summary>
        /// Otvara glavni dashboard s izvještajima.
        /// </summary>
        private async void OpenReports() => await NavigateAsync(Routes.Dashboard);

        /// <summary>
        /// Otvara ekran za audit/GMP log.
        /// </summary>
        private async void OpenAuditLog() => await NavigateAsync(Routes.AuditLog);
    }
}
