using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Developer-facing harness that runs a deterministic smoke test covering
/// authentication context, module navigation and a minimal digital signature check.
/// </summary>
public sealed class DebugSmokeTestService
{
    /// <summary>
    /// Represents the environment toggle name value.
    /// </summary>
    public const string EnvironmentToggleName = "YASGMP_SMOKE";

    private const string SmokeDisabledStatusKey = "Shell.Status.Smoke.Disabled";
    private const string SmokeAlreadyRunningStatusKey = "Shell.Status.Smoke.AlreadyRunning";
    private const string SmokeResultWithLogStatusKey = "Shell.Status.Smoke.Result.WithLog";
    private const string SmokeResultLogFailureStatusKey = "Shell.Status.Smoke.Result.LogFailure";

    private static readonly string[] EnabledTokens =
    {
        "1", "true", "yes", "y", "on", "enable", "enabled"
    };

    private static readonly string[] ModuleSequence =
    {
        DashboardModuleViewModel.ModuleKey,
        AssetsModuleViewModel.ModuleKey,
        ComponentsModuleViewModel.ModuleKey,
        AttachmentsModuleViewModel.ModuleKey,
        ExternalServicersModuleViewModel.ModuleKey,
        IncidentsModuleViewModel.ModuleKey,
        CapaModuleViewModel.ModuleKey,
        ChangeControlModuleViewModel.ModuleKey,
        WorkOrdersModuleViewModel.ModuleKey,
        AuditModuleViewModel.ModuleKey,
        ApiAuditModuleViewModel.ModuleKey,
        ValidationsModuleViewModel.ModuleKey
    };

    private readonly IUserSession _userSession;
    private readonly IAuthContext _authContext;
    private readonly DatabaseService _databaseService;
    private readonly IModuleNavigationService _moduleNavigation;
    private readonly IModuleRegistry _moduleRegistry;

    private int _isRunning;
    /// <summary>
    /// Initializes a new instance of the DebugSmokeTestService class.
    /// </summary>

    public DebugSmokeTestService(
        IUserSession userSession,
        IAuthContext authContext,
        DatabaseService databaseService,
        IModuleNavigationService moduleNavigation,
        IModuleRegistry moduleRegistry)
    {
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _moduleNavigation = moduleNavigation ?? throw new ArgumentNullException(nameof(moduleNavigation));
        _moduleRegistry = moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
    }

    /// <summary>Whether the smoke harness is currently executing.</summary>
    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    /// <summary>Evaluates the environment toggle each time the property is queried.</summary>
    public bool IsEnabled => IsToggleEnabled(Environment.GetEnvironmentVariable(EnvironmentToggleName));

    /// <summary>
    /// Executes the smoke-test workflow when the <c>YASGMP_SMOKE</c> environment toggle is enabled.
    /// </summary>
    public async Task<DebugSmokeTestResult> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return DebugSmokeTestResult.Skipped(
                $"Smoke test disabled. Set {EnvironmentToggleName}=1 to enable the debug harness.",
                SmokeDisabledStatusKey,
                EnvironmentToggleName);
        }

        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
        {
            return DebugSmokeTestResult.Skipped("Smoke test already running.", SmokeAlreadyRunningStatusKey);
        }

        try
        {
            var start = DateTimeOffset.Now;
            var steps = new List<DebugSmokeTestStep>();
            var logBuilder = new StringBuilder();

            logBuilder.AppendLine("YasGMP debug smoke test");
            logBuilder.AppendLine($"Timestamp: {start:O}");
            logBuilder.AppendLine($"User session: {_userSession.Username} (#{_userSession.UserId})");
            logBuilder.AppendLine($"Auth session: {_authContext.CurrentSessionId}");
            logBuilder.AppendLine($"Device: {_authContext.CurrentDeviceInfo}");
            logBuilder.AppendLine($"IP address: {_authContext.CurrentIpAddress}");

            var modules = _moduleRegistry.Modules;
            if (modules.Count > 0)
            {
                logBuilder.AppendLine($"Registered modules: {modules.Count} – {string.Join(", ", modules.Select(m => m.Title))}");
            }
            else
            {
                logBuilder.AppendLine("Registered modules: none");
            }

            logBuilder.AppendLine($"Environment toggle ({EnvironmentToggleName}): {Environment.GetEnvironmentVariable(EnvironmentToggleName) ?? "<unset>"}");
            logBuilder.AppendLine();

            async Task AddStepAsync(string name, Func<CancellationToken, Task<string>> action)
            {
                var step = await ExecuteStepAsync(name, action, cancellationToken);
                steps.Add(step);
                logBuilder.AppendLine($"[{(step.Succeeded ? "PASS" : "FAIL")}] {step.Name} ({step.Duration.TotalMilliseconds:N0} ms) – {step.Message}");
                if (step.Exception is not null)
                {
                    logBuilder.AppendLine(step.Exception.ToString());
                }

                logBuilder.AppendLine();
            }

            await AddStepAsync("Session bootstrap", token => Task.FromResult(BuildSessionMessage()));
            await AddStepAsync("Module navigation", NavigateModulesAsync);
            await AddStepAsync("External servicers mode cycle", token => ExerciseFormModesAsync(ExternalServicersModuleViewModel.ModuleKey, token));
            await AddStepAsync("Incidents workflow transitions", token => ExerciseQualityWorkflowAsync(IncidentsModuleViewModel.ModuleKey, token));
            await AddStepAsync("CAPA workflow transitions", token => ExerciseQualityWorkflowAsync(CapaModuleViewModel.ModuleKey, token));
            await AddStepAsync("Change control workflow transitions", token => ExerciseQualityWorkflowAsync(ChangeControlModuleViewModel.ModuleKey, token));
            await AddStepAsync("Add/Find cycle", token => ExerciseFormModesAsync(WorkOrdersModuleViewModel.ModuleKey, token));
            await AddStepAsync("Attachments upload/download/delete", ExerciseAttachmentsWorkflowAsync);
            await AddStepAsync("Audit trail fetch", FetchAuditTrailAsync);
            await AddStepAsync("Digital signature verification", VerifyDigitalSignatureAsync);

            var passedSteps = steps.Count(static s => s.Succeeded);

            var logPath = ResolveLogPath(start);
            logBuilder.AppendLine($"Summary: {passedSteps}/{steps.Count} steps succeeded.");
            logBuilder.AppendLine($"Elapsed: {(DateTimeOffset.Now - start).TotalSeconds:F2} s");
            logBuilder.AppendLine($"Log path: {logPath}");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.WriteAllText(logPath, logBuilder.ToString());
                var allPassed = steps.All(static s => s.Succeeded);
                var summary = $"{passedSteps}/{steps.Count} smoke checks succeeded. Log written to {logPath}.";
                return DebugSmokeTestResult.Completed(
                    allPassed,
                    summary,
                    logPath,
                    steps,
                    SmokeResultWithLogStatusKey,
                    passedSteps,
                    steps.Count,
                    logPath);
            }
            catch (Exception ex)
            {
                steps.Add(new DebugSmokeTestStep("Persist log", false, ex.Message, TimeSpan.Zero, ex));
                var allPassed = steps.All(static s => s.Succeeded);
                var passedAfter = steps.Count(static s => s.Succeeded);
                var summary = $"{passedAfter}/{steps.Count} smoke checks succeeded. Failed to persist log: {ex.Message}";
                return DebugSmokeTestResult.Completed(
                    allPassed,
                    summary,
                    null,
                    steps,
                    SmokeResultLogFailureStatusKey,
                    passedAfter,
                    steps.Count,
                    ex.Message);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }

    private string BuildSessionMessage()
    {
        var user = _authContext.CurrentUser;
        if (user is not null)
        {
            return $"Authenticated as {user.Username} (#{user.Id}) – session {_authContext.CurrentSessionId}.";
        }

        return $"No hydrated user context – using {_userSession.Username} (#{_userSession.UserId}) for session {_authContext.CurrentSessionId}.";
    }

    private async Task<string> NavigateModulesAsync(CancellationToken cancellationToken)
    {
        var visited = new List<string>();
        foreach (var key in ModuleSequence)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = _moduleNavigation.OpenModule(key);
            _moduleNavigation.Activate(document);
            await document.InitializeAsync(null);
            visited.Add(document.Title);
        }

        return visited.Count > 0
            ? $"Visited {visited.Count} module(s): {string.Join(", ", visited)}"
            : "No modules were opened.";
    }

    private async Task<string> ExerciseFormModesAsync(string moduleKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = _moduleNavigation.OpenModule(moduleKey);
        _moduleNavigation.Activate(document);
        await document.InitializeAsync(null);

        var transitions = new List<FormMode>();
        await document.EnterFindModeCommand.ExecuteAsync(null);
        transitions.Add(document.Mode);
        await document.EnterAddModeCommand.ExecuteAsync(null);
        transitions.Add(document.Mode);
        await document.EnterUpdateModeCommand.ExecuteAsync(null);
        transitions.Add(document.Mode);
        await document.EnterViewModeCommand.ExecuteAsync(null);
        transitions.Add(document.Mode);
        await document.SaveCommand.ExecuteAsync(null);
        document.CancelCommand.Execute(null);

        return $"Mode transitions: {string.Join(" -> ", transitions)}; Records={document.Records.Count}; Status='{document.StatusMessage}'";
    }

    private async Task<string> ExerciseQualityWorkflowAsync(string moduleKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = _moduleNavigation.OpenModule(moduleKey);
        _moduleNavigation.Activate(document);
        await document.InitializeAsync(null);

        if (document.Records.Count > 0)
        {
            document.SelectedRecord = document.Records[0];
        }

        await document.EnterFindModeCommand.ExecuteAsync(null);
        await document.EnterAddModeCommand.ExecuteAsync(null);
        document.CancelCommand.Execute(null);
        await document.EnterUpdateModeCommand.ExecuteAsync(null);
        document.CancelCommand.Execute(null);
        await document.EnterViewModeCommand.ExecuteAsync(null);

        var inspectorFieldCounts = document.Records
            .Select(record => record.InspectorFields.Count)
            .ToArray();

        return inspectorFieldCounts.Length == 0
            ? $"Records=0; Mode={document.Mode}; Status='{document.StatusMessage}'"
            : $"Records={document.Records.Count}; Inspector fields avg={inspectorFieldCounts.Average():F1}; Mode={document.Mode}";
    }

    private async Task<string> FetchAuditTrailAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var events = await _databaseService.GetRecentDashboardEventsAsync(10, cancellationToken);
        var count = events?.Count ?? 0;
        if (count == 0)
        {
            return "No dashboard events returned.";
        }

        var latest = events[0];
        return $"Fetched {count} event(s); latest {latest.EventType} at {latest.Timestamp:O}.";
    }

    private async Task<string> VerifyDigitalSignatureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signatures = await _databaseService.GetAllSignaturesFullAsync(cancellationToken);
        if (signatures.Count == 0)
        {
            return "No digital signatures available to verify.";
        }

        var signature = signatures[0];
        var ok = await _databaseService.VerifySignatureAsync(signature.Id, cancellationToken);
        if (!ok)
        {
            throw new InvalidOperationException($"Signature {signature.Id} ({signature.TableName} #{signature.RecordId}) failed verification.");
        }

        return $"Signature {signature.Id} ({signature.TableName} #{signature.RecordId}) verified successfully.";
    }

    private async Task<string> ExerciseAttachmentsWorkflowAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = _moduleNavigation.OpenModule(AttachmentsModuleViewModel.ModuleKey);
        _moduleNavigation.Activate(document);
        await document.InitializeAsync(null).ConfigureAwait(false);

        if (document is not AttachmentsModuleViewModel attachments)
        {
            return $"Resolved attachments module as {document.GetType().Name}; skipping workflow.";
        }

        await attachments.EnterAddModeCommand.ExecuteAsync(null).ConfigureAwait(false);

        var timestamp = DateTimeOffset.Now;
        var fileName = $"smoke_{timestamp:yyyyMMdd_HHmmssfff}.txt";
        var stageDirectory = Path.Combine(Path.GetTempPath(), "YasGMP", "smoke", "attachments", Guid.NewGuid().ToString("N"));
        var downloadDirectory = Path.Combine(Path.GetTempPath(), "YasGMP", "smoke", "attachments", "downloads", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stageDirectory);
        Directory.CreateDirectory(downloadDirectory);

        var stagedPath = Path.Combine(stageDirectory, fileName);
        var stagedContent = Encoding.UTF8.GetBytes($"YasGMP smoke attachment {timestamp:O}\n");
        await File.WriteAllBytesAsync(stagedPath, stagedContent, cancellationToken).ConfigureAwait(false);

        var stagedUpload = new AttachmentsModuleViewModel.StagedAttachmentUploadViewModel
        {
            FileName = fileName,
            ContentType = "text/plain",
            EntityType = "attachments",
            EntityId = 0,
            Notes = "Smoke harness upload",
            TempDirectory = stageDirectory,
            TempPath = stagedPath,
            FileSize = stagedContent.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(stagedContent))
        };

        attachments.StagedUploads.Add(stagedUpload);

        await attachments.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);
        var uploadStatus = attachments.StatusMessage;

        var records = await _databaseService
            .GetAttachmentsFilteredAsync(null, null, fileName, cancellationToken)
            .ConfigureAwait(false);

        var uploaded = records.FirstOrDefault(a => string.Equals(a.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        if (uploaded is null)
        {
            throw new InvalidOperationException($"Uploaded attachment '{fileName}' not found in database.");
        }

        var attachmentService = ResolveAttachmentService(attachments)
            ?? throw new InvalidOperationException("Attachment service unavailable for download.");

        var downloadPath = Path.Combine(downloadDirectory, fileName);
        AttachmentStreamResult downloadResult;
        await using (var destination = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
        {
            var request = new AttachmentReadRequest
            {
                Reason = "wpf:attachments:smoke",
                SourceHost = Environment.MachineName,
                SourceIp = "ui:wpf"
            };

            downloadResult = await attachmentService
                .StreamContentAsync(uploaded.Id, destination, request, cancellationToken)
                .ConfigureAwait(false);
        }

        var downloadedBytes = downloadResult.BytesWritten;

        await _databaseService.DeleteAttachmentAsync(uploaded.Id, cancellationToken).ConfigureAwait(false);
        await attachments.RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);

        TryDeleteFile(downloadPath);
        TryDeleteDirectory(downloadDirectory);
        TryDeleteDirectory(stageDirectory);

        var messageBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(uploadStatus))
        {
            messageBuilder.Append(uploadStatus.Trim());
        }

        if (messageBuilder.Length > 0)
        {
            messageBuilder.Append(' ');
        }

        messageBuilder.Append($"Downloaded {downloadedBytes:N0} byte(s) and deleted attachment #{uploaded.Id}.");
        messageBuilder.Append(" Temporary files cleaned.");

        return messageBuilder.ToString();
    }

    private static IAttachmentService? ResolveAttachmentService(AttachmentsModuleViewModel viewModel)
    {
        var field = typeof(AttachmentsModuleViewModel)
            .GetField("_attachmentService", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(viewModel) as IAttachmentService;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup failures; temporary files will be removed later.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures; temporary directories will be removed later.
        }
    }

    private static async Task<DebugSmokeTestStep> ExecuteStepAsync(
        string name,
        Func<CancellationToken, Task<string>> action,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = await action(cancellationToken);
            stopwatch.Stop();
            return new DebugSmokeTestStep(name, true, message, stopwatch.Elapsed, null);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new DebugSmokeTestStep(name, false, ex.Message, stopwatch.Elapsed, ex);
        }
    }

    private static string ResolveLogPath(DateTimeOffset start)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Environment.CurrentDirectory;
        }

        var directory = Path.Combine(localAppData, "YasGMP", "logs");
        var fileName = $"smoke_{start:yyyyMMdd_HHmmss}.log";
        return Path.Combine(directory, fileName);
    }

    private static bool IsToggleEnabled(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var token = value.Trim().ToLowerInvariant();
        return EnabledTokens.Contains(token);
    }
}

/// <summary>Result payload returned by <see cref="DebugSmokeTestService"/>.</summary>
public sealed record DebugSmokeTestResult(
    bool WasRun,
    bool Passed,
    string Summary,
    string? LogPath,
    IReadOnlyList<DebugSmokeTestStep> Steps,
    string? SummaryResourceKey,
    IReadOnlyList<object?> SummaryResourceArguments)
{
    /// <summary>
    /// Executes the skipped operation.
    /// </summary>
    public static DebugSmokeTestResult Skipped(string summary, string? resourceKey = null, params object?[]? arguments)
        => new(false, false, summary, null, Array.Empty<DebugSmokeTestStep>(), resourceKey, Normalize(arguments));
    /// <summary>
    /// Executes the completed operation.
    /// </summary>

    public static DebugSmokeTestResult Completed(
        bool passed,
        string summary,
        string? logPath,
        IReadOnlyList<DebugSmokeTestStep> steps,
        string? resourceKey = null,
        params object?[]? arguments)
        => new(true, passed, summary, logPath, steps, resourceKey, Normalize(arguments));

    private static IReadOnlyList<object?> Normalize(object?[]? arguments)
        => arguments is { Length: > 0 } ? arguments.ToArray() : Array.Empty<object?>();
}

/// <summary>Represents a single step executed by the smoke harness.</summary>
public sealed record DebugSmokeTestStep(string Name, bool Succeeded, string Message, TimeSpan Duration, Exception? Exception);
