#!/usr/bin/env pwsh
# Automated release script
# Usage: release.ps1 <version>
# Example: release.ps1 1.0.1

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "   Outer Wilds Head Tracking Release" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "❌ ERROR: Version '$Version' is not valid semantic versioning" -ForegroundColor Red
    Write-Host "Use format: X.Y.Z (e.g., 1.0.1)" -ForegroundColor Yellow
    exit 1
}

Write-Host "Target version: $Version" -ForegroundColor Green
Write-Host ""

# Step 1: Check git status
Write-Host "[1/8] Checking git repository status..." -ForegroundColor Cyan
$gitStatus = git status --porcelain 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Not a git repository" -ForegroundColor Red
    exit 1
}

if ($gitStatus) {
    Write-Host "❌ ERROR: Working directory has uncommitted changes:" -ForegroundColor Red
    git status --short
    Write-Host ""
    Write-Host "Please commit or stash changes before releasing" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Working directory is clean" -ForegroundColor Green
Write-Host ""

# Step 2: Check if tag already exists
Write-Host "[2/8] Checking if tag v$Version already exists..." -ForegroundColor Cyan
$tagExists = git tag -l "v$Version" 2>$null
if ($LASTEXITCODE -eq 0 -and $tagExists) {
    Write-Host "❌ ERROR: Git tag v$Version already exists" -ForegroundColor Red
    Write-Host "Choose a different version number" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Tag v$Version is available" -ForegroundColor Green
Write-Host ""

# Step 3: Update manifest.json
Write-Host "[3/8] Updating manifest.json..." -ForegroundColor Cyan
$manifestPath = "manifest.json"
if (-not (Test-Path $manifestPath)) {
    Write-Host "❌ ERROR: manifest.json not found" -ForegroundColor Red
    exit 1
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$oldVersion = $manifest.version
$manifest.version = $Version
$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath

Write-Host "  Updated version: $oldVersion → $Version" -ForegroundColor Yellow
Write-Host "✅ manifest.json updated" -ForegroundColor Green
Write-Host ""

# Step 4: Update CHANGELOG.md
Write-Host "[4/8] Generating CHANGELOG.md from git history..." -ForegroundColor Cyan
$changelogPath = "CHANGELOG.md"
if (-not (Test-Path $changelogPath)) {
    Write-Host "❌ ERROR: CHANGELOG.md not found" -ForegroundColor Red
    Write-Host "Create a CHANGELOG.md file first" -ForegroundColor Yellow
    exit 1
}

$changelog = Get-Content $changelogPath -Raw

# Check if version already has an entry
if ($changelog -match "\[$Version\]") {
    Write-Host "✅ CHANGELOG.md already has entry for v$Version" -ForegroundColor Green
} else {
    # Get commits since last tag (or all commits if no tags)
    $lastTag = git describe --tags --abbrev=0 2>$null
    $commitRange = if ($lastTag) { "$lastTag..HEAD" } else { "HEAD" }

    Write-Host "  Collecting commits from $commitRange..." -ForegroundColor Gray

    # Get commit messages
    $commits = git log $commitRange --pretty=format:"%s" --reverse 2>$null

    if (-not $commits) {
        Write-Host "⚠️  No commits found since last tag" -ForegroundColor Yellow
        $commits = @("Initial release")
    }

    # Categorize commits by conventional commit type
    $features = @()
    $fixes = @()
    $changes = @()
    $other = @()

    foreach ($commit in $commits) {
        if ($commit -match '^feat(\(.*?\))?:\s*(.+)$') {
            $features += "- $($matches[2])"
        } elseif ($commit -match '^fix(\(.*?\))?:\s*(.+)$') {
            $fixes += "- $($matches[2])"
        } elseif ($commit -match '^(chore|refactor|perf|docs)(\(.*?\))?:\s*(.+)$') {
            $changes += "- $($matches[3])"
        } else {
            # Include non-conventional commits too
            $other += "- $commit"
        }
    }

    # Build changelog entry
    $date = Get-Date -Format 'yyyy-MM-dd'
    $newEntry = "`n## [$Version] - $date`n`n"

    if ($features.Count -gt 0) {
        $newEntry += "### Added`n`n"
        $newEntry += ($features -join "`n") + "`n`n"
    }

    if ($changes.Count -gt 0) {
        $newEntry += "### Changed`n`n"
        $newEntry += ($changes -join "`n") + "`n`n"
    }

    if ($fixes.Count -gt 0) {
        $newEntry += "### Fixed`n`n"
        $newEntry += ($fixes -join "`n") + "`n`n"
    }

    if ($other.Count -gt 0 -and ($features.Count -eq 0 -and $changes.Count -eq 0 -and $fixes.Count -eq 0)) {
        # Only add "other" if there are no categorized commits
        $newEntry += ($other -join "`n") + "`n`n"
    }

    # Insert after the changelog header
    if ($changelog -match '(?s)(# Changelog.*?)(## \[)') {
        $changelog = $changelog -replace '(?s)(# Changelog.*?\n\n)', "`$1$newEntry"
    } else {
        # Find end of header section and insert there
        $changelog = $changelog -replace '(?s)(# Changelog.*?adheres to.*?\n\n)', "`$1$newEntry"
    }

    Set-Content $changelogPath $changelog

    Write-Host "✅ CHANGELOG.md generated from commits" -ForegroundColor Green
    Write-Host "   Found: $($features.Count) features, $($fixes.Count) fixes, $($changes.Count) changes" -ForegroundColor Gray
}
Write-Host ""

# Step 5: Run validation checks
Write-Host "[5/8] Running pre-release validation..." -ForegroundColor Cyan
$validateScript = Join-Path "scripts" "validate-release.ps1"
if (Test-Path $validateScript) {
    & $validateScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ ERROR: Validation failed" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "⚠️  Warning: validate-release.ps1 not found, skipping validation" -ForegroundColor Yellow
}
Write-Host ""

# Step 6: Build release version
Write-Host "[6/8] Building release version..." -ForegroundColor Cyan
try {
    $buildOutput = dotnet build HeadTracking.csproj --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ ERROR: Build failed" -ForegroundColor Red
        Write-Host $buildOutput
        exit 1
    }
    Write-Host "✅ Build succeeded" -ForegroundColor Green
} catch {
    Write-Host "❌ ERROR: Build failed with exception" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 7: Commit changes (if any)
Write-Host "[7/8] Committing version bump..." -ForegroundColor Cyan

# Check if there are any changes to commit
$gitStatus = git status --porcelain 2>$null
if ($gitStatus) {
    git add manifest.json CHANGELOG.md
    git commit -m "chore: bump version to $Version"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ ERROR: Failed to commit changes" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Changes committed" -ForegroundColor Green
} else {
    Write-Host "✅ No changes to commit (version and changelog already up to date)" -ForegroundColor Green
}
Write-Host ""

# Step 8: Create and push tag
Write-Host "[8/8] Creating and pushing release tag..." -ForegroundColor Cyan

# Extract changelog for this version
$changelogContent = Get-Content $changelogPath -Raw
$versionSection = ""
if ($changelogContent -match "(?s)## \[$Version\].*?(?=(## \[|\z))") {
    $versionSection = $matches[0].Trim()
}

# Create annotated tag with changelog
git tag -a "v$Version" -m "Release v$Version`n`n$versionSection"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Failed to create git tag" -ForegroundColor Red
    exit 1
}

# Push commit and tag
git push origin main
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Failed to push commits" -ForegroundColor Red
    Write-Host "Tag created locally but not pushed. Run: git push origin main --tags" -ForegroundColor Yellow
    exit 1
}

git push origin --tags
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Failed to push tags" -ForegroundColor Red
    Write-Host "Run manually: git push origin --tags" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Tag v$Version created and pushed" -ForegroundColor Green
Write-Host ""

# Success!
Write-Host "======================================" -ForegroundColor Green
Write-Host "   ✨ Release Complete! ✨" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Version $Version has been released!" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. GitHub Actions will automatically build and create the release" -ForegroundColor White
Write-Host "  2. Monitor the workflow at: https://github.com/udkyo/outer-wilds-head-tracking/actions" -ForegroundColor White
Write-Host "  3. Once complete, the release will be available at:" -ForegroundColor White
Write-Host "     https://github.com/udkyo/outer-wilds-head-tracking/releases/tag/v$Version" -ForegroundColor White
Write-Host ""
