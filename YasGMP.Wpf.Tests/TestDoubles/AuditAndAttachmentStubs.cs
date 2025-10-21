using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests.TestDoubles;

public sealed class StubAuditService : AuditService
{
    public StubAuditService(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<AuditEntryDto> FilteredAudits { get; } = new();

    public (string User, string Entity, string Action, DateTime From, DateTime To)? LastFilter { get; private set; }

    public Exception? FilteredAuditsException { get; set; }

    public override Task<List<AuditEntryDto>> GetFilteredAudits(
        string user,
        string entity,
        string action,
        DateTime from,
        DateTime to)
    {
        LastFilter = (user, entity, action, from, to);
        if (FilteredAuditsException is not null)
        {
            throw FilteredAuditsException;
        }

        return Task.FromResult(FilteredAudits.Select(Clone).ToList());
    }

    private static AuditEntryDto Clone(AuditEntryDto source)
        => new()
        {
            Id = source.Id,
            Entity = source.Entity,
            EntityId = source.EntityId,
            Action = source.Action,
            Timestamp = source.Timestamp,
            UserId = source.UserId,
            Username = source.Username,
            IpAddress = source.IpAddress,
            DeviceInfo = source.DeviceInfo,
            SessionId = source.SessionId,
            Note = source.Note,
            Status = source.Status,
            DigitalSignature = source.DigitalSignature,
            SignatureHash = source.SignatureHash
        };
}

public sealed class ConfigurableAttachmentWorkflowService : IAttachmentWorkflowService
{
    private int _nextAttachmentId = 1;

    public bool IsEncryptionEnabled => false;

    public string EncryptionKeyId { get; set; } = string.Empty;

    public Dictionary<(string EntityType, int EntityId), List<AttachmentLinkWithAttachment>> Links { get; } = new();

    public List<AttachmentWorkflowUploadResult> Uploads { get; } = new();

    public List<(int AttachmentId, AttachmentReadRequest? Request)> DownloadRequests { get; } = new();

    public List<(string EntityType, int EntityId)> LinkRequests { get; } = new();

    public Func<int, Stream>? DownloadStreamFactory { get; set; }

    public Task<AttachmentWorkflowUploadResult> UploadAsync(
        Stream content,
        AttachmentUploadRequest request,
        CancellationToken token = default)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var attachmentId = request.AttachmentId ?? _nextAttachmentId++;
        var attachment = new Attachment
        {
            Id = attachmentId,
            FileName = request.FileName,
            EntityTable = request.EntityType,
            EntityId = request.EntityId
        };
        var link = new AttachmentLink
        {
            Id = attachmentId,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            AttachmentId = attachmentId
        };
        var retention = new RetentionPolicy
        {
            PolicyName = request.RetentionPolicyName,
            RetainUntil = request.RetainUntil
        };
        var upload = new AttachmentUploadResult(attachment, link, retention);
        var result = new AttachmentWorkflowUploadResult(upload, string.Empty, 0, false, null);
        Uploads.Add(result);
        return Task.FromResult(result);
    }

    public Task<AttachmentStreamResult> DownloadAsync(
        int attachmentId,
        Stream destination,
        AttachmentReadRequest? request = null,
        CancellationToken token = default)
    {
        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        DownloadRequests.Add((attachmentId, request));
        if (DownloadStreamFactory is not null)
        {
            using var source = DownloadStreamFactory(attachmentId);
            source.CopyTo(destination);
        }

        return Task.FromResult(new AttachmentStreamResult(new Attachment { Id = attachmentId }, 0, 0, false, request));
    }

    public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(
        string entityType,
        int entityId,
        CancellationToken token = default)
    {
        LinkRequests.Add((entityType, entityId));
        if (Links.TryGetValue((entityType, entityId), out var list))
        {
            var clones = list
                .Select(item => new AttachmentLinkWithAttachment(
                    new AttachmentLink
                    {
                        Id = item.Link.Id,
                        EntityType = item.Link.EntityType,
                        EntityId = item.Link.EntityId,
                        AttachmentId = item.Link.AttachmentId
                    },
                    new Attachment
                    {
                        Id = item.Attachment.Id,
                        FileName = item.Attachment.FileName,
                        Name = item.Attachment.Name,
                        EntityTable = item.Attachment.EntityTable,
                        EntityId = item.Attachment.EntityId
                    }))
                .ToList();
            return Task.FromResult<IReadOnlyList<AttachmentLinkWithAttachment>>(clones);
        }

        return Task.FromResult<IReadOnlyList<AttachmentLinkWithAttachment>>(Array.Empty<AttachmentLinkWithAttachment>());
    }

    public Task<IReadOnlyList<Attachment>> GetAttachmentSummariesAsync(
        string? entityFilter,
        string? typeFilter,
        string? searchTerm,
        CancellationToken token = default)
        => Task.FromResult<IReadOnlyList<Attachment>>(Array.Empty<Attachment>());

    public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
        => Task.CompletedTask;

    public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
        => Task.CompletedTask;

    public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
        => Task.FromResult<Attachment?>(null);

    public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
        => Task.FromResult<Attachment?>(null);
}

public sealed class RecordingModuleNavigationService : IModuleNavigationService
{
    private readonly List<(string ModuleKey, object? Parameter)> _opened = new();

    public IReadOnlyList<(string ModuleKey, object? Parameter)> OpenedModules => _opened;

    public ModuleDocumentViewModel? LastActivated { get; private set; }

    public Func<string, object?, ModuleDocumentViewModel>? Resolver { get; set; }

    public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
    {
        var document = Resolver?.Invoke(moduleKey, parameter) ?? new DummyModuleDocumentViewModel(moduleKey);
        _opened.Add((moduleKey, parameter));
        return document;
    }

    public void Activate(ModuleDocumentViewModel document)
    {
        LastActivated = document;
    }

    private sealed class DummyModuleDocumentViewModel : ModuleDocumentViewModel
    {
        private static readonly FakeLocalizationService Localization = new(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>()
            },
            "en");

        public DummyModuleDocumentViewModel(string moduleKey)
            : base(moduleKey, moduleKey, Localization, new StubCflDialogService(), new StubShellInteractionService(), new StubModuleNavigationService())
        {
        }

        protected override Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
            => Task.FromResult<IReadOnlyList<ModuleRecord>>(Array.Empty<ModuleRecord>());

        protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
            => Array.Empty<ModuleRecord>();
    }
}
