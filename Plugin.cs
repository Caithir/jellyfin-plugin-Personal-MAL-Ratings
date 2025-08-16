using System;
using System.Collections.Generic;
using Jellyfin.Plugin.PersonalMALRatings.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PersonalMALRatings;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override string Name => "Personal MAL Ratings";

    public override Guid Id => Guid.Parse("85E35A5A-8C2D-4F3A-9B1E-7F8D6C4E2A91");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace)
            }
        };
    }
}