using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.PersonalMALRatings.Models;

public class MALAnimeEntry
{
    [JsonPropertyName("node")]
    public MALAnimeNode Node { get; set; } = new();
    
    [JsonPropertyName("list_status")]
    public MALListStatus? ListStatus { get; set; }
}

public class MALAnimeNode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("alternative_titles")]
    public MALAlternativeTitles? AlternativeTitles { get; set; }
}

public class MALAlternativeTitles
{
    [JsonPropertyName("synonyms")]
    public string[] Synonyms { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("en")]
    public string English { get; set; } = string.Empty;
    
    [JsonPropertyName("ja")]
    public string Japanese { get; set; } = string.Empty;
}

public class MALListStatus
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    [JsonPropertyName("num_episodes_watched")]
    public int EpisodesWatched { get; set; }
    
    [JsonPropertyName("is_rewatching")]
    public bool IsRewatching { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class MALUserAnimeListResponse
{
    [JsonPropertyName("data")]
    public MALAnimeEntry[] Data { get; set; } = Array.Empty<MALAnimeEntry>();
    
    [JsonPropertyName("paging")]
    public MALPaging? Paging { get; set; }
}

public class MALPaging
{
    [JsonPropertyName("next")]
    public string? Next { get; set; }
    
    [JsonPropertyName("previous")]
    public string? Previous { get; set; }
}