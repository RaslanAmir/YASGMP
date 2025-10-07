// ==============================================================================
// File: Services/DatabaseService.Layouts.Extensions.cs
// Purpose: User window layout persistence helpers for DatabaseService.
// ==============================================================================

#nullable enable

using System;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension helpers that persist user-specific window layouts through <see cref="DatabaseService"/>.
    /// </summary>
    public static class DatabaseServiceLayoutsExtensions
    {
        /// <summary>
        /// Represents dock/window geometry persisted alongside a layout definition.
        /// </summary>
        /// <param name="Left">The left coordinate.</param>
        /// <param name="Top">The top coordinate.</param>
        /// <param name="Width">The width.</param>
        /// <param name="Height">The height.</param>
        public sealed record UserWindowLayoutGeometry(double? Left, double? Top, double? Width, double? Height);

        /// <summary>
        /// Represents a persisted layout snapshot.
        /// </summary>
        /// <param name="LayoutXml">The serialized layout XML.</param>
        /// <param name="Geometry">The persisted geometry.</param>
        /// <param name="SavedAt">Timestamp when the layout was last saved.</param>
        /// <param name="CreatedAt">Timestamp when the record was created.</param>
        /// <param name="UpdatedAt">Timestamp when the record was last updated.</param>
        public sealed record UserWindowLayoutSnapshot(string? LayoutXml, UserWindowLayoutGeometry Geometry, DateTime? SavedAt, DateTime? CreatedAt, DateTime? UpdatedAt);

        /// <summary>
        /// Retrieves the persisted layout for the specified user and page type.
        /// </summary>
        /// <param name="db">The <see cref="DatabaseService"/> instance.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="pageType">Layout page type key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The <see cref="UserWindowLayoutSnapshot"/> if found; otherwise <c>null</c>.</returns>
        public static async Task<UserWindowLayoutSnapshot?> GetUserWindowLayoutAsync(
            this DatabaseService db,
            int userId,
            string pageType,
            CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (string.IsNullOrWhiteSpace(pageType)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pageType));

            const string sql = @"SELECT layout_xml, pos_x, pos_y, width, height, saved_at, created_at, updated_at
FROM user_window_layouts WHERE user_id=@u AND page_type=@p LIMIT 1;";

            var parameters = new[]
            {
                new MySqlParameter("@u", userId),
                new MySqlParameter("@p", pageType)
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, token).ConfigureAwait(false);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            var geometry = new UserWindowLayoutGeometry(
                ReadDouble(row, "pos_x"),
                ReadDouble(row, "pos_y"),
                ReadDouble(row, "width"),
                ReadDouble(row, "height"));

            string? layoutXml = row.Table.Columns.Contains("layout_xml") && row["layout_xml"] != DBNull.Value
                ? row["layout_xml"].ToString()
                : null;

            DateTime? savedAt = row.Table.Columns.Contains("saved_at") && row["saved_at"] != DBNull.Value
                ? Convert.ToDateTime(row["saved_at"], CultureInfo.InvariantCulture)
                : null;
            DateTime? createdAt = row.Table.Columns.Contains("created_at") && row["created_at"] != DBNull.Value
                ? Convert.ToDateTime(row["created_at"], CultureInfo.InvariantCulture)
                : null;
            DateTime? updatedAt = row.Table.Columns.Contains("updated_at") && row["updated_at"] != DBNull.Value
                ? Convert.ToDateTime(row["updated_at"], CultureInfo.InvariantCulture)
                : null;

            return new UserWindowLayoutSnapshot(layoutXml, geometry, savedAt, createdAt, updatedAt);
        }

        /// <summary>
        /// Persists or updates the layout for the specified user/page combination.
        /// </summary>
        /// <param name="db">The <see cref="DatabaseService"/> instance.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="pageType">Layout page type key.</param>
        /// <param name="layoutXml">Serialized layout XML.</param>
        /// <param name="geometry">Persisted geometry values.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task SaveUserWindowLayoutAsync(
            this DatabaseService db,
            int userId,
            string pageType,
            string? layoutXml,
            UserWindowLayoutGeometry geometry,
            CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (string.IsNullOrWhiteSpace(pageType)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pageType));
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));

            const string sql = @"INSERT INTO user_window_layouts (user_id, page_type, layout_xml, pos_x, pos_y, width, height, saved_at, created_at, updated_at)
VALUES (@u, @p, @layout, @x, @y, @w, @h, UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    layout_xml = VALUES(layout_xml),
    pos_x = VALUES(pos_x),
    pos_y = VALUES(pos_y),
    width = VALUES(width),
    height = VALUES(height),
    saved_at = UTC_TIMESTAMP(),
    updated_at = UTC_TIMESTAMP(),
    created_at = COALESCE(created_at, VALUES(created_at));";

            var parameters = new[]
            {
                new MySqlParameter("@u", userId),
                new MySqlParameter("@p", pageType),
                new MySqlParameter("@layout", string.IsNullOrWhiteSpace(layoutXml) ? DBNull.Value : layoutXml),
                CreateNullableDoubleParameter("@x", geometry.Left),
                CreateNullableDoubleParameter("@y", geometry.Top),
                CreateNullableDoubleParameter("@w", geometry.Width),
                CreateNullableDoubleParameter("@h", geometry.Height)
            };

            await db.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the persisted layout for the specified user/page combination.
        /// </summary>
        /// <param name="db">The <see cref="DatabaseService"/> instance.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="pageType">Layout page type key.</param>
        /// <param name="token">Cancellation token.</param>
        public static Task DeleteUserWindowLayoutAsync(
            this DatabaseService db,
            int userId,
            string pageType,
            CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (string.IsNullOrWhiteSpace(pageType)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pageType));

            const string sql = "DELETE FROM user_window_layouts WHERE user_id=@u AND page_type=@p;";
            var parameters = new[]
            {
                new MySqlParameter("@u", userId),
                new MySqlParameter("@p", pageType)
            };

            return db.ExecuteNonQueryAsync(sql, parameters, token);
        }

        private static double? ReadDouble(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
            {
                return null;
            }

            return Convert.ToDouble(row[column], CultureInfo.InvariantCulture);
        }

        private static MySqlParameter CreateNullableDoubleParameter(string name, double? value)
        {
            var parameter = new MySqlParameter(name, MySqlDbType.Double)
            {
                Value = value.HasValue ? value.Value : DBNull.Value
            };
            return parameter;
        }
    }
}
