using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Wpf.Services;

namespace YasGMP.Models
{
    public class ExternalServicer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public string? VatOrId { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? CooperationStart { get; set; }
        public DateTime? CooperationEnd { get; set; }
        public string? Comment { get; set; }
        public string? ExtraNotes { get; set; }
        public string? DigitalSignature { get; set; }
        public List<string> CertificateFiles { get; set; } = new();
    }
}

namespace YasGMP.Wpf.Services
{
    public sealed partial class FakeExternalServicerCrudService : IExternalServicerCrudService
    {
        private readonly List<ExternalServicer> _store = new();
        private readonly List<(ExternalServicer Entity, ExternalServicerCrudContext Context)> _savedSnapshots = new();

        public List<ExternalServicer> Saved => _store;
        public IReadOnlyList<(ExternalServicer Entity, ExternalServicerCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ExternalServicerCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public ExternalServicer? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ExternalServicerCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<ExternalServicer> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<ExternalServicer>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<ExternalServicer>>(_store.ToList());

        public Task<ExternalServicer?> TryGetByIdAsync(int id)
            => Task.FromResult<ExternalServicer?>(_store.FirstOrDefault(s => s.Id == id));

        public Task<int> CreateCoreAsync(ExternalServicer servicer, ExternalServicerCrudContext context)
        {
            if (servicer.Id == 0)
            {
                servicer.Id = _store.Count == 0 ? 1 : _store.Max(s => s.Id) + 1;
            }

            var stored = Clone(servicer);
            _store.Add(stored);
            TrackSnapshot(stored, context);
            return Task.FromResult(servicer.Id);
        }

        public Task UpdateCoreAsync(ExternalServicer servicer, ExternalServicerCrudContext context)
        {
            var existing = _store.FirstOrDefault(s => s.Id == servicer.Id);
            if (existing is null)
            {
                var stored = Clone(servicer);
                _store.Add(stored);
                TrackSnapshot(stored, context);
            }
            else
            {
                Copy(servicer, existing);
                TrackSnapshot(existing, context);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, ExternalServicerCrudContext context)
        {
            _store.RemoveAll(s => s.Id == id);
            _savedSnapshots.RemoveAll(tuple => tuple.Entity.Id == id);
            return Task.CompletedTask;
        }

        public void Validate(ExternalServicer servicer)
        {
            if (string.IsNullOrWhiteSpace(servicer.Name))
            {
                throw new InvalidOperationException("External servicer name is required.");
            }

            if (string.IsNullOrWhiteSpace(servicer.Email))
            {
                throw new InvalidOperationException("External servicer email is required.");
            }

            if (servicer.CooperationEnd is not null && servicer.CooperationStart is not null
                && servicer.CooperationEnd < servicer.CooperationStart)
            {
                throw new InvalidOperationException("Cooperation end cannot precede its start date.");
            }
        }

        public string NormalizeStatus(string? status)
            => ExternalServicerCrudExtensions.NormalizeStatusDefault(status);

        private void TrackSnapshot(ExternalServicer servicer, ExternalServicerCrudContext context)
            => _savedSnapshots.Add((Clone(servicer), context));

        private static ExternalServicer Clone(ExternalServicer source)
            => new()
            {
                Id = source.Id,
                Name = source.Name,
                Code = source.Code,
                Status = source.Status,
                Type = source.Type,
                VatOrId = source.VatOrId,
                ContactPerson = source.ContactPerson,
                Email = source.Email,
                Phone = source.Phone,
                Address = source.Address,
                CooperationStart = source.CooperationStart,
                CooperationEnd = source.CooperationEnd,
                Comment = source.Comment,
                ExtraNotes = source.ExtraNotes,
                DigitalSignature = source.DigitalSignature,
                CertificateFiles = new List<string>(source.CertificateFiles)
            };

        private static void Copy(ExternalServicer source, ExternalServicer destination)
        {
            destination.Name = source.Name;
            destination.Code = source.Code;
            destination.Status = source.Status;
            destination.Type = source.Type;
            destination.VatOrId = source.VatOrId;
            destination.ContactPerson = source.ContactPerson;
            destination.Email = source.Email;
            destination.Phone = source.Phone;
            destination.Address = source.Address;
            destination.CooperationStart = source.CooperationStart;
            destination.CooperationEnd = source.CooperationEnd;
            destination.Comment = source.Comment;
            destination.ExtraNotes = source.ExtraNotes;
            destination.DigitalSignature = source.DigitalSignature;
            destination.CertificateFiles = new List<string>(source.CertificateFiles);
        }
    }
}

