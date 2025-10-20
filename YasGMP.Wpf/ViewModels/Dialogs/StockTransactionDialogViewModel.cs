using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Dialogs;

/// <summary>
/// View-model that backs the WPF stock transaction dialog.
/// </summary>
public partial class StockTransactionDialogViewModel : ObservableObject
{
    private readonly int _partId;
    private readonly string _partName;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly Func<InventoryTransactionRequest, ElectronicSignatureDialogResult, Task> _submitAsync;

    public StockTransactionDialogViewModel(
        int partId,
        string partDisplayName,
        InventoryTransactionType type,
        IElectronicSignatureDialogService signatureDialog,
        Func<InventoryTransactionRequest, ElectronicSignatureDialogResult, Task> submitAsync)
    {
        _partId = partId;
        _partName = partDisplayName ?? throw new ArgumentNullException(nameof(partDisplayName));
        TransactionType = type;
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _submitAsync = submitAsync ?? throw new ArgumentNullException(nameof(submitAsync));

        Warehouses = new ObservableCollection<Warehouse>();
        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, CanConfirm);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, false));

        DialogTitle = type switch
        {
            InventoryTransactionType.Receive => "Receive Stock",
            InventoryTransactionType.Issue => "Issue Stock",
            InventoryTransactionType.Adjust => "Adjust Stock",
            _ => "Stock Transaction"
        };
    }

    public InventoryTransactionType TransactionType { get; }

    public bool IsAdjustment => TransactionType == InventoryTransactionType.Adjust;

    public ObservableCollection<Warehouse> Warehouses { get; }

    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    [ObservableProperty]
    private string _quantityText = string.Empty;

    [ObservableProperty]
    private string? _document;

    [ObservableProperty]
    private string? _note;

    [ObservableProperty]
    private string? _adjustmentReason;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _dialogTitle;

    public string PartLabel => _partName;

    public IAsyncRelayCommand ConfirmCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public InventoryTransactionRequest? SubmittedRequest { get; private set; }

    public ElectronicSignatureDialogResult? SubmittedSignature { get; private set; }

    public event EventHandler<bool>? CloseRequested;

    public void LoadWarehouses(System.Collections.Generic.IEnumerable<Warehouse> warehouses)
    {
        Warehouses.Clear();
        foreach (var warehouse in warehouses)
        {
            Warehouses.Add(warehouse);
        }
    }

    private bool CanConfirm()
        => !IsBusy && SelectedWarehouse is not null && TryParseQuantity(out _);

    private async Task ConfirmAsync()
    {
        ErrorMessage = null;
        if (SelectedWarehouse is null)
        {
            ErrorMessage = "Select a warehouse.";
            return;
        }

        if (!TryParseQuantity(out int quantity))
        {
            ErrorMessage = TransactionType == InventoryTransactionType.Adjust
                ? "Enter a non-zero quantity."
                : "Enter a quantity greater than zero.";
            return;
        }

        var request = BuildRequest(quantity);
        if (request is null)
        {
            ErrorMessage = "Unable to prepare the inventory request.";
            return;
        }

        IsBusy = true;
        ConfirmCommand.NotifyCanExecuteChanged();
        try
        {
            var context = new ElectronicSignatureContext("inventory_transactions", _partId);
            var signature = await _signatureDialog.CaptureSignatureAsync(context);
            if (signature is null)
            {
                ErrorMessage = "Signature capture cancelled.";
                return;
            }

            if (signature.Signature is null)
            {
                ErrorMessage = "Signature payload missing.";
                return;
            }

            await _submitAsync(request.Value, signature);

            SubmittedRequest = request;
            SubmittedSignature = signature;
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            ConfirmCommand.NotifyCanExecuteChanged();
        }
    }

    private bool TryParseQuantity(out int quantity)
    {
        if (string.IsNullOrWhiteSpace(QuantityText))
        {
            quantity = 0;
            return false;
        }

        if (!int.TryParse(QuantityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity))
        {
            return false;
        }

        return TransactionType == InventoryTransactionType.Adjust
            ? quantity != 0
            : quantity > 0;
    }

    private InventoryTransactionRequest? BuildRequest(int quantity)
    {
        if (SelectedWarehouse is null)
        {
            return null;
        }

        return TransactionType switch
        {
            InventoryTransactionType.Receive => InventoryTransactionRequest.CreateReceive(
                _partId,
                SelectedWarehouse.Id,
                quantity,
                Document,
                Note),
            InventoryTransactionType.Issue => InventoryTransactionRequest.CreateIssue(
                _partId,
                SelectedWarehouse.Id,
                quantity,
                Document,
                Note),
            InventoryTransactionType.Adjust => InventoryTransactionRequest.CreateAdjustment(
                _partId,
                SelectedWarehouse.Id,
                quantity,
                Document,
                Note,
                AdjustmentReason),
            _ => null
        };
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    partial void OnQuantityTextChanged(string value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}
