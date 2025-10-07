using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views.Dialogs
{
    /// <summary>
    /// Dialog presenting detailed information about a part including suppliers and stock.
    /// </summary>
    public partial class PartDetailDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        public Task<bool> Result => _tcs.Task;

        private readonly DatabaseService _db;
        private readonly Part _part;
        /// <summary>
        /// Gets or sets the stocks.
        /// </summary>

        public ObservableCollection<StockRow> Stocks { get; } = new();
        /// <summary>
        /// Gets or sets the transactions.
        /// </summary>
        public ObservableCollection<TxRow> Transactions { get; } = new();
        /// <summary>
        /// Initializes a new instance of the PartDetailDialog class.
        /// </summary>

        public PartDetailDialog(DatabaseService db, Part part)
        {
            InitializeComponent();
            _db = db; _part = part;
            PartNameLabel.Text = part?.Name ?? "";
            PartCodeLabel.Text = part?.Code ?? "";
            MinStockEntry.Text = part?.MinStockAlert?.ToString() ?? string.Empty;
            StockList.ItemsSource = Stocks;
            TxList.ItemsSource = Transactions;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                // Stock by warehouse
                var levels = await _db.GetStockLevelsForPartAsync(_part.Id).ConfigureAwait(false);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Stocks.Clear();
                    foreach (var (wid, wname, qty, min, max) in levels)
                        Stocks.Add(new StockRow(_db, _part.Id, wid, wname, qty, min, max));
                });

                // Recent transactions
                var dt = await _db.GetInventoryTransactionsForPartAsync(_part.Id, take: 200).ConfigureAwait(false);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Transactions.Clear();
                    foreach (System.Data.DataRow r in dt.Rows)
                    {
                        var row = new TxRow
                        {
                            Date = r.Table.Columns.Contains("transaction_date") && r["transaction_date"] != DBNull.Value ? Convert.ToDateTime(r["transaction_date"]).ToString("yyyy-MM-dd HH:mm") : string.Empty,
                            Type = r["transaction_type"]?.ToString() ?? string.Empty,
                            Qty = r.Table.Columns.Contains("quantity") && r["quantity"] != DBNull.Value ? Convert.ToInt32(r["quantity"]) : 0,
                            Note = r["note"]?.ToString() ?? r["related_document"]?.ToString() ?? string.Empty
                        };
                        Transactions.Add(row);
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async void OnSaveMinClicked(object? sender, EventArgs e)
        {
            try
            {
                int? min = null;
                if (int.TryParse(MinStockEntry.Text, out int m) && m >= 0) min = m; else MinStockEntry.Text = string.Empty;
                await _db.UpdatePartMinStockAlertAsync(_part.Id, min).ConfigureAwait(false);
                _tcs.TrySetResult(true);
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }
        /// <summary>
        /// Represents the Stock Row.
        /// </summary>

        public sealed class StockRow
        {
            private readonly DatabaseService _db;
            private readonly int _partId;
            /// <summary>
            /// Gets or sets the warehouse id.
            /// </summary>
            public int WarehouseId { get; }
            /// <summary>
            /// Gets or sets the warehouse name.
            /// </summary>
            public string WarehouseName { get; }
            /// <summary>
            /// Gets or sets the quantity.
            /// </summary>
            public int Quantity { get; }
            /// <summary>
            /// Gets or sets the min.
            /// </summary>
            public int? Min { get; set; }
            /// <summary>
            /// Gets or sets the max.
            /// </summary>
            public int? Max { get; set; }
            /// <summary>
            /// Gets or sets the save threshold command.
            /// </summary>
            public Command SaveThresholdCommand { get; }
            /// <summary>
            /// Initializes a new instance of the StockRow class.
            /// </summary>

            public StockRow(DatabaseService db, int partId, int warehouseId, string warehouseName, int qty, int? min, int? max)
            {
                _db = db; _partId = partId; WarehouseId = warehouseId; WarehouseName = warehouseName; Quantity = qty; Min = min; Max = max;
                SaveThresholdCommand = new Command(async () => await SaveAsync());
            }

            private async Task SaveAsync()
            {
                try
                {
                    await _db.UpdateStockThresholdsAsync(_partId, WarehouseId, Min, Max).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Application.Current?.MainPage?.DisplayAlert("Greška", ex.Message, "OK");
                }
            }
        }
        /// <summary>
        /// Represents the Tx Row.
        /// </summary>

        public sealed class TxRow
        {
            /// <summary>
            /// Gets or sets the date.
            /// </summary>
            public string Date { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the type.
            /// </summary>
            public string Type { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the qty.
            /// </summary>
            public int Qty { get; set; }
            /// <summary>
            /// Gets or sets the note.
            /// </summary>
            public string Note { get; set; } = string.Empty;
        }
    }
}

