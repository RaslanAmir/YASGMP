using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace YasGMP.Services
{
    /// <summary>
    /// Lightweight testing hooks that allow unit/integration harnesses to intercept database operations
    /// without requiring a live MySQL server. These are internal so they remain invisible to production code.
    /// </summary>
    public sealed partial class DatabaseService
    {
        internal Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>>? ExecuteNonQueryOverride { get; set; }

        internal Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<object?>>? ExecuteScalarOverride { get; set; }

        internal Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>? ExecuteSelectOverride { get; set; }

        internal void ResetTestOverrides()
        {
            ExecuteNonQueryOverride = null;
            ExecuteScalarOverride = null;
            ExecuteSelectOverride = null;
        }
    }
}

