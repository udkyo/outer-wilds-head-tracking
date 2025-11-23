#!/usr/bin/env pwsh
# Unrelease script - safely revert a release
# Usage: unrelease.ps1 <version>
# Example: unrelease.ps1 1.0.1

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Red
Write-Host "   Unreleasing v$Version" -ForegroundColor Red
Write-Host "======================================" -ForegroundColor Red
Write-Host ""

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "❌ ERROR: Version '$Version' is not valid semantic versioning" -ForegroundColor Red
    Write-Host "Use format: X.Y.Z (e.g., 1.0.1)" -ForegroundColor Yellow
    exit 1
}

# Confirmation prompt
Write-Host "⚠️  WARNING: This will:" -ForegroundColor Yellow
Write-Host "  1. Delete the local git tag v$Version" -ForegroundColor White
Write-Host "  2. Delete the remote git tag v$Version from GitHub" -ForegroundColor White
Write-Host "  3. Revert the version bump commit (if it's the last commit)" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  NOTE: This does NOT delete the GitHub Release." -ForegroundColor Yellow
Write-Host "  You must manually delete it at:" -ForegroundColor Yellow
Write-Host "  https://github.com/udkyo/outer-wilds-head-tracking/releases/tag/v$Version" -ForegroundColor Cyan
Write-Host ""

$confirmation = Read-Host "Are you sure you want to unrelease v$Version? Type 'yes' to confirm"
if ($confirmation -ne 'yes') {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# Step 1: Check if tag exists locally
Write-Host "[1/4] Checking local tag..." -ForegroundColor Cyan
$localTag = git tag -l "v$Version" 2>$null
if ($LASTEXITCODE -ne 0 -or -not $localTag) {
    Write-Host "⚠️  Local tag v$Version does not exist" -ForegroundColor Yellow
} else {
    Write-Host "✅ Found local tag v$Version" -ForegroundColor Green
}
Write-Host ""

# Step 2: Check if tag exists on remote
Write-Host "[2/4] Checking remote tag..." -ForegroundColor Cyan
$remoteTag = git ls-remote --tags origin "refs/tags/v$Version" 2>$null
if ($LASTEXITCODE -ne 0 -or -not $remoteTag) {
    Write-Host "⚠️  Remote tag v$Version does not exist" -ForegroundColor Yellow
} else {
    Write-Host "✅ Found remote tag v$Version" -ForegroundColor Green
}
Write-Host ""

# Step 3: Delete remote tag first (if exists)
if ($remoteTag) {
    Write-Host "[3/4] Deleting remote tag..." -ForegroundColor Cyan

    # Suppress all output and errors from git push
    $prevErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"

    git push origin --delete "v$Version" *>&1 | Out-Null
    $pushExitCode = $LASTEXITCODE

    $ErrorActionPreference = $prevErrorActionPreference

    if ($pushExitCode -eq 0) {
        Write-Host "✅ Remote tag v$Version deleted from GitHub" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Failed to delete remote tag (may not exist or no permissions)" -ForegroundColor Yellow
        Write-Host "  You may need to delete it manually from GitHub" -ForegroundColor Yellow
    }
} else {
    Write-Host "[3/4] Skipping remote tag deletion (doesn't exist)" -ForegroundColor Cyan
}
Write-Host ""

# Step 4: Delete local tag (if exists)
if ($localTag) {
    Write-Host "[4/4] Deleting local tag..." -ForegroundColor Cyan
    git tag -d "v$Version"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Local tag v$Version deleted" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to delete local tag" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[4/4] Skipping local tag deletion (doesn't exist)" -ForegroundColor Cyan
}
Write-Host ""

# Step 5: Check if we should revert the version bump commit
Write-Host "Checking for version bump commit..." -ForegroundColor Cyan
$lastCommit = git log -1 --pretty=format:"%s"
if ($lastCommit -match "chore: bump version to $Version") {
    Write-Host "Found version bump commit: $lastCommit" -ForegroundColor Yellow
    Write-Host ""
    $revertCommit = Read-Host "Do you want to revert this commit? (yes/no)"

    if ($revertCommit -eq 'yes') {
        Write-Host ""
        Write-Host "Reverting last commit..." -ForegroundColor Cyan
        git reset --hard HEAD~1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Version bump commit reverted" -ForegroundColor Green
            Write-Host ""
            Write-Host "⚠️  WARNING: You need to force push to update the remote:" -ForegroundColor Yellow
            Write-Host "  git push origin main --force" -ForegroundColor White
            Write-Host ""
            $forcePush = Read-Host "Push now? (yes/no)"
            if ($forcePush -eq 'yes') {
                git push origin main --force
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ Forced push completed" -ForegroundColor Green
                } else {
                    Write-Host "❌ Force push failed" -ForegroundColor Red
                    Write-Host "Run manually: git push origin main --force" -ForegroundColor Yellow
                }
            }
        } else {
            Write-Host "❌ Failed to revert commit" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "Commit left in place" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  Last commit is not a version bump for v$Version" -ForegroundColor Yellow
    Write-Host "  Last commit: $lastCommit" -ForegroundColor White
    Write-Host "  You may need to manually revert changes to manifest.json and CHANGELOG.md" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "   Unrelease Complete" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Manually delete the GitHub Release at:" -ForegroundColor White
Write-Host "     https://github.com/udkyo/outer-wilds-head-tracking/releases/tag/v$Version" -ForegroundColor Cyan
Write-Host "  2. Verify manifest.json has the correct version" -ForegroundColor White
Write-Host "  3. Verify CHANGELOG.md is up to date" -ForegroundColor White
Write-Host ""
