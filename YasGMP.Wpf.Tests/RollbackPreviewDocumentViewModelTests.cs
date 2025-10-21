using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Xunit;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class RollbackPreviewDocumentViewModelTests
{
    private static readonly CultureInfo TestCulture = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void Constructor_PopulatesDocumentFields_AndValidSignatureStatus()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = TestCulture;
            CultureInfo.CurrentUICulture = TestCulture;

            var database = new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;");
            var shell = new RecordingShellInteractionService();
            var localization = CreateLocalizationService();
            var audit = CreateAuditEntry(validSignature: true);

            var viewModel = new RollbackPreviewDocumentViewModel(database, shell, localization, audit);

            Assert.Equal("Rollback Preview", viewModel.Title);
            Assert.Equal("UPDATE • machine #42", viewModel.Header);
            Assert.Equal("machine #42", viewModel.EntityDisplay);
            Assert.Equal("tester (#7)", viewModel.UserDisplay);
            Assert.Equal(audit.Timestamp.ToLocalTime().ToString("f", CultureInfo.CurrentCulture), viewModel.TimestampDisplay);
            Assert.Equal(localization["Audit.Rollback.Signature.Valid"], viewModel.SignatureStatus);
            Assert.Equal(Brushes.SeaGreen, viewModel.SignatureBrush);
            Assert.True(viewModel.CanRollback);
            Assert.Equal(localization["Audit.Rollback.Status.Ready"], viewModel.StatusMessage);

            Assert.Equal(audit.SignatureHash, viewModel.SignatureHash);
            Assert.Equal("{\n  \"id\": 1,\n  \"name\": \"before\"\n}", viewModel.OldJson);
            Assert.Equal("{\n  \"id\": 1,\n  \"name\": \"after\"\n}", viewModel.NewJson);

            Assert.Collection(shell.StatusUpdates,
                status => Assert.Equal(localization["Audit.Rollback.Status.Ready"], status));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public async Task RollbackCommand_WhenDatabaseSucceeds_PublishesSuccess()
    {
        var database = new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;");
        var shell = new RecordingShellInteractionService();
        var localization = CreateLocalizationService();
        var audit = CreateAuditEntry(validSignature: true);

        var callCount = 0;
        typeof(DatabaseService)
            .GetProperty("ExecuteNonQueryOverride", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(database, (Func<string, IEnumerable<MySqlConnector.MySqlParameter>?, CancellationToken, Task<int>>)((_, _, _) =>
            {
                callCount++;
                return Task.FromResult(1);
            }));

        var viewModel = new RollbackPreviewDocumentViewModel(database, shell, localization, audit);

        await viewModel.RollbackCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.Equal(1, callCount);
        Assert.False(viewModel.IsBusy);
        var success = string.Format(CultureInfo.CurrentCulture, localization["Audit.Rollback.Status.Success"], viewModel.EntityDisplay);
        Assert.Equal(success, viewModel.StatusMessage);
        Assert.Collection(shell.StatusUpdates,
            status => Assert.Equal(localization["Audit.Rollback.Status.Ready"], status),
            status => Assert.Equal(localization["Audit.Rollback.Status.Submitting"], status),
            status => Assert.Equal(success, status));
    }

    [Fact]
    public async Task RollbackCommand_WhenDatabaseThrows_SetsFailureStatus()
    {
        var database = new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;");
        var shell = new RecordingShellInteractionService();
        var localization = CreateLocalizationService();
        var audit = CreateAuditEntry(validSignature: true);

        typeof(DatabaseService)
            .GetProperty("ExecuteNonQueryOverride", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(database, (Func<string, IEnumerable<MySqlConnector.MySqlParameter>?, CancellationToken, Task<int>>)((_, _, _) =>
                Task.FromException<int>(new InvalidOperationException("rollback failed"))));

        var viewModel = new RollbackPreviewDocumentViewModel(database, shell, localization, audit);

        await viewModel.RollbackCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.False(viewModel.IsBusy);
        var expected = string.Format(CultureInfo.CurrentCulture, localization["Audit.Rollback.Status.Failure"], viewModel.EntityDisplay, "rollback failed");
        Assert.Equal(expected, viewModel.StatusMessage);
        Assert.Collection(shell.StatusUpdates,
            status => Assert.Equal(localization["Audit.Rollback.Status.Ready"], status),
            status => Assert.Equal(localization["Audit.Rollback.Status.Submitting"], status),
            status => Assert.Equal(expected, status));
    }

    private static AuditEntryDto CreateAuditEntry(bool validSignature)
    {
        var timestamp = DateTime.SpecifyKind(new DateTime(2026, 5, 12, 15, 30, 0), DateTimeKind.Utc);
        var payload = "{\"id\":1,\"name\":\"before\"}";
        var updated = "{\"id\":1,\"name\":\"after\"}";
        var note = "manual edit";
        var action = "UPDATE";
        var signaturePayload = $"{action}|{note}|{timestamp:O}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signaturePayload)));

        return new AuditEntryDto
        {
            Id = 128,
            Entity = "machine",
            EntityId = "42",
            Action = action,
            Timestamp = timestamp,
            UserId = 7,
            Username = "tester",
            Note = note,
            OldValue = payload,
            NewValue = updated,
            SignatureHash = validSignature ? hash : "deadbeef",
        };
    }

    private static TestLocalizationService CreateLocalizationService()
        => new TestLocalizationService(new Dictionary<string, string>
        {
            ["Audit.Rollback.Document.Title"] = "Rollback Preview",
            ["Audit.Rollback.Status.Ready"] = "Ready for rollback",
            ["Audit.Rollback.Status.Submitting"] = "Submitting rollback request...",
            ["Audit.Rollback.Status.Success"] = "Rollback requested for {0}.",
            ["Audit.Rollback.Status.Failure"] = "Rollback failed: {0}",
            ["Audit.Rollback.Status.MissingData"] = "Rollback payloads are unavailable for the selected entry.",
            ["Audit.Rollback.Signature.Valid"] = "Signature valid",
            ["Audit.Rollback.Signature.Invalid"] = "Signature invalid",
            ["Audit.Rollback.Signature.Unknown"] = "Signature unavailable",
        });

    private sealed class RecordingShellInteractionService : IShellInteractionService
    {
        public List<string> StatusUpdates { get; } = new();
        public List<DocumentViewModel> ClosedDocuments { get; } = new();
        public List<(DocumentViewModel Document, bool Activate)> OpenedDocuments { get; } = new();
        public InspectorContext? LastInspector { get; private set; }

        public void UpdateStatus(string message)
        {
            if (message is not null)
            {
                StatusUpdates.Add(message);
            }
        }

        public DocumentViewModel OpenDocument(DocumentViewModel document, bool activate = true)
        {
            OpenedDocuments.Add((document, activate));
            return document;
        }

        public void CloseDocument(DocumentViewModel document)
        {
            ClosedDocuments.Add(document);
        }

        public void UpdateInspector(InspectorContext context)
        {
            LastInspector = context;
        }
    }

    private sealed class TestLocalizationService : ILocalizationService
    {
        private readonly IReadOnlyDictionary<string, string> _map;

        public TestLocalizationService(IReadOnlyDictionary<string, string> map)
        {
            _map = map;
        }

        public string CurrentLanguage => "en";

        public event EventHandler? LanguageChanged
        {
            add { }
            remove { }
        }

        public string this[string key] => GetString(key);

        public string GetString(string key)
            => _map.TryGetValue(key, out var value) ? value : key;

        public void SetLanguage(string language)
        {
        }
    }
}
