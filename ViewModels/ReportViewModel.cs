using System.Collections.ObjectModel;
using YasGMP.Models;
using CommunityToolkit.Mvvm.Input; 

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za Reports – sumarizirani izvještaji, PDF/Excel export, analitika.
    /// </summary>
    public class ReportViewModel
    {
        public ObservableCollection<Report> Reports { get; set; } = new ObservableCollection<Report>();

        // TODO: Dodaj metodu za generiranje izvještaja po odabiru (datumi, tip, entitet)
        // TODO: PDF/Excel export
        // TODO: Pretraga i filtriranje rezultata
    }
}
