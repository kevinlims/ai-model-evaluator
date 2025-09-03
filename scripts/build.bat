@echo off
REM Build script for AI Model Evaluator
echo Building AI Model Evaluator...
dotnet build ModelEvaluator.csproj
if %ERRORLEVEL% EQU 0 (
    echo ✅ Build successful!
) else (
    echo ❌ Build failed!
    exit /b %ERRORLEVEL%
)
