using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.PersonalMALRatings.Models;

/// <summary>
/// Shoko API response models for anime metadata
/// </summary>
public class ShokoSeries
{
    [JsonPropertyName("IDs")]
    public ShokoSeriesIDs? IDs { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Sizes")]
    public ShokoSizes? Sizes { get; set; }

    [JsonPropertyName("Created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("Updated")]
    public DateTime Updated { get; set; }

    [JsonPropertyName("AniDB")]
    public ShokoAniDBInfo? AniDB { get; set; }
}

public class ShokoSeriesIDs
{
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    [JsonPropertyName("ParentGroup")]
    public int? ParentGroup { get; set; }

    [JsonPropertyName("TopLevelGroup")]
    public int? TopLevelGroup { get; set; }

    [JsonPropertyName("AniDB")]
    public int? AniDB { get; set; }

    [JsonPropertyName("TMDB")]
    public List<int>? TMDB { get; set; }

    [JsonPropertyName("TvDB")]
    public List<int>? TvDB { get; set; }
}

public class ShokoSizes
{
    [JsonPropertyName("FileSources")]
    public ShokoFileSourceCounts? FileSources { get; set; }

    [JsonPropertyName("Total")]
    public ShokoTotalCounts? Total { get; set; }
}

public class ShokoFileSourceCounts
{
    [JsonPropertyName("Unknown")]
    public int Unknown { get; set; }

    [JsonPropertyName("Other")]
    public int Other { get; set; }

    [JsonPropertyName("TV")]
    public int TV { get; set; }

    [JsonPropertyName("DVD")]
    public int DVD { get; set; }

    [JsonPropertyName("BluRay")]
    public int BluRay { get; set; }

    [JsonPropertyName("Web")]
    public int Web { get; set; }

    [JsonPropertyName("VHS")]
    public int VHS { get; set; }

    [JsonPropertyName("LaserDisc")]
    public int LaserDisc { get; set; }

    [JsonPropertyName("Camera")]
    public int Camera { get; set; }
}

public class ShokoTotalCounts
{
    [JsonPropertyName("Episodes")]
    public int Episodes { get; set; }

    [JsonPropertyName("Specials")]
    public int Specials { get; set; }

    [JsonPropertyName("Credits")]
    public int Credits { get; set; }

    [JsonPropertyName("Trailers")]
    public int Trailers { get; set; }

    [JsonPropertyName("Parodies")]
    public int Parodies { get; set; }

    [JsonPropertyName("Others")]
    public int Others { get; set; }
}

public class ShokoAniDBInfo
{
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("Title")]
    public string? Title { get; set; }

    [JsonPropertyName("Titles")]
    public List<ShokoTitle>? Titles { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("Restricted")]
    public bool Restricted { get; set; }

    [JsonPropertyName("Poster")]
    public string? Poster { get; set; }

    [JsonPropertyName("EpisodeCount")]
    public int EpisodeCount { get; set; }

    [JsonPropertyName("AirDate")]
    public DateTime? AirDate { get; set; }

    [JsonPropertyName("EndDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("Relation")]
    public string? Relation { get; set; }

    [JsonPropertyName("Rating")]
    public ShokoRating? Rating { get; set; }

    [JsonPropertyName("UserRating")]
    public ShokoRating? UserRating { get; set; }
}

public class ShokoTitle
{
    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Language")]
    public string? Language { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("Default")]
    public bool Default { get; set; }
}

public class ShokoRating
{
    [JsonPropertyName("Rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("MaxRating")]
    public int MaxRating { get; set; }

    [JsonPropertyName("Source")]
    public string? Source { get; set; }

    [JsonPropertyName("Votes")]
    public int? Votes { get; set; }
}

public class ShokoFile
{
    [JsonPropertyName("IDs")]
    public ShokoFileIDs? IDs { get; set; }

    [JsonPropertyName("Path")]
    public string? Path { get; set; }

    [JsonPropertyName("Size")]
    public long Size { get; set; }

    [JsonPropertyName("Hashes")]
    public ShokoFileHashes? Hashes { get; set; }

    [JsonPropertyName("SeriesIDs")]
    public List<int>? SeriesIDs { get; set; }
}

public class ShokoFileIDs
{
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    [JsonPropertyName("AniDB")]
    public int? AniDB { get; set; }
}

public class ShokoFileHashes
{
    [JsonPropertyName("ED2K")]
    public string? ED2K { get; set; }

    [JsonPropertyName("MD5")]
    public string? MD5 { get; set; }

    [JsonPropertyName("SHA1")]
    public string? SHA1 { get; set; }

    [JsonPropertyName("CRC32")]
    public string? CRC32 { get; set; }
}

public class ShokoSearchResult
{
    [JsonPropertyName("Series")]
    public List<ShokoSeries>? Series { get; set; }

    [JsonPropertyName("Total")]
    public int Total { get; set; }
}

/// <summary>
/// AniDB to MAL ID mapping entry
/// </summary>
public class AniDBToMALMapping
{
    public int AniDBId { get; set; }
    public int MALId { get; set; }
    public string? Title { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsConfirmed { get; set; }
}