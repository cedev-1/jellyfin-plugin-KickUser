using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KickUser.Services
{
    /// <summary>
    /// Service responsible for checking and processing inactive users.
    /// </summary>
    public class InactiveUserService
    {
        private readonly IUserManager _userManager;
        private readonly ILogger<InactiveUserService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InactiveUserService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="logger">The logger instance.</param>
        public InactiveUserService(IUserManager userManager, ILogger<InactiveUserService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Checks all users for inactivity and processes them according to configuration.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CheckAndProcessInactiveUsers()
        {
            var config = Plugin.Config;

            if (!config.EnablePlugin)
            {
                _logger.LogInformation("KickUser plugin is disabled. Skipping inactive user check.");
                return;
            }

            _logger.LogInformation(
                "Starting inactive user check. Threshold: {Days} days, Action: {Action}, DryRun: {DryRun}",
                config.InactivityThresholdDays,
                config.ActionType,
                config.DryRun);

            var users = _userManager.Users.ToList();
            var now = DateTime.UtcNow;
            var threshold = TimeSpan.FromDays(config.InactivityThresholdDays);

            int processedCount = 0;
            int skippedAdminCount = 0;
            int skippedWhitelistCount = 0;
            int actionedCount = 0;

            foreach (var user in users)
            {
                processedCount++;

                var userDto = _userManager.GetUserDto(user);
                if (userDto.Policy != null && userDto.Policy.IsAdministrator)
                {
                    _logger.LogDebug(
                        "Skipping user {Username} (ID: {UserId}) - User is an administrator",
                        user.Username,
                        user.Id);
                    skippedAdminCount++;
                    continue;
                }

                if (config.WhitelistedUserIds.Contains(user.Id.ToString()))
                {
                    _logger.LogDebug(
                        "Skipping user {Username} (ID: {UserId}) - User is in whitelist",
                        user.Username,
                        user.Id);
                    skippedWhitelistCount++;
                    continue;
                }

                var lastActivityDate = user.LastActivityDate ?? DateTime.MinValue;
                var inactiveDuration = now - lastActivityDate;

                if (inactiveDuration <= threshold)
                {
                    _logger.LogDebug(
                        "Skipping user {Username} (ID: {UserId}) - Last activity: {LastActivity} ({InactiveDays} days ago)",
                        user.Username,
                        user.Id,
                        lastActivityDate,
                        inactiveDuration.TotalDays);
                    continue;
                }

                _logger.LogInformation(
                    "User {Username} (ID: {UserId}) is inactive. Last activity: {LastActivity} ({InactiveDays} days ago)",
                    user.Username,
                    user.Id,
                    lastActivityDate,
                    (int)inactiveDuration.TotalDays);

                if (config.DryRun)
                {
                    _logger.LogWarning(
                        "[DRY RUN] Would {Action} user {Username} (ID: {UserId})",
                        config.ActionType.ToLower(),
                        user.Username,
                        user.Id);
                    actionedCount++;
                    continue;
                }

                try
                {
                    if (config.ActionType.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                    {
                        await _userManager.DeleteUserAsync(user.Id).ConfigureAwait(false);
                        _logger.LogWarning(
                            "DELETED user {Username} (ID: {UserId}) due to inactivity",
                            user.Username,
                            user.Id);
                    }
                    else
                    {
                        if (userDto.Policy != null)
                        {
                            userDto.Policy.IsDisabled = true;
                            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

                            _logger.LogWarning(
                                "DISABLED user {Username} (ID: {UserId}) due to inactivity",
                                user.Username,
                                user.Id);
                        }
                        else
                        {
                            _logger.LogError("Could not disable user {Username} (ID: {UserId}) - Policy is null", user.Username, user.Id);
                        }
                    }

                    actionedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing user {Username} (ID: {UserId})",
                        user.Username,
                        user.Id);
                }
            }

            _logger.LogInformation(
                "Inactive user check completed. Processed: {Processed}, Skipped (Admin): {Admin}, Skipped (Whitelist): {Whitelist}, Actioned: {Actioned}",
                processedCount,
                skippedAdminCount,
                skippedWhitelistCount,
                actionedCount);
        }
    }
}
