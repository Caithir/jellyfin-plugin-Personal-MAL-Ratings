# Jellyfin Plugin: Personal MAL Ratings - Development Guide

## Overview
This Jellyfin plugin integrates with the MyAnimeList (MAL) API v2 to fetch personal anime ratings and override community ratings in Jellyfin. The plugin implements sophisticated anime matching algorithms and efficient caching to provide seamless rating synchronization.

## Project Architecture

### Core Components

#### 1. Plugin Entry Point (`Plugin.cs`)
- Main plugin class inheriting from `BasePlugin<PluginConfiguration>`
- Implements `IHasWebPages` for configuration UI
- Plugin GUID: `85E35A5A-8C2D-4F3A-9B1E-7F8D6C4E2A91`
- Exposes static `Instance` property for global access

#### 2. Metadata Provider (`Providers/PersonalMALRatingProvider.cs`)
- Implements `IRemoteMetadataProvider` for Series, Season, and Episode
- Core rating override logic
- Thread-safe caching mechanism with configurable refresh intervals
- Converts MAL scores (1-10) to Jellyfin community ratings (1.0-10.0)

#### 3. MAL API Client (`Services/MALApiClient.cs`)
- OAuth2-based authentication with access/refresh tokens
- Paginated anime list fetching (1000 entries per page)
- Comprehensive error handling and logging
- Token validation functionality

#### 4. Anime Matching Service (`Services/AnimeMatchingService.cs`)
- Multi-tier matching algorithm:
  1. **Exact Match**: Direct title comparison (original, English, synonyms)
  2. **Normalized Match**: Removes special characters, years, season indicators
  3. **Fuzzy Match**: Word-based scoring with 70% threshold
- Stop word filtering and significant word extraction
- Handles season patterns (`Season 1` → `S1`) and year removal

#### 5. Data Models (`Models/MALAnimeEntry.cs`)
- Complete MAL API v2 response models
- JSON serialization attributes for API compatibility
- Handles alternative titles, list status, and pagination

#### 6. Configuration (`Configuration/PluginConfiguration.cs`)
- MAL API credentials (Client ID, Access Token, Refresh Token)
- Plugin behavior settings (enable/disable, refresh intervals)
- Web UI configuration page (`configPage.html`)

## Development Workflow

### Building the Plugin

```bash
# Standard .NET build
dotnet build --configuration Release

# The plugin DLL will be output to: bin/Release/net8.0/Jellyfin.Plugin.PersonalMALRatings.dll
```

### Key Build Files
- **`build.yaml`**: Plugin manifest for Jellyfin plugin repository
- **`meta.json`**: Plugin metadata for distribution
- **`.csproj`**: Targets .NET 8.0, references Jellyfin.Controller 10.*

### Development Dependencies
- .NET 8.0 SDK
- Jellyfin.Controller (10.* - automatically compatible with Jellyfin 10.9.0+)
- Microsoft.Extensions.Http for HTTP client factory

## Plugin Architecture Patterns

### Dependency Injection Integration
The plugin leverages Jellyfin's built-in DI container:
- `IHttpClientFactory` for HTTP operations
- `ILogger<T>` for structured logging
- Services are instantiated in provider constructors

### Caching Strategy
- **Static cache** with thread-safe access using `lock(CacheLock)`
- **Time-based expiration** based on user-configured refresh intervals (1-168 hours)
- **Failure tolerance** - returns cached data if API calls fail

### Metadata Provider Pattern
- Implements Jellyfin's `IRemoteMetadataProvider` interface
- Supports Series, Season, and Episode entity types
- Returns `MetadataResult<T>` with updated community ratings
- Only modifies ratings for anime with personal MAL scores > 0

## MAL API Integration

### Authentication Flow
1. User registers application at https://myanimelist.net/apiconfig/create
2. OAuth2 flow generates access/refresh tokens
3. Plugin stores credentials securely in configuration
4. API requests use Bearer token authentication

### API Endpoints Used
- `GET /v2/users/@me/animelist` - Fetch user's anime list with scores
- `GET /v2/users/@me` - Token validation

### Rate Limiting Considerations
- Caching minimizes API calls
- Configurable refresh intervals prevent excessive requests
- Graceful handling of API failures

## Anime Matching Algorithm

### Matching Hierarchy
1. **Exact Title Matching**
   - Original title, English title, synonyms
   - Case-insensitive comparison

2. **Normalized Matching**
   - Removes special characters: `[^\w\s]`
   - Converts season patterns: `Season 1` → `S1`
   - Removes years in parentheses: `(2023)`
   - Normalizes whitespace

3. **Fuzzy Matching**
   - Extracts significant words (length > 2, excludes stop words)
   - Calculates word intersection score
   - Threshold: 70% match required
   - Stop words: "the", "season", "anime", "movie", etc.

### Edge Cases Handled
- Multiple season formats
- Alternative/localized titles
- Special characters and punctuation
- Year variations
- Common anime terminology

## Configuration Management

### Settings Structure
```csharp
public class PluginConfiguration : BasePluginConfiguration
{
    public string MALUsername { get; set; }        // Reference only
    public string MALClientId { get; set; }        // OAuth2 client ID
    public string MALAccessToken { get; set; }     // API access token
    public string MALRefreshToken { get; set; }    // Token refresh
    public bool EnabledForAnime { get; set; }      // Feature toggle
    public int RefreshIntervalHours { get; set; }  // Cache duration (1-168)
    public bool OverwriteExistingRatings { get; set; } // Rating behavior
}
```

### Web Configuration Interface
- HTML-based configuration page embedded in plugin
- JavaScript integration with Jellyfin's Dashboard API
- Real-time MAL connection testing
- Input validation and user feedback

## Logging and Debugging

### Log Levels Used
- **Information**: Successful rating updates, API fetch summaries
- **Debug**: Matching attempts, cache operations, API responses
- **Error**: API failures, authentication issues, matching errors

### Key Debug Points
- Anime matching process with detailed logging
- API request/response cycles
- Cache hit/miss scenarios
- Token validation results

## Testing Strategy

### Manual Testing
1. Configure MAL API credentials
2. Ensure anime library exists in Jellyfin
3. Trigger metadata refresh on anime series
4. Verify ratings are updated in Jellyfin UI

### Integration Points to Test
- MAL API authentication and data retrieval
- Anime title matching across various formats
- Rating conversion accuracy (1-10 scale)
- Cache behavior under different refresh intervals
- Error handling for invalid credentials or network issues

## Common Development Patterns

### Error Handling
- Extensive try-catch blocks with specific error logging
- Graceful degradation when API is unavailable
- User-friendly error messages in configuration UI

### Async/Await Usage
- All API operations are fully asynchronous
- Proper cancellation token propagation
- No blocking calls in metadata providers

### Thread Safety
- Static caching with explicit locking
- Immutable data structures where possible
- Safe access to shared plugin configuration

## Deployment Considerations

### Plugin Distribution
- Single DLL output: `Jellyfin.Plugin.PersonalMALRatings.dll`
- Compatible with Jellyfin 10.9.0+
- No external dependencies beyond Jellyfin framework

### Installation Requirements
- User must obtain MAL API credentials
- OAuth2 setup required for API access
- Network connectivity to api.myanimelist.net

This plugin demonstrates advanced Jellyfin plugin development patterns including custom metadata providers, external API integration, sophisticated matching algorithms, and comprehensive error handling.