using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Routes Machine module requests from the WPF shell into the shared MAUI
    /// <see cref="YasGMP.Services.MachineService"/> pipeline.
    /// </summary>
    /// <remarks>
    /// Module view models issue CRUD commands through this adapter, which immediately forwards to
    /// <see cref="YasGMP.Services.MachineService"/> and the shared <see cref="YasGMP.Services.AuditService"/> so MAUI and WPF
    /// stay aligned. Operations should be awaited off the dispatcher thread with UI updates dispatched via
    /// <see cref="WpfUiDispatcher"/>. The <see cref="CrudSaveResult"/> contains identifiers, signature metadata, and status/note
    /// text that callers translate using <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before
    /// presenting them in the shell.
    /// </remarks>
    public sealed class MachineCrudServiceAdapter : IMachineCrudService
    {
        private readonly MachineService _inner;

        public MachineCrudServiceAdapter(MachineService inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<IReadOnlyList<Machine>> GetAllAsync()
            => await _inner.GetAllAsync().ConfigureAwait(false);

        public async Task<Machine?> TryGetByIdAsync(int id)
        {
            try
            {
                return await _inner.GetByIdAsync(id).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        public async Task<CrudSaveResult> CreateAsync(Machine machine, MachineCrudContext context)
        {
            if (machine is null) throw new ArgumentNullException(nameof(machine));

            var signature = ApplyContext(machine, context);
            var metadata = CreateMetadata(context, signature);

            await _inner.CreateAsync(machine, context.UserId, context.Ip, context.DeviceInfo, context.SessionId, metadata)
                .ConfigureAwait(false);

            // Preserve the captured signature metadata for the caller until core services accept it directly.
            machine.DigitalSignature = signature;
            return new CrudSaveResult(machine.Id, metadata);
        }

        public async Task<CrudSaveResult> UpdateAsync(Machine machine, MachineCrudContext context)
        {
            if (machine is null) throw new ArgumentNullException(nameof(machine));

            var signature = ApplyContext(machine, context);
            var metadata = CreateMetadata(context, signature);

            await _inner.UpdateAsync(machine, context.UserId, context.Ip, context.DeviceInfo, context.SessionId, metadata)
                .ConfigureAwait(false);

            machine.DigitalSignature = signature;
            return new CrudSaveResult(machine.Id, metadata);
        }

        public void Validate(Machine machine)
        {
            if (machine is null) throw new ArgumentNullException(nameof(machine));
            _inner.ValidateMachine(machine);
        }

        public string NormalizeStatus(string? status) => MachineService.NormalizeStatus(status);

        private static string ApplyContext(Machine machine, MachineCrudContext context)
        {
            var signature = context.SignatureHash ?? machine.DigitalSignature ?? string.Empty;
            machine.DigitalSignature = signature;

            if (context.UserId > 0)
            {
                machine.LastModifiedById = context.UserId;
            }

            SetExtraField(machine, "signature.id", context.SignatureId);
            SetExtraField(machine, "signature.hash", signature);
            SetExtraField(machine, "signature.method", context.SignatureMethod);
            SetExtraField(machine, "signature.status", context.SignatureStatus);
            SetExtraField(machine, "signature.note", context.SignatureNote);
            SetExtraField(machine, "signature.ip", context.Ip);
            SetExtraField(machine, "signature.device", context.DeviceInfo);
            SetExtraField(machine, "signature.session", context.SessionId);

            return signature;
        }

        private static void SetExtraField(Machine machine, string key, object? value)
        {
            if (machine.ExtraFields is null)
            {
                return;
            }

            if (value is null)
            {
                machine.ExtraFields.Remove(key);
            }
            else
            {
                machine.ExtraFields[key] = value;
            }
        }

        private static SignatureMetadataDto CreateMetadata(MachineCrudContext context, string signature)
            => new()
            {
                Id = context.SignatureId,
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };
    }
}
