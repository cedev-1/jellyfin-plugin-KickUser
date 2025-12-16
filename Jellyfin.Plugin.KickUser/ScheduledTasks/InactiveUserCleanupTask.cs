using Jellyfin.Plugin.KickUser.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KickUser.ScheduledTasks
{
    /// <summary>
    /// Scheduled task for cleaning up inactive users.
    /// </summary>
    public class InactiveUserCleanupTask : IScheduledTask
    {
        private readonly InactiveUserService _inactiveUserService;
        private readonly ILogger<InactiveUserCleanupTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InactiveUserCleanupTask"/> class.
        /// </summary>
        /// <param name="inactiveUserService">The inactive user service.</param>
        /// <param name="logger">The logger instance.</param>
        public InactiveUserCleanupTask(InactiveUserService inactiveUserService, ILogger<InactiveUserCleanupTask> logger)
        {
            _inactiveUserService = inactiveUserService;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Clean Up Inactive Users";

        /// <inheritdoc />
        public string Key => "KickUserInactiveCleanup";

        /// <inheritdoc />
        public string Description => "Checks for inactive users and disables or deletes them according to the KickUser plugin configuration.";

        /// <inheritdoc />
        public string Category => "User Management";

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting inactive user cleanup task");
            progress.Report(0);

            try
            {
                await _inactiveUserService.CheckAndProcessInactiveUsers();
                progress.Report(100);
                _logger.LogInformation("Inactive user cleanup task completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during inactive user cleanup task");
                throw;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var checkHour = Plugin.Instance?.Configuration.CheckHour ?? 3;
            if (checkHour < 0 || checkHour > 23)
            {
                checkHour = 3;
            }

            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.DailyTrigger,
                    TimeOfDayTicks = TimeSpan.FromHours(checkHour).Ticks
                }
            };
        }
    }
}
