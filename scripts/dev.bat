@echo off
REM AI Model Evaluator - Development Shortcuts
REM ==========================================

if "%1"=="" goto usage
if "%1"=="build" goto build
if "%1"=="run" goto run
if "%1"=="clean" goto clean
if "%1"=="restore" goto restore
if "%1"=="publish" goto publish
if "%1"=="release" goto release
if "%1"=="test" goto test
goto usage

:build
echo 🔨 Building AI Model Evaluator...
dotnet build ModelEvaluator.csproj
if %ERRORLEVEL% EQU 0 (
    echo ✅ Build successful!
) else (
    echo ❌ Build failed!
)
goto end

:run
echo 🚀 Starting AI Model Evaluator...
dotnet run ModelEvaluator.csproj
goto end

:clean
echo 🧹 Cleaning build artifacts...
dotnet clean ModelEvaluator.csproj
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul
echo ✅ Clean complete!
goto end

:restore
echo 📦 Restoring NuGet packages...
dotnet restore ModelEvaluator.csproj
echo ✅ Restore complete!
goto end

:publish
echo 📋 Publishing release build...
dotnet publish ModelEvaluator.csproj -c Release -o publish
echo ✅ Published to 'publish' folder!
goto end

:release
echo 🎯 Building release version...
dotnet build ModelEvaluator.csproj -c Release
if %ERRORLEVEL% EQU 0 (
    echo ✅ Release build successful!
) else (
    echo ❌ Release build failed!
)
goto end

:test
echo 🧪 Running tests...
dotnet test
goto end

:usage
echo AI Model Evaluator - Development Shortcuts
echo ==========================================
echo.
echo Usage: dev [command]
echo.
echo Commands:
echo   build     - Build the project
echo   run       - Run the application
echo   clean     - Clean build artifacts
echo   restore   - Restore NuGet packages
echo   publish   - Publish release build
echo   release   - Build in release mode
echo   test      - Run tests
echo.
echo Examples:
echo   dev build
echo   dev run
echo   dev clean
echo.

:end
