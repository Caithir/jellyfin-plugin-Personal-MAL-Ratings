@echo off
echo Stopping Jellyfin...

REM Try to stop the service first
echo Attempting to stop Jellyfin service...
net stop "Jellyfin Server" 2>nul
if %errorlevel% equ 0 (
    echo Jellyfin service stopped successfully.
) else (
    echo Jellyfin service not found or already stopped.
)

REM Kill any remaining processes
echo Stopping any running Jellyfin processes...
taskkill /f /im jellyfin.exe 2>nul
taskkill /f /im "Jellyfin Server.exe" 2>nul

echo Waiting for processes to terminate...
timeout /t 3 /nobreak >nul

echo Jellyfin stop attempt completed.