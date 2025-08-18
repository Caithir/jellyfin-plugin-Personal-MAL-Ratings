@echo off
echo Deploying plugin to UserProfile location...

set "SOURCE=bin\Release\net8.0\Jellyfin.Plugin.PersonalMALRatings.dll"
set "DEST=%UserProfile%\AppData\Local\jellyfin\plugins\Personal MAL Ratings"

if not exist "%SOURCE%" (
    echo Error: Plugin DLL not found at %SOURCE%
    echo Please build the plugin first.
    exit /b 1
)

echo Creating plugin directory: %DEST%
if not exist "%DEST%" mkdir "%DEST%"

echo Copying plugin from %SOURCE% to %DEST%
copy "%SOURCE%" "%DEST%\" /Y

if %errorlevel% equ 0 (
    echo Plugin deployed successfully to UserProfile location!
) else (
    echo Failed to deploy plugin.
    exit /b 1
)