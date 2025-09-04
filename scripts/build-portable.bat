@echo off
REM Build script for creating a portable ModelEvaluator executable for Windows
REM This batch file calls the PowerShell build script

echo 🚀 ModelEvaluator Portable Build
echo ===============================

REM Check if PowerShell is available
where pwsh >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Using PowerShell Core (pwsh)
    pwsh -ExecutionPolicy Bypass -File "%~dp0build-portable.ps1" %*
) else (
    where powershell >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        echo Using Windows PowerShell
        powershell -ExecutionPolicy Bypass -File "%~dp0build-portable.ps1" %*
    ) else (
        echo ❌ PowerShell not found. Please install PowerShell to run this build script.
        echo You can download PowerShell from: https://github.com/PowerShell/PowerShell/releases
        pause
        exit /b 1
    )
)

if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed with error code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ✅ Build completed! Check the dist folder for your portable executable.
pause
