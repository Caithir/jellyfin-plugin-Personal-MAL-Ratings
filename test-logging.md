# Plugin File Logging Test Guide

## Setup Complete ✅

Your plugin now logs to both:
1. **Jellyfin logs** (standard location)
2. **Plugin-specific files** in the plugin data directory

## Log File Locations

### Plugin Logs Directory:
```
{JellyfinDataDir}/plugins/configurations/PersonalMALRatings/logs/
```

**Typical Windows locations:**
- `C:\ProgramData\Jellyfin\Server\plugins\configurations\PersonalMALRatings\logs\`
- Or: `{JellyfinInstallDir}\data\plugins\configurations\PersonalMALRatings\logs\`

### Log Files:
- `mal-plugin-{YYYY-MM-DD}.log` (daily rotation)
- Keeps 7 days of logs automatically

## What Gets Logged to Files:

✅ **Plugin initialization and configuration validation**
✅ **MAL API requests and responses** 
✅ **Successful anime matches and rating updates**
✅ **Authentication failures and errors**
✅ **Configuration changes**

## Testing File Logging:

1. **Install/restart** the plugin to see initialization logs
2. **Configure MAL credentials** to see configuration validation
3. **Test MAL connection** to see API validation logs  
4. **Refresh anime metadata** to see matching and rating logs

## Sample Log Output:
```
[2024-01-15 14:30:15.123 +00:00] [INF] [Plugin] Personal MAL Ratings plugin initialized (Version: 1.0.0.0) - Log files location: C:\ProgramData\Jellyfin\Server\plugins\configurations\PersonalMALRatings\logs
[2024-01-15 14:30:15.125 +00:00] [INF] [Plugin] Configuration validation passed - plugin ready to use
[2024-01-15 14:30:20.456 +00:00] [INF] [MALApiClient] Starting MAL API request to fetch user anime list
[2024-01-15 14:30:21.789 +00:00] [INF] [MALApiClient] Successfully fetched 157 anime entries from MAL (89 have personal scores)
[2024-01-15 14:30:22.123 +00:00] [INF] [PersonalMALRatingProvider] ✓ Updated rating for series 'Attack on Titan' to 9.0 (MAL Score: 9) - matched with 'Shingeki no Kyojin'
```

The logs provide detailed debugging information separate from Jellyfin's main logs!