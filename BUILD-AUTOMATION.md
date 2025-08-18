# Automated Build System with Version Increment

## ✅ What I Created

Your build system now **automatically increments the patch version** every time you build! Here's what happens:

### **Build Process:**
1. **Version Increment** - Automatically bumps patch version (1.4.0.0 → 1.4.0.1 → 1.4.0.2...)
2. **Project Files Update** - Updates `.csproj`, `meta.json`, and `manifest.json`
3. **Clean Build** - Removes old build artifacts
4. **Compile Plugin** - Builds the Release version
5. **Create Package** - Generates zip file for distribution
6. **Update Manifest** - Calculates and updates checksum

## 🚀 How to Use

### **Windows Batch Script:**
```bash
cd scripts
./build.bat
```

### **PowerShell Script:**
```powershell
cd scripts
./build.ps1
```

## 📁 Build Scripts Created

### **`scripts/increment-version.ps1`**
- Reads current version from project file
- Increments patch number by 1
- Updates all version references in project files
- Adds new version entry to manifest.json

### **`scripts/build.bat`** 
- Windows batch script that orchestrates the full build
- Calls PowerShell scripts for version management
- Provides colored output and error handling

### **`scripts/build.ps1`**
- PowerShell version of the build script
- Same functionality as batch script
- Better for PowerShell environments

### **`scripts/package.ps1`**
- Creates plugin zip package
- Calculates MD5 checksum
- Updates manifest with checksum
- Shows package information

## 📋 Example Build Output

```
🚀 Starting automated build with version increment...

📈 Step 1: Incrementing patch version...
Version: 1.4.0.0 -> 1.4.0.1
Updated meta.json
Updated manifest.json

🧹 Step 2: Cleaning previous build...
✅ Clean completed

🔨 Step 3: Building plugin...
✅ Build completed successfully

📦 Step 4: Creating plugin package...
Package Information:
  File: jellyfin-plugin-personal-mal-ratings_1.4.0.1.zip
  Size: 24.28 KB
  MD5: 4C78B05F0B9ED6C988E96371ECD474CF
  Version: 1.4.0.1

🎉 Automated build completed successfully!
```

## 🔄 Version Management

### **Automatic Updates:**
- **AssemblyVersion** in `.csproj` file
- **FileVersion** in `.csproj` file  
- **version** in `meta.json`
- **versions array** in `manifest.json` (adds new entry)
- **timestamp** updated to current time

### **Version Format:** 
`Major.Minor.Build.Patch` (e.g., 1.4.0.1)
- Only **patch** number increments automatically
- Manual version bumps for major/minor changes

## 📦 Package Output

Each build creates:
- **Plugin DLL**: `bin/Release/net8.0/Jellyfin.Plugin.PersonalMALRatings.dll`
- **Package ZIP**: `releases/jellyfin-plugin-personal-mal-ratings_1.4.0.1.zip`
- **Updated Manifest**: `manifest.json` with new version and checksum

## 🎯 GitHub Release Workflow

After each build:

1. **Check Version**: Note the new version (e.g., 1.4.0.1)
2. **Commit Changes**: `git add . && git commit -m "Build v1.4.0.1"`
3. **Create Release**: 
   - Tag: `v1.4.0.1`
   - Upload: `releases/jellyfin-plugin-personal-mal-ratings_1.4.0.1.zip`
4. **Update Repository**: Push to GitHub

## 🔧 Manual Version Control

To manually set a specific version, edit the `<AssemblyVersion>` in the `.csproj` file before running the build script.

## 🎉 Benefits

✅ **No Manual Version Management** - Automatic patch increments  
✅ **Consistent Versioning** - All files stay in sync  
✅ **Ready for Distribution** - Package created automatically  
✅ **GitHub Integration** - Manifest ready for plugin repository  
✅ **Error Handling** - Build fails gracefully if issues occur  

Now every time you run a build, you get a new version ready for release! 🚀