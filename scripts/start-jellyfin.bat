@echo off
echo Starting Jellyfin...

REM Try to start the service first
echo Attempting to start Jellyfin service...
net start "Jellyfin Server" 2>nul
if %errorlevel% equ 0 (
    echo Jellyfin service started successfully.
    goto :end
)

echo Service not available, trying to start executable...

REM Try common installation paths
set "JELLYFIN_PATHS="C:\Program Files\Jellyfin\Server\jellyfin.exe" "C:\ProgramData\Jellyfin\Server\jellyfin.exe" "C:\Jellyfin\jellyfin.exe""

for %%P in (%JELLYFIN_PATHS%) do (
    if exist %%P (
        echo Starting Jellyfin from %%P
        start "" %%P
        goto :end
    )
)

echo Could not find Jellyfin executable. Please start it manually.
echo Common locations:
echo - C:\Program Files\Jellyfin\Server\jellyfin.exe
echo - C:\ProgramData\Jellyfin\Server\jellyfin.exe

:end
echo Jellyfin start attempt completed.