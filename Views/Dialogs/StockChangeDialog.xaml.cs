using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views.Dialogs
{
    public partial class StockChangeDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;

        private readonly DatabaseService _db;
        private readonly bool _increase;
        private readonly Part _part;
        private readonly int? _actorUserId;
        private readonly string _ip;
        private readonly string _device;
        private readonly string? _sessionId;

        public ObservableCollection<Warehouse> Warehouses { get; } = new();

        public StockChangeDialog(DatabaseService db, Part part, bool increase, int? actorUserId, string ip, string device, string? sessionId)
        {
            InitializeComponent();
            _db = db; _increase = increase; _part = part; _actorUserId = actorUserId; _ip = ip; _device = device; _sessionId = sessionId;
            TitleLabel.Text = increase ? "+ Povećaj zalihu" : "- Smanji zalihu";
            PartLabel.Text = $"Dio: {part?.Name} ({part?.Code})";
            _ = LoadWarehousesAsync();
        }

        private async Task LoadWarehousesAsync()
        {
            try
            {
                var list = await _db.GetWarehousesAsync().ConfigureAwait(false);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WarehousePicker.ItemsSource = list;
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            if (WarehousePicker.SelectedItem is not Warehouse wh)
            {
                await DisplayAlert("Napomena", "Odaberite skladište.", "OK");
                return;
            }
            if (!int.TryParse(QuantityEntry.Text, out int qty) || qty <= 0)
            {
                await DisplayAlert("Napomena", "Unesite ispravnu količinu.", "OK");
                return;
            }

            string? doc = DocumentEntry.Text;
            string? note = NoteEditor.Text;

            try
            {
                // Wire real user/session/IP if not provided
                var app = Application.Current as App;
                int userId = _actorUserId ?? (app?.LoggedUser?.Id ?? 0);
                string ip = string.IsNullOrWhiteSpace(_ip) || _ip == "ui"
                    ? (DependencyService.Get<IPlatformService>()?.GetLocalIpAddress() ?? _ip)
                    : _ip;
                string? sessionId = _sessionId ?? app?.SessionId;

                if (_increase)
                    await _db.ReceiveStockAsync(_part.Id, wh.Id, qty, userId, doc, note, ip, _device, sessionId).ConfigureAwait(false);
                else
                    await _db.IssueStockAsync(_part.Id, wh.Id, qty, userId, doc, note, ip, _device, sessionId).ConfigureAwait(false);

                _tcs.TrySetResult(true);
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(false);
            await Navigation.PopModalAsync();
        }
    }
}
