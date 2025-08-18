# PowerShell script to package the plugin and update manifest with checksum
param(
    [string]$ProjectPath = "Jellyfin.Plugin.PersonalMALRatings.csproj",
    [string]$ManifestPath = "manifest.json",
    [string]$ReleasesDir = "releases"
)

Write-Host "Creating plugin package..." -ForegroundColor Cyan

# Read current version from project file
$projectContent = Get-Content $ProjectPath -Raw
$versionMatch = [regex]::Match($projectContent, '<AssemblyVersion>(\d+\.\d+\.\d+\.\d+)</AssemblyVersion>')

if (-not $versionMatch.Success) {
    Write-Host "ERROR: Could not find version in project file" -ForegroundColor Red
    exit 1
}

$version = $versionMatch.Groups[1].Value
$packageName = "jellyfin-plugin-personal-mal-ratings_$version.zip"
$packagePath = Join-Path $ReleasesDir $packageName
$dllPath = "bin\Release\net8.0\Jellyfin.Plugin.PersonalMALRatings.dll"

Write-Host "Version: $version" -ForegroundColor Yellow
Write-Host "Package: $packageName" -ForegroundColor Yellow

# Ensure releases directory exists
if (-not (Test-Path $ReleasesDir)) {
    New-Item -ItemType Directory -Path $ReleasesDir | Out-Null
}

# Check if DLL exists
if (-not (Test-Path $dllPath)) {
    Write-Host "ERROR: Plugin DLL not found at $dllPath" -ForegroundColor Red
    Write-Host "Make sure to build the project first!" -ForegroundColor Red
    exit 1
}

# Create the package
try {
    if (Test-Path $packagePath) {
        Remove-Item $packagePath -Force
    }
    
    Compress-Archive -Path $dllPath -DestinationPath $packagePath -Force
    Write-Host "Package created: $packagePath" -ForegroundColor Green
    
    # Calculate checksum
    $checksum = (Get-FileHash $packagePath -Algorithm MD5).Hash
    Write-Host "Checksum: $checksum" -ForegroundColor Yellow
    
    # Update manifest with checksum
    if (Test-Path $ManifestPath) {
        $manifestContent = Get-Content $ManifestPath -Raw | ConvertFrom-Json
        
        # Update the first (latest) version entry with checksum (manifest is now an array)
        if ($manifestContent[0].versions -and $manifestContent[0].versions.Count -gt 0) {
            $manifestContent[0].versions[0].checksum = $checksum
            $manifestContent | ConvertTo-Json -Depth 10 | Set-Content $ManifestPath
            Write-Host "Updated manifest with checksum" -ForegroundColor Green
        }
    }
    
    # Show package info
    $packageSize = (Get-Item $packagePath).Length
    $packageSizeKB = [math]::Round($packageSize / 1024, 2)
    
    Write-Host ""
    Write-Host "Package Information:" -ForegroundColor Cyan
    Write-Host "File: $packageName" -ForegroundColor White
    Write-Host "Size: $packageSizeKB KB" -ForegroundColor White
    Write-Host "MD5: $checksum" -ForegroundColor White
    Write-Host "Version: $version" -ForegroundColor White
    
} catch {
    Write-Host "ERROR: Error creating package: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Packaging completed successfully!" -ForegroundColor Green