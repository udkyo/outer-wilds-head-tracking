# Release Process

How to create a new release of the Head Tracking mod.

## Quick Release (Recommended)

```bash
pixi run release 1.0.1
```

This automated script will:
1. Validate git status is clean and tag doesn't exist
2. Update `manifest.json` version
3. Prompt you to edit `CHANGELOG.md` entry
4. Build and validate the release
5. Commit version bump with changelog
6. Create git tag
7. Push to GitHub (triggers automated release)

GitHub Actions will then automatically build, package, and publish the release.

## Reverting a Release

If you need to undo a release (e.g., found a critical bug):

```bash
pixi run unrelease 1.0.1
```

This deletes the local and remote git tags, and optionally reverts the version bump commit.

**Note:** You must manually delete the GitHub Release page (the script provides the URL).

## Manual Release (If Needed)

If the automated script isn't working or you need more control:

### 1. Update Version and Changelog

Edit `manifest.json`:
```json
{
  "version": "1.0.1",
  ...
}
```

Edit `CHANGELOG.md` (follow [Keep a Changelog](https://keepachangelog.com/) format):
```markdown
## [1.0.1] - 2025-01-22

### Added
- New features

### Fixed
- Bug fixes

### Changed
- Changes to existing functionality
```

### 2. Commit and Validate

```bash
git add CHANGELOG.md manifest.json
git commit -m "chore: release v1.0.1"
pixi run validate-release  # Optional but recommended
git push origin main
```

### 3. Create and Push Tag

```bash
git tag v1.0.1
git push origin main --tags
```

GitHub Actions will handle the rest (build, package, publish).

### 4. Verify

1. Check https://github.com/udkyo/outer-wilds-head-tracking/releases
2. Download the ZIP and verify contents
3. Test installation in OWML

## Local Testing

Test packaging without creating a release:

```bash
pixi run build-release  # Build release configuration
pixi run package        # Create ZIP in dist/ directory
```

## Troubleshooting

### Common Validation Errors

**"Version mismatch! Tag is v1.0.1 but manifest.json has 1.0.0"**
→ Update `manifest.json` to match the tag version

**"CHANGELOG.md missing entry for version 1.0.1"**
→ Add a section: `## [1.0.1] - YYYY-MM-DD`

**"Release v1.0.0 already exists!"**
→ Bump to a new version number or delete the existing release first

### Deleting a Failed Release

```bash
# Delete GitHub release (requires gh CLI)
gh release delete v1.0.1

# Delete tags
git tag -d v1.0.1                    # Local
git push origin :refs/tags/v1.0.1    # Remote

# Fix issues, then retry
git tag v1.0.1
git push origin main --tags
```

## What Gets Released

The release ZIP contains:
- `HeadTracking.dll`
- `manifest.json`
- `default-config.json`
- `README.md`
- `LICENSE`

Named: `udkyo.HeadTracking-v{version}.zip`

## CI/CD Workflows

**`.github/workflows/build.yml`** - Runs on every push to `main`:
- Builds Debug and Release configurations
- Verifies build outputs

**`.github/workflows/release.yml`** - Runs on tags matching `v*.*.*`:
- Validates version, changelog, and manifest
- Builds and packages the mod
- Creates GitHub Release with changelog excerpt
- Uploads ZIP asset

## Release Checklist

Before running `pixi run release`:
- [ ] All changes committed and pushed
- [ ] Code tested locally (built and deployed)
- [ ] You know what version number you're releasing
- [ ] You know what changes to list in the changelog

The script will handle the rest!
