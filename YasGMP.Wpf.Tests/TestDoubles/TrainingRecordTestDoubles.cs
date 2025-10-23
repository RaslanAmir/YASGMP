using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;

namespace YasGMP.Wpf.Tests.TestDoubles;

public static class TrainingRecordViewModelFactory
{
    public static TrainingRecordViewModel Create(IEnumerable<TrainingRecord>? records = null)
    {
        var instance = (TrainingRecordViewModel)FormatterServices
            .GetUninitializedObject(typeof(TrainingRecordViewModel));

        var materialized = records?.Select(Clone) ?? Enumerable.Empty<TrainingRecord>();
        instance.TrainingRecords = new ObservableCollection<TrainingRecord>(materialized);
        instance.FilteredTrainingRecords = new ObservableCollection<TrainingRecord>(instance.TrainingRecords);
        instance.SelectedTrainingRecord = instance.TrainingRecords.FirstOrDefault();
        instance.StatusMessage = string.Empty;
        instance.SearchTerm = null;
        instance.StatusFilter = null;
        instance.TypeFilter = null;
        instance.IsBusy = false;

        return instance;
    }

    public static TrainingRecord Clone(TrainingRecord source)
        => new()
        {
            Id = source.Id,
            Code = source.Code,
            Title = source.Title,
            TrainingType = source.TrainingType,
            Status = source.Status,
            Description = source.Description,
            DueDate = source.DueDate,
            TrainingDate = source.TrainingDate,
            ExpiryDate = source.ExpiryDate,
            AssignedTo = source.AssignedTo,
            AssignedToName = source.AssignedToName,
            Note = source.Note,
            EffectivenessCheck = source.EffectivenessCheck,
            TraineeId = source.TraineeId,
            TraineeSignature = source.TraineeSignature,
            TrainerSignature = source.TrainerSignature,
            Attachments = source.Attachments?.Select(CloneAttachment).ToList() ?? new List<Attachment>(),
            WorkflowHistory = source.WorkflowHistory?.ToList() ?? new List<string>()
        };

    private static Attachment CloneAttachment(Attachment attachment)
        => new()
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FileType = attachment.FileType,
            FileSize = attachment.FileSize,
            UploadedAt = attachment.UploadedAt,
            UploadedById = attachment.UploadedById,
            Note = attachment.Note
        };
}

public sealed class RecordingTrainingRecordService : ITrainingRecordService
{
    private Func<CancellationToken, Task<TrainingRecordLoadResult>> _loadHandler
        = _ => Task.FromResult(new TrainingRecordLoadResult(true, string.Empty, Array.Empty<TrainingRecord>()));

    public List<IList<TrainingRecord>> ExportSnapshots { get; } = new();

    public List<(TrainingRecord Draft, string? Note)> InitiateRequests { get; } = new();

    public List<(TrainingRecord Record, int? AssigneeId, string? Note)> AssignRequests { get; } = new();

    public List<(TrainingRecord Record, string? Note)> ApproveRequests { get; } = new();

    public List<(TrainingRecord Record, string? Note, IEnumerable<TrainingRecordAttachmentUpload>? Attachments)> CompleteRequests { get; } = new();

    public List<(TrainingRecord Record, string? Note, IEnumerable<TrainingRecordAttachmentUpload>? Attachments)> CloseRequests { get; } = new();

    public List<(IList<TrainingRecord> Records, string Format)> ExportRequests { get; } = new();

    public TrainingRecordOperationResult InitiateResult { get; set; } = new(true, "Initiated.");

    public TrainingRecordOperationResult AssignResult { get; set; } = new(true, "Assigned.");

    public TrainingRecordOperationResult ApproveResult { get; set; } = new(true, "Approved.");

    public TrainingRecordOperationResult CompleteResult { get; set; } = new(true, "Completed.");

    public TrainingRecordOperationResult CloseResult { get; set; } = new(true, "Closed.");

    public TrainingRecordExportResult ExportResult { get; set; } = new(true, "Exported.", null);

    public int LoadCallCount { get; private set; }

    public void SetLoadResult(TrainingRecordLoadResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        _loadHandler = _ => Task.FromResult(result);
    }

    public void SetLoadException(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        _loadHandler = _ => Task.FromException<TrainingRecordLoadResult>(exception);
    }

    public Task<TrainingRecordLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        LoadCallCount++;
        return _loadHandler(cancellationToken);
    }

    public Task<TrainingRecordOperationResult> InitiateAsync(
        TrainingRecord draft,
        string? note,
        CancellationToken cancellationToken = default)
    {
        InitiateRequests.Add((TrainingRecordViewModelFactory.Clone(draft), note));
        return Task.FromResult(InitiateResult);
    }

    public Task<TrainingRecordOperationResult> AssignAsync(
        TrainingRecord record,
        int? assigneeId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        AssignRequests.Add((TrainingRecordViewModelFactory.Clone(record), assigneeId, note));
        return Task.FromResult(AssignResult);
    }

    public Task<TrainingRecordOperationResult> ApproveAsync(
        TrainingRecord record,
        string? note,
        CancellationToken cancellationToken = default)
    {
        ApproveRequests.Add((TrainingRecordViewModelFactory.Clone(record), note));
        return Task.FromResult(ApproveResult);
    }

    public Task<TrainingRecordOperationResult> CompleteAsync(
        TrainingRecord record,
        string? note,
        IEnumerable<TrainingRecordAttachmentUpload>? attachments,
        CancellationToken cancellationToken = default)
    {
        CompleteRequests.Add((TrainingRecordViewModelFactory.Clone(record), note, attachments));
        return Task.FromResult(CompleteResult);
    }

    public Task<TrainingRecordOperationResult> CloseAsync(
        TrainingRecord record,
        string? note,
        IEnumerable<TrainingRecordAttachmentUpload>? attachments,
        CancellationToken cancellationToken = default)
    {
        CloseRequests.Add((TrainingRecordViewModelFactory.Clone(record), note, attachments));
        return Task.FromResult(CloseResult);
    }

    public Task<TrainingRecordExportResult> ExportAsync(
        IList<TrainingRecord> records,
        string format,
        CancellationToken cancellationToken = default)
    {
        ExportSnapshots.Add(records.Select(TrainingRecordViewModelFactory.Clone).ToList());
        ExportRequests.Add((records, format));
        return Task.FromResult(ExportResult);
    }
}
