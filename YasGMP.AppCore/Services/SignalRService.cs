using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using YasGMP.Models.DTO;

namespace YasGMP.Services
{
    /// <summary>
    /// Real-time SignalR client for receiving new audit events instantly.
    /// </summary>
    public static class SignalRService
    {
        /// <summary>Back-channel connection; initialized in <see cref="InitializeAsync"/>.</summary>
        private static HubConnection? _hub; // CS8618 fix: nullable

        /// <summary>Raised when a server-side AuditCreated event is received.</summary>
        public static event Func<AuditEntryDto, Task>? OnAuditReceived; // CS8618 fix: nullable

        /// <summary>Creates and starts a SignalR connection to the specified hub URL.</summary>
        public static async Task InitializeAsync(string hubUrl)
        {
            _hub = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();

            _hub.On<AuditEntryDto>("AuditCreated", async audit =>
            {
                if (OnAuditReceived != null)
                    await OnAuditReceived.Invoke(audit);
            });

            await _hub.StartAsync().ConfigureAwait(false);
        }
    }
}
