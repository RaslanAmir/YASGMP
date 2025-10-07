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
    private readonly IUserSession _userSession;
    private readonly DatabaseOptions _databaseOptions;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly TimeProvider _timeProvider;
    private readonly DispatcherTimer _utcTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellStatusBarViewModel"/> class.
    /// </summary>
    public ShellStatusBarViewModel(
        IUserSession userSession,
        DatabaseOptions databaseOptions,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        TimeProvider timeProvider)
    {
        _userSession = userSession;
        _databaseOptions = databaseOptions;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _timeProvider = timeProvider;

        Company = ResolveCompany();
        Environment = ResolveEnvironment();
        Server = string.IsNullOrWhiteSpace(_databaseOptions.Server) ? "<unknown>" : _databaseOptions.Server;
        Database = string.IsNullOrWhiteSpace(_databaseOptions.Database) ? "<unknown>" : _databaseOptions.Database;
        User = ResolveUser();
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

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _activeModule = string.Empty;

    private string ResolveCompany()
    {
        return _configuration["Shell:Company"]
               ?? _configuration["Company"]
               ?? _configuration["AppTitle"]
               ?? "YasGMP";
    }

    private string ResolveEnvironment()
    {
        return _configuration["Shell:Environment"]
               ?? _configuration["Environment"]
               ?? _hostEnvironment.EnvironmentName
               ?? "Production";
    }

    private string ResolveUser()
    {
        return _userSession.FullName
               ?? _userSession.Username
               ?? "Offline";
    }

    private static string FormatUtc(DateTimeOffset timestamp)
        => timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
}
