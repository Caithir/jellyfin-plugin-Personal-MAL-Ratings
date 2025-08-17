using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Plugin.PersonalMALRatings.Configuration;
using Jellyfin.Plugin.PersonalMALRatings.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalMALRatings.Services;

public class MALApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MALApiClient> _logger;
    private const string BaseUrl = "https://api.myanimelist.net/v2";

    public MALApiClient(HttpClient httpClient, ILogger<MALApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<MALAnimeEntry>> GetUserAnimeListAsync(PluginConfiguration config, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting MAL API request to fetch user anime list");
        PluginLogger.LogToFile(LogLevel.Information, "MALApiClient", "Starting MAL API request to fetch user anime list");
        
        if (string.IsNullOrEmpty(config.MALAccessToken))
        {
            _logger.LogError("MAL access token is not configured - cannot proceed with API request");
            PluginLogger.LogToFile(LogLevel.Error, "MALApiClient", "MAL access token is not configured - cannot proceed with API request");
            throw new InvalidOperationException("MAL access token is required");
        }

        if (string.IsNullOrEmpty(config.MALClientId))
        {
            _logger.LogWarning("MAL Client ID is not configured - this may cause authentication issues");
        }

        _logger.LogDebug("Using MAL Client ID: {ClientId}", string.IsNullOrEmpty(config.MALClientId) ? "[Not Set]" : config.MALClientId);
        _logger.LogDebug("Access token configured: {TokenConfigured}", !string.IsNullOrEmpty(config.MALAccessToken));

        var allEntries = new List<MALAnimeEntry>();
        var url = $"{BaseUrl}/users/@me/animelist?fields=list_status,alternative_titles&limit=1000";
        _logger.LogDebug("Initial request URL: {Url}", url);

        var pageCount = 0;
        while (!string.IsNullOrEmpty(url))
        {
            pageCount++;
            _logger.LogDebug("Making API request to page {PageCount}: {Url}", pageCount, url);
            
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {config.MALAccessToken}");
                
                if (!string.IsNullOrEmpty(config.MALClientId))
                {
                    request.Headers.Add("X-MAL-Client-ID", config.MALClientId);
                    _logger.LogDebug("Added X-MAL-Client-ID header: {ClientId}", config.MALClientId);
                }
                else
                {
                    _logger.LogWarning("X-MAL-Client-ID header not added - Client ID is missing");
                }

                _logger.LogDebug("Sending HTTP request to MAL API...");
                var response = await _httpClient.SendAsync(request, cancellationToken);
                _logger.LogDebug("Received HTTP response with status: {StatusCode}", response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to fetch user anime list. Status: {StatusCode}, Reason: {ReasonPhrase}, Response: {Response}", 
                        response.StatusCode, response.ReasonPhrase, errorContent);
                    
                    // Check for common auth issues
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogError("Authentication failed - check if your access token is valid and not expired");
                    }
                    break;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("Successfully received JSON response of length: {Length}", json.Length);
                
                var listResponse = JsonSerializer.Deserialize<MALUserAnimeListResponse>(json);

                if (listResponse?.Data != null)
                {
                    var entriesWithScores = listResponse.Data.Where(e => e.ListStatus?.Score > 0).ToArray();
                    allEntries.AddRange(listResponse.Data);
                    _logger.LogDebug("Page {PageCount}: Fetched {TotalCount} anime entries ({WithScores} have scores > 0)", 
                        pageCount, listResponse.Data.Length, entriesWithScores.Length);
                }
                else
                {
                    _logger.LogWarning("Page {PageCount}: No data in response or failed to deserialize", pageCount);
                }

                url = listResponse?.Paging?.Next;
                if (!string.IsNullOrEmpty(url))
                {
                    _logger.LogDebug("Next page URL found: {NextUrl}", url);
                }
                else
                {
                    _logger.LogDebug("No more pages to fetch");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user anime list from MAL on page {PageCount}", pageCount);
                break;
            }
        }

        var scoredEntries = allEntries.Where(e => e.ListStatus?.Score > 0).ToList();
        _logger.LogInformation("Successfully fetched {TotalCount} anime entries from MAL ({ScoredCount} have personal scores)", 
            allEntries.Count, scoredEntries.Count);
        PluginLogger.LogToFile(LogLevel.Information, "MALApiClient", "Successfully fetched {0} anime entries from MAL ({1} have personal scores)", 
            allEntries.Count, scoredEntries.Count);
        
        if (scoredEntries.Count == 0)
        {
            _logger.LogWarning("No anime entries with personal scores found - check if you have rated any anime on MyAnimeList");
            PluginLogger.LogToFile(LogLevel.Warning, "MALApiClient", "No anime entries with personal scores found - check if you have rated any anime on MyAnimeList");
        }
        
        return scoredEntries;
    }

    public async Task<bool> ValidateTokenAsync(string accessToken, string? clientId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating MAL access token");
        PluginLogger.LogToFile(LogLevel.Information, "MALApiClient", "Validating MAL access token");
        
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError("Cannot validate empty access token");
            PluginLogger.LogToFile(LogLevel.Error, "MALApiClient", "Cannot validate empty access token");
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/users/@me");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            
            if (!string.IsNullOrEmpty(clientId))
            {
                request.Headers.Add("X-MAL-Client-ID", clientId);
                _logger.LogDebug("Added X-MAL-Client-ID header for validation: {ClientId}", clientId);
            }
            else
            {
                _logger.LogWarning("No Client ID provided for token validation");
            }

            _logger.LogDebug("Sending token validation request to: {Url}", $"{BaseUrl}/users/@me");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            _logger.LogDebug("Token validation response: {StatusCode} - {ReasonPhrase}", 
                response.StatusCode, response.ReasonPhrase);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("MAL token validation successful");
                _logger.LogDebug("User info response length: {Length}", responseContent.Length);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MAL token validation failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Token is invalid or expired - please refresh your access token");
                }
                
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating MAL token");
            return false;
        }
    }
}