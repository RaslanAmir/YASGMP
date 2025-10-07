using System;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>Persists and restores AvalonDock layouts using the shared user_window_layouts table.</summary>
    public sealed class DockLayoutPersistenceService
    {
        private readonly DatabaseService _database;
        private readonly IUserSession _session;
        /// <summary>
        /// Initializes a new instance of the DockLayoutPersistenceService class.
        /// </summary>
        public DockLayoutPersistenceService(DatabaseService database, IUserSession session)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }
        /// <summary>
        /// Executes the load async operation.
        /// </summary>

        public async Task<LayoutSnapshot?> LoadAsync(string layoutKey, CancellationToken token = default)
        {
            var snapshot = await _database
                .GetUserWindowLayoutAsync(GetUserId(), layoutKey, token)
                .ConfigureAwait(false);

            if (snapshot is null)
            {
                return null;
            }

            var geometry = snapshot.Geometry;
            return new LayoutSnapshot(
                snapshot.LayoutXml ?? string.Empty,
                geometry.Left,
                geometry.Top,
                geometry.Width,
                geometry.Height);
        }
        /// <summary>
        /// Executes the save async operation.
        /// </summary>

        public async Task SaveAsync(string layoutKey, string layoutXml, WindowGeometry geometry, CancellationToken token = default)
        {
            var layoutGeometry = new DatabaseServiceLayoutsExtensions.UserWindowLayoutGeometry(
                geometry.Left,
                geometry.Top,
                geometry.Width,
                geometry.Height);

            await _database
                .SaveUserWindowLayoutAsync(GetUserId(), layoutKey, layoutXml, layoutGeometry, token)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the persisted layout for the current user.
        /// </summary>
        public Task ResetAsync(string layoutKey, CancellationToken token = default)
        {
            return _database.DeleteUserWindowLayoutAsync(GetUserId(), layoutKey, token);
        }

        private int GetUserId()
        {
            return _session.UserId ?? throw new InvalidOperationException("Current session does not have a user id.");
        }
    }
    /// <summary>
    /// Executes the struct operation.
    /// </summary>

    public readonly record struct WindowGeometry(double? Left, double? Top, double? Width, double? Height);
    /// <summary>
    /// Executes the struct operation.
    /// </summary>

    public readonly record struct LayoutSnapshot(string LayoutXml, double? Left, double? Top, double? Width, double? Height)
    {
        /// <summary>
        /// Executes the geometry operation.
        /// </summary>
        public WindowGeometry Geometry => new(Left, Top, Width, Height);
    }
}
