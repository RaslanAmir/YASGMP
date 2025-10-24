using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels;

/// <summary>
/// Status bar view-model displayed along the bottom edge of the shell.
/// Exposes environment diagnostics required for SAP B1 parity and Part 11 overlays.
/// </summary>
public partial class ShellStatusBarViewModel : ObservableObject
{
    private readonly DispatcherTimer _utcTimer;

    /// <summary>Initializes a new instance of the <see cref="ShellStatusBarViewModel"/> class.</summary>
    /// <param name="userSession">User session for current username.</param>
    /// <param name="dbOptions">Database connection settings to display DB and host.</param>
    /// <param name="hostEnvironment">Host environment for environment name.</param>
    public ShellStatusBarViewModel(IUserSession userSession, DatabaseOptions dbOptions, IHostEnvironment hostEnvironment)
    {
        _statusText = "Ready";
        _activeModule = string.Empty;

        UserName = string.IsNullOrWhiteSpace(userSession.Username) ? "wpf-shell" : userSession.Username;
        (ServerName, DatabaseName) = TryParseServerAndDb(dbOptions.ConnectionString);
        EnvironmentName = string.IsNullOrWhiteSpace(hostEnvironment.EnvironmentName) ? "Production" : hostEnvironment.EnvironmentName;
        SmokeStatus = GetLastSmokeStatus();
        if (IsStrictEnabled())
        {
            SmokeStatus = string.IsNullOrWhiteSpace(SmokeStatus) ? "Smoke: Strict" : $"{SmokeStatus} • Strict";
        }
        UtcNow = DateTime.UtcNow.ToString("u");

        _utcTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _utcTimer.Tick += (_, _) =>
        {
            UtcNow = DateTime.UtcNow.ToString("u");
        };
        _utcTimer.Start();
    }

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private string _activeModule;

    /// <summary>Gets or sets the current UTC timestamp string.</summary>
    [ObservableProperty]
    private string _utcNow = string.Empty;

    /// <summary>Gets or sets the logical deployment environment (e.g., Development/Staging/Production).</summary>
    [ObservableProperty]
    private string _environmentName = string.Empty;

    /// <summary>Gets or sets the connected database name.</summary>
    [ObservableProperty]
    private string _databaseName = string.Empty;

    /// <summary>Gets or sets the database server or host name.</summary>
    [ObservableProperty]
    private string _serverName = string.Empty;

    /// <summary>Gets or sets the signed-in user label for the status bar.</summary>
    [ObservableProperty]
    private string _userName = string.Empty;

    /// <summary>Gets or sets the latest smoke harness status short message.</summary>
    [ObservableProperty]
    private string _smokeStatus = string.Empty;

    private static (string server, string database) TryParseServerAndDb(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return ("localhost", "YASGMP");
        }

        // Very small parser for common MySql connection string keys
        string server = Extract(connectionString, "Server|Host|Data Source");
        string database = Extract(connectionString, "Database|Initial Catalog");
        if (string.IsNullOrWhiteSpace(server)) server = "localhost";
        if (string.IsNullOrWhiteSpace(database)) database = "YASGMP";
        return (server, database);

        static string Extract(string cs, string keyPattern)
        {
            var rx = new Regex($@"(?i)(?:^|;)\s*(?:{keyPattern})\s*=\s*([^;]+)");
            var m = rx.Match(cs);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }
    }

    private static string GetLastSmokeStatus()
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YasGMP", "logs");
            if (!Directory.Exists(dir))
            {
                return "Smoke: idle";
            }
            var latest = Directory.EnumerateFiles(dir, "smoke-*.txt").Concat(Directory.EnumerateFiles(dir, "smoke_*.log")).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (string.IsNullOrEmpty(latest)) return "Smoke: idle";
            return $"Smoke: {File.GetLastWriteTimeUtc(latest):u}";
        }
        catch
        {
            return "Smoke: n/a";
        }
    }

    private static bool IsStrictEnabled()
    {
        try
        {
            var v = Environment.GetEnvironmentVariable("YASGMP_STRICT_SMOKE");
            if (string.IsNullOrWhiteSpace(v)) return false;
            v = v.Trim().ToLowerInvariant();
            return v is "1" or "true" or "yes" or "y" or "on" or "enable" or "enabled";
        }
        catch { return false; }
    }
}

