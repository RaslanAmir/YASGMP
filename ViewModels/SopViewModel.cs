using System.Collections.ObjectModel;
using YasGMP.Models;
using CommunityToolkit.Mvvm.Input; 

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za SOP (Standard Operating Procedure) dokumentaciju.
    /// </summary>
    public class SopViewModel
    {
        public ObservableCollection<SopDocument> SopDocuments { get; set; } = new ObservableCollection<SopDocument>();

        // TODO: Dodaj metode za dohvat, filtriranje i upravljanje SOP dokumentima
    }
}
