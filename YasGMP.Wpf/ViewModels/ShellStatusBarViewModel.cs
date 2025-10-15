using System;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using YasGMP.Services;
using YasGMP.Wpf.Configuration;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Status bar view-model displayed along the bottom edge of the shell.</summary>
public partial class ShellStatusBarViewModel : ObservableObject
{
    private const string CompanyFallbackKey = "Shell.StatusBar.Company.Default";
    private const string EnvironmentFallbackKey = "Shell.StatusBar.Environment.Default";
    private const string ServerFallbackKey = "Shell.StatusBar.Server.Default";
    private const string DatabaseFallbackKey = "Shell.StatusBar.Database.Default";
    private const string UserFallbackKey = "Shell.StatusBar.User.Default";

    private readonly TimeProvider _timeProvider;
    private readonly DispatcherTimer _utcTimer;
    private readonly IConfiguration _configuration;
    private readonly DatabaseOptions _databaseOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IUserSession _userSession;
    private readonly ILocalizationService _localization;
    private readonly ISignalRClientService _realtime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellStatusBarViewModel"/> class.
    /// </summary>
    public ShellStatusBarViewModel(
        TimeProvider timeProvider,
        IConfiguration configuration,
        DatabaseOptions databaseOptions,
        IHostEnvironment hostEnvironment,
        IUserSession userSession,
        ILocalizationService localization,
        ISignalRClientService realtime)
    {
        _timeProvider = timeProvider;
        _configuration = configuration;
        _databaseOptions = databaseOptions;
        _hostEnvironment = hostEnvironment;
        _userSession = userSession;
        _localization = localization;
        _realtime = realtime ?? throw new ArgumentNullException(nameof(realtime));

        _localization.LanguageChanged += OnLanguageChanged;
        _realtime.ConnectionStateChanged += OnRealtimeStateChanged;

        RefreshMetadata();
        UtcTime = FormatUtc(_timeProvider.GetUtcNow());
        StatusText = FormatRealtimeStatus(_realtime.ConnectionState, _realtime.LastError, _realtime.NextRetryUtc);

        _utcTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _utcTimer.Tick += (_, _) => UtcTime = FormatUtc(_timeProvider.GetUtcNow());
        _utcTimer.Start();
    }

    /// <summary>
    /// Gets or sets the normalized company name resolved from <see cref="IConfiguration"/> or
    /// session-provided metadata.
    /// </summary>
    [ObservableProperty]
    private string _company = string.Empty;

    /// <summary>
    /// Gets or sets the normalized database name extracted from the injected
    /// <see cref="DatabaseOptions"/> instance.
    /// </summary>
    [ObservableProperty]
    private string _database = string.Empty;

    /// <summary>
    /// Gets or sets the normalized interactive user label pulled from the active
    /// <see cref="IUserSession"/>.
    /// </summary>
    [ObservableProperty]
    private string _user = string.Empty;

    /// <summary>
    /// Gets or sets the normalized environment descriptor derived from
    /// <see cref="IHostEnvironment"/> and configuration values.
    /// </summary>
    [ObservableProperty]
    private string _environment = string.Empty;

    /// <summary>
    /// Gets or sets the normalized database server host name extracted from
    /// <see cref="DatabaseOptions"/>.
    /// </summary>
    [ObservableProperty]
    private string _server = string.Empty;

    /// <summary>
    /// Gets or sets the formatted UTC timestamp refreshed via the injected <see cref="TimeProvider"/>.
    /// </summary>
    [ObservableProperty]
    private string _utcTime = string.Empty;

    /// <summary>
    /// Gets or sets the localized shell status message presented to the operator.
    /// </summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

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
        => timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

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

    private string NormalizeCompany(string? company)
        => string.IsNullOrWhiteSpace(company) ? _localization.GetString(CompanyFallbackKey) : company;

    private string NormalizeEnvironment(string? environment)
        => string.IsNullOrWhiteSpace(environment) ? _localization.GetString(EnvironmentFallbackKey) : environment;

    private string NormalizeServer(string? server)
        => string.IsNullOrWhiteSpace(server) ? _localization.GetString(ServerFallbackKey) : server;

    private string NormalizeDatabase(string? database)
        => string.IsNullOrWhiteSpace(database) ? _localization.GetString(DatabaseFallbackKey) : database;

    private string NormalizeUser(string? user)
        => string.IsNullOrWhiteSpace(user) ? _localization.GetString(UserFallbackKey) : user;

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshMetadata();
        StatusText = FormatRealtimeStatus(_realtime.ConnectionState, _realtime.LastError, _realtime.NextRetryUtc);
    }

    private void OnRealtimeStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        StatusText = FormatRealtimeStatus(e.State, e.Message, e.NextRetryUtc);
    }

    private string FormatRealtimeStatus(RealtimeConnectionState state, string? message, DateTimeOffset? nextRetry)
    {
        return state switch
        {
            RealtimeConnectionState.Connecting => _localization.GetString("Shell.Status.SignalR.Connecting"),
            RealtimeConnectionState.Connected => _localization.GetString("Shell.Status.SignalR.Connected"),
            RealtimeConnectionState.Retrying when nextRetry.HasValue =>
                _localization.GetString(
                    "Shell.Status.SignalR.Retrying",
                    nextRetry.Value.ToString("HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)),
            RealtimeConnectionState.Retrying => _localization.GetString("Shell.Status.SignalR.RetryingShort"),
            _ when !string.IsNullOrWhiteSpace(message) =>
                _localization.GetString("Shell.Status.SignalR.DisconnectedWithError", message!),
            _ => _localization.GetString("Shell.Status.SignalR.Disconnected")
        };
    }
}
