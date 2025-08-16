using System.Net.Http;
using Jellyfin.Plugin.PersonalMALRatings.Models;
using Jellyfin.Plugin.PersonalMALRatings.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalMALRatings.Providers;

public class PersonalMALRatingProvider : IRemoteMetadataProvider<Series, SeriesInfo>,
    IRemoteMetadataProvider<Season, SeasonInfo>,
    IRemoteMetadataProvider<Episode, EpisodeInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PersonalMALRatingProvider> _logger;
    private readonly MALApiClient _malApiClient;
    private readonly AnimeMatchingService _matchingService;
    private static readonly object CacheLock = new();
    private static List<MALAnimeEntry>? _cachedEntries;
    private static DateTime _lastCacheUpdate = DateTime.MinValue;

    public string Name => "Personal MAL Rating Provider";

    public PersonalMALRatingProvider(IHttpClientFactory httpClientFactory, ILogger<PersonalMALRatingProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _malApiClient = new MALApiClient(_httpClientFactory.CreateClient(), logger);
        _matchingService = new AnimeMatchingService(logger);
    }

    public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Series>();

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || !config.EnabledForAnime || string.IsNullOrEmpty(config.MALAccessToken))
            {
                return result;
            }

            var malEntries = await GetCachedMALEntries(config, cancellationToken);
            if (malEntries == null || malEntries.Count == 0)
            {
                return result;
            }

            var series = new Series
            {
                Name = info.Name,
                OriginalTitle = info.OriginalTitle
            };

            var match = _matchingService.FindMatch(series, malEntries);
            if (match?.ListStatus?.Score > 0)
            {
                result.Item = series;
                result.HasMetadata = true;

                var rating = ConvertMALScoreToRating(match.ListStatus.Score);
                result.Item.CommunityRating = rating;
                
                _logger.LogInformation("Updated rating for {SeriesName} to {Rating} (MAL Score: {MALScore})", 
                    info.Name, rating, match.ListStatus.Score);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for series {SeriesName}", info.Name);
        }

        return result;
    }

    public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Season>();

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || !config.EnabledForAnime || string.IsNullOrEmpty(config.MALAccessToken))
            {
                return result;
            }

            var malEntries = await GetCachedMALEntries(config, cancellationToken);
            if (malEntries == null || malEntries.Count == 0)
            {
                return result;
            }

            var season = new Season
            {
                Name = info.Name,
                SeriesName = info.SeriesName
            };

            var searchName = !string.IsNullOrEmpty(info.SeriesName) ? info.SeriesName : info.Name;
            var tempSeries = new Series { Name = searchName };
            
            var match = _matchingService.FindMatch(tempSeries, malEntries);
            if (match?.ListStatus?.Score > 0)
            {
                result.Item = season;
                result.HasMetadata = true;

                var rating = ConvertMALScoreToRating(match.ListStatus.Score);
                result.Item.CommunityRating = rating;
                
                _logger.LogInformation("Updated rating for season {SeasonName} to {Rating} (MAL Score: {MALScore})", 
                    info.Name, rating, match.ListStatus.Score);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for season {SeasonName}", info.Name);
        }

        return result;
    }

    public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Episode>();

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || !config.EnabledForAnime || string.IsNullOrEmpty(config.MALAccessToken))
            {
                return result;
            }

            var malEntries = await GetCachedMALEntries(config, cancellationToken);
            if (malEntries == null || malEntries.Count == 0)
            {
                return result;
            }

            var episode = new Episode
            {
                Name = info.Name,
                SeriesName = info.SeriesName
            };

            var searchName = !string.IsNullOrEmpty(info.SeriesName) ? info.SeriesName : info.Name;
            var tempSeries = new Series { Name = searchName };
            
            var match = _matchingService.FindMatch(tempSeries, malEntries);
            if (match?.ListStatus?.Score > 0)
            {
                result.Item = episode;
                result.HasMetadata = true;

                var rating = ConvertMALScoreToRating(match.ListStatus.Score);
                result.Item.CommunityRating = rating;
                
                _logger.LogDebug("Updated rating for episode {EpisodeName} to {Rating} (MAL Score: {MALScore})", 
                    info.Name, rating, match.ListStatus.Score);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for episode {EpisodeName}", info.Name);
        }

        return result;
    }

    private async Task<List<MALAnimeEntry>?> GetCachedMALEntries(Configuration.PluginConfiguration config, CancellationToken cancellationToken)
    {
        lock (CacheLock)
        {
            var cacheExpiry = TimeSpan.FromHours(config.RefreshIntervalHours);
            if (_cachedEntries != null && DateTime.UtcNow - _lastCacheUpdate < cacheExpiry)
            {
                return _cachedEntries;
            }
        }

        try
        {
            var entries = await _malApiClient.GetUserAnimeListAsync(config, cancellationToken);
            
            lock (CacheLock)
            {
                _cachedEntries = entries;
                _lastCacheUpdate = DateTime.UtcNow;
            }
            
            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch MAL entries");
            return _cachedEntries;
        }
    }

    private static float ConvertMALScoreToRating(int malScore)
    {
        return malScore switch
        {
            10 => 10.0f,
            9 => 9.0f,
            8 => 8.0f,
            7 => 7.0f,
            6 => 6.0f,
            5 => 5.0f,
            4 => 4.0f,
            3 => 3.0f,
            2 => 2.0f,
            1 => 1.0f,
            _ => 0.0f
        };
    }

    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
    {
        return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
    }

    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
    {
        return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
    }

    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
    {
        return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}