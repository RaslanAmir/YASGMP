using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Wpf.Services;

namespace YasGMP.Models
{
    public partial class DatabaseService
    {
        public List<Deviation> Deviations { get; } = new();

        public Task<List<Deviation>> GetAllDeviationsAsync()
            => Task.FromResult(Deviations);
    }

    public sealed partial class FakeDeviationCrudService : IDeviationCrudService
    {
        private readonly List<Deviation> _store = new();
        private readonly List<(Deviation Entity, DeviationCrudContext Context)> _savedSnapshots = new();

        public List<Deviation> Saved => _store;

        public IReadOnlyList<(Deviation Entity, DeviationCrudContext Context)> SavedWithContext => _savedSnapshots;

        public DeviationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;

        public Deviation? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);

        public IEnumerable<DeviationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);

        public IEnumerable<Deviation> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<Deviation?> TryGetByIdAsync(int id)
            => Task.FromResult(_store.FirstOrDefault(d => d.Id == id));

        public Task<int> CreateCoreAsync(Deviation deviation, DeviationCrudContext context)
        {
            if (deviation.Id == 0)
            {
                deviation.Id = _store.Count == 0 ? 1 : _store.Max(d => d.Id) + 1;
            }

            _store.Add(Clone(deviation));
            TrackSnapshot(deviation, context);
            return Task.FromResult(deviation.Id);
        }

        public Task UpdateCoreAsync(Deviation deviation, DeviationCrudContext context)
        {
            var existing = _store.FirstOrDefault(d => d.Id == deviation.Id);
            if (existing is null)
            {
                _store.Add(Clone(deviation));
            }
            else
            {
                Copy(deviation, existing);
            }

            TrackSnapshot(deviation, context);
            return Task.CompletedTask;
        }

        public void Validate(Deviation deviation)
        {
            if (string.IsNullOrWhiteSpace(deviation.Title))
            {
                throw new InvalidOperationException("Deviation title is required.");
            }

            if (string.IsNullOrWhiteSpace(deviation.Description))
            {
                throw new InvalidOperationException("Deviation description is required.");
            }

            if (string.IsNullOrWhiteSpace(deviation.Severity))
            {
                throw new InvalidOperationException("Deviation severity is required.");
            }
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "OPEN" : status.Trim().ToUpperInvariant();

        private void TrackSnapshot(Deviation deviation, DeviationCrudContext context)
            => _savedSnapshots.Add((Clone(deviation), context));

        private static Deviation Clone(Deviation source)
            => new()
            {
                Id = source.Id,
                Code = source.Code,
                Title = source.Title,
                Description = source.Description,
                ReportedAt = source.ReportedAt,
                ReportedById = source.ReportedById,
                Severity = source.Severity,
                IsCritical = source.IsCritical,
                Status = source.Status,
                AssignedInvestigatorId = source.AssignedInvestigatorId,
                AssignedInvestigatorName = source.AssignedInvestigatorName,
                InvestigationStartedAt = source.InvestigationStartedAt,
                RootCause = source.RootCause,
                LinkedCapaId = source.LinkedCapaId,
                ClosureComment = source.ClosureComment,
                ClosedAt = source.ClosedAt,
                RiskScore = source.RiskScore,
                AnomalyScore = source.AnomalyScore,
                SourceIp = source.SourceIp,
                AuditNote = source.AuditNote,
                DigitalSignature = source.DigitalSignature,
                LastModified = source.LastModified,
                LastModifiedById = source.LastModifiedById
            };

        private static void Copy(Deviation source, Deviation destination)
        {
            destination.Code = source.Code;
            destination.Title = source.Title;
            destination.Description = source.Description;
            destination.ReportedAt = source.ReportedAt;
            destination.ReportedById = source.ReportedById;
            destination.Severity = source.Severity;
            destination.IsCritical = source.IsCritical;
            destination.Status = source.Status;
            destination.AssignedInvestigatorId = source.AssignedInvestigatorId;
            destination.AssignedInvestigatorName = source.AssignedInvestigatorName;
            destination.InvestigationStartedAt = source.InvestigationStartedAt;
            destination.RootCause = source.RootCause;
            destination.LinkedCapaId = source.LinkedCapaId;
            destination.ClosureComment = source.ClosureComment;
            destination.ClosedAt = source.ClosedAt;
            destination.RiskScore = source.RiskScore;
            destination.AnomalyScore = source.AnomalyScore;
            destination.SourceIp = source.SourceIp;
            destination.AuditNote = source.AuditNote;
            destination.DigitalSignature = source.DigitalSignature;
            destination.LastModified = source.LastModified;
            destination.LastModifiedById = source.LastModifiedById;
        }
    }
}
