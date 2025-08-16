using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PersonalMALRatings.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string MALUsername { get; set; } = string.Empty;
    
    public string MALClientId { get; set; } = string.Empty;
    
    public string MALAccessToken { get; set; } = string.Empty;
    
    public string MALRefreshToken { get; set; } = string.Empty;
    
    public bool EnabledForAnime { get; set; } = true;
    
    public int RefreshIntervalHours { get; set; } = 24;
    
    public bool OverwriteExistingRatings { get; set; } = true;
}