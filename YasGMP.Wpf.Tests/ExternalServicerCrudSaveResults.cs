using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Wpf.Services;

namespace YasGMP.Models
{
    public sealed partial class FakeExternalServicerCrudService
    {
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

        private static SignatureMetadataDto BuildMetadata(ExternalServicerCrudContext context, string? signature)
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
