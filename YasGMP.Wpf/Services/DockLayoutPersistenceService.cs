using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>Persists and restores AvalonDock layouts using the shared user_window_layouts table.</summary>
    public sealed class DockLayoutPersistenceService
    {
        private readonly string _connectionString;
        private readonly IUserSession _session;

        public DockLayoutPersistenceService(DatabaseOptions options, IUserSession session)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            _connectionString = options.ConnectionString;
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task<LayoutSnapshot?> LoadAsync(string layoutKey, CancellationToken token = default)
        {
            const string sql = @"
SELECT layout_xml, pos_x, pos_y, width, height
FROM user_window_layouts
WHERE user_id=@u AND page_type=@p
LIMIT 1;";

            var parameters = new[]
            {
                new MySqlParameter("@u", _session.UserId),
                new MySqlParameter("@p", layoutKey)
            };

            try
            {
                await using var conn = CreateConnection();
                await conn.OpenAsync(token).ConfigureAwait(false);
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters);

                await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (!await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    return null;
                }

                string layoutXml = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                double? posX = reader.IsDBNull(1) ? null : reader.GetDouble(1);
                double? posY = reader.IsDBNull(2) ? null : reader.GetDouble(2);
                double? width = reader.IsDBNull(3) ? null : reader.GetDouble(3);
                double? height = reader.IsDBNull(4) ? null : reader.GetDouble(4);

                return new LayoutSnapshot(layoutXml, posX, posY, width, height);
            }
            catch (MySqlException ex) when (IsSchemaMissing(ex))
            {
                Debug.WriteLine($"[DockLayoutPersistence] Layout table schema incomplete. Skipping restore for '{layoutKey}': {ex.Message}");
                return null;
            }
        }

        public async Task SaveAsync(string layoutKey, string layoutXml, WindowGeometry geometry, CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO user_window_layouts (user_id, page_type, layout_xml, pos_x, pos_y, width, height, saved_at)
VALUES (@u,@p,@layout,@x,@y,@w,@h,UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
layout_xml=VALUES(layout_xml),
pos_x=VALUES(pos_x),
pos_y=VALUES(pos_y),
width=VALUES(width),
height=VALUES(height),
saved_at=UTC_TIMESTAMP();";

            var parameters = new[]
            {
                new MySqlParameter("@u", _session.UserId),
                new MySqlParameter("@p", layoutKey),
                new MySqlParameter("@layout", layoutXml),
                new MySqlParameter("@x", geometry.Left.HasValue ? geometry.Left.Value : (object)DBNull.Value),
                new MySqlParameter("@y", geometry.Top.HasValue ? geometry.Top.Value : (object)DBNull.Value),
                new MySqlParameter("@w", geometry.Width.HasValue ? geometry.Width.Value : (object)DBNull.Value),
                new MySqlParameter("@h", geometry.Height.HasValue ? geometry.Height.Value : (object)DBNull.Value)
            };

            try
            {
                await using var conn = CreateConnection();
                await conn.OpenAsync(token).ConfigureAwait(false);
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters);
                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (IsSchemaMissing(ex))
            {
                Debug.WriteLine($"[DockLayoutPersistence] Layout table schema incomplete. Skipping save for '{layoutKey}': {ex.Message}");
            }
        }

        private MySqlConnection CreateConnection() => new(_connectionString);

        private static bool IsSchemaMissing(MySqlException ex) => ex.Number == 1054 /* Unknown column */ || ex.Number == 1146 /* Table doesn't exist */;
    }

    public readonly record struct WindowGeometry(double? Left, double? Top, double? Width, double? Height);

    public readonly record struct LayoutSnapshot(string LayoutXml, double? Left, double? Top, double? Width, double? Height)
    {
        public WindowGeometry Geometry => new(Left, Top, Width, Height);
    }
}

