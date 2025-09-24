using System;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace YasGMP.Services
{
    public sealed partial class DatabaseService
    {
        /// <summary>
        /// Executes the provided async delegate within a single MySQL transaction using the
        /// internal connection factory. Commits on success, rolls back on exception.
        /// </summary>
        public async Task WithTransactionAsync(
            Func<MySqlConnection, MySqlTransaction, Task> work,
            CancellationToken token = default)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));

            await using var conn = CreateConnection();
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var tx = await conn.BeginTransactionAsync(token).ConfigureAwait(false);
            try
            {
                await work(conn, tx).ConfigureAwait(false);
                await tx.CommitAsync(token).ConfigureAwait(false);
            }
            catch
            {
                try { await tx.RollbackAsync(token).ConfigureAwait(false); } catch { }
                throw;
            }
        }
    }
}

