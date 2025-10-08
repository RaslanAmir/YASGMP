using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Status bar view-model displayed along the bottom edge of the shell.</summary>
public partial class ShellStatusBarViewModel : ObservableObject
{
    private readonly TimeProvider _timeProvider;
    private readonly DispatcherTimer _utcTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellStatusBarViewModel"/> class.
    /// </summary>
    public ShellStatusBarViewModel(
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        UtcTime = FormatUtc(_timeProvider.GetUtcNow());

        _utcTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _utcTimer.Tick += (_, _) => UtcTime = FormatUtc(_timeProvider.GetUtcNow());
        _utcTimer.Start();
    }

    /// <summary>Gets or sets the company name displayed in the status bar.</summary>
    [ObservableProperty]
    private string _company = string.Empty;

    /// <summary>Gets or sets the database name surfaced in the status bar.</summary>
    [ObservableProperty]
    private string _database = string.Empty;

    /// <summary>Gets or sets the interactive user label.</summary>
    [ObservableProperty]
    private string _user = string.Empty;

    /// <summary>Gets or sets the active environment descriptor.</summary>
    [ObservableProperty]
    private string _environment = string.Empty;

    /// <summary>Gets or sets the database server host name.</summary>
    [ObservableProperty]
    private string _server = string.Empty;

    /// <summary>Gets or sets the formatted UTC timestamp.</summary>
    [ObservableProperty]
    private string _utcTime = string.Empty;

    /// <summary>Gets or sets the current shell status message presented to the operator.</summary>
    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>Gets or sets the label for the module currently in focus.</summary>
    [ObservableProperty]
    private string _activeModule = string.Empty;

    /// <summary>
    /// Applies shell metadata resolved by the hosting view-model or service.
    /// </summary>
    /// <param name="company">Connected company name.</param>
    /// <param name="environment">Runtime environment descriptor.</param>
    /// <param name="server">Database server host.</param>
    /// <param name="database">Database catalog name.</param>
    /// <param name="user">Authenticated user display name.</param>
    public void UpdateMetadata(string? company, string? environment, string? server, string? database, string? user)
    {
        Company = NormalizeCompany(company);
        Environment = NormalizeEnvironment(environment);
        Server = NormalizeServer(server);
        Database = NormalizeDatabase(database);
        User = NormalizeUser(user);
    }

    private static string FormatUtc(DateTimeOffset timestamp)
        => timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    private static string NormalizeCompany(string? company)
        => string.IsNullOrWhiteSpace(company) ? "YasGMP" : company;

    private static string NormalizeEnvironment(string? environment)
        => string.IsNullOrWhiteSpace(environment) ? "Production" : environment;

    private static string NormalizeServer(string? server)
        => string.IsNullOrWhiteSpace(server) ? "<unknown>" : server;

    private static string NormalizeDatabase(string? database)
        => string.IsNullOrWhiteSpace(database) ? "<unknown>" : database;

    private static string NormalizeUser(string? user)
        => string.IsNullOrWhiteSpace(user) ? "Offline" : user;
}
