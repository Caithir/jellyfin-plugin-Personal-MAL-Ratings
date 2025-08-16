using System.Net.Http;
using System.Text.Json;
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
        if (string.IsNullOrEmpty(config.MALAccessToken))
        {
            _logger.LogError("MAL access token is not configured");
            throw new InvalidOperationException("MAL access token is required");
        }

        var allEntries = new List<MALAnimeEntry>();
        var url = $"{BaseUrl}/users/@me/animelist?fields=list_status,alternative_titles&limit=1000";

        while (!string.IsNullOrEmpty(url))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {config.MALAccessToken}");
                
                if (!string.IsNullOrEmpty(config.MALClientId))
                {
                    request.Headers.Add("X-MAL-Client-ID", config.MALClientId);
                }

                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch user anime list. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
                    break;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var listResponse = JsonSerializer.Deserialize<MALUserAnimeListResponse>(json);

                if (listResponse?.Data != null)
                {
                    allEntries.AddRange(listResponse.Data);
                    _logger.LogDebug("Fetched {Count} anime entries", listResponse.Data.Length);
                }

                url = listResponse?.Paging?.Next;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user anime list from MAL");
                break;
            }
        }

        _logger.LogInformation("Successfully fetched {TotalCount} anime entries from MAL", allEntries.Count);
        return allEntries.Where(e => e.ListStatus?.Score > 0).ToList();
    }

    public async Task<bool> ValidateTokenAsync(string accessToken, string? clientId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/users/@me");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            
            if (!string.IsNullOrEmpty(clientId))
            {
                request.Headers.Add("X-MAL-Client-ID", clientId);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating MAL token");
            return false;
        }
    }
}