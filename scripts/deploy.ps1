# Deploy built mod to OWML Mods folder
# Usage: deploy.ps1 <Configuration>
# Example: deploy.ps1 Debug  or  deploy.ps1 Release

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration
)

# Detect OWML Mods path
$modsPath = if (Test-Path (Join-Path $env:APPDATA 'OuterWildsModManager\OWML\Mods')) {
    Join-Path $env:APPDATA 'OuterWildsModManager\OWML\Mods'
} elseif (Test-Path 'C:/Program Files (x86)/Steam/steamapps/common/Outer Wilds/OWML/Mods') {
    'C:/Program Files (x86)/Steam/steamapps/common/Outer Wilds/OWML/Mods'
} elseif (Test-Path 'C:/Program Files/Epic Games/OuterWilds/OWML/Mods') {
    'C:/Program Files/Epic Games/OuterWilds/OWML/Mods'
} elseif ($env:OWML_MODS_PATH -and (Test-Path $env:OWML_MODS_PATH)) {
    $env:OWML_MODS_PATH
} else {
    Write-Host 'ERROR: Could not find OWML Mods directory.' -ForegroundColor Red
    Write-Host 'Please install OWML or set OWML_MODS_PATH environment variable' -ForegroundColor Yellow
    Write-Host '' -ForegroundColor Yellow
    Write-Host 'Expected locations:' -ForegroundColor Yellow
    Write-Host '  - %APPDATA%\OuterWildsModManager\OWML\Mods' -ForegroundColor Yellow
    Write-Host '  - C:/Program Files (x86)/Steam/steamapps/common/Outer Wilds/OWML/Mods' -ForegroundColor Yellow
    Write-Host '  - C:/Program Files/Epic Games/OuterWilds/OWML/Mods' -ForegroundColor Yellow
    Write-Host '  - Or set OWML_MODS_PATH environment variable' -ForegroundColor Yellow
    exit 1
}
if (-not (Test-Path $modsPath)) {
    Write-Host "ERROR: Mods directory not found at $modsPath" -ForegroundColor Red
    exit 1
}

# Get mod name from manifest.json
$manifestPath = 'manifest.json'
if (-not (Test-Path $manifestPath)) {
    Write-Host 'ERROR: manifest.json not found in current directory' -ForegroundColor Red
    exit 1
}

$manifest = Get-Content $manifestPath | ConvertFrom-Json
$modName = $manifest.uniqueName
if (-not $modName) {
    Write-Host 'ERROR: Could not read uniqueName from manifest.json' -ForegroundColor Red
    exit 1
}

$targetPath = Join-Path $modsPath $modName
$buildPath = "bin/$Configuration/net48"

# Validate build output exists
if (-not (Test-Path $buildPath)) {
    Write-Host "ERROR: Build output not found at $buildPath" -ForegroundColor Red
    Write-Host "Please run 'pixi run build' or 'pixi run build-release' first" -ForegroundColor Yellow
    exit 1
}

# Create mod directory if it doesn't exist
if (-not (Test-Path $targetPath)) {
    New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
    Write-Host "Created mod directory: $targetPath" -ForegroundColor Cyan
}

Write-Host "Deploying $modName ($Configuration) to OWML..." -ForegroundColor Green
Write-Host "  Source: $buildPath" -ForegroundColor Gray
Write-Host "  Target: $targetPath" -ForegroundColor Gray

# Copy DLL and dependencies
Copy-Item "$buildPath/*.dll" $targetPath -Force
Copy-Item "$buildPath/*.pdb" $targetPath -Force -ErrorAction SilentlyContinue

# Copy manifest and config
Copy-Item 'manifest.json' $targetPath -Force
Copy-Item 'default-config.json' $targetPath -Force -ErrorAction SilentlyContinue

Write-Host '' -ForegroundColor Green
Write-Host "[OK] Deployment complete!" -ForegroundColor Green
Write-Host "Mod location: $targetPath" -ForegroundColor Cyan
Write-Host '' -ForegroundColor Green
Write-Host "Launch Outer Wilds through OWML to test your changes." -ForegroundColor Yellow
