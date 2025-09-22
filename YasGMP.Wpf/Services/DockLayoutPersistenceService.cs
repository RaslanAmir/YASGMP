using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>Persists and restores AvalonDock layouts using the shared user_window_layouts table.</summary>
    public sealed class DockLayoutPersistenceService
    {
        private readonly DatabaseService _database;
        private readonly IUserSession _session;

        public DockLayoutPersistenceService(DatabaseService database, IUserSession session)
        {
            _database = database;
            _session = session;
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

            var table = await _database.ExecuteSelectAsync(sql, parameters, token).ConfigureAwait(false);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            var row = table.Rows[0];
            return new LayoutSnapshot(
                row["layout_xml"] as string ?? string.Empty,
                ReadNullableDouble(row, "pos_x"),
                ReadNullableDouble(row, "pos_y"),
                ReadNullableDouble(row, "width"),
                ReadNullableDouble(row, "height"));
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

            await _database.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);
        }

        private static double? ReadNullableDouble(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column))
            {
                return null;
            }

            var value = row[column];
            if (value == null || value is DBNull)
            {
                return null;
            }

            return Convert.ToDouble(value);
        }
    }

    public readonly record struct WindowGeometry(double? Left, double? Top, double? Width, double? Height);

    public readonly record struct LayoutSnapshot(string LayoutXml, double? Left, double? Top, double? Width, double? Height)
    {
        public WindowGeometry Geometry => new(Left, Top, Width, Height);
    }
}
