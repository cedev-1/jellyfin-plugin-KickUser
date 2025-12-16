using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.KickUser.Configuration
{
    /// <summary>
    /// Represents the configuration settings for the KickUser plugin.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            EnablePlugin = true;
            InactivityThresholdDays = 30;
            ActionType = "Disable";
            WhitelistedUserIds = new List<string>();
            DryRun = false;
            CheckHour = 3;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin is enabled.
        /// </summary>
        public bool EnablePlugin { get; set; }

        /// <summary>
        /// Gets or sets the number of days of inactivity before an action is taken.
        /// </summary>
        public int InactivityThresholdDays { get; set; }

        /// <summary>
        /// Gets or sets the type of action to take on inactive users.
        /// Valid values: "Disable" or "Delete".
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the list of user IDs that should be excluded from automatic actions.
        /// </summary>
        public List<string> WhitelistedUserIds { get; set; }

        /// <summary>
        /// Gets or sets the hour (0-23) at which the daily inactive user check should run.
        /// </summary>
        public int CheckHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to run in dry-run mode (log actions without executing them).
        /// </summary>
        public bool DryRun { get; set; }
    }
}
