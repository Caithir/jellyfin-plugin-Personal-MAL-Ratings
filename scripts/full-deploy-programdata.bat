@echo off
echo ========================================
echo  Full Deployment to ProgramData
echo ========================================

echo Step 1: Building plugin...
call scripts\build.bat
if %errorlevel% neq 0 goto :error

echo.
echo Step 2: Stopping Jellyfin...
call scripts\stop-jellyfin.bat

echo.
echo Step 3: Deploying plugin...
call scripts\deploy-programdata.bat
if %errorlevel% neq 0 goto :error

echo.
echo Step 4: Starting Jellyfin...
call scripts\start-jellyfin.bat

echo.
echo ========================================
echo  Deployment completed successfully!
echo ========================================
goto :end

:error
echo.
echo ========================================
echo  Deployment failed!
echo ========================================
exit /b 1

:end