# Development Scripts

This folder contains the individual development scripts that were moved here to keep the root folder clean.

## Scripts

- **`build.bat`** / **`build.sh`** / **`build.ps1`** - Simple build scripts for Windows/Linux/PowerShell
- **`run.bat`** / **`run.sh`** / **`run.ps1`** - Simple run scripts for Windows/Linux/PowerShell  
- **`dev.bat`** - Comprehensive Windows batch development utility

## Usage

**For most development tasks, use the main `dev.ps1` script in the root folder instead:**

```powershell
# In the root folder
.\dev.ps1 build
.\dev.ps1 run
.\dev.ps1 clean
```

**These individual scripts are available if you need platform-specific simple commands:**

```bash
# Linux/macOS
./scripts/build.sh
./scripts/run.sh

# Windows Command Prompt  
scripts\build.bat
scripts\run.bat

# Windows PowerShell
.\scripts\build.ps1
.\scripts\run.ps1
```

The main `dev.ps1` script in the root provides the most comprehensive development experience with additional commands like watch mode, packaging, and status checking.
