# Jellyfin Plugin: Personal MAL Ratings

This Jellyfin plugin fetches your personal anime ratings from MyAnimeList (MAL) and uses them to override the community ratings in your Jellyfin anime library.

## Features

- Fetches your personal anime ratings from MyAnimeList using the official MAL API v2
- Automatically matches Jellyfin anime series with your MAL anime list entries
- Overwrites community ratings with your personal MAL scores
- Configurable refresh intervals for updating ratings
- Supports Series, Seasons, and Episodes
- Intelligent anime matching using titles, alternative titles, and synonyms
- Caching to minimize API calls and improve performance

## Requirements

- Jellyfin 10.9.0 or higher
- MyAnimeList account
- MAL API Client ID and Access Token

## Installation

1. Download the latest release
2. Copy the `Jellyfin.Plugin.PersonalMALRatings.dll` to your Jellyfin plugins directory:
   - Windows: `%UserProfile%\AppData\Local\jellyfin\plugins` or `%ProgramData%\Jellyfin\Server\plugins`
   - Linux: `/var/lib/jellyfin/plugins/`
3. Restart Jellyfin
4. Navigate to Dashboard > Plugins > Personal MAL Ratings to configure

## Configuration

### Setting up MAL API Access

1. Register your application at https://myanimelist.net/apiconfig/create
2. Obtain your Client ID from the registered application
3. Use OAuth2 flow to get Access Token and Refresh Token
4. Enter these credentials in the plugin configuration page

### Plugin Settings

- **MAL Username**: Your MyAnimeList username (for reference)
- **MAL Client ID**: Your registered application's Client ID
- **MAL Access Token**: OAuth2 access token for API access
- **MAL Refresh Token**: OAuth2 refresh token for automatic renewal
- **Enable for Anime**: Enable rating override for anime content
- **Overwrite Existing Ratings**: Replace existing community ratings
- **Refresh Interval**: How often to update your MAL anime list (1-168 hours)

## How It Works

1. The plugin fetches your complete anime list from MAL API
2. For each anime in your Jellyfin library, it attempts to match with your MAL entries using:
   - Exact title matching
   - Alternative/English title matching
   - Normalized title matching (removes special characters, years, etc.)
   - Fuzzy matching based on significant words
3. When a match is found, your personal MAL score (1-10) is applied as the community rating
4. Results are cached based on your configured refresh interval

## Building from Source

```bash
git clone https://github.com/your-repo/jellyfin-plugin-Personal-MAL-Ratings
cd jellyfin-plugin-Personal-MAL-Ratings
dotnet build --configuration Release
```

## License

This project is licensed under the GPL-3.0 License - see the LICENSE file for details.

## Disclaimer

This plugin is not affiliated with MyAnimeList or Jellyfin. Use at your own risk.
