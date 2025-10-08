using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using YasGMP.Services;
using YasGMP.Wpf.Configuration;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Status bar view-model displayed along the bottom edge of the shell.</summary>
public partial class ShellStatusBarViewModel : ObservableObject
{
    private readonly TimeProvider _timeProvider;
    private readonly DispatcherTimer _utcTimer;
    private readonly IConfiguration _configuration;
    private readonly DatabaseOptions _databaseOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IUserSession _userSession;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellStatusBarViewModel"/> class.
    /// </summary>
    public ShellStatusBarViewModel(
        TimeProvider timeProvider,
        IConfiguration configuration,
        DatabaseOptions databaseOptions,
        IHostEnvironment hostEnvironment,
        IUserSession userSession)
    {
        _timeProvider = timeProvider;
        _configuration = configuration;
        _databaseOptions = databaseOptions;
        _hostEnvironment = hostEnvironment;
        _userSession = userSession;

        RefreshMetadata();
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
    /// Refreshes shell metadata from the injected configuration, environment, and session services.
    /// </summary>
    public void RefreshMetadata()
    {
        Company = NormalizeCompany(ResolveCompany());
        Environment = NormalizeEnvironment(ResolveEnvironment());
        Server = NormalizeServer(_databaseOptions.Server);
        Database = NormalizeDatabase(_databaseOptions.Database);
        User = NormalizeUser(ResolveUser());
    }

    private static string FormatUtc(DateTimeOffset timestamp)
        => timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    private string ResolveCompany()
    {
        var company = _configuration["Shell:Company"]
                       ?? _configuration["Company"]
                       ?? _configuration["AppTitle"]
                       ?? string.Empty;

        return company;
    }

    private string ResolveEnvironment()
    {
        var environment = _configuration["Shell:Environment"]
                          ?? _configuration["Environment"]
                          ?? _hostEnvironment.EnvironmentName
                          ?? string.Empty;

        return environment;
    }

    private string ResolveUser()
    {
        if (!string.IsNullOrWhiteSpace(_userSession.FullName))
        {
            return _userSession.FullName!;
        }

        if (!string.IsNullOrWhiteSpace(_userSession.Username))
        {
            return _userSession.Username!;
        }

        return string.Empty;
    }

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
