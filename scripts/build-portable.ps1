#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for creating a portable ModelEvaluator executable for Windows

.DESCRIPTION
    This script builds the ModelEvaluator application as a self-contained, single-file executable
    that can run on Windows machines without requiring .NET runtime installation.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.

.PARAMETER OutputDir
    Output directory for the portable executable. Default is .\dist

.PARAMETER Clean
    Clean the output directory before building

.EXAMPLE
    .\build-portable.ps1
    
.EXAMPLE
    .\build-portable.ps1 -Configuration Debug -Clean
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDir = ".\dist",
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean
)

# Set error handling
$ErrorActionPreference = "Stop"

Write-Host "🚀 ModelEvaluator Portable Build Script" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

# Get script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$ProjectFile = Join-Path $ProjectRoot "ModelEvaluator.csproj"

# Validate project file exists
if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

# Clean output directory if requested
if ($Clean -and (Test-Path $OutputDir)) {
    Write-Host "🧹 Cleaning output directory: $OutputDir" -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force
}

# Create output directory
if (-not (Test-Path $OutputDir)) {
    Write-Host "📁 Creating output directory: $OutputDir" -ForegroundColor Green
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Build parameters
$RuntimeIdentifier = "win-x64"
$PublishDir = Join-Path $OutputDir $RuntimeIdentifier

Write-Host "🔧 Build Configuration:" -ForegroundColor Magenta
Write-Host "  - Configuration: $Configuration" -ForegroundColor White
Write-Host "  - Runtime: $RuntimeIdentifier" -ForegroundColor White
Write-Host "  - Output: $PublishDir" -ForegroundColor White
Write-Host "  - Project: $ProjectFile" -ForegroundColor White

# Restore dependencies
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
try {
    dotnet restore $ProjectFile --runtime $RuntimeIdentifier
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet restore failed with exit code $LASTEXITCODE"
    }
    Write-Host "✅ NuGet packages restored successfully" -ForegroundColor Green
}
catch {
    Write-Error "❌ Failed to restore NuGet packages: $_"
    exit 1
}

# Build the application
Write-Host "🔨 Building application..." -ForegroundColor Yellow
try {
    dotnet build $ProjectFile --configuration $Configuration --runtime $RuntimeIdentifier --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
}
catch {
    Write-Error "❌ Build failed: $_"
    exit 1
}

# Publish as portable executable
Write-Host "📦 Publishing portable executable..." -ForegroundColor Yellow
try {
    $PublishArgs = @(
        "publish"
        $ProjectFile
        "--configuration", $Configuration
        "--runtime", $RuntimeIdentifier
        "--self-contained", "true"
        "--output", $PublishDir
        "--no-build"
        "/p:PublishSingleFile=true"
        "/p:PublishTrimmed=false"
        "/p:PublishReadyToRun=false"
    )
    
    & dotnet @PublishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    Write-Host "✅ Portable executable published successfully" -ForegroundColor Green
}
catch {
    Write-Error "❌ Publish failed: $_"
    exit 1
}

# Get executable info
$ExeName = "ModelEvaluator.exe"
$ExePath = Join-Path $PublishDir $ExeName

if (Test-Path $ExePath) {
    $FileInfo = Get-Item $ExePath
    $FileSizeMB = [math]::Round($FileInfo.Length / 1MB, 2)
    
    Write-Host "🎉 Build Complete!" -ForegroundColor Green
    Write-Host "==================" -ForegroundColor Green
    Write-Host "📄 Executable: $ExePath" -ForegroundColor White
    Write-Host "📊 Size: $FileSizeMB MB" -ForegroundColor White
    Write-Host "📅 Created: $($FileInfo.CreationTime)" -ForegroundColor White
    
    # Copy configuration files
    $ConfigFiles = @("appsettings.json", "appsettings.Development.json")
    foreach ($ConfigFile in $ConfigFiles) {
        $SourceConfig = Join-Path $ProjectRoot $ConfigFile
        if (Test-Path $SourceConfig) {
            $DestConfig = Join-Path $PublishDir $ConfigFile
            Copy-Item $SourceConfig $DestConfig -Force
            Write-Host "📝 Copied: $ConfigFile" -ForegroundColor Cyan
        }
    }
    
    Write-Host ""
    Write-Host "🚀 Ready to Deploy!" -ForegroundColor Green
    Write-Host "The portable executable can now be copied to any Windows machine and run without installing .NET." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "💡 Usage:" -ForegroundColor Cyan
    Write-Host "  Copy the entire contents of '$PublishDir' to the target machine" -ForegroundColor White
    Write-Host "  Run '$ExeName' to start the application" -ForegroundColor White
    
} else {
    Write-Error "❌ Executable not found at expected location: $ExePath"
    exit 1
}

Write-Host ""
Write-Host "🏁 Build script completed successfully!" -ForegroundColor Green
