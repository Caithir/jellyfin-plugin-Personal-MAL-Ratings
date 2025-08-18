# GitHub Repository Setup Guide

## Step 1: Create GitHub Repository

1. Go to [GitHub](https://github.com) and create a new repository
2. **Repository name**: `jellyfin-plugin-personal-mal-ratings`
3. **Description**: `Jellyfin plugin for MyAnimeList personal rating integration`
4. Set to **Public** (required for Jellyfin plugin installation)
5. **Don't** initialize with README (we have our own)

## Step 2: Initialize Local Git Repository

```bash
# Initialize git repository
git init

# Add all files
git add .

# Initial commit
git commit -m "Initial commit - Personal MAL Ratings plugin v1.4.0"

# Add remote (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/jellyfin-plugin-personal-mal-ratings.git

# Push to GitHub
git branch -M main
git push -u origin main
```

## Step 3: Create GitHub Release

1. Go to your repository on GitHub
2. Click **Releases** → **Create a new release**
3. **Tag version**: `v1.4.0.0`
4. **Release title**: `Personal MAL Ratings v1.4.0.0`
5. **Description**:
   ```
   Enhanced metadata updates with database persistence, comprehensive logging, and web-based testing tools.

   ## Features
   - OAuth2 MyAnimeList integration
   - Intelligent anime matching algorithm
   - Comprehensive file-based logging
   - Web interface with testing tools
   - Real-time metadata updates

   ## Installation
   Add plugin repository: https://raw.githubusercontent.com/YOUR_USERNAME/jellyfin-plugin-personal-mal-ratings/main/manifest.json
   ```
6. **Upload** the file: `releases/jellyfin-plugin-personal-mal-ratings_1.4.0.0.zip`
7. Click **Publish release**

## Step 4: Update Manifest URLs

After creating the repository, update these files with your actual GitHub username:

### manifest.json
Replace `YOUR_USERNAME` with your GitHub username in:
- `imageUrl` (optional)
- `sourceUrl`

### README.md
Replace `YOUR_USERNAME` with your GitHub username in:
- Badge URLs
- Installation instructions
- Download links

## Step 5: Test Plugin Installation

1. **Add Repository** in Jellyfin:
   - Dashboard → Plugins → Repositories
   - Add: `https://raw.githubusercontent.com/YOUR_USERNAME/jellyfin-plugin-personal-mal-ratings/main/manifest.json`

2. **Install Plugin**:
   - Dashboard → Plugins → Catalog
   - Find "Personal MAL Ratings" and install

3. **Verify Installation**:
   - Plugin should appear in installed plugins list
   - Configuration page should be accessible
   - Version should show 1.4.0.0

## Repository Structure

Your final repository should look like:
```
jellyfin-plugin-personal-mal-ratings/
├── README.md
├── LICENSE
├── .gitignore
├── manifest.json
├── mal-auth.http
├── Jellyfin.Plugin.PersonalMALRatings.csproj
├── Plugin.cs
├── Configuration/
│   ├── PluginConfiguration.cs
│   └── configPage.html
├── Services/
│   ├── MALApiClient.cs
│   ├── AnimeMatchingService.cs
│   └── PluginLogger.cs
├── Providers/
│   └── PersonalMALRatingProvider.cs
├── Api/
│   └── PersonalMALRatingsController.cs
├── Models/
│   └── MALAnimeEntry.cs
├── releases/
│   └── jellyfin-plugin-personal-mal-ratings_1.4.0.0.zip
├── build.yaml
└── meta.json
```

## Notes

- **Public Repository Required**: Jellyfin can only install from public repositories
- **Manifest URL**: Must point to the raw GitHub file (raw.githubusercontent.com)
- **Release Assets**: The zip file must be attached to the GitHub release
- **Checksums**: MD5 checksum in manifest must match the release file

Once set up, users can install your plugin by adding your manifest URL to their Jellyfin plugin repositories!