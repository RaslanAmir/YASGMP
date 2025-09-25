using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Default <see cref="IMachineCrudService"/> implementation that forwards calls to
    /// the shared <see cref="MachineService"/> from <c>YasGMP.AppCore</c>.
    /// </summary>
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

        public async Task<int> CreateAsync(Machine machine, MachineCrudContext context)
        {
            if (machine is null) throw new ArgumentNullException(nameof(machine));
            await _inner.CreateAsync(machine, context.UserId, context.Ip, context.DeviceInfo, context.SessionId)
                .ConfigureAwait(false);
            return machine.Id;
        }

        public Task UpdateAsync(Machine machine, MachineCrudContext context)
        {
            if (machine is null) throw new ArgumentNullException(nameof(machine));
            return _inner.UpdateAsync(machine, context.UserId, context.Ip, context.DeviceInfo, context.SessionId);
        }

        public void Validate(Machine machine)
        {
            if (machine is null) throw new ArgumentNullException(nameof(machine));
            _inner.ValidateMachine(machine);
        }

        public string NormalizeStatus(string? status) => MachineService.NormalizeStatus(status);
    }
}
