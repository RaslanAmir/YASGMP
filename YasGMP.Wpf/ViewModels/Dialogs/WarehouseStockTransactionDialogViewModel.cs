using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Dialogs;

/// <summary>
/// View-model backing the warehouse stock transaction dialog.
/// </summary>
public sealed partial class WarehouseStockTransactionDialogViewModel : ObservableObject
{
    private readonly int _warehouseId;
    private readonly string _warehouseName;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly Func<InventoryTransactionRequest, ElectronicSignatureDialogResult, Task> _submitAsync;

    public WarehouseStockTransactionDialogViewModel(
        int warehouseId,
        string warehouseDisplayName,
        InventoryTransactionType type,
        IElectronicSignatureDialogService signatureDialog,
        Func<InventoryTransactionRequest, ElectronicSignatureDialogResult, Task> submitAsync)
    {
        _warehouseId = warehouseId;
        _warehouseName = warehouseDisplayName ?? throw new ArgumentNullException(nameof(warehouseDisplayName));
        TransactionType = type;
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _submitAsync = submitAsync ?? throw new ArgumentNullException(nameof(submitAsync));

        Parts = new ObservableCollection<WarehouseStockPartOption>();
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

    public ObservableCollection<WarehouseStockPartOption> Parts { get; }

    [ObservableProperty]
    private WarehouseStockPartOption? _selectedPart;

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

    public string WarehouseLabel => _warehouseName;

    public InventoryTransactionType TransactionType { get; }

    public bool IsAdjustment => TransactionType == InventoryTransactionType.Adjust;

    public IAsyncRelayCommand ConfirmCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public InventoryTransactionRequest? SubmittedRequest { get; private set; }

    public ElectronicSignatureDialogResult? SubmittedSignature { get; private set; }

    public event EventHandler<bool>? CloseRequested;

    public void LoadParts(IEnumerable<WarehouseStockPartOption> parts)
    {
        if (parts is null)
        {
            throw new ArgumentNullException(nameof(parts));
        }

        Parts.Clear();
        foreach (var option in parts.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            Parts.Add(option);
        }

        SelectedPart = Parts.FirstOrDefault();
    }

    private bool CanConfirm()
        => !IsBusy && SelectedPart is not null && TryParseQuantity(out _);

    private async Task ConfirmAsync()
    {
        ErrorMessage = null;
        if (SelectedPart is null)
        {
            ErrorMessage = "Select a part.";
            return;
        }

        if (!TryParseQuantity(out var quantity))
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
            var context = new ElectronicSignatureContext("inventory_transactions", SelectedPart.Value.PartId);
            var signature = await _signatureDialog.CaptureSignatureAsync(context).ConfigureAwait(false);
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

            await _submitAsync(request.Value, signature).ConfigureAwait(false);
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
        quantity = 0;
        if (string.IsNullOrWhiteSpace(QuantityText))
        {
            return false;
        }

        if (!int.TryParse(QuantityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity))
        {
            return false;
        }

        return TransactionType == InventoryTransactionType.Adjust ? quantity != 0 : quantity > 0;
    }

    private InventoryTransactionRequest? BuildRequest(int quantity)
    {
        if (SelectedPart is null)
        {
            return null;
        }

        return TransactionType switch
        {
            InventoryTransactionType.Receive => InventoryTransactionRequest.CreateReceive(
                SelectedPart.Value.PartId,
                _warehouseId,
                quantity,
                Document,
                Note),
            InventoryTransactionType.Issue => InventoryTransactionRequest.CreateIssue(
                SelectedPart.Value.PartId,
                _warehouseId,
                quantity,
                Document,
                Note),
            InventoryTransactionType.Adjust => InventoryTransactionRequest.CreateAdjustment(
                SelectedPart.Value.PartId,
                _warehouseId,
                quantity,
                Document,
                Note,
                AdjustmentReason),
            _ => null
        };
    }

    partial void OnSelectedPartChanged(WarehouseStockPartOption? value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    partial void OnQuantityTextChanged(string value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}

public readonly record struct WarehouseStockPartOption(
    int PartId,
    string DisplayName,
    string PartCode,
    int CurrentQuantity,
    int? Minimum,
    int? Maximum);
