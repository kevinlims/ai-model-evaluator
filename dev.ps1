# AI Model Evaluator - Main Development Script
# ============================================
# This is the primary development entry point for the AI Model Evaluator project.
# All other development scripts have been moved to the 'scripts/' folder for organization.
# 
# Usage: .\dev.ps1 [command] [options]

param(
    [Parameter(Position=0)]
    [string]$Command = "",
    
    [Parameter(Position=1)]
    [string]$Configuration = "Debug"
)

function Show-Usage {
    Write-Host "AI Model Evaluator - Main Development Script" -ForegroundColor Cyan
    Write-Host "===========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\dev.ps1 [command] [configuration]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor White
    Write-Host "  build     - Build the project (default: Debug)" -ForegroundColor Green
    Write-Host "  run       - Run the application" -ForegroundColor Green
    Write-Host "  clean     - Clean build artifacts" -ForegroundColor Green
    Write-Host "  restore   - Restore NuGet packages" -ForegroundColor Green
    Write-Host "  publish   - Publish release build" -ForegroundColor Green
    Write-Host "  release   - Build in release mode" -ForegroundColor Green
    Write-Host "  test      - Run tests" -ForegroundColor Green
    Write-Host "  watch     - Run in watch mode (auto-restart on changes)" -ForegroundColor Green
    Write-Host "  package   - Package for distribution" -ForegroundColor Green
    Write-Host "  status    - Show git status and project info" -ForegroundColor Green
    Write-Host ""
    Write-Host "Configuration:" -ForegroundColor White
    Write-Host "  Debug     - Debug build (default)" -ForegroundColor Yellow
    Write-Host "  Release   - Release build" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\dev.ps1 build" -ForegroundColor Cyan
    Write-Host "  .\dev.ps1 build Release" -ForegroundColor Cyan
    Write-Host "  .\dev.ps1 run" -ForegroundColor Cyan
    Write-Host "  .\dev.ps1 watch" -ForegroundColor Cyan
    Write-Host "  .\dev.ps1 clean" -ForegroundColor Cyan
}

switch ($Command.ToLower()) {
    "build" {
        Write-Host "🔨 Building AI Model Evaluator ($Configuration)..." -ForegroundColor Cyan
        & dotnet build ModelEvaluator.csproj -c $Configuration
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Build successful!" -ForegroundColor Green
        } else {
            Write-Host "❌ Build failed!" -ForegroundColor Red
        }
    }
    
    "run" {
        Write-Host "🚀 Starting AI Model Evaluator..." -ForegroundColor Cyan
        & dotnet run ModelEvaluator.csproj
    }
    
    "clean" {
        Write-Host "🧹 Cleaning build artifacts..." -ForegroundColor Cyan
        & dotnet clean ModelEvaluator.csproj
        if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
        if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
        Write-Host "✅ Clean complete!" -ForegroundColor Green
    }
    
    "restore" {
        Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Cyan
        & dotnet restore ModelEvaluator.csproj
        Write-Host "✅ Restore complete!" -ForegroundColor Green
    }
    
    "publish" {
        Write-Host "📋 Publishing release build..." -ForegroundColor Cyan
        & dotnet publish ModelEvaluator.csproj -c Release -o publish
        Write-Host "✅ Published to 'publish' folder!" -ForegroundColor Green
    }
    
    "release" {
        Write-Host "🎯 Building release version..." -ForegroundColor Cyan
        & dotnet build ModelEvaluator.csproj -c Release
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Release build successful!" -ForegroundColor Green
        } else {
            Write-Host "❌ Release build failed!" -ForegroundColor Red
        }
    }
    
    "test" {
        Write-Host "🧪 Running tests..." -ForegroundColor Cyan
        & dotnet test
    }
    
    "watch" {
        Write-Host "👀 Starting in watch mode (press Ctrl+C to stop)..." -ForegroundColor Cyan
        Write-Host "The application will automatically restart when files change." -ForegroundColor Yellow
        & dotnet watch run --project ModelEvaluator.csproj
    }
    
    "package" {
        Write-Host "📦 Packaging for distribution..." -ForegroundColor Cyan
        
        # Create package directory
        if (Test-Path "package") { Remove-Item "package" -Recurse -Force }
        New-Item -ItemType Directory -Path "package" | Out-Null
        
        # Publish for multiple platforms
        $platforms = @("win-x64", "linux-x64", "osx-x64")
        foreach ($platform in $platforms) {
            Write-Host "  📋 Publishing for $platform..." -ForegroundColor Yellow
            & dotnet publish ModelEvaluator.csproj -c Release -r $platform --self-contained -o "package/$platform"
        }
        
        Write-Host "✅ Packaging complete! Check 'package' folder." -ForegroundColor Green
    }
    
    "status" {
        Write-Host "📊 Project Status" -ForegroundColor Cyan
        Write-Host "=================" -ForegroundColor Cyan
        Write-Host ""
        
        # Git status
        Write-Host "📍 Git Status:" -ForegroundColor Yellow
        & git status --short
        Write-Host ""
        
        # Project info
        Write-Host "📂 Project Info:" -ForegroundColor Yellow
        Write-Host "  Name: AI Model Evaluator" -ForegroundColor White
        Write-Host "  Framework: .NET 8" -ForegroundColor White
        Write-Host "  Type: Console Application" -ForegroundColor White
        Write-Host ""
        
        # Check for recent reports
        if (Test-Path "reports") {
            $reportCount = (Get-ChildItem "reports" -Filter "*.html").Count
            Write-Host "📊 Reports: $reportCount HTML files in 'reports' folder" -ForegroundColor White
        }
        
        # Build status
        Write-Host ""
        Write-Host "🏗️  Last Build:" -ForegroundColor Yellow
        if (Test-Path "bin/Debug/net8.0/ModelEvaluator.dll") {
            $buildTime = (Get-Item "bin/Debug/net8.0/ModelEvaluator.dll").LastWriteTime
            Write-Host "  Debug: $buildTime" -ForegroundColor Green
        } else {
            Write-Host "  Debug: Not built" -ForegroundColor Red
        }
        
        if (Test-Path "bin/Release/net8.0/ModelEvaluator.dll") {
            $buildTime = (Get-Item "bin/Release/net8.0/ModelEvaluator.dll").LastWriteTime
            Write-Host "  Release: $buildTime" -ForegroundColor Green
        } else {
            Write-Host "  Release: Not built" -ForegroundColor Yellow
        }
    }
    
    default {
        Show-Usage
    }
}
