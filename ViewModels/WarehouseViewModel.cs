using System.Collections.ObjectModel;
using YasGMP.Models;
using CommunityToolkit.Mvvm.Input; 

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za rad sa skladištima – prikaz, dodavanje i ažuriranje skladišta te vezanih stanja zaliha.
    /// </summary>
    public class WarehouseViewModel
    {
        public ObservableCollection<Warehouse> Warehouses { get; set; } = new ObservableCollection<Warehouse>();
        public ObservableCollection<StockLevel> StockLevels { get; set; } = new ObservableCollection<StockLevel>();

        // TODO: Filter po skladištu i dijelu (part)
        // TODO: Dodaj/uredi skladište, poveži s odgovornim korisnikom
        // TODO: Brzi prikaz artikala ispod minimalnog praga
        // TODO: Pregled povijesti ulaza/izlaza (dokumentiraj za budući modul)
    }
}
