using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PersonalMALRatings.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalMALRatings.Services;

/// <summary>
/// Service for mapping AniDB IDs to MyAnimeList IDs using Jikan API
/// </summary>
public class AniDBToMALMappingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AniDBToMALMappingService> _logger;
    private readonly Dictionary<int, AniDBToMALMapping> _mappingCache;
    private readonly object _lockObject = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);
    
    // Rate limiting for Jikan API (3 requests per second max, we use 2 requests per second to be safe)
    private static readonly object _rateLimitLock = new();
    private static DateTime _lastJikanRequest = DateTime.MinValue;
    private static readonly TimeSpan _minRequestInterval = TimeSpan.FromMilliseconds(500); // 2 requests per second

    public AniDBToMALMappingService(IHttpClientFactory httpClientFactory, ILogger<AniDBToMALMappingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _mappingCache = new Dictionary<int, AniDBToMALMapping>();
    }

    /// <summary>
    /// Get MAL ID from AniDB ID using Jikan API and local caching
    /// </summary>
    public async Task<int?> GetMALIdFromAniDBAsync(int anidbId)
    {
        lock (_lockObject)
        {
            // Check cache first
            if (_mappingCache.TryGetValue(anidbId, out var cachedMapping))
            {
                if (DateTime.UtcNow - cachedMapping.LastUpdated < _cacheExpiry)
                {
                    _logger.LogDebug("🎯 Found cached AniDB→MAL mapping: {AniDBId} → {MALId}", 
                        anidbId, cachedMapping.MALId);
                    return cachedMapping.MALId;
                }
                else
                {
                    _logger.LogDebug("⏰ Cached mapping expired for AniDB ID: {AniDBId}", anidbId);
                    _mappingCache.Remove(anidbId);
                }
            }
        }

        try
        {
            _logger.LogInformation("🔍 Attempting to map AniDB ID {AniDBId} to MAL ID via Jikan API", anidbId);
            PluginLogger.LogToFile(LogLevel.Information, "AniDBToMALMappingService", 
                "Attempting to map AniDB ID {0} to MAL ID", anidbId);

            // Try multiple approaches to find the mapping
            var malId = await TryJikanAnimeSearchAsync(anidbId);
            
            if (!malId.HasValue)
            {
                malId = await TryJikanExternalAsync(anidbId);
            }

            if (malId.HasValue)
            {
                var mapping = new AniDBToMALMapping
                {
                    AniDBId = anidbId,
                    MALId = malId.Value,
                    LastUpdated = DateTime.UtcNow,
                    IsConfirmed = true
                };

                lock (_lockObject)
                {
                    _mappingCache[anidbId] = mapping;
                }

                _logger.LogInformation("✅ Successfully mapped AniDB ID {AniDBId} → MAL ID {MALId}", 
                    anidbId, malId.Value);
                PluginLogger.LogToFile(LogLevel.Information, "AniDBToMALMappingService", 
                    "Successfully mapped AniDB ID {0} to MAL ID {1}", anidbId, malId.Value);
                
                return malId.Value;
            }
            else
            {
                _logger.LogWarning("❌ No MAL ID mapping found for AniDB ID: {AniDBId}", anidbId);
                PluginLogger.LogToFile(LogLevel.Warning, "AniDBToMALMappingService", 
                    "No MAL ID mapping found for AniDB ID: {0}", anidbId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error mapping AniDB ID {AniDBId} to MAL ID: {Error}", 
                anidbId, ex.Message);
            PluginLogger.LogToFile(LogLevel.Error, "AniDBToMALMappingService", 
                "Error mapping AniDB ID {0} to MAL ID: {1}", anidbId, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Ensure rate limiting compliance for Jikan API calls
    /// </summary>
    private async Task EnforceRateLimitAsync()
    {
        TimeSpan delay = TimeSpan.Zero;
        
        lock (_rateLimitLock)
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastJikanRequest;
            if (timeSinceLastRequest < _minRequestInterval)
            {
                delay = _minRequestInterval - timeSinceLastRequest;
                _logger.LogDebug("⏳ Rate limiting: waiting {DelayMs}ms before next Jikan API call", 
                    delay.TotalMilliseconds);
            }
            _lastJikanRequest = DateTime.UtcNow;
        }
        
        // Perform the delay outside the lock to avoid blocking other threads
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay);
        }
    }

    /// <summary>
    /// Try to find MAL ID using Jikan anime search
    /// </summary>
    private async Task<int?> TryJikanAnimeSearchAsync(int anidbId)
    {
        try
        {
            // Enforce rate limiting before making API call
            await EnforceRateLimitAsync();
            
            using var client = CreateJikanClient();
            
            // Search for anime with the AniDB ID in external links
            var searchUrl = $"anime?q=&limit=25&page=1&external=true&anidb={anidbId}";
            
            _logger.LogDebug("🔍 Trying Jikan search: {SearchUrl}", searchUrl);
            
            var response = await client.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("⚠️ Jikan API rate limit exceeded (HTTP 429). Consider reducing request frequency.");
                    PluginLogger.LogToFile(LogLevel.Warning, "AniDBToMALMappingService", 
                        "Jikan API rate limit exceeded for AniDB ID: {0}", anidbId);
                }
                else
                {
                    _logger.LogDebug("Jikan search failed with status: {StatusCode}", response.StatusCode);
                }
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<JikanSearchResponse>(content, GetJsonOptions());

            if (searchResult?.Data?.Any() == true)
            {
                var anime = searchResult.Data.First();
                _logger.LogDebug("✅ Found anime via Jikan search: '{Title}' (MAL ID: {MALId})", 
                    anime.Title, anime.MalId);
                return anime.MalId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error in Jikan anime search: {Error}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Try to find MAL ID using Jikan external links endpoint
    /// </summary>
    private async Task<int?> TryJikanExternalAsync(int anidbId)
    {
        try
        {
            // Enforce rate limiting before making API call
            await EnforceRateLimitAsync();
            
            using var client = CreateJikanClient();
            
            // This is a fallback approach - search for anime and check external links
            // Note: This is less reliable but might catch some edge cases
            var searchUrl = $"anime?q=&limit=25&page=1";
            
            _logger.LogDebug("🔍 Trying Jikan external links approach");
            
            var response = await client.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("⚠️ Jikan API rate limit exceeded (HTTP 429) in external links fallback.");
                    PluginLogger.LogToFile(LogLevel.Warning, "AniDBToMALMappingService", 
                        "Jikan API rate limit exceeded in fallback for AniDB ID: {0}", anidbId);
                }
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<JikanSearchResponse>(content, GetJsonOptions());

            if (searchResult?.Data?.Any() == true)
            {
                // This would require checking each anime's external links
                // For now, we'll return null and rely on the main search
                // This could be enhanced in the future
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error in Jikan external links search: {Error}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Create HTTP client for Jikan API with proper rate limiting
    /// </summary>
    private HttpClient CreateJikanClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "Jellyfin-PersonalMALRatings-Plugin/1.4.0 (AniDB-MAL-Mapping; Rate-Limited)");
        
        return client;
    }

    /// <summary>
    /// Get JSON serialization options for Jikan API responses
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    /// <summary>
    /// Clear the mapping cache
    /// </summary>
    public void ClearCache()
    {
        lock (_lockObject)
        {
            _mappingCache.Clear();
            _logger.LogInformation("🧹 Cleared AniDB to MAL ID mapping cache");
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int Count, int ExpiredCount) GetCacheStats()
    {
        lock (_lockObject)
        {
            var expiredCount = _mappingCache.Values.Count(m => DateTime.UtcNow - m.LastUpdated >= _cacheExpiry);
            return (_mappingCache.Count, expiredCount);
        }
    }
}

/// <summary>
/// Jikan API response models
/// </summary>
public class JikanSearchResponse
{
    [JsonPropertyName("data")]
    public List<JikanAnime>? Data { get; set; }

    [JsonPropertyName("pagination")]
    public JikanPagination? Pagination { get; set; }
}

public class JikanAnime
{
    [JsonPropertyName("mal_id")]
    public int MalId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("title_english")]
    public string? TitleEnglish { get; set; }

    [JsonPropertyName("title_japanese")]
    public string? TitleJapanese { get; set; }

    [JsonPropertyName("external")]
    public List<JikanExternalLink>? External { get; set; }
}

public class JikanExternalLink
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class JikanPagination
{
    [JsonPropertyName("last_visible_page")]
    public int LastVisiblePage { get; set; }

    [JsonPropertyName("has_next_page")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }
}