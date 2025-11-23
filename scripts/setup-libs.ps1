# Setup OWML DLLs from OWML installation
# This script detects your OWML installation and copies the required DLLs to libs/

# Detect Outer Wilds installation path
$gameBasePath = if (Test-Path 'C:/Program Files (x86)/Steam/steamapps/common/Outer Wilds') {
    'C:/Program Files (x86)/Steam/steamapps/common/Outer Wilds'
} elseif (Test-Path 'C:/Program Files/Epic Games/OuterWilds') {
    'C:/Program Files/Epic Games/OuterWilds'
} elseif ($env:OUTER_WILDS_PATH -and (Test-Path $env:OUTER_WILDS_PATH)) {
    $env:OUTER_WILDS_PATH
} else {
    Write-Host 'ERROR: Could not find Outer Wilds installation.' -ForegroundColor Red
    Write-Host 'Please install Outer Wilds and OWML or set OUTER_WILDS_PATH environment variable' -ForegroundColor Yellow
    Write-Host '' -ForegroundColor Yellow
    Write-Host 'Expected locations:' -ForegroundColor Yellow
    Write-Host '  - C:/Program Files (x86)/Steam/steamapps/common/Outer Wilds' -ForegroundColor Yellow
    Write-Host '  - C:/Program Files/Epic Games/OuterWilds' -ForegroundColor Yellow
    Write-Host '  - Or set OUTER_WILDS_PATH environment variable' -ForegroundColor Yellow
    exit 1
}

# OWML DLLs are in the Managed folder
$owmlPath = Join-Path $gameBasePath 'OuterWilds_Data/Managed'
if (-not (Test-Path $owmlPath)) {
    Write-Host 'ERROR: Could not find OuterWilds_Data/Managed directory' -ForegroundColor Red
    Write-Host "Looked in: $owmlPath" -ForegroundColor Yellow
    Write-Host 'Is OWML installed? Run the OWML installer first.' -ForegroundColor Yellow
    exit 1
}

Write-Host "Found OWML at: $owmlPath" -ForegroundColor Green

# Create libs directory if it doesn't exist
if (-not (Test-Path 'libs')) {
    New-Item -ItemType Directory -Path 'libs' | Out-Null
    Write-Host 'Created libs/ directory' -ForegroundColor Cyan
}

# Copy required OWML DLLs
$dlls = @('OWML.Common.dll', 'OWML.ModHelper.dll', 'OWML.ModHelper.Menus.dll')
$allSuccess = $true

foreach ($dll in $dlls) {
    $src = Join-Path $owmlPath $dll
    if (Test-Path $src) {
        Copy-Item $src 'libs/' -Force
        Write-Host "[OK] Copied $dll" -ForegroundColor Cyan
    } else {
        Write-Host "[ERROR] $dll not found at $src" -ForegroundColor Red
        $allSuccess = $false
    }
}

if ($allSuccess) {
    Write-Host '' -ForegroundColor Green
    Write-Host 'OWML DLLs setup complete!' -ForegroundColor Green
    exit 0
} else {
    Write-Host '' -ForegroundColor Red
    Write-Host 'Some DLLs failed to copy. Please check your OWML installation.' -ForegroundColor Red
    exit 1
}
