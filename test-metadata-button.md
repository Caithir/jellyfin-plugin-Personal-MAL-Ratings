# Force Metadata Updates Button - Test Guide

## ✅ What I Added

### **New "Force Metadata Updates on Anime" Button**

This button will:
- **Find anime series** in your Jellyfin library (first 5 for testing)
- **Run the metadata provider** on each series  
- **Trigger the complete matching workflow** (exact → normalized → fuzzy)
- **Generate comprehensive logs** showing the entire process
- **Update ratings** where matches are found
- **Report results** with processed/updated counts

### **How It Works**

1. **Library Scanning**: Identifies anime series using heuristics:
   - Path contains "anime" or "アニメ"
   - Name contains "anime"
   - Has "anime" genre
   - Has studio information

2. **Metadata Processing**: For each found series:
   - Creates SeriesInfo object
   - Calls PersonalMALRatingProvider.GetMetadata()
   - Logs detailed matching attempts
   - Reports success/failure for each

3. **Comprehensive Logging**: Generates logs like:
   ```
   [API] Force metadata updates requested via web interface
   [API] Found 3 anime series, testing metadata provider on each
   [API] Testing metadata provider on series: Attack on Titan
   [PersonalMALRatingProvider] 🔍 Starting match process for: 'Attack on Titan'
   [AnimeMatchingService] ✅ Found EXACT match for 'Attack on Titan' → 'Shingeki no Kyojin' (Score: 9)
   [API] Successfully updated rating for Attack on Titan to 9.0
   ```

## 🔧 How to Test

### **Step 1: Deploy Updated Plugin**
```bash
# Build version 1.2.0
dotnet build Jellyfin.Plugin.PersonalMALRatings.csproj --configuration Release

# Copy DLL to Jellyfin plugins directory
# The plugin should now show version 1.2.0
```

### **Step 2: Restart Jellyfin**
Restart Jellyfin completely to load the new API endpoint.

### **Step 3: Clear Browser Cache**
- Hard refresh the plugin page: `Ctrl + Shift + R`
- Ensure you see version 1.2.0 in the plugin list

### **Step 4: Prerequisites**
1. **Have anime in your library** (with paths containing "anime" or "アニメ")
2. **Configure MAL credentials** in the plugin
3. **Enable plugin for anime** in settings

### **Step 5: Click the Button**
1. Go to plugin configuration page
2. You should now see **3 buttons**:
   - "Test MAL Connection & Generate Logs"
   - "Force Cache Refresh" 
   - **"Force Metadata Updates on Anime"** ← New!
3. Click **"Force Metadata Updates on Anime"**

### **Step 6: Check Results**

**Web Interface:**
- Success: "Metadata updates completed! Processed X anime series, updated Y ratings."
- Failure: Specific error message with guidance

**Plugin Logs:**
```
{JellyfinDataDir}/plugins/configurations/PersonalMALRatings/logs/mal-plugin-{date}.log
```

**Example Success Log:**
```
[2025-01-17 15:30:00] [INF] [API] Force metadata updates requested via web interface
[2025-01-17 15:30:00] [INF] [API] Found 3 anime series, testing metadata provider on each
[2025-01-17 15:30:00] [INF] [API] Testing metadata provider on series: Death Note
[2025-01-17 15:30:01] [INF] [PersonalMALRatingProvider] ✓ Updated rating for series 'Death Note' to 10.0 (MAL Score: 10) - matched with 'Death Note'
[2025-01-17 15:30:01] [INF] [API] Successfully updated rating for Death Note to 10.0
[2025-01-17 15:30:01] [INF] [API] Metadata updates completed - 3 processed, 2 updated
```

## 🎯 What This Tests

This button proves that:
✅ **Library integration works** - can find anime series  
✅ **Metadata provider runs** - processes each series  
✅ **Matching algorithm works** - finds MAL entries  
✅ **Rating conversion works** - MAL scores → Jellyfin ratings  
✅ **Logging system works** - detailed debugging output  
✅ **End-to-end functionality** - complete plugin workflow  

This is the **most comprehensive test** as it simulates exactly what happens during normal Jellyfin metadata refresh!

## 🚨 Troubleshooting

**"No anime series found":**
- Check that anime paths contain "anime"
- Verify library is properly configured
- Try renaming a test folder to include "anime"

**"Plugin is not enabled":**
- Enable "Enable for Anime" in plugin settings
- Save configuration and try again

**No matches found:**
- Check MAL credentials are working
- Ensure you have rated anime on MyAnimeList
- Verify anime titles match your MAL list