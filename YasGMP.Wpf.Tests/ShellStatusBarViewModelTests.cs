using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;
using YasGMP.Wpf.Controls;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Services;
using YasGMP.Wpf.Configuration;
using YasGMP.Models;

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

            AssertResources(control, new Dictionary<string, string>
            {
                ["Shell.StatusBar.Container.ToolTip"] = "Displays YasGMP connection and session metadata.",
                ["Shell.StatusBar.Container.AutomationName"] = "Shell status bar",
                ["Shell.StatusBar.Status.Label"] = "Status:",
                ["Shell.StatusBar.Status.Label.ToolTip"] = "Describes the current shell operation status.",
                ["Shell.StatusBar.Status.Label.AutomationName"] = "Status label",
                ["Shell.StatusBar.Status.Value.ToolTip"] = "Displays the current shell status message.",
                ["Shell.StatusBar.Status.Value.AutomationName"] = "Status value",
                ["Shell.StatusBar.ActiveModule.Label"] = "Active module:",
                ["Shell.StatusBar.ActiveModule.Label.ToolTip"] = "Indicates which module currently has focus.",
                ["Shell.StatusBar.ActiveModule.Label.AutomationName"] = "Active module label",
                ["Shell.StatusBar.ActiveModule.Value.ToolTip"] = "Displays the name of the active module.",
                ["Shell.StatusBar.ActiveModule.Value.AutomationName"] = "Active module value",
                ["Shell.StatusBar.Company.Label"] = "Company:",
                ["Shell.StatusBar.Company.Label.ToolTip"] = "Shows the connected company context.",
                ["Shell.StatusBar.Company.Label.AutomationName"] = "Company label",
                ["Shell.StatusBar.Company.Value.ToolTip"] = "Displays the connected company.",
                ["Shell.StatusBar.Company.Value.AutomationName"] = "Company value",
                ["Shell.StatusBar.Environment.Label"] = "Environment:",
                ["Shell.StatusBar.Environment.Label.ToolTip"] = "Shows the current runtime environment.",
                ["Shell.StatusBar.Environment.Label.AutomationName"] = "Environment label",
                ["Shell.StatusBar.Environment.Value.ToolTip"] = "Displays the active runtime environment.",
                ["Shell.StatusBar.Environment.Value.AutomationName"] = "Environment value",
                ["Shell.StatusBar.Server.Label"] = "Server:",
                ["Shell.StatusBar.Server.Label.ToolTip"] = "Identifies the database server host.",
                ["Shell.StatusBar.Server.Label.AutomationName"] = "Server label",
                ["Shell.StatusBar.Server.Value.ToolTip"] = "Displays the connected database server.",
                ["Shell.StatusBar.Server.Value.AutomationName"] = "Server value",
                ["Shell.StatusBar.Database.Label"] = "Database:",
                ["Shell.StatusBar.Database.Label.ToolTip"] = "Identifies the database name in use.",
                ["Shell.StatusBar.Database.Label.AutomationName"] = "Database label",
                ["Shell.StatusBar.Database.Value.ToolTip"] = "Displays the connected database name.",
                ["Shell.StatusBar.Database.Value.AutomationName"] = "Database value",
                ["Shell.StatusBar.User.Label"] = "User:",
                ["Shell.StatusBar.User.Label.ToolTip"] = "Identifies the authenticated user.",
                ["Shell.StatusBar.User.Label.AutomationName"] = "User label",
                ["Shell.StatusBar.User.Value.ToolTip"] = "Displays the authenticated user.",
                ["Shell.StatusBar.User.Value.AutomationName"] = "User value",
                ["Shell.StatusBar.UtcTime.Label"] = "UTC time:",
                ["Shell.StatusBar.UtcTime.Label.ToolTip"] = "Shows the coordinated universal time.",
                ["Shell.StatusBar.UtcTime.Label.AutomationName"] = "UTC time label",
                ["Shell.StatusBar.UtcTime.Value.ToolTip"] = "Displays the current coordinated universal time.",
                ["Shell.StatusBar.UtcTime.Value.AutomationName"] = "UTC time value"
            });

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

            AssertResources(control, new Dictionary<string, string>
            {
                ["Shell.StatusBar.Container.ToolTip"] = "Prikazuje YasGMP metapodatke veze i sesije.",
                ["Shell.StatusBar.Container.AutomationName"] = "Statusna traka ljuske",
                ["Shell.StatusBar.Status.Label"] = "Status:",
                ["Shell.StatusBar.Status.Label.ToolTip"] = "Opisuje trenutačno stanje ljuske.",
                ["Shell.StatusBar.Status.Label.AutomationName"] = "Oznaka statusa",
                ["Shell.StatusBar.Status.Value.ToolTip"] = "Prikazuje trenutačnu statusnu poruku ljuske.",
                ["Shell.StatusBar.Status.Value.AutomationName"] = "Vrijednost statusa",
                ["Shell.StatusBar.ActiveModule.Label"] = "Aktivni modul:",
                ["Shell.StatusBar.ActiveModule.Label.ToolTip"] = "Označava koji modul je trenutačno u fokusu.",
                ["Shell.StatusBar.ActiveModule.Label.AutomationName"] = "Oznaka aktivnog modula",
                ["Shell.StatusBar.ActiveModule.Value.ToolTip"] = "Prikazuje naziv aktivnog modula.",
                ["Shell.StatusBar.ActiveModule.Value.AutomationName"] = "Vrijednost aktivnog modula",
                ["Shell.StatusBar.Company.Label"] = "Tvrtka:",
                ["Shell.StatusBar.Company.Label.ToolTip"] = "Prikazuje povezani kontekst tvrtke.",
                ["Shell.StatusBar.Company.Label.AutomationName"] = "Oznaka tvrtke",
                ["Shell.StatusBar.Company.Value.ToolTip"] = "Prikazuje povezanu tvrtku.",
                ["Shell.StatusBar.Company.Value.AutomationName"] = "Vrijednost tvrtke",
                ["Shell.StatusBar.Environment.Label"] = "Okruženje:",
                ["Shell.StatusBar.Environment.Label.ToolTip"] = "Prikazuje trenutačno radno okruženje.",
                ["Shell.StatusBar.Environment.Label.AutomationName"] = "Oznaka okruženja",
                ["Shell.StatusBar.Environment.Value.ToolTip"] = "Prikazuje aktivno radno okruženje.",
                ["Shell.StatusBar.Environment.Value.AutomationName"] = "Vrijednost okruženja",
                ["Shell.StatusBar.Server.Label"] = "Poslužitelj:",
                ["Shell.StatusBar.Server.Label.ToolTip"] = "Identificira poslužitelja baze podataka.",
                ["Shell.StatusBar.Server.Label.AutomationName"] = "Oznaka poslužitelja",
                ["Shell.StatusBar.Server.Value.ToolTip"] = "Prikazuje povezani poslužitelj baze podataka.",
                ["Shell.StatusBar.Server.Value.AutomationName"] = "Vrijednost poslužitelja",
                ["Shell.StatusBar.Database.Label"] = "Baza podataka:",
                ["Shell.StatusBar.Database.Label.ToolTip"] = "Identificira korištenu bazu podataka.",
                ["Shell.StatusBar.Database.Label.AutomationName"] = "Oznaka baze podataka",
                ["Shell.StatusBar.Database.Value.ToolTip"] = "Prikazuje naziv povezane baze podataka.",
                ["Shell.StatusBar.Database.Value.AutomationName"] = "Vrijednost baze podataka",
                ["Shell.StatusBar.User.Label"] = "Korisnik:",
                ["Shell.StatusBar.User.Label.ToolTip"] = "Identificira prijavljenog korisnika.",
                ["Shell.StatusBar.User.Label.AutomationName"] = "Oznaka korisnika",
                ["Shell.StatusBar.User.Value.ToolTip"] = "Prikazuje prijavljenog korisnika.",
                ["Shell.StatusBar.User.Value.AutomationName"] = "Vrijednost korisnika",
                ["Shell.StatusBar.UtcTime.Label"] = "UTC vrijeme:",
                ["Shell.StatusBar.UtcTime.Label.ToolTip"] = "Prikazuje koordinirano univerzalno vrijeme.",
                ["Shell.StatusBar.UtcTime.Label.AutomationName"] = "Oznaka UTC vremena",
                ["Shell.StatusBar.UtcTime.Value.ToolTip"] = "Prikazuje trenutačno koordinirano univerzalno vrijeme.",
                ["Shell.StatusBar.UtcTime.Value.AutomationName"] = "Vrijednost UTC vremena"
            });

            await Task.CompletedTask;
        });
    }

    [Fact]
    public void RefreshMetadata_NormalizesAndPopulatesFields()
    {
        var defaultViewModel = CreateViewModel();
        defaultViewModel.RefreshMetadata();

        Assert.Equal("YasGMP", defaultViewModel.Company);
        Assert.Equal("Production", defaultViewModel.Environment);
        Assert.Equal("<unknown>", defaultViewModel.Server);
        Assert.Equal("<unknown>", defaultViewModel.Database);
        Assert.Equal("Offline", defaultViewModel.User);

        var configuredViewModel = CreateViewModel(
            new Dictionary<string, string?>
            {
                ["Shell:Company"] = "Acme Biotech",
                ["Shell:Environment"] = "QA"
            },
            new DatabaseOptions
            {
                Server = "db01",
                Database = "yasgmp"
            },
            new StubUserSession
            {
                FullNameValue = "Jane Doe"
            },
            environmentName: "Sandbox");

        configuredViewModel.RefreshMetadata();

        Assert.Equal("Acme Biotech", configuredViewModel.Company);
        Assert.Equal("QA", configuredViewModel.Environment);
        Assert.Equal("db01", configuredViewModel.Server);
        Assert.Equal("yasgmp", configuredViewModel.Database);
        Assert.Equal("Jane Doe", configuredViewModel.User);
    }

    [Fact]
    public async Task ShellStatusBarControl_AssignsAutomationMetadata()
    {
        await RunOnStaThread(async () =>
        {
            EnsureApplicationResources();

            var control = new ShellStatusBar
            {
                DataContext = CreateMetadataViewModel()
            };

            control.Measure(new Size(1, 1));
            control.Arrange(new Rect(new Size(1, 1)));
            control.UpdateLayout();

            var border = Assert.IsType<Border>(control.Content);
            Assert.Equal("StatusBar.Container", AutomationProperties.GetAutomationId(border));
            Assert.Equal("Shell status bar", AutomationProperties.GetName(border));
            var borderTooltip = ToolTipService.GetToolTip(border) as string;
            Assert.Equal("Displays YasGMP connection and session metadata.", borderTooltip);
            Assert.Equal(borderTooltip, AutomationProperties.GetHelpText(border));

            var textBlocks = FindVisualChildren<TextBlock>(border).ToList();
            Assert.NotEmpty(textBlocks);

                foreach (var textBlock in textBlocks)
                {
                    Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetAutomationId(textBlock)));
                    Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(textBlock)));
                    var tooltip = ToolTipService.GetToolTip(textBlock) as string;
                    Assert.False(string.IsNullOrWhiteSpace(tooltip));
                    Assert.Equal(tooltip, AutomationProperties.GetHelpText(textBlock));
                }

            await Task.CompletedTask;
        });
    }

    public void Dispose()
    {
        _localizationService.SetLanguage(_originalLanguage);
    }

    private static ShellStatusBarViewModel CreateMetadataViewModel()
    {
        return CreateViewModel(
            new Dictionary<string, string?>
            {
                ["Shell:Company"] = "Acme Biotech",
                ["Shell:Environment"] = "QA"
            },
            new DatabaseOptions
            {
                Server = "db01",
                Database = "yasgmp"
            },
            new StubUserSession
            {
                FullNameValue = "Jane Doe"
            },
            environmentName: "QA");
    }

    private static ShellStatusBarViewModel CreateViewModel(
        IDictionary<string, string?>? configurationValues = null,
        DatabaseOptions? databaseOptions = null,
        StubUserSession? userSession = null,
        string? environmentName = null)
    {
        var builder = new ConfigurationBuilder();
        if (configurationValues != null)
        {
            builder.AddInMemoryCollection(configurationValues);
        }

        var configuration = builder.Build();
        var options = databaseOptions ?? new DatabaseOptions();
        var hostEnvironment = new StubHostEnvironment
        {
            EnvironmentName = environmentName ?? string.Empty
        };

        var session = userSession ?? new StubUserSession();

        return new ShellStatusBarViewModel(
            TimeProvider.System,
            configuration,
            options,
            hostEnvironment,
            session);
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

    private static void AssertResources(FrameworkElement control, IDictionary<string, string> expectations)
    {
        foreach (var pair in expectations)
        {
            Assert.Equal(pair.Value, control.TryFindResource(pair.Key) as string);
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T typed)
            {
                yield return typed;
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < childrenCount; i++)
            {
                queue.Enqueue(VisualTreeHelper.GetChild(current, i));
            }
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;

        public string ApplicationName { get; set; } = "YasGMP";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    }

    private sealed class StubUserSession : IUserSession
    {
        public User? CurrentUser => null;

        public int? UserId => null;

        public string? Username => UsernameValue;

        public string? FullName => FullNameValue;

        public string SessionId => "test-session";

        public string? UsernameValue { get; set; }

        public string? FullNameValue { get; set; }
    }
}
