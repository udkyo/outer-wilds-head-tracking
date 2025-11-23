#!/usr/bin/env pwsh
# Package release ZIP for distribution

$ErrorActionPreference = "Stop"

Write-Host "Packaging release..." -ForegroundColor Cyan

# Read manifest.json to get version and unique name
$manifest = Get-Content "manifest.json" -Raw | ConvertFrom-Json
$version = $manifest.version
$uniqueName = $manifest.uniqueName

$buildPath = "bin/Release/net48"
$packageName = "$uniqueName-v$version"
$packageDir = "dist/$packageName"
$zipPath = "dist/$packageName.zip"

# Clean and create dist directory
if (Test-Path "dist") {
    Remove-Item "dist" -Recurse -Force
}
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

# Verify build output exists
if (-not (Test-Path $buildPath)) {
    Write-Host "❌ Build output not found at $buildPath" -ForegroundColor Red
    Write-Host "Run 'pixi run build-release' first" -ForegroundColor Yellow
    exit 1
}

# Copy required files to package directory
Write-Host "Copying files to $packageDir..." -ForegroundColor Yellow

$filesToCopy = @(
    @{Path="$buildPath/HeadTracking.dll"; Required=$true},
    @{Path="manifest.json"; Required=$true},
    @{Path="default-config.json"; Required=$true},
    @{Path="README.md"; Required=$false},
    @{Path="LICENSE"; Required=$false}
)

foreach ($file in $filesToCopy) {
    if (Test-Path $file.Path) {
        Copy-Item $file.Path $packageDir -Force
        Write-Host "  ✅ $(Split-Path $file.Path -Leaf)" -ForegroundColor Green
    } elseif ($file.Required) {
        Write-Host "  ❌ Missing required file: $($file.Path)" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "  ⚠️  Optional file not found: $($file.Path)" -ForegroundColor Yellow
    }
}

# Create ZIP archive
Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
Compress-Archive -Path "$packageDir/*" -DestinationPath $zipPath -Force

# Get file size
$zipSize = (Get-Item $zipPath).Length / 1KB
Write-Host ""
Write-Host "✅ Package created successfully!" -ForegroundColor Green
Write-Host "   Location: $zipPath" -ForegroundColor Cyan
Write-Host "   Size: $([math]::Round($zipSize, 2)) KB" -ForegroundColor Cyan
Write-Host ""
