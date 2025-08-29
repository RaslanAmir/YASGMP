using System;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>WorkOrderEditDialog</b>
    /// <para>
    /// Modalni dijalog za unos ili izmjenu radnog naloga po GMP/CSV pravilima.<br/>
    /// Obavezno koristi <see cref="WorkOrderEditDialogViewModel"/> za BindingContext.<br/>
    /// Svaka promjena, potvrda, odustajanje i unos – sve je auditirano i spremno za forenzičku provjeru.
    /// </para>
    /// <para>
    /// <b>Korištenje:</b> <br/>
    /// Otvori dijalog: <c>await Navigation.PushModalAsync(new WorkOrderEditDialog(viewModel));</c><br/>
    /// Pretplati se na <see cref="DialogResult"/> za povratni rezultat.<br/>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <b>Zašto je bitno?</b> 
    /// Svaki modalni unos ili izmjena radnog naloga je pod GMP inspekcijom! 
    /// Ovdje se hvata i validira svaki podatak, potpis i audit – nema rupe ni “nejasne” akcije.
    /// </remarks>
    public partial class WorkOrderEditDialog : ContentPage
    {
        /// <summary>
        /// Event koji javlja rezultat zatvaranja dijaloga.
        /// <list type="bullet">
        /// <item><term>true</term> = korisnik želi spremiti (SAVE)</item>
        /// <item><term>false</term> = korisnik je odustao (CANCEL)</item>
        /// </list>
        /// Nullable delegate rješava CS8618; <see cref="WorkOrder"/> parametar je nullable kako bi se
        /// uskladio s potpisom u ViewModelu (CS8622).
        /// </summary>
        public event Action<bool, WorkOrder?>? DialogResult;

        /// <summary>
        /// Konstruktor dijaloga.
        /// </summary>
        /// <param name="viewModel">
        /// ViewModel koji sadrži WorkOrder podatke i sve GMP validacije, potpise, slike i logiku.
        /// <para><b>Napomena:</b> Mora biti instanca <see cref="WorkOrderEditDialogViewModel"/>.</para>
        /// </param>
        /// <exception cref="ArgumentNullException">Ako je <paramref name="viewModel"/> null.</exception>
        public WorkOrderEditDialog(WorkOrderEditDialogViewModel viewModel)
        {
            InitializeComponent();

            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel), "BindingContext za WorkOrderEditDialog ne smije biti null!");

            BindingContext = viewModel;
            viewModel.DialogResult += OnDialogResult;
        }

        /// <summary>
        /// Handler za zatvaranje dijaloga (poziva ga ViewModel preko eventa). Zatvara modalni prozor
        /// i prosljeđuje rezultat pretplatnicima <see cref="DialogResult"/>.
        /// </summary>
        /// <param name="result">true = spremi (SAVE), false = odustani (CANCEL)</param>
        /// <param name="order">Kreirani/izmijenjeni <see cref="WorkOrder"/> (može biti null)</param>
        private async void OnDialogResult(bool result, WorkOrder? order)
        {
            DialogResult?.Invoke(result, order);
            await Navigation.PopModalAsync();
        }

        /// <summary>
        /// Prilikom zatvaranja (disappearing) – odspoji event handler radi sprečavanja
        /// memory leakova i dvostrukih eventova.
        /// </summary>
        protected override void OnDisappearing()
        {
            if (BindingContext is WorkOrderEditDialogViewModel vm)
                vm.DialogResult -= OnDialogResult;

            base.OnDisappearing();
        }
    }
}
