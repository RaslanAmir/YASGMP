using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;

namespace YasGMP.ViewModels;

/// <summary>
/// Contract consumed by desktop shells to surface the shared analytics reporting experience.
/// </summary>
public interface IReportAnalyticsViewModel : INotifyPropertyChanged
{
    /// <summary>Filtered reports exposed to UI consumers.</summary>
    ObservableCollection<Report> Reports { get; }

    /// <summary>Indicates whether an asynchronous pipeline is running.</summary>
    bool IsBusy { get; }

    /// <summary>Last localized status or error message.</summary>
    string? StatusMessage { get; }

    /// <summary>Inclusive start date filter.</summary>
    DateTime? FromDate { get; set; }

    /// <summary>Inclusive end date filter.</summary>
    DateTime? ToDate { get; set; }

    /// <summary>Selected analytics report type filter.</summary>
    string? SelectedReportType { get; set; }

    /// <summary>Selected linked entity type filter.</summary>
    string? SelectedEntityType { get; set; }

    /// <summary>Linked entity identifier filter.</summary>
    string? EntityIdText { get; set; }

    /// <summary>Free-text search term applied across cached reports.</summary>
    string? SearchTerm { get; set; }

    /// <summary>Status filter applied to cached reports.</summary>
    string? StatusFilter { get; set; }

    /// <summary>Available report types surfaced for filtering.</summary>
    IReadOnlyList<string> AvailableReportTypes { get; }

    /// <summary>Available entity types surfaced for filtering.</summary>
    IReadOnlyList<string> AvailableEntityTypes { get; }

    /// <summary>Available status tokens surfaced for filtering.</summary>
    IReadOnlyList<string> AvailableStatuses { get; }

    /// <summary>Loads reports honoring the current filters.</summary>
    IAsyncRelayCommand LoadReportsCommand { get; }

    /// <summary>Generates reports honoring the current filters.</summary>
    IAsyncRelayCommand GenerateReportsCommand { get; }

    /// <summary>Exports the current filtered snapshot to PDF.</summary>
    IAsyncRelayCommand ExportPdfCommand { get; }

    /// <summary>Exports the current filtered snapshot to Excel.</summary>
    IAsyncRelayCommand ExportExcelCommand { get; }

    /// <summary>Applies in-memory filters.</summary>
    IRelayCommand ApplyFiltersCommand { get; }

    /// <summary>Clears active filters.</summary>
    IRelayCommand ClearFiltersCommand { get; }

    /// <summary>Applies in-memory filters explicitly (used by desktop shells).</summary>
    void ApplyFilters();
}
