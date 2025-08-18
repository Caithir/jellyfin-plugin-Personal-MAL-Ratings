# PowerShell script to increment patch version automatically
param(
    [string]$ProjectPath = "Jellyfin.Plugin.PersonalMALRatings.csproj",
    [string]$MetaPath = "meta.json",
    [string]$ManifestPath = "manifest.json"
)

Write-Host "Incrementing patch version..." -ForegroundColor Cyan

# Read current version from project file
$projectContent = Get-Content $ProjectPath -Raw
$versionMatch = [regex]::Match($projectContent, '<AssemblyVersion>(\d+)\.(\d+)\.(\d+)\.(\d+)</AssemblyVersion>')

if (-not $versionMatch.Success) {
    Write-Host "ERROR: Could not find version in project file" -ForegroundColor Red
    exit 1
}

$major = [int]$versionMatch.Groups[1].Value
$minor = [int]$versionMatch.Groups[2].Value
$build = [int]$versionMatch.Groups[3].Value
$patch = [int]$versionMatch.Groups[4].Value

# Increment patch version
$newPatch = $patch + 1
$newVersion = "$major.$minor.$build.$newPatch"

Write-Host "Version: $major.$minor.$build.$patch -> $newVersion" -ForegroundColor Green

# Update project file using simple string replacement
$oldAssemblyVersion = "<AssemblyVersion>$major.$minor.$build.$patch</AssemblyVersion>"
$newAssemblyVersion = "<AssemblyVersion>$newVersion</AssemblyVersion>"
$oldFileVersion = "<FileVersion>$major.$minor.$build.$patch</FileVersion>"
$newFileVersion = "<FileVersion>$newVersion</FileVersion>"

$newProjectContent = $projectContent.Replace($oldAssemblyVersion, $newAssemblyVersion)
$newProjectContent = $newProjectContent.Replace($oldFileVersion, $newFileVersion)
Set-Content $ProjectPath $newProjectContent

# Update meta.json
if (Test-Path $MetaPath) {
    $metaContent = Get-Content $MetaPath -Raw | ConvertFrom-Json
    $metaContent.version = $newVersion
    $metaContent.timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
    $metaContent | ConvertTo-Json -Depth 10 | Set-Content $MetaPath
    Write-Host "Updated meta.json" -ForegroundColor Green
}

# Update manifest.json - add new version entry
if (Test-Path $ManifestPath) {
    $manifestContent = Get-Content $ManifestPath -Raw | ConvertFrom-Json
    
    # Create new version entry
    $newVersionEntry = @{
        version = $newVersion
        changelog = "Automated build with incremental improvements"
        targetAbi = "10.10.7.0"
        sourceUrl = "https://github.com/Caithir/jellyfin-plugin-personal-mal-ratings/releases/download/v$newVersion/jellyfin-plugin-personal-mal-ratings_$newVersion.zip"
        checksum = "TO_BE_CALCULATED"
        timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
    }
    
    # Add new version to the beginning of versions array
    $versions = @($newVersionEntry) + $manifestContent[0].versions
    $manifestContent[0].versions = $versions
    
    $manifestContent | ConvertTo-Json -Depth 10 | Set-Content $ManifestPath
    Write-Host "Updated manifest.json" -ForegroundColor Green
}

Write-Host "Version incremented to $newVersion" -ForegroundColor Green
return $newVersion