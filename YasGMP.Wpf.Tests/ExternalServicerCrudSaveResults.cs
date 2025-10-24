using System;
using System.Threading.Tasks;
using YasGMP.Models.DTO;
using YasGMP.Wpf.Services;

namespace YasGMP.Models
{
    public sealed partial class FakeExternalServicerCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(ExternalServicer servicer, ExternalServicerCrudContext context)
        {
            var id = await CreateCoreAsync(servicer, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, servicer.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(ExternalServicer servicer, ExternalServicerCrudContext context)
        {
            await UpdateCoreAsync(servicer, context).ConfigureAwait(false);
            return new CrudSaveResult(servicer.Id, BuildMetadata(context, servicer.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(ExternalServicerCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }
}

