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
    
    // Shoko Server Integration Settings
    public bool EnableShokoIntegration { get; set; } = false;
    
    public string ShokoServerUrl { get; set; } = "http://localhost:8111";
    
    public string ShokoApiKey { get; set; } = string.Empty;
    
    public bool UseShokoAsprimary { get; set; } = true;
    
    public bool FallbackToStringMatching { get; set; } = true;
    
    // Unrated Shows Handling  
    public bool SetUnratedRatingToZero { get; set; } = false;
    
    // Unmatched Shows Handling
    public bool SetUnmatchedRatingToZero { get; set; } = false;
}