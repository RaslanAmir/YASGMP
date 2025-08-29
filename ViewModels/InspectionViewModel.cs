using System.Collections.ObjectModel;
using YasGMP.Models;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za Inspections – GMP, HALMED, interne i druge inspekcije.
    /// </summary>
    public class InspectionViewModel
    {
        public ObservableCollection<Inspection> Inspections { get; set; } = new ObservableCollection<Inspection>();

        // TODO: Dodaj metode za filtriranje po datumu, tipu, rezultatu, izvoz izvještaja
    }
}
