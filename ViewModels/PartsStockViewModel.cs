using System.Collections.ObjectModel;
using YasGMP.Models;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za prikaz svih dijelova i njihovih stanja u skladištima.
    /// Omogućuje pretragu, filtriranje po skladištu i prikaz zaliha po dijelu/komponenti.
    /// </summary>
    public class PartsStockViewModel
    {
        public ObservableCollection<Part> Parts { get; set; } = new ObservableCollection<Part>();
        public ObservableCollection<Warehouse> Warehouses { get; set; } = new ObservableCollection<Warehouse>();
        public ObservableCollection<StockLevel> StockLevels { get; set; } = new ObservableCollection<StockLevel>();

        // TODO: Brza pretraga po imenu ili kodu dijela
        // TODO: Filtriranje prema skladištu ili minimalnoj količini
        // TODO: Vizualni prikaz stanja (crveno/zeleno za ispod/iznad praga)
        // TODO: Povezivanje s narudžbama za popunu skladišta (za budući modul)
    }
}
