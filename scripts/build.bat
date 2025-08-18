@echo off
setlocal enabledelayedexpansion

echo 🚀 Starting automated build with version increment...
echo.

REM Change to project root directory
cd /d "%~dp0\.."

REM Step 1: Increment version
echo 📈 Step 1: Incrementing patch version...
powershell -ExecutionPolicy Bypass -File "scripts\increment-version.ps1"
if %errorlevel% neq 0 (
    echo ❌ Version increment failed!
    exit /b 1
)
echo.

REM Step 2: Clean previous build
echo 🧹 Step 2: Cleaning previous build...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
echo ✅ Clean completed
echo.

REM Step 3: Build the plugin
echo 🔨 Step 3: Building plugin...
dotnet build "Jellyfin.Plugin.PersonalMALRatings.csproj" --configuration Release --verbosity minimal
if %errorlevel% neq 0 (
    echo ❌ Build failed!
    exit /b 1
)
echo ✅ Build completed successfully
echo.

REM Step 4: Create package
echo 📦 Step 4: Creating plugin package...
powershell -ExecutionPolicy Bypass -File "scripts\package.ps1"
if %errorlevel% neq 0 (
    echo ❌ Packaging failed!
    exit /b 1
)
echo.

echo 🎉 Automated build completed successfully!
echo 📁 Check the releases/ directory for the new package
echo 📋 Don't forget to create a GitHub release with the new version