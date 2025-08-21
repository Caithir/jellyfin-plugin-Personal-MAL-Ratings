using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.PersonalMALRatings.Configuration;
using Jellyfin.Plugin.PersonalMALRatings.Models;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalMALRatings.Services;

/// <summary>
/// Enhanced anime matching service that integrates Shoko Server with fallback to traditional string matching
/// </summary>
public class EnhancedAnimeMatchingService
{
    private readonly ILogger<EnhancedAnimeMatchingService> _logger;
    private readonly ShokoApiClient _shokoClient;
    private readonly AniDBToMALMappingService _mappingService;
    private readonly AnimeMatchingService _stringMatchingService;

    public EnhancedAnimeMatchingService(
        ILogger<EnhancedAnimeMatchingService> logger,
        ShokoApiClient shokoClient,
        AniDBToMALMappingService mappingService,
        AnimeMatchingService stringMatchingService)
    {
        _logger = logger;
        _shokoClient = shokoClient;
        _mappingService = mappingService;
        _stringMatchingService = stringMatchingService;
    }

    /// <summary>
    /// Find the best match for a Jellyfin item using Shoko integration and fallback strategies
    /// </summary>
    public async Task<EnhancedMatchResult> FindMatchAsync(BaseItem jellyfinItem, List<MALAnimeEntry> malEntries)
    {
        if (jellyfinItem == null || string.IsNullOrEmpty(jellyfinItem.Name))
        {
            _logger.LogWarning("Cannot match null or empty Jellyfin item");
            return new EnhancedMatchResult { Match = null };
        }

        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration not available");
            return new EnhancedMatchResult { Match = null };
        }

        _logger.LogInformation("🎯 Starting enhanced anime matching for: '{Title}'", jellyfinItem.Name);
        PluginLogger.LogToFile(LogLevel.Information, "EnhancedMatching", 
            "Starting enhanced anime matching for: '{0}'", jellyfinItem.Name);

        MALAnimeEntry? match = null;

        // Strategy 1: Shoko integration (if enabled)
        if (config.EnableShokoIntegration && config.UseShokoAsprimary)
        {
            _logger.LogDebug("🔍 Attempting Shoko-based matching...");
            match = await TryMatchViaShokoAsync(jellyfinItem, malEntries, config);
            
            if (match != null)
            {
                _logger.LogInformation("✅ Found match via Shoko: '{JellyfinTitle}' → '{MALTitle}' (Score: {Score})", 
                    jellyfinItem.Name, match.Node.Title, match.ListStatus?.Score ?? 0);
                PluginLogger.LogToFile(LogLevel.Information, "EnhancedMatching", 
                    "Found match via Shoko: '{0}' → '{1}' (Score: {2})", 
                    jellyfinItem.Name, match.Node.Title, match.ListStatus?.Score ?? 0);
                return ProcessMatchResult(match, config);
            }
        }

        // Strategy 2: Traditional string matching (fallback or primary)
        if (match == null && config.FallbackToStringMatching)
        {
            _logger.LogDebug("🔍 Attempting string-based matching...");
            match = _stringMatchingService.FindMatch(jellyfinItem, malEntries);
            
            if (match != null)
            {
                _logger.LogInformation("✅ Found match via string matching: '{JellyfinTitle}' → '{MALTitle}' (Score: {Score})", 
                    jellyfinItem.Name, match.Node.Title, match.ListStatus?.Score ?? 0);
                PluginLogger.LogToFile(LogLevel.Information, "EnhancedMatching", 
                    "Found match via string matching: '{0}' → '{1}' (Score: {2})", 
                    jellyfinItem.Name, match.Node.Title, match.ListStatus?.Score ?? 0);
                return ProcessMatchResult(match, config);
            }
        }

        _logger.LogWarning("❌ No match found for '{Title}' using any strategy", jellyfinItem.Name);
        PluginLogger.LogToFile(LogLevel.Warning, "EnhancedMatching", 
            "No match found for '{0}' using any strategy", jellyfinItem.Name);
        
        // Handle unmatched shows based on configuration
        var result = new EnhancedMatchResult { Match = null, IsUnmatched = true };
        
        if (config.SetUnmatchedRatingToZero)
        {
            result.ShouldSetRatingToZero = true;
            _logger.LogInformation("⭐ Will set unmatched show '{Title}' community rating to 0", jellyfinItem.Name);
            PluginLogger.LogToFile(LogLevel.Information, "EnhancedMatching", 
                "Will set unmatched show '{0}' community rating to 0", jellyfinItem.Name);
        }
        
        return result;
    }

    /// <summary>
    /// Attempt to match via Shoko Server integration
    /// </summary>
    private async Task<MALAnimeEntry?> TryMatchViaShokoAsync(BaseItem jellyfinItem, List<MALAnimeEntry> malEntries, PluginConfiguration config)
    {
        try
        {
            _logger.LogDebug("🔍 Querying Shoko for: '{Title}'", jellyfinItem.Name);
            PluginLogger.LogToFile(LogLevel.Debug, "EnhancedMatching", 
                "Querying Shoko for: '{0}'", jellyfinItem.Name);

            // Step 1: Try to find series by file path first (most accurate)
            List<ShokoSeries> shokoSeries = new List<ShokoSeries>();
            
            if (!string.IsNullOrEmpty(jellyfinItem.Path))
            {
                _logger.LogDebug("🔍 Searching Shoko by file path: '{Path}'", jellyfinItem.Path);
                shokoSeries = await _shokoClient.GetSeriesByFilePathAsync(jellyfinItem.Path, config.ShokoServerUrl, config.ShokoApiKey);
            }

            // Step 2: If no results from file path, try name search
            if (!shokoSeries.Any())
            {
                _logger.LogDebug("🔍 Searching Shoko by series name: '{Name}'", jellyfinItem.Name);
                shokoSeries = await _shokoClient.SearchSeriesByNameAsync(jellyfinItem.Name, config.ShokoServerUrl, config.ShokoApiKey);
            }

            if (!shokoSeries.Any())
            {
                _logger.LogDebug("No Shoko series found for '{Title}'", jellyfinItem.Name);
                return null;
            }

            // Step 3: Try to extract AniDB ID from Shoko results
            foreach (var series in shokoSeries.Take(3)) // Limit to top 3 results
            {
                var anidbId = _shokoClient.GetAniDBId(series);
                if (!anidbId.HasValue)
                {
                    _logger.LogDebug("No AniDB ID found for Shoko series: '{SeriesName}'", 
                        series.Name ?? series.AniDB?.Title ?? "Unknown");
                    continue;
                }

                _logger.LogDebug("🎯 Found AniDB ID {AniDBId} for Shoko series: '{SeriesName}'", 
                    anidbId.Value, series.Name ?? series.AniDB?.Title ?? "Unknown");

                // Step 4: Map AniDB ID to MAL ID
                var malId = await _mappingService.GetMALIdFromAniDBAsync(anidbId.Value);
                if (!malId.HasValue)
                {
                    _logger.LogDebug("No MAL ID mapping found for AniDB ID: {AniDBId}", anidbId.Value);
                    continue;
                }

                _logger.LogDebug("🎯 Mapped AniDB ID {AniDBId} to MAL ID {MALId}", anidbId.Value, malId.Value);

                // Step 5: Find the MAL entry in user's list
                var malEntry = malEntries.FirstOrDefault(entry => entry.Node.Id == malId.Value);
                if (malEntry != null)
                {
                    _logger.LogInformation("🎉 Successfully matched via Shoko: '{JellyfinTitle}' → AniDB {AniDBId} → MAL {MALId} → '{MALTitle}'",
                        jellyfinItem.Name, anidbId.Value, malId.Value, malEntry.Node.Title);
                    PluginLogger.LogToFile(LogLevel.Information, "EnhancedMatching", 
                        "Successfully matched via Shoko: '{0}' → AniDB {1} → MAL {2} → '{3}'",
                        jellyfinItem.Name, anidbId.Value, malId.Value, malEntry.Node.Title);
                    return malEntry;
                }
                else
                {
                    _logger.LogDebug("MAL ID {MALId} not found in user's anime list", malId.Value);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during Shoko matching for '{Title}': {Error}", 
                jellyfinItem.Name, ex.Message);
            PluginLogger.LogToFile(LogLevel.Error, "EnhancedMatching", ex, 
                "Error during Shoko matching for '{0}': {1}", jellyfinItem.Name, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Get statistics about the matching service
    /// </summary>
    public async Task<MatchingStatistics> GetStatisticsAsync()
    {
        var config = Plugin.Instance?.Configuration;
        var stats = new MatchingStatistics
        {
            ShokoEnabled = config?.EnableShokoIntegration ?? false,
            ShokoUrl = config?.ShokoServerUrl ?? string.Empty,
            FallbackEnabled = config?.FallbackToStringMatching ?? false
        };

        if (stats.ShokoEnabled && !string.IsNullOrEmpty(stats.ShokoUrl))
        {
            try
            {
                stats.ShokoConnected = await _shokoClient.TestConnectionAsync(stats.ShokoUrl, config?.ShokoApiKey);
            }
            catch
            {
                stats.ShokoConnected = false;
            }
        }

        var cacheStats = _mappingService.GetCacheStats();
        stats.CachedMappings = cacheStats.Count;
        stats.ExpiredMappings = cacheStats.ExpiredCount;

        return stats;
    }

    /// <summary>
    /// Process the match result and determine actions for unrated shows
    /// </summary>
    private EnhancedMatchResult ProcessMatchResult(MALAnimeEntry match, PluginConfiguration config)
    {
        var result = new EnhancedMatchResult { Match = match };

        if (match.ListStatus?.Score == 0)
        {
            // This is an unrated show in the user's MAL list
            _logger.LogDebug("🔍 Processing unrated show: '{Title}' (Score: 0)", match.Node.Title);
            PluginLogger.LogToFile(LogLevel.Debug, "EnhancedMatching", 
                "Processing unrated show: '{0}' (Score: 0)", match.Node.Title);

            result.IsUnrated = true;
            result.ShouldSetRatingToZero = config.SetUnratedRatingToZero;

            if (config.SetUnratedRatingToZero)
            {
                _logger.LogInformation("⭐ Will set unrated show '{Title}' community rating to 0", match.Node.Title);
                PluginLogger.LogToFile(LogLevel.Information, "EnhancedMatching", 
                    "Will set unrated show '{0}' community rating to 0", match.Node.Title);
            }
        }
        else
        {
            result.IsUnrated = false;
            result.ShouldSetRatingToZero = false;
        }

        return result;
    }
}

/// <summary>
/// Enhanced match result that includes actions to take for unrated shows
/// </summary>
public class EnhancedMatchResult
{
    public MALAnimeEntry? Match { get; set; }
    public bool IsUnrated { get; set; }
    public bool IsUnmatched { get; set; }
    public bool ShouldSetRatingToZero { get; set; }
}

/// <summary>
/// Statistics about the matching service
/// </summary>
public class MatchingStatistics
{
    public bool ShokoEnabled { get; set; }
    public string ShokoUrl { get; set; } = string.Empty;
    public bool ShokoConnected { get; set; }
    public bool FallbackEnabled { get; set; }
    public int CachedMappings { get; set; }
    public int ExpiredMappings { get; set; }
}