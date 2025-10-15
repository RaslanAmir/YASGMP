namespace YasGMP.Models
{
    /// <summary>
    /// User-configurable notification toggles that control shell alert surfaces (status bar, toast, etc.).
    /// </summary>
    public sealed class NotificationPreferences
    {
        /// <summary>Gets or sets whether status bar alerts should mirror toast notifications.</summary>
        public bool ShowStatusBarAlerts { get; set; } = true;

        /// <summary>Gets or sets whether transient toast notifications are enabled.</summary>
        public bool ShowToastAlerts { get; set; } = true;

        /// <summary>Creates a shallow copy of the current preference instance.</summary>
        public NotificationPreferences Clone()
            => new()
            {
                ShowStatusBarAlerts = ShowStatusBarAlerts,
                ShowToastAlerts = ShowToastAlerts,
            };

        /// <summary>Returns a new <see cref="NotificationPreferences"/> configured with default values.</summary>
        public static NotificationPreferences CreateDefault() => new();
    }
}
