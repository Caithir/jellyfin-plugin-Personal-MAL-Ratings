# Test Web Interface with Logging

## ✅ What I Added

### **New API Endpoints:**
- `POST /Plugins/PersonalMALRatings/test` - Full MAL connection test with detailed logging
- `POST /Plugins/PersonalMALRatings/refresh` - Force cache refresh with logging

### **Enhanced Web Interface:**
- **"Test MAL Connection & Generate Logs"** button - triggers full plugin workflow
- **"Force Cache Refresh"** button - clears cache and refetches from MAL
- **Visual feedback** with success/error messages
- **Real-time status** updates during testing

## 🔧 How to Test

### **Step 1: Deploy Plugin**
```bash
# Build the plugin
dotnet build Jellyfin.Plugin.PersonalMALRatings.csproj --configuration Release

# Copy to Jellyfin plugins directory
# Windows: C:\ProgramData\Jellyfin\Server\plugins\
# Linux: /var/lib/jellyfin/plugins/
```

### **Step 2: Restart Jellyfin**
Restart Jellyfin completely to load the new API endpoints.

### **Step 3: Configure Plugin**
1. Go to **Jellyfin Dashboard** → **Plugins** → **Personal MAL Ratings**
2. Enter your MAL credentials:
   - Client ID: `8bcbcb84ffd34458c29b8777ed521f44`
   - Access Token: (your working token)
3. **Save** configuration

### **Step 4: Test the Buttons**

#### **Test MAL Connection & Generate Logs**
- Click the button
- This will:
  - Validate your MAL token
  - Fetch your complete anime list
  - Generate detailed logs in both Jellyfin logs and plugin files
  - Show success message with anime count

#### **Force Cache Refresh**
- Click this button to:
  - Clear the internal cache
  - Force a fresh API fetch
  - Trigger the matching algorithm
  - Generate comprehensive logs

### **Step 5: Check the Logs**

**Plugin log files location:**
```
{JellyfinDataDir}/plugins/configurations/PersonalMALRatings/logs/mal-plugin-{date}.log
```

**Example log entries you'll see:**
```
[API] Test MAL connection requested via web interface
[MALApiClient] Starting MAL API request to fetch user anime list
[MALApiClient] Successfully fetched 157 anime entries from MAL (89 have personal scores)
[API] Test completed successfully - 89 entries fetched
```

## 📋 Expected Results

### **Successful Test:**
- ✅ Green success message: "Success! Fetched X anime entries..."
- ✅ Detailed logs in plugin files
- ✅ Cache populated for future metadata requests

### **Failed Test:**
- ❌ Red error message with specific issue
- ❌ Error logs showing the problem
- ❌ Guidance on what to fix

## 🎯 What This Proves

The web buttons will demonstrate:
1. **Plugin is working** - API endpoints respond
2. **MAL integration is functional** - can fetch data
3. **Logging is working** - detailed logs are generated
4. **Cache system is operational** - refresh functionality works
5. **Configuration is valid** - credentials are correct

Click the buttons and check the logs to see your plugin in action! 🚀