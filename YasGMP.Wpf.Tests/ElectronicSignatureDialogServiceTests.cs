using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Xunit;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Tests;

public sealed class ElectronicSignatureDialogServiceTests : IDisposable
{
    private readonly DatabaseService _databaseService;
    private readonly AuditService _auditService;
    private readonly StubAuthContext _authContext;
    private readonly ImmediateUiDispatcher _dispatcher = new();
    private readonly List<SystemEventLogEntry> _systemEvents = new();
    private readonly List<Dictionary<string, object?>> _signatureCommands = new();
    private readonly ServiceProvider _serviceProvider;

    public ElectronicSignatureDialogServiceTests()
    {
        _databaseService = new DatabaseService("Server=localhost;Database=test;Uid=test;Pwd=test;");
        _databaseService.ExecuteNonQueryOverride = CaptureNonQueryAsync;
        _databaseService.ExecuteScalarOverride = (_, _, _) => Task.FromResult<object?>(101);

        _authContext = new StubAuthContext
        {
            CurrentUser = new User
            {
                Id = 77,
                Username = "tester",
                FullName = "Test User"
            },
            CurrentDeviceInfo = "unit-test-device",
            CurrentSessionId = "session-xyz",
            CurrentIpAddress = "192.0.2.25"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IAuthContext>(_authContext);
        services.AddSingleton<IPlatformService, StubPlatformService>();
        _serviceProvider = services.BuildServiceProvider();
        ServiceLocator.RegisterFallback(() => _serviceProvider);

        _auditService = new AuditService(_databaseService);
    }

    [Fact]
    public async Task CaptureSignatureAsync_LogsAuditWhenConfirmed()
    {
        var context = new ElectronicSignatureContext("machines", 42, method: "password", status: "valid");
        var signature = new DigitalSignature
        {
            TableName = context.TableName,
            RecordId = context.RecordId,
            UserId = _authContext.CurrentUser!.Id,
            SignatureHash = "hash-123",
            Method = context.Method,
            Status = context.Status,
            SessionId = _authContext.CurrentSessionId,
            Note = "QA: detail"
        };

        var captureResult = new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "detail",
            "QA Reason",
            signature);

        var service = new ElectronicSignatureDialogService(
            _dispatcher,
            _databaseService,
            _authContext,
            _auditService,
            vm =>
            {
                SetViewModelResult(vm, captureResult);
                return true;
            });

        ElectronicSignatureDialogResult? result = await service.CaptureSignatureAsync(context);

        Assert.NotNull(result);
        Assert.Equal(captureResult.ReasonCode, result!.ReasonCode);
        Assert.Equal(captureResult.ReasonDetail, result.ReasonDetail);
        Assert.Equal(captureResult.Signature.SignatureHash, result.Signature.SignatureHash);

        var captureEvent = Assert.Single(_systemEvents.Where(e => e.EventType == "SIGNATURE_CAPTURE_CONFIRMED"));
        Assert.Equal(context.TableName, captureEvent.TableName);
        Assert.Equal(context.RecordId, captureEvent.RecordId);
        Assert.Equal(_authContext.CurrentUser!.Id, captureEvent.UserId);
        Assert.Equal(_authContext.CurrentSessionId, captureEvent.SessionId);
        Assert.Contains("reason=QA", captureEvent.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("detail=detail", captureEvent.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PersistSignatureAsync_PersistsAndAudits()
    {
        var signature = new DigitalSignature
        {
            TableName = "components",
            RecordId = 321,
            UserId = _authContext.CurrentUser!.Id,
            SignatureHash = "hash-xyz",
            Method = "password",
            Status = "valid",
            SessionId = _authContext.CurrentSessionId,
            Note = "QA approval"
        };

        var result = new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "detail",
            "QA Reason",
            signature);

        var service = new ElectronicSignatureDialogService(
            _dispatcher,
            _databaseService,
            _authContext,
            _auditService,
            _ => true);

        await service.PersistSignatureAsync(result);

        Assert.Equal(101, result.Signature.Id);
        var insertCommand = Assert.Single(_signatureCommands);
        Assert.True(
            string.Equals(GetString(insertCommand, "@table"), signature.TableName, StringComparison.OrdinalIgnoreCase)
            && Convert.ToInt32(insertCommand["@rid"] ?? 0) == signature.RecordId,
            "Expected persisted signature insert to target the original record.");

        var persistEvent = Assert.Single(_systemEvents.Where(e => e.EventType == "SIGNATURE_PERSISTED"));
        Assert.Equal(signature.TableName, persistEvent.TableName);
        Assert.Equal(signature.RecordId, persistEvent.RecordId);
        Assert.Equal(_authContext.CurrentUser!.Id, persistEvent.UserId);
        Assert.Contains("hash=hash-xyz", persistEvent.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PersistSignatureAsync_SkipsInsertWhenSignatureIdExists()
    {
        var signature = new DigitalSignature
        {
            Id = 245,
            TableName = "calibration",
            RecordId = 654,
            UserId = _authContext.CurrentUser!.Id,
            SignatureHash = "hash-existing",
            Method = "password",
            Status = "valid",
            SessionId = _authContext.CurrentSessionId,
            Note = "Existing signature"
        };

        var result = new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "detail",
            "Existing Signature",
            signature);

        var service = new ElectronicSignatureDialogService(
            _dispatcher,
            _databaseService,
            _authContext,
            _auditService,
            _ => true);

        await service.PersistSignatureAsync(result);

        Assert.Equal(245, result.Signature.Id);
        Assert.Empty(_signatureCommands);

        var persistEvent = Assert.Single(_systemEvents.Where(e => e.EventType == "SIGNATURE_PERSISTED"));
        Assert.Equal(signature.TableName, persistEvent.TableName);
        Assert.Equal(signature.RecordId, persistEvent.RecordId);
        Assert.Equal(_authContext.CurrentUser!.Id, persistEvent.UserId);
        Assert.Contains("hash=hash-existing", persistEvent.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PersistSignatureAsync_PrepopulatedId_LogsWithoutInvokingInsertOverride()
    {
        var signature = new DigitalSignature
        {
            Id = 612,
            TableName = "components",
            RecordId = 777,
            UserId = _authContext.CurrentUser!.Id,
            SignatureHash = "hash-pre", 
            Method = "password",
            Status = "valid",
            SessionId = _authContext.CurrentSessionId,
            Note = "Pre-existing signature"
        };

        var result = new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "detail",
            "Existing Signature",
            signature);

        var insertAttempts = 0;
        _databaseService.ExecuteNonQueryOverride = async (sql, parameters, token) =>
        {
            if (sql.Contains("digital_signatures", StringComparison.OrdinalIgnoreCase)
                && sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                insertAttempts++;
            }

            return await CaptureNonQueryAsync(sql, parameters, token).ConfigureAwait(false);
        };

        var service = new ElectronicSignatureDialogService(
            _dispatcher,
            _databaseService,
            _authContext,
            _auditService,
            _ => true);

        await service.PersistSignatureAsync(result);

        Assert.Equal(0, insertAttempts);
        var persistEvent = Assert.Single(_systemEvents.Where(e => e.EventType == "SIGNATURE_PERSISTED"));
        Assert.Equal(signature.TableName, persistEvent.TableName);
        Assert.Equal(signature.RecordId, persistEvent.RecordId);
        Assert.Equal(_authContext.CurrentUser!.Id, persistEvent.UserId);
        Assert.Contains("hash=hash-pre", persistEvent.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogPersistedSignatureAsync_LogsWithoutDatabaseWrites()
    {
        var signature = new DigitalSignature
        {
            Id = 305,
            TableName = "validations",
            RecordId = 910,
            UserId = _authContext.CurrentUser!.Id,
            SignatureHash = "hash-logged",
            Method = "password",
            Status = "valid",
            SessionId = _authContext.CurrentSessionId,
            Note = "Post-persist log"
        };

        var result = new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "detail",
            "QA Reason",
            signature);

        var service = new ElectronicSignatureDialogService(
            _dispatcher,
            _databaseService,
            _authContext,
            _auditService,
            _ => true);

        await service.LogPersistedSignatureAsync(result);

        Assert.Empty(_signatureCommands);
        var auditEvent = Assert.Single(_systemEvents.Where(e => e.EventType == "SIGNATURE_PERSISTED"));
        Assert.Equal(signature.TableName, auditEvent.TableName);
        Assert.Equal(signature.RecordId, auditEvent.RecordId);
        Assert.Equal(_authContext.CurrentUser!.Id, auditEvent.UserId);
        Assert.Contains("hash=hash-logged", auditEvent.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("session=session-xyz", auditEvent.Description, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _databaseService.ResetTestOverrides();
        _serviceProvider.Dispose();
        _systemEvents.Clear();
        _signatureCommands.Clear();
        ServiceLocator.RegisterFallback(() => null);
    }

    private Task<int> CaptureNonQueryAsync(string sql, IEnumerable<MySqlParameter>? parameters, CancellationToken token)
    {
        var parameterMap = ToDictionary(parameters);
        if (sql.Contains("system_event_log", StringComparison.OrdinalIgnoreCase))
        {
            _systemEvents.Add(new SystemEventLogEntry(
                GetString(parameterMap, "@etype") ?? string.Empty,
                GetString(parameterMap, "@table"),
                GetNullableInt(parameterMap, "@rid"),
                GetString(parameterMap, "@desc"),
                GetNullableInt(parameterMap, "@uid"),
                GetString(parameterMap, "@sid")));
        }
        else if (sql.Contains("digital_signatures", StringComparison.OrdinalIgnoreCase))
        {
            _signatureCommands.Add(parameterMap);
        }

        return Task.FromResult(1);
    }

    private static Dictionary<string, object?> ToDictionary(IEnumerable<MySqlParameter>? parameters)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (parameters is null)
        {
            return dict;
        }

        foreach (var parameter in parameters)
        {
            dict[parameter.ParameterName] = parameter.Value == DBNull.Value ? null : parameter.Value;
        }

        return dict;
    }

    private static string? GetString(IDictionary<string, object?> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static int? GetNullableInt(IDictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return Convert.ToInt32(value);
    }

    private static void SetViewModelResult(ElectronicSignatureDialogViewModel viewModel, ElectronicSignatureDialogResult result)
    {
        var property = typeof(ElectronicSignatureDialogViewModel)
            .GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
        var setter = property?.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException("Result setter is not accessible.");
        setter.Invoke(viewModel, new object?[] { result });
    }

    private sealed record SystemEventLogEntry(
        string EventType,
        string? TableName,
        int? RecordId,
        string? Description,
        int? UserId,
        string? SessionId);

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public bool IsDispatchRequired => false;

        public void BeginInvoke(Action action) => action();

        public Task InvokeAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public Task InvokeAsync(Func<Task> asyncAction) => asyncAction();

        public Task<T> InvokeAsync<T>(Func<T> func) => Task.FromResult(func());
    }

    private sealed class StubAuthContext : IAuthContext
    {
        public User? CurrentUser { get; set; }
        public string CurrentSessionId { get; set; } = string.Empty;
        public string CurrentDeviceInfo { get; set; } = string.Empty;
        public string CurrentIpAddress { get; set; } = string.Empty;
    }

    private sealed class StubPlatformService : IPlatformService
    {
        public string GetHostName() => "test-host";
        public string GetLocalIpAddress() => "127.0.0.1";
        public string GetOsVersion() => "Windows 11";
        public string GetUserName() => "tester";
    }
}

