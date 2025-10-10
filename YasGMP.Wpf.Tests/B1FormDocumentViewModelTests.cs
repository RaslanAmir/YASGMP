using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class B1FormDocumentViewModelTests
{
    [Fact]
    public async Task SaveAsync_PreservesCancellationMessage()
    {
        const string cancellationMessage = "Save cancelled by integration";
        var localization = new StubLocalizationService();
        var viewModel = new TestDocumentViewModel(
            new NullCflDialogService(),
            new PassiveShellInteractionService(),
            new PassiveModuleNavigationService(),
            localization,
            cancellationMessage);

        viewModel.Mode = FormMode.Add;
        viewModel.IsDirty = true;
        viewModel.StatusMessage = "Pending save";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(cancellationMessage, viewModel.StatusMessage);
        Assert.True(viewModel.IsDirty);
        Assert.Equal(FormMode.Add, viewModel.Mode);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(cancellationMessage, viewModel.StatusMessage);
        Assert.True(viewModel.IsDirty);
        Assert.Equal(FormMode.Add, viewModel.Mode);
    }

    [Fact]
    public void EnterViewAndUpdateRemainDisabledWithoutSelection()
    {
        var localization = new StubLocalizationService();
        var viewModel = new TestDocumentViewModel(
            new NullCflDialogService(),
            new PassiveShellInteractionService(),
            new PassiveModuleNavigationService(),
            localization,
            "noop");

        viewModel.Mode = FormMode.Find;
        viewModel.SelectedRecord = null;

        Assert.False(viewModel.EnterViewModeCommand.CanExecute(null));
        Assert.False(viewModel.EnterUpdateModeCommand.CanExecute(null));

        var record = new ModuleRecord("1", "First");
        viewModel.Records.Add(record);
        viewModel.SelectedRecord = record;

        Assert.True(viewModel.EnterViewModeCommand.CanExecute(null));
        Assert.True(viewModel.EnterUpdateModeCommand.CanExecute(null));
    }

    [Fact]
    public void EnterAddModeDisabledWhileDirtyUntilReset()
    {
        var localization = new StubLocalizationService();
        var viewModel = new TestDocumentViewModel(
            new NullCflDialogService(),
            new PassiveShellInteractionService(),
            new PassiveModuleNavigationService(),
            localization,
            "noop");

        Assert.True(viewModel.EnterAddModeCommand.CanExecute(null));

        viewModel.MarkAsDirty();

        Assert.False(viewModel.EnterAddModeCommand.CanExecute(null));

        viewModel.InvokeResetDirty();

        Assert.True(viewModel.EnterAddModeCommand.CanExecute(null));
    }

    [Fact]
    public void ModeTransitionsBlockedWhenValidationErrorsPresent()
    {
        var localization = new StubLocalizationService();
        var viewModel = new TestDocumentViewModel(
            new NullCflDialogService(),
            new PassiveShellInteractionService(),
            new PassiveModuleNavigationService(),
            localization,
            "noop");

        viewModel.Mode = FormMode.Find;
        var record = new ModuleRecord("1", "First");
        viewModel.Records.Add(record);
        viewModel.SelectedRecord = record;

        Assert.True(viewModel.EnterAddModeCommand.CanExecute(null));
        Assert.True(viewModel.EnterViewModeCommand.CanExecute(null));
        Assert.True(viewModel.EnterUpdateModeCommand.CanExecute(null));

        viewModel.ValidationMessages.Add("Validation failed");

        Assert.True(viewModel.HasValidationErrors);
        Assert.False(viewModel.EnterAddModeCommand.CanExecute(null));
        Assert.False(viewModel.EnterViewModeCommand.CanExecute(null));
        Assert.False(viewModel.EnterUpdateModeCommand.CanExecute(null));
    }

    private sealed class TestDocumentViewModel : B1FormDocumentViewModel
    {
        private readonly string _cancellationMessage;

        public TestDocumentViewModel(
            ICflDialogService cflDialogService,
            IShellInteractionService shellInteraction,
            IModuleNavigationService moduleNavigation,
            ILocalizationService localization,
            string cancellationMessage)
            : base("Test", "Test", localization, cflDialogService, shellInteraction, moduleNavigation)
        {
            _cancellationMessage = cancellationMessage;
        }

        public void MarkAsDirty() => MarkDirty();

        public void InvokeResetDirty() => ResetDirty();

        protected override Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
            => Task.FromResult<IReadOnlyList<ModuleRecord>>(Array.Empty<ModuleRecord>());

        protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
            => Array.Empty<ModuleRecord>();

        protected override Task<bool> OnSaveAsync()
        {
            StatusMessage = _cancellationMessage;
            return Task.FromResult(false);
        }
    }

    private sealed class NullCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request)
            => Task.FromResult<CflResult?>(null);
    }

    private sealed class PassiveShellInteractionService : IShellInteractionService
    {
        public void UpdateInspector(InspectorContext context)
        {
        }

        public void UpdateStatus(string message)
        {
        }
    }

    private sealed class PassiveModuleNavigationService : IModuleNavigationService
    {
        public void Activate(ModuleDocumentViewModel document)
        {
        }

        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();
    }

    private sealed class StubLocalizationService : ILocalizationService
    {
        public string CurrentLanguage => "en";

        public event EventHandler? LanguageChanged
        {
            add { }
            remove { }
        }

        public string GetString(string key)
            => key switch
            {
                "Module.Status.Ready" => "Ready",
                "Module.Status.Loading" => "Loading {0} records...",
                "Module.Status.Loaded" => "Loaded {0} record(s).",
                "Module.Status.OfflineFallback" => "Offline data loaded because: {0}",
                "Module.Status.NotInEditMode" => "{0} is not in Add/Update mode.",
                "Module.Status.ValidationIssues" => "{0} has {1} validation issue(s).",
                "Module.Status.SaveSuccess" => "{0} saved successfully.",
                "Module.Status.NoChanges" => "No changes to save for {0}.",
                "Module.Status.SaveFailure" => "Failed to save {0}: {1}",
                "Module.Status.Cancelled" => "{0} changes cancelled.",
                "Module.Status.Filtered" => "Filtered {0} by \"{1}\".",
                _ => key
            };

        public void SetLanguage(string language)
        {
        }
    }
}
