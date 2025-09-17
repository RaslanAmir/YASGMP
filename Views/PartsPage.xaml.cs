// ==============================================================================
//  File: Views/PartsPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Pregled/unos/ureÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚ivanje/brisanje rezervnih dijelova (Parts) uz MySQL backend.
//      Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚â€ąĂ˘â‚¬Ë‡Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚ UI-thread safe (MainThread + SafeNavigator) Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚â€ąĂ˘â‚¬Ë‡Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ă„‚Ă˘â‚¬ĹľÄ‚„Ă˘â‚¬¦Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…ÄąĹź izbjegnute WinUI 0x8001010E greĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ă„‚Ă˘â‚¬Ä…Ä‚ËĂ˘‚¬Ă‹â€ˇke
//      Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚â€ąĂ˘â‚¬Ë‡Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚ Robusno dohvaÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚â€ąĂ˘â‚¬Ë‡anje konekcijskog stringa iz App.AppConfig
//      Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚â€ąĂ˘â‚¬Ë‡Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚ Sigurne konverzije NULL vrijednosti i parametri preko MySqlConnector
//      Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚â€ąĂ˘â‚¬Ë‡Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚ Potpuna XML dokumentacija za IntelliSense
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;             // MainThread
using Microsoft.Maui.Controls;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using ClosedXML.Excel;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>PartsPage</b> Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚â€ąĂ˘â‚¬Ë‡Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ă„‚Ă˘â‚¬ĹľÄ‚„Ă˘â‚¬¦Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…ÄąĹź pregled, unos, ureÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚ivanje i brisanje rezervnih dijelova (Parts).
    /// Povezano s MySQL bazom preko <see cref="DatabaseService"/>. Svi UI dijalozi i aĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă˘â‚¬ĹľÄ‚„Ă„Äľuriranja
    /// izvrĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ă„‚Ă˘â‚¬Ä…Ä‚ËĂ˘‚¬Ă‹â€ˇavaju se na glavnoj niti putem <see cref="MainThread"/> kako bi se izbjegle WinUI
    /// <c>COMException 0x8001010E</c> situacije.
    /// </summary>
    public partial class PartsPage : ContentPage
    {
        /// <summary>Observable kolekcija za binding.</summary>
        public ObservableCollection<Part> Parts { get; } = new();
        private readonly List<Part> _all = new();
        private string? _search;
        private bool _lowOnly;
        private string? _supplierFilter;
        private string? _locationFilter;
        private string? _categoryFilter;
        private string? _statusFilter;

        /// <summary>Servis za pristup bazi.</summary>
        private readonly DatabaseService _dbService;

        /// <summary>
        /// Inicijalizira stranicu i uÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬Ă‚¦Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¤itava dijelove. Sigurno dohvaÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚â€ąĂ˘â‚¬Ë‡a konekcijski string.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako aplikacija ili konekcijski string nisu dostupni.</exception>
        public PartsPage(DatabaseService dbService)
        {
            InitializeComponent();

            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));

            BindingContext = this;

            // Ne blokirati UI – metoda sama maršalira UI ažuriranja
            _ = LoadPartsAsync();
        }

        /// <summary>Parameterless ctor for Shell/XAML; resolves dependencies via ServiceLocator.</summary>
        public PartsPage()
            : this(ServiceLocator.GetRequiredService<DatabaseService>())
        {
        }

        /// <summary>
        /// UÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬Ă‚¦Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¤itava sve dijelove iz baze i aĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă˘â‚¬ĹľÄ‚„Ă„Äľurira kolekciju <see cref="Parts"/> na glavnoj niti.
        /// </summary>
        private async Task LoadPartsAsync()
        {
            try
            {
                const string sql = @"SELECT id, code, name, supplier, price, stock, min_stock_alert, location, image FROM parts";
                var dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

                // Pripremi listu na pozadinskoj niti
                var list = new List<Part>(capacity: dt.Rows.Count);
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    list.Add(new Part
                    {
                        Id       = row["id"] == DBNull.Value ? 0 : Convert.ToInt32(row["id"]),
                        Code     = row["code"]?.ToString() ?? string.Empty,
                        Name     = row["name"]?.ToString() ?? string.Empty,
                        Supplier = row["supplier"] == DBNull.Value ? null : row["supplier"]?.ToString(),
                        Price    = row["price"]    == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["price"]),
                        Stock    = row["stock"]    == DBNull.Value ? 0 : Convert.ToInt32(row["stock"]),
                        MinStockAlert = row["min_stock_alert"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["min_stock_alert"]),
                        Location = row["location"] == DBNull.Value ? null : row["location"]?.ToString(),
                        Image    = row["image"]    == DBNull.Value ? null : row["image"]?.ToString()
                    });
                }

                // Linked work orders count per part
                try
                {
                    const string sqlCnt = "SELECT part_id, COUNT(DISTINCT work_order_id) AS cnt FROM work_order_parts GROUP BY part_id";
                    var dtCnt = await _dbService.ExecuteSelectAsync(sqlCnt).ConfigureAwait(false);
                    var map = new Dictionary<int,int>(dtCnt.Rows.Count);
                    foreach (System.Data.DataRow r in dtCnt.Rows)
                    {
                        int pid = r["part_id"] == DBNull.Value ? 0 : Convert.ToInt32(r["part_id"]);
                        int cnt = r["cnt"]     == DBNull.Value ? 0 : Convert.ToInt32(r["cnt"]);
                        if (pid > 0) map[pid] = cnt;
                    }
                    foreach (var p in list)
                        if (map.TryGetValue(p.Id, out var c)) p.LinkedWorkOrdersCount = c;
                }
                catch { }

                // AĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă˘â‚¬ĹľÄ‚„Ă„Äľuriraj UI kolekciju na glavnoj niti
                _all.Clear();
                _all.AddRange(list);
                await MainThread.InvokeOnMainThreadAsync(() => { 
                    var suppliers = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    var locations = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    var categories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    var statuses   = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in _all)
                    {
                        if (!string.IsNullOrWhiteSpace(p.Supplier)) suppliers.Add(p.Supplier!);
                        if (!string.IsNullOrWhiteSpace(p.Location)) locations.Add(p.Location!);
                        if (!string.IsNullOrWhiteSpace(p.Category)) categories.Add(p.Category!);
                        if (!string.IsNullOrWhiteSpace(p.Status))   statuses.Add(p.Status!);
                    }
                    var supList = new List<string>(suppliers); supList.Insert(0, "(sve)");
                    var locList = new List<string>(locations); locList.Insert(0, "(sve)");
                    var catList = new List<string>(categories); catList.Insert(0, "(sve)");
                    var stList  = new List<string>(statuses);  stList.Insert(0, "(sve)");
                    SupplierFilterPicker.ItemsSource = supList;
                    LocationFilterPicker.ItemsSource = locList;
                    CategoryFilterPicker.ItemsSource = catList;
                    StatusFilterPicker.ItemsSource   = stList;
                    if (SupplierFilterPicker.SelectedIndex < 0) SupplierFilterPicker.SelectedIndex = 0;
                    if (LocationFilterPicker.SelectedIndex < 0) LocationFilterPicker.SelectedIndex = 0;
                    if (CategoryFilterPicker.SelectedIndex < 0) CategoryFilterPicker.SelectedIndex = 0;
                    if (StatusFilterPicker.SelectedIndex   < 0) StatusFilterPicker.SelectedIndex   = 0;
                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("GreĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬Ä…Ä‚ËĂ˘â€šÂ¬Ă‹â€ˇka", $"UÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă‹â€ˇÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ä‚â€žĂ„â€¦Ä‚â€žĂ„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Â¤itavanje dijelova nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Otvara formu za dodavanje novog dijela, validira unos i sprema zapis u bazu.
        /// </summary>
        private async void OnAddPartClicked(object? sender, EventArgs e)
        {
            try
            {
                var newPart = new Part();
                bool result = await ShowPartFormAsync(newPart, "Unesi novi rezervni dio");
                if (!result) return;

                const string sql = @"INSERT INTO parts (code, name, supplier, price, stock, min_stock_alert, location, image)
                                     VALUES (@code, @name, @supplier, @price, @stock, @min, @location, @image)";
                var pars = new[]
                {
                    new MySqlParameter("@code",     newPart.Code ?? string.Empty),
                    new MySqlParameter("@name",     newPart.Name ?? string.Empty),
                    new MySqlParameter("@supplier", (object?)newPart.Supplier ?? DBNull.Value),
                    new MySqlParameter("@price",    (object?)newPart.Price    ?? DBNull.Value),
                    new MySqlParameter("@stock",    newPart.Stock),
                    new MySqlParameter("@min",      (object?)newPart.MinStockAlert ?? DBNull.Value),
                    new MySqlParameter("@location", (object?)newPart.Location ?? DBNull.Value),
                    new MySqlParameter("@image",    (object?)newPart.Image    ?? DBNull.Value)
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadPartsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("GreĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬Ä…Ä‚ËĂ˘â€šÂ¬Ă‹â€ˇka", $"Spremanje dijela nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Otvara formu za ureÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚ivanje postojeÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚â€ąĂ˘â‚¬Ë‡eg dijela, validira i aĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă˘â‚¬ĹľÄ‚„Ă„Äľurira zapis u bazi.
        /// </summary>
        private async void OnEditPartClicked(object? sender, EventArgs e)
        {
            try
            {
                if (PartListView.SelectedItem is not Part selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite rezervni dio iz liste za ureÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă‹â€ˇÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ä‚â€žĂ„â€¦Ä‚â€žĂ„ÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ˘â‚¬Ä…Ä‚â€šĂ‚ÂÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă‹â€ˇĂ„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Â¬Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Âivanje.", "OK");
                    return;
                }

                var partToEdit = new Part
                {
                    Id       = selected.Id,
                    Code     = selected.Code,
                    Name     = selected.Name,
                    Supplier = selected.Supplier,
                    Price    = selected.Price,
                    Stock    = selected.Stock,
                    Location = selected.Location,
                    Image    = selected.Image
                };

                bool result = await ShowPartFormAsync(partToEdit, "Uredi rezervni dio");
                if (!result) return;

                const string sql = @"UPDATE parts SET 
                                        code=@code, name=@name, supplier=@supplier, 
                                        price=@price, stock=@stock, min_stock_alert=@min, location=@location, image=@image
                                     WHERE id=@id";
                var pars = new[]
                {
                    new MySqlParameter("@code",     partToEdit.Code ?? string.Empty),
                    new MySqlParameter("@name",     partToEdit.Name ?? string.Empty),
                    new MySqlParameter("@supplier", (object?)partToEdit.Supplier ?? DBNull.Value),
                    new MySqlParameter("@price",    (object?)partToEdit.Price    ?? DBNull.Value),
                    new MySqlParameter("@stock",    partToEdit.Stock),
                    new MySqlParameter("@min",      (object?)partToEdit.MinStockAlert ?? DBNull.Value),
                    new MySqlParameter("@location", (object?)partToEdit.Location ?? DBNull.Value),
                    new MySqlParameter("@image",    (object?)partToEdit.Image    ?? DBNull.Value),
                    new MySqlParameter("@id",       partToEdit.Id)
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadPartsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("GreĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬Ä…Ä‚ËĂ˘â€šÂ¬Ă‹â€ˇka", $"AĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚â€žĂ„Äľuriranje dijela nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// BriĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ă„‚Ă˘â‚¬Ä…Ä‚ËĂ˘‚¬Ă‹â€ˇe odabrani dio iz baze nakon potvrde korisnika.
        /// </summary>
        private async void OnDeletePartClicked(object? sender, EventArgs e)
        {
            try
            {
                if (PartListView.SelectedItem is not Part selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite rezervni dio iz liste za brisanje.", "OK");
                    return;
                }

                bool confirm = await SafeNavigator.ConfirmAsync("Potvrda brisanja", $"Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąÄľelite li izbrisati dio: {selected.Name}?", "Da", "Ne");
                if (!confirm) return;

                const string sql = "DELETE FROM parts WHERE id=@id";
                var pars = new[] { new MySqlParameter("@id", selected.Id) };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadPartsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("GreĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬Ä…Ä‚ËĂ˘â€šÂ¬Ă‹â€ˇka", $"Brisanje dijela nije uspjelo: {ex.Message}", "OK");
            }
        }

        private void ApplyFilter()
        {
            Parts.Clear();
            foreach (var p in _all)
            {
                if (_lowOnly && !((p.MinStockAlert.HasValue && p.Stock < p.MinStockAlert.Value) || p.IsWarehouseStockCritical))
                    continue;
                if (!string.IsNullOrWhiteSpace(_supplierFilter) && _supplierFilter != "(sve)" && !string.Equals(p.Supplier ?? string.Empty, _supplierFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(_locationFilter) && _locationFilter != "(sve)" && !string.Equals(p.Location ?? string.Empty, _locationFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(_categoryFilter) && _categoryFilter != "(sve)" && !string.Equals(p.Category ?? string.Empty, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(_statusFilter) && _statusFilter != "(sve)" && !string.Equals(p.Status ?? string.Empty, _statusFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(_search))
                {
                    var s = _search.Trim().ToLowerInvariant();
                    bool hit = (p.Code ?? string.Empty).ToLowerInvariant().Contains(s)
                               || (p.Name ?? string.Empty).ToLowerInvariant().Contains(s)
                               || (p.Supplier ?? string.Empty).ToLowerInvariant().Contains(s);
                    if (!hit) continue;
                }
                Parts.Add(p);
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            _search = e.NewTextValue;
            ApplyFilter();
        }

        private void OnLowStockOnlyChanged(object? sender, CheckedChangedEventArgs e)
        {
            _lowOnly = e.Value;
            ApplyFilter();
        }

        private async void OnIncreaseStockClicked(object? sender, EventArgs e)
        {
            if (PartListView.SelectedItem is not Part selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite dio za promjenu zalihe.", "OK");
                return;
            }
            var dlg = new YasGMP.Views.Dialogs.StockChangeDialog(_dbService, selected, true, null, "ui", "PartsPage", null);
            await Navigation.PushModalAsync(dlg);
            if (await dlg.Result) await LoadPartsAsync().ConfigureAwait(false);
        }

        private async void OnPartDetailClicked(object? sender, EventArgs e)
        {
            if (PartListView.SelectedItem is not Part selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite dio za detalje.", "OK");
                return;
            }
            var dlg = new YasGMP.Views.Dialogs.PartDetailDialog(_dbService, selected);
            await Navigation.PushModalAsync(dlg);
            if (await dlg.Result) await LoadPartsAsync().ConfigureAwait(false);
        }

        private async void OnDecreaseStockClicked(object? sender, EventArgs e)
        {
            if (PartListView.SelectedItem is not Part selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite dio za promjenu zalihe.", "OK");
                return;
            }
            var dlg = new YasGMP.Views.Dialogs.StockChangeDialog(_dbService, selected, false, null, "ui", "PartsPage", null);
            await Navigation.PushModalAsync(dlg);
            if (await dlg.Result) await LoadPartsAsync().ConfigureAwait(false);
        }

        private async Task ChangeStockAsync(bool increase)
        {
            try
            {
                if (PartListView.SelectedItem is not Part selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite dio za promjenu zalihe.", "OK");
                    return;
                }

                var qtyStr = await MainThread.InvokeOnMainThreadAsync(() => DisplayPromptAsync(
                    increase ? "+ PoveÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă‹â€ˇÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ä‚â€žĂ„â€¦Ä‚â€žĂ„ÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ˘â‚¬Ä…Ä‚â€šĂ‚ÂÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă‹â€ˇĂ„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Â¬Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬Ă„â€¦Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€ąĂ˘â‚¬Ë‡aj zalihu" : "- Smanji zalihu",
                    "Unesi koliÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă‹â€ˇÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ä‚â€žĂ„â€¦Ä‚â€žĂ„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Â¤inu:",
                    keyboard: Keyboard.Numeric));
                if (string.IsNullOrWhiteSpace(qtyStr) || !int.TryParse(qtyStr, out int qty) || qty <= 0)
                    return;

                int delta = increase ? qty : -qty;
                const string sql = "UPDATE parts SET stock = stock + @d WHERE id=@id";
                var pars = new[] { new MySqlParameter("@d", delta), new MySqlParameter("@id", selected.Id) };
                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);

                await _dbService.LogSystemEventAsync(
                    userId: null,
                    eventType: increase ? "STOCK_INCREASE" : "STOCK_DECREASE",
                    tableName: "parts",
                    module: "PartsPage",
                    recordId: selected.Id,
                    description: $"delta={delta}; part={selected.Code}",
                    ip: "ui",
                    severity: "info",
                    deviceInfo: "PartsPage",
                    sessionId: null
                ).ConfigureAwait(false);

                await LoadPartsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("GreĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬Ä…Ä‚ËĂ˘â€šÂ¬Ă‹â€ˇka", $"Promjena zalihe nije uspjela: {ex.Message}", "OK");
            }
        }

        private async void OnExportClicked(object? sender, EventArgs e)
        {
            try
            {
                string format = await Helpers.ExportFormatPrompt.PromptAsync(); // csv, xlsx, pdf
                string dir = FileSystem.AppDataDirectory;
                Directory.CreateDirectory(dir);
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string path;

                if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    // XLSX export with useful columns
                    path = Path.Combine(dir, $"parts_export_{timestamp}.xlsx");
                    var columns = new List<(string Header, Func<Part, object?> Selector)>
                    {
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("Supplier", p => p.Supplier),
                        ("Price", p => p.Price),
                        ("Stock", p => p.Stock),
                        ("MinStock", p => p.MinStockAlert),
                        ("WH_Low_Count", p => p.LowWarehouseCount),
                        ("WH_Summary", p => p.WarehouseSummary),
                        ("Location", p => p.Location)
                    };
                    // Write via helper
                    var tmp = Helpers.XlsxExporter.WriteSheet(Parts, "parts_export", columns);
                    path = tmp;
                }
                else
                {
                    // CSV export (default)
                    var lines = new List<string>(Parts.Count + 1);
                    lines.Add("Id,Code,Name,Supplier,Price,Stock,MinStock,WH_Low_Count,WH_Summary,Location");
                    foreach (var p in Parts)
                    {
                        string esc(string? s) => string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\"", "\"\"");
                        lines.Add($"{p.Id},\"{esc(p.Code)}\",\"{esc(p.Name)}\",\"{esc(p.Supplier)}\",{p.Price},{p.Stock},{p.MinStockAlert},{p.LowWarehouseCount},\"{esc(p.WarehouseSummary)}\",\"{esc(p.Location)}\"");
                    }
                    path = Path.Combine(dir, $"parts_export_{timestamp}.csv");
                    System.IO.File.WriteAllLines(path, lines);
                }

                // Audit export with real user + IP
                try
                {
                    var app = Application.Current as App;
                    int? uid = app?.LoggedUser?.Id;
                    string ip = DependencyService.Get<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;
                    await _dbService.LogSystemEventAsync(uid, "PARTS_EXPORT", "parts", "PartsPage", null, $"format={format}; items={Parts.Count}; lowOnly={_lowOnly}; file={path}", ip, "info", "PartsPage", app?.SessionId).ConfigureAwait(false);
                }
                catch { }

                await SafeNavigator.ShowAlertAsync("Export", $"Export spreman: {path}", "OK");
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("GreĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬Ä…Ä‚ËĂ˘â€šÂ¬Ă‹â€ˇka", $"Export nije uspio: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Jednostavna forma preko DisplayPromptAsync za unos/ureÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚ivanje.
        /// Svi promptovi se izvrĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ă„‚Ă˘â‚¬Ä…Ä‚ËĂ˘‚¬Ă‹â€ˇavaju na glavnoj niti kako bi se izbjegle COM/WinUI greĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚‚Ă‚¦Ă„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚„Ă˘â‚¬¦Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ă„‚Ă˘â‚¬Ä…Ä‚ËĂ˘‚¬Ă‹â€ˇke.
        /// </summary>
        /// <param name="part">Model koji se puni/ureÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚uje.</param>
        /// <param name="title">Naslov forme.</param>
        /// <returns><c>true</c> ako je unos valjan i potvrÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚en; inaÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬Ă‚¦Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¤e <c>false</c>.</returns>
        private async Task<bool> ShowPartFormAsync(Part part, string title)
        {
            // Lokalni pomoÄ‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬ÄąÄľĂ„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇÄ‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘‚¬ÄąË‡Ä‚‚Ă‚¬Ä‚„Ă„…Ä‚„Ă„ÄľĂ„‚Ă˘â‚¬ĹľÄ‚ËĂ˘‚¬ÄąË‡Ă„‚Ă˘â‚¬Ä…Ä‚‚Ă‚Ä‚„Ă˘â‚¬ĹˇÄ‚â€ąĂ‚Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ă„Ä…Ă‹â€ˇĂ„‚Ă˘â‚¬ĹˇÄ‚‚Ă‚¬Ä‚„Ă˘â‚¬ĹˇÄ‚ËĂ˘‚¬Ă„…Ă„‚Ă‹Ä‚ËĂ˘â‚¬ĹˇĂ‚¬Ä‚â€ąĂ˘â‚¬Ë‡nik: UI-safe prompt
            Task<string?> PromptAsync(string caption, string msg, string? initial = null) =>
                MainThread.InvokeOnMainThreadAsync(() => DisplayPromptAsync(caption, msg, initialValue: initial));

            var code = await PromptAsync(title, "Interna oznaka (Code):", part.Code);
            if (code is null) return false;
            part.Code = code;

            var name = await PromptAsync(title, "Naziv dijela:", part.Name);
            if (name is null) return false;
            part.Name = name;

            var supplier = await PromptAsync(title, "DobavljaÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă‹â€ˇÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ä‚â€žĂ„â€¦Ä‚â€žĂ„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Â¤:", part.Supplier);
            if (supplier is null) return false;
            part.Supplier = supplier;

            var priceStr = await PromptAsync(title, "Cijena:", part.Price.HasValue ? part.Price.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
            if (!string.IsNullOrWhiteSpace(priceStr) && decimal.TryParse(priceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                part.Price = price;
            else
                part.Price = null; // prefer null nad 0 kad nije unio vrijednost

            var stockStr = await PromptAsync(title, "KoliÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă‹â€ˇÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ä‚â€žĂ„â€¦Ä‚â€žĂ„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Â¤ina na skladiĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬Ä…Ä‚ËĂ˘â€šÂ¬Ă‹â€ˇtu:", part.Stock > 0 ? part.Stock.ToString(CultureInfo.InvariantCulture) : string.Empty);
            if (!string.IsNullOrWhiteSpace(stockStr) && int.TryParse(stockStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock))
                part.Stock = stock;
            else
                part.Stock = 0;

            part.Location = await PromptAsync(title, "Lokacija skladiĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€šĂ‚Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬Ä…Ä‚ËĂ˘â€šÂ¬Ă‹â€ˇta:", part.Location) ?? part.Location;
            var minStr = await PromptAsync(title, "Minimalna koliĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľĂ„â€šĂ˘â‚¬ĹľÄ‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬Ä…Ă„Ä…Ă„â€žĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąÄľina (alarm):", part.MinStockAlert.HasValue ? part.MinStockAlert.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);

            if (!string.IsNullOrWhiteSpace(minStr) && int.TryParse(minStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minq))

                part.MinStockAlert = minq;

            else

                part.MinStockAlert = null;


            part.Location = await PromptAsync(title, "Lokacija skladiĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľĂ„â€šĂ˘â‚¬ĹľÄ‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬Ä…Ă„Ä…Ă„â€žĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ä‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąÄľta:", part.Location) ?? part.Location;

            part.Image    = await PromptAsync(title, "Putanja ili ime slike:", part.Image) ?? part.Image;


            return true;
        }

        // Filter handlers for supplier and location pickers
        private void OnSupplierFilterChanged(object? sender, EventArgs e)
        {
            _supplierFilter = SupplierFilterPicker.SelectedItem as string;
            ApplyFilter();
        }

        private void OnLocationFilterChanged(object? sender, EventArgs e)
        {
            _locationFilter = LocationFilterPicker.SelectedItem as string;
            ApplyFilter();
        }

        private void OnCategoryFilterChanged(object? sender, EventArgs e)
        {
            _categoryFilter = CategoryFilterPicker.SelectedItem as string;
            ApplyFilter();
        }

        private void OnStatusFilterChanged(object? sender, EventArgs e)
        {
            _statusFilter = StatusFilterPicker.SelectedItem as string;
            ApplyFilter();
        }
    }
}




