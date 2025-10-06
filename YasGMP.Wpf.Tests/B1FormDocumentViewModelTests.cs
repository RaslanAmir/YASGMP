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
        var viewModel = new TestDocumentViewModel(
            new NullCflDialogService(),
            new PassiveShellInteractionService(),
            new PassiveModuleNavigationService(),
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

    private sealed class TestDocumentViewModel : B1FormDocumentViewModel
    {
        private readonly string _cancellationMessage;

        public TestDocumentViewModel(
            ICflDialogService cflDialogService,
            IShellInteractionService shellInteraction,
            IModuleNavigationService moduleNavigation,
            string cancellationMessage)
            : base("Test", "Test", cflDialogService, shellInteraction, moduleNavigation)
        {
            _cancellationMessage = cancellationMessage;
        }

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
}

