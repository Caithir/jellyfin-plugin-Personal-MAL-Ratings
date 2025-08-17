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
            _logger.LogWarning("Cannot match null or empty Jellyfin item");
            return null;
        }

        var jellyfinTitle = jellyfinItem.Name;
        _logger.LogDebug("🔍 Starting match process for: '{Title}' against {EntryCount} MAL entries", 
            jellyfinTitle, malEntries.Count);

        // Step 1: Exact match
        _logger.LogDebug("Step 1: Attempting exact title match...");
        var exactMatch = FindExactMatch(jellyfinTitle, malEntries);
        if (exactMatch != null)
        {
            _logger.LogInformation("✅ Found EXACT match for '{JellyfinTitle}' → '{MALTitle}' (Score: {Score})", 
                jellyfinTitle, exactMatch.Node.Title, exactMatch.ListStatus?.Score ?? 0);
            return exactMatch;
        }
        _logger.LogDebug("No exact match found");

        // Step 2: Normalized match
        _logger.LogDebug("Step 2: Attempting normalized title match...");
        var normalizedMatch = FindNormalizedMatch(jellyfinTitle, malEntries);
        if (normalizedMatch != null)
        {
            _logger.LogInformation("✅ Found NORMALIZED match for '{JellyfinTitle}' → '{MALTitle}' (Score: {Score})", 
                jellyfinTitle, normalizedMatch.Node.Title, normalizedMatch.ListStatus?.Score ?? 0);
            return normalizedMatch;
        }
        _logger.LogDebug("No normalized match found");

        // Step 3: Fuzzy match
        _logger.LogDebug("Step 3: Attempting fuzzy match...");
        var fuzzyMatch = FindFuzzyMatch(jellyfinTitle, malEntries);
        if (fuzzyMatch != null)
        {
            _logger.LogInformation("✅ Found FUZZY match for '{JellyfinTitle}' → '{MALTitle}' (Score: {Score})", 
                jellyfinTitle, fuzzyMatch.Node.Title, fuzzyMatch.ListStatus?.Score ?? 0);
            return fuzzyMatch;
        }
        _logger.LogDebug("No fuzzy match found");

        _logger.LogWarning("❌ No match found for '{Title}' - no ratings will be applied", jellyfinTitle);
        return null;
    }

    private MALAnimeEntry? FindExactMatch(string jellyfinTitle, List<MALAnimeEntry> malEntries)
    {
        _logger.LogDebug("Checking exact matches for: '{Title}'", jellyfinTitle);
        
        foreach (var entry in malEntries)
        {
            // Check main title
            if (string.Equals(entry.Node.Title, jellyfinTitle, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Exact match found via main title: '{JellyfinTitle}' = '{MALTitle}'", 
                    jellyfinTitle, entry.Node.Title);
                return entry;
            }
            
            // Check English title
            if (entry.Node.AlternativeTitles?.English != null && 
                string.Equals(entry.Node.AlternativeTitles.English, jellyfinTitle, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Exact match found via English title: '{JellyfinTitle}' = '{EnglishTitle}' (MAL: '{MALTitle}')", 
                    jellyfinTitle, entry.Node.AlternativeTitles.English, entry.Node.Title);
                return entry;
            }
            
            // Check synonyms
            if (entry.Node.AlternativeTitles?.Synonyms != null)
            {
                foreach (var synonym in entry.Node.AlternativeTitles.Synonyms)
                {
                    if (string.Equals(synonym, jellyfinTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Exact match found via synonym: '{JellyfinTitle}' = '{Synonym}' (MAL: '{MALTitle}')", 
                            jellyfinTitle, synonym, entry.Node.Title);
                        return entry;
                    }
                }
            }
        }
        
        return null;
    }

    private MALAnimeEntry? FindNormalizedMatch(string jellyfinTitle, List<MALAnimeEntry> malEntries)
    {
        var normalizedJellyfin = NormalizeTitle(jellyfinTitle);
        _logger.LogDebug("Normalized Jellyfin title: '{Original}' → '{Normalized}'", jellyfinTitle, normalizedJellyfin);

        foreach (var entry in malEntries)
        {
            // Check main title
            var normalizedMAL = NormalizeTitle(entry.Node.Title);
            if (string.Equals(normalizedMAL, normalizedJellyfin, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Normalized match found via main title: '{JellyfinNorm}' = '{MALNorm}' (Original: '{MALTitle}')",
                    normalizedJellyfin, normalizedMAL, entry.Node.Title);
                return entry;
            }

            // Check English title
            if (entry.Node.AlternativeTitles?.English != null)
            {
                var normalizedEnglish = NormalizeTitle(entry.Node.AlternativeTitles.English);
                if (string.Equals(normalizedEnglish, normalizedJellyfin, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Normalized match found via English title: '{JellyfinNorm}' = '{EnglishNorm}' (Original: '{MALTitle}')",
                        normalizedJellyfin, normalizedEnglish, entry.Node.Title);
                    return entry;
                }
            }

            // Check synonyms
            if (entry.Node.AlternativeTitles?.Synonyms != null)
            {
                foreach (var synonym in entry.Node.AlternativeTitles.Synonyms)
                {
                    var normalizedSynonym = NormalizeTitle(synonym);
                    if (string.Equals(normalizedSynonym, normalizedJellyfin, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Normalized match found via synonym: '{JellyfinNorm}' = '{SynonymNorm}' (Original: '{MALTitle}')",
                            normalizedJellyfin, normalizedSynonym, entry.Node.Title);
                        return entry;
                    }
                }
            }
        }

        return null;
    }

    private MALAnimeEntry? FindFuzzyMatch(string jellyfinTitle, List<MALAnimeEntry> malEntries)
    {
        var jellyfinWords = GetSignificantWords(jellyfinTitle);
        _logger.LogDebug("Significant words extracted from '{Title}': [{Words}]", 
            jellyfinTitle, string.Join(", ", jellyfinWords));
        
        if (jellyfinWords.Count == 0)
        {
            _logger.LogDebug("No significant words found for fuzzy matching");
            return null;
        }

        var matches = malEntries
            .Select(entry => new
            {
                Entry = entry,
                Score = CalculateMatchScore(jellyfinWords, entry)
            })
            .Where(x => x.Score > 0.7)
            .OrderByDescending(x => x.Score)
            .Take(5) // Log top 5 candidates
            .ToList();

        if (matches.Any())
        {
            _logger.LogDebug("Fuzzy match candidates (threshold > 0.7):");
            foreach (var match in matches)
            {
                _logger.LogDebug("  - {Score:F3}: '{MALTitle}'", match.Score, match.Entry.Node.Title);
            }
            
            var bestMatch = matches.First();
            _logger.LogDebug("Selected best fuzzy match: {Score:F3} for '{MALTitle}'", 
                bestMatch.Score, bestMatch.Entry.Node.Title);
            return bestMatch.Entry;
        }
        else
        {
            _logger.LogDebug("No fuzzy matches found above threshold (0.7)");
            return null;
        }
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