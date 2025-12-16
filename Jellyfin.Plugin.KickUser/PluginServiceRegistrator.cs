using Jellyfin.Plugin.KickUser.ScheduledTasks;
using Jellyfin.Plugin.KickUser.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.KickUser
{
    /// <summary>
    /// Service registrator for the KickUser plugin.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<InactiveUserService>();
            serviceCollection.AddSingleton<InactiveUserCleanupTask>();
        }
    }
}
