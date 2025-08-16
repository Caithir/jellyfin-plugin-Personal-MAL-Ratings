using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.PersonalMALRatings.Models;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalMALRatings.Services;

public class AnimeMatchingService
{
    private readonly ILogger<AnimeMatchingService> _logger;
    private static readonly Regex SeasonPattern = new(@"Season\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex YearPattern = new(@"\((\d{4})\)", RegexOptions.Compiled);
    private static readonly Regex SpecialCharsPattern = new(@"[^\w\s]", RegexOptions.Compiled);

    public AnimeMatchingService(ILogger<AnimeMatchingService> logger)
    {
        _logger = logger;
    }

    public MALAnimeEntry? FindMatch(BaseItem jellyfinItem, List<MALAnimeEntry> malEntries)
    {
        if (jellyfinItem == null || string.IsNullOrEmpty(jellyfinItem.Name))
        {
            return null;
        }

        var jellyfinTitle = jellyfinItem.Name;
        _logger.LogDebug("Attempting to match Jellyfin item: {Title}", jellyfinTitle);

        var exactMatch = FindExactMatch(jellyfinTitle, malEntries);
        if (exactMatch != null)
        {
            _logger.LogDebug("Found exact match for {Title}: {MALTitle}", jellyfinTitle, exactMatch.Node.Title);
            return exactMatch;
        }

        var normalizedMatch = FindNormalizedMatch(jellyfinTitle, malEntries);
        if (normalizedMatch != null)
        {
            _logger.LogDebug("Found normalized match for {Title}: {MALTitle}", jellyfinTitle, normalizedMatch.Node.Title);
            return normalizedMatch;
        }

        var fuzzyMatch = FindFuzzyMatch(jellyfinTitle, malEntries);
        if (fuzzyMatch != null)
        {
            _logger.LogDebug("Found fuzzy match for {Title}: {MALTitle}", jellyfinTitle, fuzzyMatch.Node.Title);
            return fuzzyMatch;
        }

        _logger.LogDebug("No match found for {Title}", jellyfinTitle);
        return null;
    }

    private MALAnimeEntry? FindExactMatch(string jellyfinTitle, List<MALAnimeEntry> malEntries)
    {
        return malEntries.FirstOrDefault(entry =>
            string.Equals(entry.Node.Title, jellyfinTitle, StringComparison.OrdinalIgnoreCase) ||
            (entry.Node.AlternativeTitles?.English != null && 
             string.Equals(entry.Node.AlternativeTitles.English, jellyfinTitle, StringComparison.OrdinalIgnoreCase)) ||
            (entry.Node.AlternativeTitles?.Synonyms?.Any(s => 
                string.Equals(s, jellyfinTitle, StringComparison.OrdinalIgnoreCase)) == true));
    }

    private MALAnimeEntry? FindNormalizedMatch(string jellyfinTitle, List<MALAnimeEntry> malEntries)
    {
        var normalizedJellyfin = NormalizeTitle(jellyfinTitle);

        return malEntries.FirstOrDefault(entry =>
        {
            var normalizedMAL = NormalizeTitle(entry.Node.Title);
            if (string.Equals(normalizedMAL, normalizedJellyfin, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (entry.Node.AlternativeTitles?.English != null)
            {
                var normalizedEnglish = NormalizeTitle(entry.Node.AlternativeTitles.English);
                if (string.Equals(normalizedEnglish, normalizedJellyfin, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (entry.Node.AlternativeTitles?.Synonyms != null)
            {
                return entry.Node.AlternativeTitles.Synonyms.Any(synonym =>
                {
                    var normalizedSynonym = NormalizeTitle(synonym);
                    return string.Equals(normalizedSynonym, normalizedJellyfin, StringComparison.OrdinalIgnoreCase);
                });
            }

            return false;
        });
    }

    private MALAnimeEntry? FindFuzzyMatch(string jellyfinTitle, List<MALAnimeEntry> malEntries)
    {
        var jellyfinWords = GetSignificantWords(jellyfinTitle);
        
        if (jellyfinWords.Count == 0)
        {
            return null;
        }

        var bestMatch = malEntries
            .Select(entry => new
            {
                Entry = entry,
                Score = CalculateMatchScore(jellyfinWords, entry)
            })
            .Where(x => x.Score > 0.7)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return bestMatch?.Entry;
    }

    private double CalculateMatchScore(List<string> jellyfinWords, MALAnimeEntry malEntry)
    {
        var malTitles = new List<string> { malEntry.Node.Title };
        
        if (!string.IsNullOrEmpty(malEntry.Node.AlternativeTitles?.English))
        {
            malTitles.Add(malEntry.Node.AlternativeTitles.English);
        }

        if (malEntry.Node.AlternativeTitles?.Synonyms != null)
        {
            malTitles.AddRange(malEntry.Node.AlternativeTitles.Synonyms);
        }

        double bestScore = 0;

        foreach (var malTitle in malTitles)
        {
            var malWords = GetSignificantWords(malTitle);
            if (malWords.Count == 0) continue;

            var matchingWords = jellyfinWords.Intersect(malWords, StringComparer.OrdinalIgnoreCase).Count();
            var score = (double)matchingWords / Math.Max(jellyfinWords.Count, malWords.Count);
            
            bestScore = Math.Max(bestScore, score);
        }

        return bestScore;
    }

    private string NormalizeTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return string.Empty;
        }

        title = SeasonPattern.Replace(title, "S$1");
        title = YearPattern.Replace(title, "");
        title = SpecialCharsPattern.Replace(title, " ");
        title = Regex.Replace(title, @"\s+", " ").Trim();

        return title;
    }

    private List<string> GetSignificantWords(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return new List<string>();
        }

        var normalized = NormalizeTitle(title);
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "season", "series", "anime", "movie", "film", "ova", "special", "episode", "ep"
        };

        return words
            .Where(word => word.Length > 2 && !stopWords.Contains(word))
            .ToList();
    }
}