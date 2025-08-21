using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.PersonalMALRatings.Configuration;
using Jellyfin.Plugin.PersonalMALRatings.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalMALRatings.Services;

/// <summary>
/// Client for interacting with Shoko Server API to get anime metadata and AniDB IDs
/// </summary>
public class ShokoApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShokoApiClient> _logger;
    private readonly object _lockObject = new();

    public ShokoApiClient(IHttpClientFactory httpClientFactory, ILogger<ShokoApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Test connection to Shoko server
    /// </summary>
    public async Task<bool> TestConnectionAsync(string baseUrl, string? apiKey = null)
    {
        try
        {
            _logger.LogInformation("Testing connection to Shoko server at: {BaseUrl}", baseUrl);
            PluginLogger.LogToFile(LogLevel.Information, "ShokoApiClient", 
                "Testing connection to Shoko server at: {0}", baseUrl);

            using var client = CreateHttpClient(baseUrl, apiKey);
            
            // Test with a simple endpoint - get server info
            var response = await client.GetAsync("api/v3/init/status");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ Shoko server connection successful. Response: {Response}", content);
                PluginLogger.LogToFile(LogLevel.Information, "ShokoApiClient", 
                    "Shoko server connection successful");
                return true;
            }

            _logger.LogWarning("❌ Shoko server connection failed. Status: {StatusCode}", response.StatusCode);
            PluginLogger.LogToFile(LogLevel.Warning, "ShokoApiClient", 
                "Shoko server connection failed. Status: {0}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error testing Shoko server connection: {Error}", ex.Message);
            PluginLogger.LogToFile(LogLevel.Error, "ShokoApiClient", 
                "Error testing Shoko server connection: {0}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Search for anime series by name in Shoko
    /// </summary>
    public async Task<List<ShokoSeries>> SearchSeriesByNameAsync(string seriesName, string baseUrl, string? apiKey = null)
    {
        try
        {
            _logger.LogDebug("🔍 Searching Shoko for series: '{SeriesName}'", seriesName);
            PluginLogger.LogToFile(LogLevel.Debug, "ShokoApiClient", 
                "Searching Shoko for series: '{0}'", seriesName);

            using var client = CreateHttpClient(baseUrl, apiKey);
            
            // Use fuzzy search endpoint
            var encodedName = Uri.EscapeDataString(seriesName);
            var endpoint = $"api/v3/Series/Search?query={encodedName}&fuzzy=true&limit=10";
            
            var response = await client.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Shoko search failed. Status: {StatusCode}, Series: '{SeriesName}'", 
                    response.StatusCode, seriesName);
                PluginLogger.LogToFile(LogLevel.Warning, "ShokoApiClient", 
                    "Shoko search failed. Status: {0}, Series: '{1}'", response.StatusCode, seriesName);
                return new List<ShokoSeries>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<List<ShokoSeries>>(content, GetJsonOptions());

            if (searchResults?.Any() == true)
            {
                _logger.LogInformation("✅ Found {Count} Shoko series results for '{SeriesName}'", 
                    searchResults.Count, seriesName);
                PluginLogger.LogToFile(LogLevel.Information, "ShokoApiClient", 
                    "Found {0} Shoko series results for '{1}'", searchResults.Count, seriesName);

                // Log the top results for debugging
                foreach (var series in searchResults.Take(3))
                {
                    var anidbId = series.IDs?.AniDB ?? 0;
                    _logger.LogDebug("  - '{SeriesName}' (AniDB: {AniDBId})", 
                        series.Name ?? series.AniDB?.Title ?? "Unknown", anidbId);
                }
            }
            else
            {
                _logger.LogInformation("No Shoko series found for '{SeriesName}'", seriesName);
                PluginLogger.LogToFile(LogLevel.Information, "ShokoApiClient", 
                    "No Shoko series found for '{0}'", seriesName);
            }

            return searchResults ?? new List<ShokoSeries>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error searching Shoko for series '{SeriesName}': {Error}", 
                seriesName, ex.Message);
            PluginLogger.LogToFile(LogLevel.Error, "ShokoApiClient", 
                "Error searching Shoko for series '{0}': {1}", seriesName, ex.Message);
            return new List<ShokoSeries>();
        }
    }

    /// <summary>
    /// Get series details by Shoko series ID
    /// </summary>
    public async Task<ShokoSeries?> GetSeriesByIdAsync(int seriesId, string baseUrl, string? apiKey = null)
    {
        try
        {
            _logger.LogDebug("📖 Getting Shoko series details for ID: {SeriesId}", seriesId);
            PluginLogger.LogToFile(LogLevel.Debug, "ShokoApiClient", 
                "Getting Shoko series details for ID: {0}", seriesId);

            using var client = CreateHttpClient(baseUrl, apiKey);
            
            var response = await client.GetAsync($"api/v3/Series/{seriesId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Shoko series {SeriesId}. Status: {StatusCode}", 
                    seriesId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var series = JsonSerializer.Deserialize<ShokoSeries>(content, GetJsonOptions());

            if (series != null)
            {
                var anidbId = series.IDs?.AniDB ?? 0;
                _logger.LogDebug("✅ Retrieved Shoko series: '{SeriesName}' (AniDB: {AniDBId})", 
                    series.Name ?? series.AniDB?.Title ?? "Unknown", anidbId);
            }

            return series;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting Shoko series {SeriesId}: {Error}", seriesId, ex.Message);
            PluginLogger.LogToFile(LogLevel.Error, "ShokoApiClient", 
                "Error getting Shoko series {0}: {1}", seriesId, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Find anime series in Shoko by file path
    /// </summary>
    public async Task<List<ShokoSeries>> GetSeriesByFilePathAsync(string filePath, string baseUrl, string? apiKey = null)
    {
        try
        {
            _logger.LogDebug("🔍 Searching Shoko for series by file path: '{FilePath}'", filePath);
            PluginLogger.LogToFile(LogLevel.Debug, "ShokoApiClient", 
                "Searching Shoko for series by file path: '{0}'", filePath);

            using var client = CreateHttpClient(baseUrl, apiKey);
            
            // First try to find the file in Shoko
            var encodedPath = Uri.EscapeDataString(filePath);
            var fileResponse = await client.GetAsync($"api/v3/File/PathEndsWith/{encodedPath}");
            
            if (!fileResponse.IsSuccessStatusCode)
            {
                _logger.LogDebug("File not found in Shoko for path: '{FilePath}'", filePath);
                return new List<ShokoSeries>();
            }

            var fileContent = await fileResponse.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<ShokoFile>>(fileContent, GetJsonOptions());

            if (files?.Any() != true)
            {
                _logger.LogDebug("No files found in Shoko for path: '{FilePath}'", filePath);
                return new List<ShokoSeries>();
            }

            // Get series from the file's series IDs
            var series = new List<ShokoSeries>();
            var file = files.First();
            
            if (file.SeriesIDs?.Any() == true)
            {
                foreach (var seriesId in file.SeriesIDs)
                {
                    var seriesData = await GetSeriesByIdAsync(seriesId, baseUrl, apiKey);
                    if (seriesData != null)
                    {
                        series.Add(seriesData);
                    }
                }
            }

            if (series.Any())
            {
                _logger.LogInformation("✅ Found {Count} Shoko series for file path '{FilePath}'", 
                    series.Count, filePath);
                PluginLogger.LogToFile(LogLevel.Information, "ShokoApiClient", 
                    "Found {0} Shoko series for file path '{1}'", series.Count, filePath);
            }

            return series;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error searching Shoko by file path '{FilePath}': {Error}", 
                filePath, ex.Message);
            PluginLogger.LogToFile(LogLevel.Error, "ShokoApiClient", 
                "Error searching Shoko by file path '{0}': {1}", filePath, ex.Message);
            return new List<ShokoSeries>();
        }
    }

    /// <summary>
    /// Extract AniDB ID from Shoko series data
    /// </summary>
    public int? GetAniDBId(ShokoSeries series)
    {
        var anidbId = series.IDs?.AniDB ?? series.AniDB?.ID;
        
        if (anidbId.HasValue && anidbId.Value > 0)
        {
            _logger.LogDebug("🎯 Extracted AniDB ID: {AniDBId} from series: '{SeriesName}'", 
                anidbId, series.Name ?? series.AniDB?.Title ?? "Unknown");
            return anidbId;
        }

        _logger.LogWarning("⚠️ No valid AniDB ID found in Shoko series: '{SeriesName}'", 
            series.Name ?? series.AniDB?.Title ?? "Unknown");
        return null;
    }

    /// <summary>
    /// Create HTTP client with proper configuration for Shoko API
    /// </summary>
    private HttpClient CreateHttpClient(string baseUrl, string? apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
        
        // Add API key if provided
        if (!string.IsNullOrEmpty(apiKey))
        {
            client.DefaultRequestHeaders.Add("ApiKey", apiKey);
        }

        // Set user agent
        client.DefaultRequestHeaders.Add("User-Agent", "Jellyfin-PersonalMALRatings-Plugin/1.0");

        return client;
    }

    /// <summary>
    /// Get JSON serialization options for Shoko API responses
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}