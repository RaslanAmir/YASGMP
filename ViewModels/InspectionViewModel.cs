using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using YasGMP.Models;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za Inspections – omogućuje filtriranje po datumu, vrsti i rezultatu te izvoz sažetaka.
    /// </summary>
    public class InspectionViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Inspection> _inspections = new();
        private ObservableCollection<Inspection> _filteredInspections = new();
        private List<InspectionSummary> _currentSummaries = new();
        private DateTime? _filterFromDate;
        private DateTime? _filterToDate;
        private string? _inspectionTypeFilter;
        private string? _inspectionResultFilter;
        private string? _statusMessage;
        private string? _lastExportPath;

        /// <summary>
        /// Inicijalizira novu instancu i primjenjuje početne filtere.
        /// </summary>
        public InspectionViewModel()
        {
            ApplyFilters();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Sve učitane inspekcije.</summary>
        public ObservableCollection<Inspection> Inspections
        {
            get => _inspections;
            set
            {
                _inspections = value ?? new ObservableCollection<Inspection>();
                OnPropertyChanged();
                OnPropertyChanged(nameof(AvailableInspectionTypes));
                OnPropertyChanged(nameof(AvailableResults));
                ApplyFilters();
            }
        }

        /// <summary>Filtrirani prikaz inspekcija za UI.</summary>
        public ObservableCollection<Inspection> FilteredInspections
        {
            get => _filteredInspections;
            private set
            {
                _filteredInspections = value ?? new ObservableCollection<Inspection>();
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredInspectionCount));
            }
        }

        /// <summary>Trenutni sažetak (broj po tipu/rezultatu).</summary>
        public IReadOnlyList<InspectionSummary> CurrentSummaries => _currentSummaries;

        /// <summary>Broj inspekcija u filtriranom prikazu.</summary>
        public int FilteredInspectionCount => FilteredInspections.Count;

        /// <summary>Početni datum filtera (uključivo).</summary>
        public DateTime? FilterFromDate
        {
            get => _filterFromDate;
            set
            {
                var normalized = NormalizeDate(value);
                if (_filterFromDate != normalized)
                {
                    _filterFromDate = normalized;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        /// <summary>Završni datum filtera (uključivo).</summary>
        public DateTime? FilterToDate
        {
            get => _filterToDate;
            set
            {
                var normalized = NormalizeDate(value);
                if (_filterToDate != normalized)
                {
                    _filterToDate = normalized;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        /// <summary>Filter po vrsti inspekcije.</summary>
        public string? InspectionTypeFilter
        {
            get => _inspectionTypeFilter;
            set
            {
                var normalized = NormalizeFilter(value);
                if (!string.Equals(_inspectionTypeFilter, normalized, StringComparison.Ordinal))
                {
                    _inspectionTypeFilter = normalized;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        /// <summary>Filter po rezultatu (prolaz, pao, napomena...).</summary>
        public string? ResultFilter
        {
            get => _inspectionResultFilter;
            set
            {
                var normalized = NormalizeFilter(value);
                if (!string.Equals(_inspectionResultFilter, normalized, StringComparison.Ordinal))
                {
                    _inspectionResultFilter = normalized;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        /// <summary>Indikator da su primijenjeni neki filteri.</summary>
        public bool HasActiveFilters =>
            FilterFromDate.HasValue || FilterToDate.HasValue ||
            !string.IsNullOrWhiteSpace(InspectionTypeFilter) ||
            !string.IsNullOrWhiteSpace(ResultFilter);

        /// <summary>Poruka o statusu (za UI).</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            private set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Zadnji generirani put izvoza.</summary>
        public string? LastExportPath
        {
            get => _lastExportPath;
            private set
            {
                _lastExportPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Dostupne vrste inspekcija (iz izvora podataka).</summary>
        public IReadOnlyList<string> AvailableInspectionTypes => Inspections
            .Select(i => i?.Type ?? string.Empty)
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(type => type, StringComparer.OrdinalIgnoreCase)
            .ToList();

        /// <summary>Dostupni rezultati inspekcija (iz izvora podataka).</summary>
        public IReadOnlyList<string> AvailableResults => Inspections
            .Select(i => i?.Result ?? string.Empty)
            .Where(result => !string.IsNullOrWhiteSpace(result))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(result => result, StringComparer.OrdinalIgnoreCase)
            .ToList();

        /// <summary>
        /// Primjenjuje filtriranje po datumima, vrsti i rezultatu.
        /// </summary>
        public void ApplyFilters()
        {
            IEnumerable<Inspection> query = Inspections;

            var from = FilterFromDate;
            var to = FilterToDate;
            if (from.HasValue && to.HasValue && from > to)
            {
                (from, to) = (to, from);
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                query = query.Where(i => (i?.InspectionDate ?? default) >= fromDate);
            }

            if (to.HasValue)
            {
                var toInclusive = to.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => (i?.InspectionDate ?? default) <= toInclusive);
            }

            if (!string.IsNullOrWhiteSpace(InspectionTypeFilter))
            {
                query = query.Where(i => string.Equals(i?.Type, InspectionTypeFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(ResultFilter))
            {
                query = query.Where(i => string.Equals(i?.Result, ResultFilter, StringComparison.OrdinalIgnoreCase));
            }

            var ordered = query
                .Where(i => i is not null)
                .OrderByDescending(i => i!.InspectionDate)
                .ToList();

            FilteredInspections = new ObservableCollection<Inspection>(ordered);

            StatusMessage = FilteredInspections.Count == Inspections.Count
                ? $"Prikazano {FilteredInspections.Count} inspekcija."
                : $"Prikazano {FilteredInspections.Count} od {Inspections.Count} inspekcija.";

            OnPropertyChanged(nameof(HasActiveFilters));
            UpdateSummaries();
        }

        /// <summary>
        /// Briše sve filtere i prikazuje cijeli popis.
        /// </summary>
        public void ClearFilters()
        {
            _filterFromDate = null;
            _filterToDate = null;
            _inspectionTypeFilter = null;
            _inspectionResultFilter = null;

            OnPropertyChanged(nameof(FilterFromDate));
            OnPropertyChanged(nameof(FilterToDate));
            OnPropertyChanged(nameof(InspectionTypeFilter));
            OnPropertyChanged(nameof(ResultFilter));

            ApplyFilters();
        }

        /// <summary>
        /// Izvoz sažetka u CSV datoteku (UTF-8, bez BOM).
        /// </summary>
        /// <param name="directory">Opcionalna ciljna mapa (ako nije zadana koristi se AppData).</param>
        /// <param name="token">Token za otkazivanje.</param>
        /// <returns>Put do generirane datoteke.</returns>
        public async Task<string> ExportSummaryToCsvAsync(string? directory = null, CancellationToken token = default)
        {
            var summaries = CurrentSummaries.ToList();
            var exportDirectory = EnsureExportDirectory(directory);
            var filePath = Path.Combine(exportDirectory, $"InspectionSummary_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            await writer.WriteLineAsync("InspectionType,Result,Count,FirstInspection,LastInspection").ConfigureAwait(false);

            foreach (var summary in summaries)
            {
                token.ThrowIfCancellationRequested();

                var line = string.Join(',',
                    Quote(summary.InspectionType),
                    Quote(summary.Result),
                    summary.Count.ToString(CultureInfo.InvariantCulture),
                    Quote(FormatDate(summary.FirstInspection)),
                    Quote(FormatDate(summary.LastInspection)));

                await writer.WriteLineAsync(line).ConfigureAwait(false);
            }

            await writer.FlushAsync().ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);

            LastExportPath = filePath;
            StatusMessage = summaries.Count == 0
                ? "Sažetak izvezen (nema zapisa u filtriranom prikazu)."
                : $"Sažetak izvezen u {filePath}.";

            return filePath;
        }

        /// <summary>
        /// Izvoz sažetka u JSON (strukturirani pregled s aktivnim filtrima).
        /// </summary>
        /// <param name="directory">Opcionalna ciljna mapa (ako nije zadana koristi se AppData).</param>
        /// <param name="token">Token za otkazivanje.</param>
        /// <returns>Put do generirane datoteke.</returns>
        public async Task<string> ExportSummaryToJsonAsync(string? directory = null, CancellationToken token = default)
        {
            var summaries = CurrentSummaries.ToList();
            var exportDirectory = EnsureExportDirectory(directory);
            var filePath = Path.Combine(exportDirectory, $"InspectionSummary_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            var payload = new InspectionSummaryExport(
                DateTime.UtcNow,
                new FilterSnapshot(FilterFromDate, FilterToDate, InspectionTypeFilter, ResultFilter),
                FilteredInspections.Count,
                Inspections.Count,
                summaries);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(stream, payload, options, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);

            LastExportPath = filePath;
            StatusMessage = summaries.Count == 0
                ? "Sažetak izvezen (nema zapisa u filtriranom prikazu)."
                : $"Sažetak izvezen u {filePath}.";

            return filePath;
        }

        private void UpdateSummaries()
        {
            var summary = FilteredInspections
                .GroupBy(i => (Type: i?.Type ?? string.Empty, Result: i?.Result ?? string.Empty))
                .Select(g =>
                {
                    var dates = g
                        .Select(i => i?.InspectionDate ?? default)
                        .Where(d => d != default)
                        .Select(d => d.Date)
                        .ToList();

                    DateTime? first = dates.Count == 0 ? null : dates.Min();
                    DateTime? last = dates.Count == 0 ? null : dates.Max();

                    var type = string.IsNullOrWhiteSpace(g.Key.Type) ? "N/A" : g.Key.Type;
                    var result = string.IsNullOrWhiteSpace(g.Key.Result) ? "N/A" : g.Key.Result;

                    return new InspectionSummary(type, result, g.Count(), first, last);
                })
                .OrderBy(s => s.InspectionType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Result, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _currentSummaries = summary;
            OnPropertyChanged(nameof(CurrentSummaries));
        }

        private static DateTime? NormalizeDate(DateTime? date) => date?.Date;

        private static string? NormalizeFilter(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static string FormatDate(DateTime? date) => date.HasValue
            ? date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Empty;

        private static string Quote(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        private static string EnsureExportDirectory(string? directory)
        {
            var path = directory;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = TryGetDefaultExportDirectory();
            }

            Directory.CreateDirectory(path);
            return path;
        }

        private static string TryGetDefaultExportDirectory()
        {
            try
            {
                return Path.Combine(FileSystem.Current.AppDataDirectory, "Exports", "Inspections");
            }
            catch
            {
                var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YasGMP", "Exports", "Inspections");
                return fallback;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// DTO za agregirani sažetak inspekcija.
        /// </summary>
        public sealed record InspectionSummary(
            string InspectionType,
            string Result,
            int Count,
            DateTime? FirstInspection,
            DateTime? LastInspection);

        private sealed record FilterSnapshot(DateTime? From, DateTime? To, string? Type, string? Result);

        private sealed record InspectionSummaryExport(
            DateTime GeneratedUtc,
            FilterSnapshot Filters,
            int FilteredCount,
            int TotalCount,
            IReadOnlyList<InspectionSummary> Rows);
    }
}
