using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the health report value.
    /// </summary>
    public static class HealthReport
    {
        /// <summary>
        /// Executes the object operation.
        /// </summary>
        public static Dictionary<string, object?> BuildBasic()
        {
            var p = Process.GetCurrentProcess();
            return new Dictionary<string, object?>
            {
                ["ts_utc"] = DateTime.UtcNow.ToString("o"),
                ["process_id"] = p.Id,
                ["proc_start_utc"] = p.StartTime.ToUniversalTime().ToString("o"),
                ["working_set_mb"] = p.WorkingSet64 / (1024*1024),
                ["os"] = Environment.OSVersion?.VersionString,
                ["framework"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                ["assembly"] = Assembly.GetExecutingAssembly().GetName().Name,
                ["assembly_ver"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            };
        }
    }
}

