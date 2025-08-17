using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.PersonalMALRatings.Configuration;
using Jellyfin.Plugin.PersonalMALRatings.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalMALRatings;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;

    public override string Name => "Personal MAL Ratings";

    public override Guid Id => Guid.Parse("85E35A5A-8C2D-4F3A-9B1E-7F8D6C4E2A91");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        _logger = logger;
        Instance = this;
        
        // Initialize plugin-specific file logging
        var pluginDataPath = GetPluginDataPath();
        PluginLogger.Initialize(pluginDataPath);
        
        _logger.LogInformation("Personal MAL Ratings plugin initialized (Version: {Version})", Version);
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "Personal MAL Ratings plugin initialized (Version: {0}) - Log files location: {1}", Version, Path.Combine(pluginDataPath, "logs"));
        
        LogConfigurationStatus();
        
        // Subscribe to configuration changes
        ConfigurationChanged += OnConfigurationChanged;
    }

    private string GetPluginDataPath()
    {
        // Create plugin-specific data directory
        var pluginDataPath = Path.Combine(ApplicationPaths.PluginConfigurationsPath, "PersonalMALRatings");
        Directory.CreateDirectory(pluginDataPath);
        return pluginDataPath;
    }

    public static Plugin? Instance { get; private set; }

    private void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
    {
        _logger.LogInformation("Plugin configuration changed, validating new settings...");
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "Plugin configuration changed, validating new settings...");
        LogConfigurationStatus();
    }

    private void LogConfigurationStatus()
    {
        var config = Configuration;
        
        // Log to both Jellyfin logs and plugin files
        _logger.LogInformation("=== MAL Plugin Configuration Status ===");
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "=== MAL Plugin Configuration Status ===");
        
        _logger.LogInformation("Plugin enabled for anime: {Enabled}", config.EnabledForAnime);
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "Plugin enabled for anime: {0}", config.EnabledForAnime);
        
        _logger.LogInformation("MAL Username: {Username}", string.IsNullOrEmpty(config.MALUsername) ? "[Not Set]" : config.MALUsername);
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "MAL Username: {0}", string.IsNullOrEmpty(config.MALUsername) ? "[Not Set]" : config.MALUsername);
        
        _logger.LogInformation("MAL Client ID: {ClientId}", string.IsNullOrEmpty(config.MALClientId) ? "[Not Set]" : config.MALClientId);
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "MAL Client ID: {0}", string.IsNullOrEmpty(config.MALClientId) ? "[Not Set]" : config.MALClientId);
        
        _logger.LogInformation("MAL Access Token: {AccessToken}", string.IsNullOrEmpty(config.MALAccessToken) ? "[Not Set]" : "[Configured]");
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "MAL Access Token: {0}", string.IsNullOrEmpty(config.MALAccessToken) ? "[Not Set]" : "[Configured]");
        
        _logger.LogInformation("MAL Refresh Token: {RefreshToken}", string.IsNullOrEmpty(config.MALRefreshToken) ? "[Not Set]" : "[Configured]");
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "MAL Refresh Token: {0}", string.IsNullOrEmpty(config.MALRefreshToken) ? "[Not Set]" : "[Configured]");
        
        _logger.LogInformation("Refresh interval: {Hours} hours", config.RefreshIntervalHours);
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "Refresh interval: {0} hours", config.RefreshIntervalHours);
        
        _logger.LogInformation("Overwrite existing ratings: {Overwrite}", config.OverwriteExistingRatings);
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "Overwrite existing ratings: {0}", config.OverwriteExistingRatings);
        
        // Validate configuration
        var issues = new List<string>();
        
        if (!config.EnabledForAnime)
        {
            _logger.LogWarning("⚠️  Plugin is disabled for anime - no ratings will be applied");
            PluginLogger.LogToFile(LogLevel.Warning, "Plugin", "Plugin is disabled for anime - no ratings will be applied");
        }
        
        if (string.IsNullOrEmpty(config.MALClientId))
        {
            issues.Add("MAL Client ID is missing");
        }
        
        if (string.IsNullOrEmpty(config.MALAccessToken))
        {
            issues.Add("MAL Access Token is missing");
        }
        
        if (config.RefreshIntervalHours < 1 || config.RefreshIntervalHours > 168)
        {
            issues.Add($"Invalid refresh interval: {config.RefreshIntervalHours} hours (must be 1-168)");
        }
        
        if (issues.Count > 0)
        {
            _logger.LogError("❌ Configuration validation failed:");
            PluginLogger.LogToFile(LogLevel.Error, "Plugin", "Configuration validation failed:");
            foreach (var issue in issues)
            {
                _logger.LogError("   - {Issue}", issue);
                PluginLogger.LogToFile(LogLevel.Error, "Plugin", "   - {0}", issue);
            }
            _logger.LogError("Plugin will not function properly until these issues are resolved.");
            PluginLogger.LogToFile(LogLevel.Error, "Plugin", "Plugin will not function properly until these issues are resolved.");
        }
        else if (config.EnabledForAnime)
        {
            _logger.LogInformation("✅ Configuration validation passed - plugin ready to use");
            PluginLogger.LogToFile(LogLevel.Information, "Plugin", "Configuration validation passed - plugin ready to use");
        }
        
        _logger.LogInformation("======================================");
        PluginLogger.LogToFile(LogLevel.Information, "Plugin", "======================================");
    }

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