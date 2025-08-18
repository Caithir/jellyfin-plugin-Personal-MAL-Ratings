# PowerShell build script with automatic version increment
Write-Host "🚀 Starting automated build with version increment..." -ForegroundColor Cyan
Write-Host ""

# Change to project root directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
Set-Location $projectRoot

try {
    # Step 1: Increment version
    Write-Host "📈 Step 1: Incrementing patch version..." -ForegroundColor Yellow
    $newVersion = & "$projectRoot\scripts\increment-version.ps1"
    if ($LASTEXITCODE -ne 0) {
        throw "Version increment failed"
    }
    Write-Host ""

    # Step 2: Clean previous build
    Write-Host "🧹 Step 2: Cleaning previous build..." -ForegroundColor Yellow
    if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
    if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
    Write-Host "✅ Clean completed" -ForegroundColor Green
    Write-Host ""

    # Step 3: Build the plugin
    Write-Host "🔨 Step 3: Building plugin..." -ForegroundColor Yellow
    & dotnet build "Jellyfin.Plugin.PersonalMALRatings.csproj" --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
    Write-Host ""

    # Step 4: Create package
    Write-Host "📦 Step 4: Creating plugin package..." -ForegroundColor Yellow
    & "$projectRoot\scripts\package.ps1"
    if ($LASTEXITCODE -ne 0) {
        throw "Packaging failed"
    }
    Write-Host ""

    Write-Host "🎉 Automated build completed successfully!" -ForegroundColor Green
    Write-Host "📁 Check the releases/ directory for the new package" -ForegroundColor Cyan
    Write-Host "📋 Don't forget to create a GitHub release with the new version" -ForegroundColor Cyan

} catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}