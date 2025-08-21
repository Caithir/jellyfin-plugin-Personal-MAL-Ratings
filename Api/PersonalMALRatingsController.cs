using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.PersonalMALRatings.Services;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalMALRatings.Api;

/// <summary>
/// API controller for Personal MAL Ratings plugin
/// </summary>
[ApiController]
[Route("Plugins/PersonalMALRatings")]
public class PersonalMALRatingsController : ControllerBase
{
    private readonly ILogger<PersonalMALRatingsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILibraryManager _libraryManager;

    public PersonalMALRatingsController(
        ILogger<PersonalMALRatingsController> logger,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Test MAL connection and fetch anime list
    /// </summary>
    /// <returns>Test result</returns>
    [HttpPost("test")]
    public async Task<ActionResult<TestConnectionResult>> TestConnection()
    {
        _logger.LogInformation("API: Test MAL connection requested via web interface");
        PluginLogger.LogToFile(LogLevel.Information, "API", "Test MAL connection requested via web interface");

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                var error = "Plugin configuration not available";
                _logger.LogError(error);
                PluginLogger.LogToFile(LogLevel.Error, "API", error);
                return BadRequest(new TestConnectionResult { Success = false, Message = error });
            }

            if (string.IsNullOrEmpty(config.MALAccessToken))
            {
                var error = "MAL Access Token not configured";
                _logger.LogError(error);
                PluginLogger.LogToFile(LogLevel.Error, "API", error);
                return BadRequest(new TestConnectionResult { Success = false, Message = error });
            }

            // Test token validation
            var httpClient = _httpClientFactory.CreateClient();
            var malLogger = _loggerFactory.CreateLogger<MALApiClient>();
            var malApiClient = new MALApiClient(httpClient, malLogger);

            _logger.LogInformation("API: Starting MAL token validation test");
            PluginLogger.LogToFile(LogLevel.Information, "API", "Starting MAL token validation test");

            var isValidToken = await malApiClient.ValidateTokenAsync(config.MALAccessToken, config.MALClientId);

            if (!isValidToken)
            {
                var error = "MAL token validation failed";
                _logger.LogError(error);
                PluginLogger.LogToFile(LogLevel.Error, "API", error);
                return Ok(new TestConnectionResult { Success = false, Message = error });
            }

            // Test fetching anime list
            _logger.LogInformation("API: Starting MAL anime list fetch test");
            PluginLogger.LogToFile(LogLevel.Information, "API", "Starting MAL anime list fetch test");

            var animeEntries = await malApiClient.GetUserAnimeListAsync(config);

            var message = $"Success! Fetched {animeEntries.Count} anime entries with personal scores. Check the plugin logs for detailed information.";
            _logger.LogInformation("API: Test completed successfully - {EntryCount} entries fetched", animeEntries.Count);
            PluginLogger.LogToFile(LogLevel.Information, "API", "Test completed successfully - {0} entries fetched", animeEntries.Count);

            return Ok(new TestConnectionResult 
            { 
                Success = true, 
                Message = message,
                EntriesCount = animeEntries.Count
            });
        }
        catch (Exception ex)
        {
            var error = $"Test failed with exception: {ex.Message}";
            _logger.LogError(ex, "API: Test connection failed");
            PluginLogger.LogToFile(LogLevel.Error, "API", ex, "Test connection failed");
            return Ok(new TestConnectionResult { Success = false, Message = error });
        }
    }

    /// <summary>
    /// Force refresh MAL cache
    /// </summary>
    /// <returns>Refresh result</returns>
    [HttpPost("refresh")]
    public async Task<ActionResult<TestConnectionResult>> RefreshCache()
    {
        _logger.LogInformation("API: Force cache refresh requested via web interface");
        PluginLogger.LogToFile(LogLevel.Information, "API", "Force cache refresh requested via web interface");

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                var error = "Plugin configuration not available";
                return BadRequest(new TestConnectionResult { Success = false, Message = error });
            }

            // Force refresh by simulating a metadata request
            var httpClient = _httpClientFactory.CreateClient();
            var providerLogger = _loggerFactory.CreateLogger<Providers.PersonalMALRatingProvider>();
            var loggerFactory = _loggerFactory;
            
            var provider = new Providers.PersonalMALRatingProvider(_httpClientFactory, providerLogger, loggerFactory);

            // This will trigger cache refresh
            var testSeriesInfo = new MediaBrowser.Controller.Providers.SeriesInfo { Name = "Test Series" };
            await provider.GetMetadata(testSeriesInfo, default);

            var message = "Cache refresh completed! Check the plugin logs for detailed information.";
            _logger.LogInformation("API: Cache refresh completed");
            PluginLogger.LogToFile(LogLevel.Information, "API", "Cache refresh completed");

            return Ok(new TestConnectionResult { Success = true, Message = message });
        }
        catch (Exception ex)
        {
            var error = $"Cache refresh failed: {ex.Message}";
            _logger.LogError(ex, "API: Cache refresh failed");
            PluginLogger.LogToFile(LogLevel.Error, "API", ex, "Cache refresh failed");
            return Ok(new TestConnectionResult { Success = false, Message = error });
        }
    }

    /// <summary>
    /// Force metadata updates on anime series to trigger rating provider
    /// </summary>
    /// <returns>Metadata update result</returns>
    [HttpPost("force-metadata")]
    public async Task<ActionResult<TestConnectionResult>> ForceMetadataUpdates()
    {
        _logger.LogInformation("API: Force metadata updates requested via web interface");
        PluginLogger.LogToFile(LogLevel.Information, "API", "Force metadata updates requested via web interface");

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || !config.EnabledForAnime)
            {
                var error = "Plugin is not enabled for anime";
                return BadRequest(new TestConnectionResult { Success = false, Message = error });
            }

            // Find anime series in the library
            var allItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Series },
                Recursive = true
            });

            var animeSeries = allItems.OfType<Series>()
                .ToList();

            if (!animeSeries.Any())
            {
                var error = "No anime series found in library";
                _logger.LogWarning("API: No anime series found in library");
                PluginLogger.LogToFile(LogLevel.Warning, "API", "No anime series found in library");
                return Ok(new TestConnectionResult { Success = false, Message = error });
            }

            _logger.LogInformation("API: Found {Count} anime series, testing metadata provider on each", animeSeries.Count);
            PluginLogger.LogToFile(LogLevel.Information, "API", "Found {0} anime series, testing metadata provider on each", animeSeries.Count);

            // Create metadata provider
            var providerLogger = _loggerFactory.CreateLogger<Providers.PersonalMALRatingProvider>();
            var provider = new Providers.PersonalMALRatingProvider(_httpClientFactory, providerLogger, _loggerFactory);

            var processedCount = 0;
            var updatedCount = 0;

            foreach (var series in animeSeries)
            {
                try
                {
                    _logger.LogInformation("API: Processing series: {SeriesName} (Current rating: {CurrentRating})", 
                        series.Name, series.CommunityRating?.ToString() ?? "None");
                    PluginLogger.LogToFile(LogLevel.Information, "API", "Processing series: {0} (Current rating: {1})", 
                        series.Name, series.CommunityRating?.ToString() ?? "None");

                    var seriesInfo = new SeriesInfo
                    {
                        Name = series.Name,
                        OriginalTitle = series.OriginalTitle,
                        Year = series.ProductionYear
                    };

                    var result = await provider.GetMetadata(seriesInfo, default);

                    processedCount++;

                    if (result.HasMetadata && result.Item != null)
                    {
                        bool hasChanges = false;
                        var oldRating = series.CommunityRating;

                        // Handle rating changes
                        if (result.Item.CommunityRating.HasValue)
                        {
                            var newRating = result.Item.CommunityRating.Value;
                            
                            // Check if we should overwrite existing ratings
                            if (oldRating.HasValue && !config.OverwriteExistingRatings && newRating > 0)
                            {
                                _logger.LogInformation("API: Skipping rating for {SeriesName} - already has rating {ExistingRating} and overwrite disabled", 
                                    series.Name, oldRating.Value);
                                PluginLogger.LogToFile(LogLevel.Information, "API", "Skipping rating for {0} - already has rating {1} and overwrite disabled", 
                                    series.Name, oldRating.Value);
                            }
                            else
                            {
                                series.CommunityRating = newRating;
                                hasChanges = true;
                                
                                if (newRating == 0)
                                {
                                    _logger.LogInformation("API: ⭐ Set community rating to 0 for unmatched/unrated series '{SeriesName}'", series.Name);
                                    PluginLogger.LogToFile(LogLevel.Information, "API", "⭐ Set community rating to 0 for unmatched/unrated series '{0}'", series.Name);
                                }
                                else
                                {
                                    _logger.LogInformation("API: ✅ RATING UPDATED: {SeriesName} from {OldRating} to {NewRating}", 
                                        series.Name, oldRating?.ToString() ?? "None", newRating);
                                    PluginLogger.LogToFile(LogLevel.Information, "API", "✅ RATING UPDATED: {0} from {1} to {2}", 
                                        series.Name, oldRating?.ToString() ?? "None", newRating);
                                }
                            }
                        }


                        // Save changes to database if any were made
                        if (hasChanges)
                        {
                            await _libraryManager.UpdateItemAsync(series, series.GetParent(), ItemUpdateType.MetadataEdit, default);
                            updatedCount++;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("API: No rating update for {SeriesName} (no match or no score)", series.Name);
                        PluginLogger.LogToFile(LogLevel.Information, "API", "No rating update for {0} (no match or no score)", series.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API: Error processing series {SeriesName}", series.Name);
                    PluginLogger.LogToFile(LogLevel.Error, "API", ex, "Error processing series {0}", series.Name);
                }
            }

            var message = $"Metadata updates completed! Processed {processedCount} anime series, updated {updatedCount} ratings. Check plugin logs for detailed information.";
            _logger.LogInformation("API: Metadata updates completed - {ProcessedCount} processed, {UpdatedCount} updated", 
                processedCount, updatedCount);
            PluginLogger.LogToFile(LogLevel.Information, "API", "Metadata updates completed - {0} processed, {1} updated", 
                processedCount, updatedCount);

            return Ok(new TestConnectionResult 
            { 
                Success = true, 
                Message = message,
                EntriesCount = updatedCount
            });
        }
        catch (Exception ex)
        {
            var error = $"Metadata updates failed: {ex.Message}";
            _logger.LogError(ex, "API: Metadata updates failed");
            PluginLogger.LogToFile(LogLevel.Error, "API", ex, "Metadata updates failed");
            return Ok(new TestConnectionResult { Success = false, Message = error });
        }
    }

    /// <summary>
    /// Test Shoko server connection
    /// </summary>
    /// <param name="request">Shoko test request</param>
    /// <returns>Test result</returns>
    [HttpPost("test-shoko")]
    public async Task<ActionResult<TestConnectionResult>> TestShokoConnection([FromBody] ShokoTestRequest request)
    {
        _logger.LogInformation("API: Test Shoko connection requested via web interface");
        PluginLogger.LogToFile(LogLevel.Information, "API", "Test Shoko connection requested via web interface");

        try
        {
            if (string.IsNullOrEmpty(request.ShokoServerUrl))
            {
                var error = "Shoko server URL is required";
                _logger.LogError(error);
                PluginLogger.LogToFile(LogLevel.Error, "API", error);
                return BadRequest(new TestConnectionResult { Success = false, Message = error });
            }

            // Test Shoko connection
            var shokoLogger = _loggerFactory.CreateLogger<ShokoApiClient>();
            var shokoClient = new ShokoApiClient(_httpClientFactory, shokoLogger);

            _logger.LogInformation("API: Starting Shoko connection test to: {ShokoUrl}", request.ShokoServerUrl);
            PluginLogger.LogToFile(LogLevel.Information, "API", "Starting Shoko connection test to: {0}", request.ShokoServerUrl);

            var isConnected = await shokoClient.TestConnectionAsync(request.ShokoServerUrl, request.ShokoApiKey);

            if (!isConnected)
            {
                var error = "Shoko server connection failed. Check server URL and ensure Shoko Server is running.";
                _logger.LogError(error);
                PluginLogger.LogToFile(LogLevel.Error, "API", error);
                return Ok(new TestConnectionResult { Success = false, Message = error });
            }

            // Test searching for a sample anime
            _logger.LogInformation("API: Testing Shoko anime search functionality");
            PluginLogger.LogToFile(LogLevel.Information, "API", "Testing Shoko anime search functionality");

            var searchResults = await shokoClient.SearchSeriesByNameAsync("Attack on Titan", request.ShokoServerUrl, request.ShokoApiKey);

            var message = $"Success! Shoko server is connected and accessible. Found {searchResults.Count} results for test search. Check the plugin logs for detailed information.";
            _logger.LogInformation("API: Shoko test completed successfully - {ResultCount} search results", searchResults.Count);
            PluginLogger.LogToFile(LogLevel.Information, "API", "Shoko test completed successfully - {0} search results", searchResults.Count);

            return Ok(new TestConnectionResult 
            { 
                Success = true, 
                Message = message,
                EntriesCount = searchResults.Count
            });
        }
        catch (Exception ex)
        {
            var error = $"Shoko test failed with exception: {ex.Message}";
            _logger.LogError(ex, "API: Shoko connection test failed");
            PluginLogger.LogToFile(LogLevel.Error, "API", ex, "Shoko connection test failed");
            return Ok(new TestConnectionResult { Success = false, Message = error });
        }
    }
}

/// <summary>
/// Test connection result
/// </summary>
public class TestConnectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? EntriesCount { get; set; }
}

/// <summary>
/// Shoko test request
/// </summary>
public class ShokoTestRequest
{
    public string ShokoServerUrl { get; set; } = string.Empty;
    public string? ShokoApiKey { get; set; }
}