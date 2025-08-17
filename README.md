# Jellyfin Plugin: Personal MAL Ratings

[![Latest Release](https://img.shields.io/github/v/release/YOUR_USERNAME/jellyfin-plugin-personal-mal-ratings)](https://github.com/YOUR_USERNAME/jellyfin-plugin-personal-mal-ratings/releases)
[![Jellyfin Version](https://img.shields.io/badge/jellyfin-10.10.7%2B-blue)](https://jellyfin.org/)

A Jellyfin plugin that integrates with the MyAnimeList (MAL) API v2 to fetch your personal anime ratings and override community ratings in Jellyfin with your own scores.

## Features

- 🔐 **OAuth2 Authentication** - Secure integration with MyAnimeList API v2
- 🎯 **Intelligent Matching** - Multi-tier algorithm (Exact → Normalized → Fuzzy) for accurate anime identification
- 💾 **Smart Caching** - Configurable refresh intervals (1-168 hours) with automatic token refresh
- 📊 **Comprehensive Logging** - Detailed file-based logging for debugging and monitoring
- 🌐 **Web Interface** - Built-in testing tools and configuration management
- ⚡ **Real-time Testing** - Force metadata updates and cache refresh via web buttons

## Installation

### Method 1: Plugin Repository (Recommended)

1. Open **Jellyfin Dashboard** → **Plugins** → **Repositories**
2. Add custom repository:
   - **Repository Name**: `Personal MAL Ratings`
   - **Repository URL**: `https://raw.githubusercontent.com/YOUR_USERNAME/jellyfin-plugin-personal-mal-ratings/main/manifest.json`
3. Go to **Catalog** and install **Personal MAL Ratings**

### Method 2: Manual Installation

1. Download the latest release: [jellyfin-plugin-personal-mal-ratings_1.4.0.0.zip](https://github.com/YOUR_USERNAME/jellyfin-plugin-personal-mal-ratings/releases/download/v1.4.0.0/jellyfin-plugin-personal-mal-ratings_1.4.0.0.zip)
2. Extract `Jellyfin.Plugin.PersonalMALRatings.dll` to your Jellyfin plugins directory
3. Restart Jellyfin

## Quick Start

1. **Install plugin** via repository or manual download
2. **Register MAL app** at https://myanimelist.net/apiconfig/create
3. **Get OAuth tokens** using included `mal-auth.http` file
4. **Configure plugin** in Jellyfin Dashboard → Plugins
5. **Test connection** using built-in web buttons
6. **Enjoy** your personal ratings in Jellyfin!

## License

This project is licensed under the MIT License.