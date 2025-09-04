# Development Scripts

This folder contains the individual development scripts that were moved here to keep the root folder clean.

## Main Development Script

**For most development tasks, use the main `dev.ps1` script in the root folder:**

```powershell
# In the root folder
.\dev.ps1 build       # Build project
.\dev.ps1 run         # Run application  
.\dev.ps1 portable    # Build portable Windows executable
.\dev.ps1 watch       # Watch for changes
.\dev.ps1 clean       # Clean build artifacts
```

## Scripts in this folder

### Portable Build Scripts
- **`build-portable.ps1`** - PowerShell script for creating portable Windows executable
- **`build-portable.bat`** - Batch file wrapper for the PowerShell script

### Legacy Platform Scripts
- **`build.bat`** / **`build.sh`** / **`build.ps1`** - Platform-specific build scripts
- **`run.bat`** / **`run.sh`** / **`run.ps1`** - Platform-specific run scripts  
- **`dev.bat`** - Comprehensive Windows batch development utility

## Portable Executable

The portable build creates a self-contained Windows executable that requires no .NET installation:

```powershell
# Build portable executable (recommended)
.\dev.ps1 portable

# Direct script usage
.\scripts\build-portable.ps1 -Configuration Release
```

**Output:**
- `dist/win-x64/ModelEvaluator.exe` (~76MB)
- `dist/ModelEvaluator-Portable-Windows-x64.zip` (distribution package)

## Direct Script Usage

**These individual scripts are available if you need platform-specific simple commands:**

```bash
# Linux/macOS
./scripts/build.sh
./scripts/run.sh

# Windows Command Prompt  
scripts\build.bat
scripts\run.bat

# Portable Windows executable
scripts\build-portable.bat
```

**Note:** The main `dev.ps1` script provides a unified interface and is the recommended approach for development.
scripts\run.bat

# Windows PowerShell
.\scripts\build.ps1
.\scripts\run.ps1
```

The main `dev.ps1` script in the root provides the most comprehensive development experience with additional commands like watch mode, packaging, and status checking.
