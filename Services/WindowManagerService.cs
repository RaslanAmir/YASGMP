using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MySqlConnector;

namespace YasGMP.Services
{
    /// <summary>
    /// Floating/resizable window manager (Windows/macOS) with persisted geometry
    /// per (user_id + page_type) in MySQL 8.0 and JSON file fallback.
    /// </summary>
    public sealed class WindowManagerService
    {
        private readonly Dictionary<string, Window> _open = new();

        /// <summary>
        /// Opens a new floating window hosting the specified page <paramref name="pageType"/>.
        /// If already open for the same user+page key, activates that window instead.
        /// </summary>
        public void OpenWindowForType(Type pageType, string? title = null)
        {
            if (pageType is null) throw new ArgumentNullException(nameof(pageType));

            var page = (Page)Activator.CreateInstance(pageType)!;
            var window = new Window(page) { Title = title ?? pageType.Name };

#if WINDOWS
            window.MinimumWidth = 600;
            window.MinimumHeight = 400;
#endif

            var key = BuildKey(pageType);

            if (_open.TryGetValue(key, out var existing))
            {
                TryActivateWindow(existing);
                return;
            }

            _open[key] = window;

            _ = RestoreGeometryAsync(key, window);

            window.Destroying += async (_, __) =>
            {
                try { await SaveGeometryAsync(key, window).ConfigureAwait(false); }
                finally { _open.Remove(key); }
            };

            Application.Current?.OpenWindow(window);
            TryActivateWindow(window);
        }

        private static string BuildKey(Type pageType)
        {
            var uid = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            return $"{uid}|{pageType.FullName}";
        }

        #region === Geometry persistence (DB first, file fallback) ===

        private sealed class Geometry
        {
            public int? X { get; set; }
            public int? Y { get; set; }
            public int  Width { get; set; }
            public int  Height { get; set; }
            public DateTime SavedAtUtc { get; set; }
        }

        private async Task RestoreGeometryAsync(string key, Window window)
        {
            try
            {
                var g = await LoadFromDbAsync(key).ConfigureAwait(false)
                        ?? await LoadFromFileAsync(key).ConfigureAwait(false);

                if (g is null) return;

#if WINDOWS
                if (g.Width  > 100) window.Width  = g.Width;
                if (g.Height > 100) window.Height = g.Height;
                if (g.X is not null) window.X = g.X.Value;
                if (g.Y is not null) window.Y = g.Y.Value;
#else
                if (g.Width  > 100) window.Width  = g.Width;
                if (g.Height > 100) window.Height = g.Height;
#endif
            }
            catch { /* ignore */ }
        }

        private async Task SaveGeometryAsync(string key, Window window)
        {
            var g = new Geometry
            {
#if WINDOWS
                X = (int)Math.Round(window.X),
                Y = (int)Math.Round(window.Y),
                Width  = (int)Math.Round(window.Width),
                Height = (int)Math.Round(window.Height),
#else
                Width  = (int)Math.Round(window.Width),
                Height = (int)Math.Round(window.Height),
#endif
                SavedAtUtc = DateTime.UtcNow
            };

            if (!await SaveToDbAsync(key, g).ConfigureAwait(false))
                await SaveToFileAsync(key, g).ConfigureAwait(false);
        }

        private static (int userId, string pageType) ParseKey(string key)
        {
            var parts = key.Split('|');
            _ = int.TryParse(parts[0], out var userId);
            var pageType = parts.Length > 1 ? parts[1] : "Unknown";
            return (userId, pageType);
        }

        private static string? TryGetConnectionString()
        {
            try
            {
                if (Application.Current is not App app) return null;
                var cfg = app.AppConfig;

                // 1) Indexer "ConnectionStrings:MySqlDb"
                var idxer = cfg.GetType().GetProperty("Item", new[] { typeof(string) });
                if (idxer != null)
                {
                    var viaIndexer = idxer.GetValue(cfg, new object[] { "ConnectionStrings:MySqlDb" }) as string;
                    if (!string.IsNullOrWhiteSpace(viaIndexer)) return viaIndexer;
                }

                // 2) ConnectionStrings.MySqlDb
                var cs = cfg.GetType().GetProperty("ConnectionStrings")?.GetValue(cfg);
                var mysql = cs?.GetType().GetProperty("MySqlDb")?.GetValue(cs) as string;
                return string.IsNullOrWhiteSpace(mysql) ? null : mysql;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<Geometry?> LoadFromDbAsync(string key)
        {
            var connStr = TryGetConnectionString();
            if (string.IsNullOrWhiteSpace(connStr)) return null;

            var (userId, pageType) = ParseKey(key);

            try
            {
                var db = new DatabaseService(connStr);

                const string sql = @"
SELECT pos_x, pos_y, width, height
FROM user_window_layouts
WHERE user_id=@u AND page_type=@p
LIMIT 1;";

                var pars = new[]
                {
                    new MySqlParameter("@u", userId),
                    new MySqlParameter("@p", pageType)
                };

                var dt = await db.ExecuteSelectAsync(sql, pars).ConfigureAwait(false);
                if (dt.Rows.Count == 0) return null;

                var r = dt.Rows[0];

                return new Geometry
                {
                    X = r["pos_x"] is DBNull ? null : Convert.ToInt32(r["pos_x"]),
                    Y = r["pos_y"] is DBNull ? null : Convert.ToInt32(r["pos_y"]),
                    Width  = r["width"]  is DBNull ? 0 : Convert.ToInt32(r["width"]),
                    Height = r["height"] is DBNull ? 0 : Convert.ToInt32(r["height"])
                };
            }
            catch
            {
                return null;
            }
        }

        private static async Task<bool> SaveToDbAsync(string key, Geometry g)
        {
            var connStr = TryGetConnectionString();
            if (string.IsNullOrWhiteSpace(connStr)) return false;

            var (userId, pageType) = ParseKey(key);

            try
            {
                var db = new DatabaseService(connStr);

                const string sql = @"
INSERT INTO user_window_layouts (user_id, page_type, pos_x, pos_y, width, height, saved_at)
VALUES (@u,@p,@x,@y,@w,@h,UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
pos_x=VALUES(pos_x),
pos_y=VALUES(pos_y),
width=VALUES(width),
height=VALUES(height),
saved_at=UTC_TIMESTAMP();";

                var pars = new[]
                {
                    new MySqlParameter("@u", userId),
                    new MySqlParameter("@p", pageType),
                    new MySqlParameter("@x", (object?)g.X ?? DBNull.Value),
                    new MySqlParameter("@y", (object?)g.Y ?? DBNull.Value),
                    new MySqlParameter("@w", g.Width),
                    new MySqlParameter("@h", g.Height)
                };

                await db.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string FilePathFor(string key)
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, "windowLayouts");
            Directory.CreateDirectory(dir);
            var safe = key.Replace('|', '_').Replace(':', '_');
            return Path.Combine(dir, $"{safe}.json");
        }

        private static async Task<Geometry?> LoadFromFileAsync(string key)
        {
            try
            {
                var path = FilePathFor(key);
                if (!File.Exists(path)) return null;
                var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                return System.Text.Json.JsonSerializer.Deserialize<Geometry>(json);
            }
            catch { return null; }
        }

        private static async Task SaveToFileAsync(string key, Geometry g)
        {
            try
            {
                var path = FilePathFor(key);
                var json = System.Text.Json.JsonSerializer.Serialize(g);
                await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        #endregion

        #region === Platform helpers ===

        /// <summary>
        /// Attempts to bring a MAUI Window to the foreground (Windows only). Safe no-op elsewhere.
        /// </summary>
        private static void TryActivateWindow(Window w)
        {
#if WINDOWS
            try
            {
                var native = w.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                native?.Activate();
            }
            catch
            {
                // Ignore activation errors (e.g., handler not created yet).
            }
#endif
        }

        #endregion
    }
}
