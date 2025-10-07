using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Xunit;
using YasGMP.Wpf.Controls;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Tests;

public sealed class ShellStatusBarViewModelTests : IDisposable
{
    private readonly LocalizationService _localizationService;
    private readonly string _originalLanguage;

    public ShellStatusBarViewModelTests()
    {
        _localizationService = new LocalizationService();
        _originalLanguage = _localizationService.CurrentLanguage;
    }

    [Fact]
    public async Task ShellStatusBarControl_EnglishCulture_ExposesExpectedResources()
    {
        await RunOnStaThread(async () =>
        {
            EnsureApplicationResources();

            _localizationService.SetLanguage("en");

            var control = new ShellStatusBar
            {
                DataContext = CreateMetadataViewModel()
            };

            control.Measure(new Size(1, 1));
            control.Arrange(new Rect(new Size(1, 1)));
            control.UpdateLayout();

            Assert.Equal("Shell status bar", control.TryFindResource("Shell.StatusBar.Container.AutomationName"));
            Assert.Equal("Displays YasGMP connection and session metadata.", control.TryFindResource("Shell.StatusBar.Container.ToolTip"));
            Assert.Equal("Status:", control.TryFindResource("Shell.StatusBar.Status.Label"));
            Assert.Equal("Active module:", control.TryFindResource("Shell.StatusBar.ActiveModule.Label"));
            Assert.Equal("Company:", control.TryFindResource("Shell.StatusBar.Company.Label"));
            Assert.Equal("Environment:", control.TryFindResource("Shell.StatusBar.Environment.Label"));
            Assert.Equal("Server:", control.TryFindResource("Shell.StatusBar.Server.Label"));
            Assert.Equal("Database:", control.TryFindResource("Shell.StatusBar.Database.Label"));
            Assert.Equal("User:", control.TryFindResource("Shell.StatusBar.User.Label"));
            Assert.Equal("UTC time:", control.TryFindResource("Shell.StatusBar.UtcTime.Label"));

            await Task.CompletedTask;
        });
    }

    [Fact]
    public async Task ShellStatusBarControl_CroatianCulture_ExposesExpectedResources()
    {
        await RunOnStaThread(async () =>
        {
            EnsureApplicationResources();

            _localizationService.SetLanguage("hr");

            var control = new ShellStatusBar
            {
                DataContext = CreateMetadataViewModel()
            };

            control.Measure(new Size(1, 1));
            control.Arrange(new Rect(new Size(1, 1)));
            control.UpdateLayout();

            Assert.Equal("Statusna traka ljuske", control.TryFindResource("Shell.StatusBar.Container.AutomationName"));
            Assert.Equal("Prikazuje YasGMP metapodatke veze i sesije.", control.TryFindResource("Shell.StatusBar.Container.ToolTip"));
            Assert.Equal("Status:", control.TryFindResource("Shell.StatusBar.Status.Label"));
            Assert.Equal("Aktivni modul:", control.TryFindResource("Shell.StatusBar.ActiveModule.Label"));
            Assert.Equal("Tvrtka:", control.TryFindResource("Shell.StatusBar.Company.Label"));
            Assert.Equal("Okruženje:", control.TryFindResource("Shell.StatusBar.Environment.Label"));
            Assert.Equal("Poslužitelj:", control.TryFindResource("Shell.StatusBar.Server.Label"));
            Assert.Equal("Baza podataka:", control.TryFindResource("Shell.StatusBar.Database.Label"));
            Assert.Equal("Korisnik:", control.TryFindResource("Shell.StatusBar.User.Label"));
            Assert.Equal("UTC vrijeme:", control.TryFindResource("Shell.StatusBar.UtcTime.Label"));

            await Task.CompletedTask;
        });
    }

    [Fact]
    public void UpdateMetadata_NormalizesAndPopulatesFields()
    {
        var viewModel = new ShellStatusBarViewModel(TimeProvider.System);

        viewModel.UpdateMetadata(null, null, null, null, null);

        Assert.Equal("YasGMP", viewModel.Company);
        Assert.Equal("Production", viewModel.Environment);
        Assert.Equal("<unknown>", viewModel.Server);
        Assert.Equal("<unknown>", viewModel.Database);
        Assert.Equal("Offline", viewModel.User);

        viewModel.UpdateMetadata("Acme Biotech", "QA", "db01", "yasgmp", "Jane Doe");

        Assert.Equal("Acme Biotech", viewModel.Company);
        Assert.Equal("QA", viewModel.Environment);
        Assert.Equal("db01", viewModel.Server);
        Assert.Equal("yasgmp", viewModel.Database);
        Assert.Equal("Jane Doe", viewModel.User);
    }

    public void Dispose()
    {
        _localizationService.SetLanguage(_originalLanguage);
    }

    private static ShellStatusBarViewModel CreateMetadataViewModel()
    {
        var viewModel = new ShellStatusBarViewModel(TimeProvider.System);
        viewModel.UpdateMetadata("Acme Biotech", "QA", "db01", "yasgmp", "Jane Doe");
        return viewModel;
    }

    private static void EnsureApplicationResources()
    {
        if (Application.Current is null)
        {
            _ = new Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
        }

        var mergedDictionaries = Application.Current!.Resources.MergedDictionaries;
        var shellDictionaryUri = new Uri("pack://application:,,,/YasGMP.Wpf;component/Resources/Strings.xaml", UriKind.Absolute);
        if (!mergedDictionaries.Any(d => d.Source == shellDictionaryUri))
        {
            mergedDictionaries.Add(new ResourceDictionary
            {
                Source = shellDictionaryUri
            });
        }
    }

    private static Task RunOnStaThread(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<object?>();
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
                var dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await action().ConfigureAwait(true);
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    finally
                    {
                        dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                    }
                });

                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }
}
