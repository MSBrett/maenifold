# WinGet Submission Guide

This directory contains WinGet manifests for submitting maenifold to Microsoft's official package repository.

## Submitting to WinGet

### 1. Fork the winget-pkgs repository

```bash
# On GitHub, fork: microsoft/winget-pkgs
git clone https://github.com/YOUR_USERNAME/winget-pkgs.git
cd winget-pkgs
```

### 2. Create manifest directory

WinGet uses a structured path based on publisher and package name:
```bash
# For version 1.0.0:
mkdir -p manifests/m/MaCollective/Maenifold/1.0.0
```

### 3. Copy manifests

Copy all three manifest files:
```bash
cp /path/to/maenifold/distribution/winget/*.yaml manifests/m/MaCollective/Maenifold/1.0.0/
```

The directory should contain:
- `MaCollective.Maenifold.yaml` (version manifest)
- `MaCollective.Maenifold.installer.yaml` (installer manifest)
- `MaCollective.Maenifold.locale.en-US.yaml` (locale manifest)

### 4. Validate manifests

Install WinGet validation tool:
```powershell
winget install Microsoft.WingetCreate
```

Validate the manifests:
```powershell
winget validate manifests/m/MaCollective/Maenifold/1.0.0/
```

### 5. Submit pull request

```bash
git checkout -b maenifold-1.0.0
git add manifests/m/MaCollective/Maenifold/1.0.0/
git commit -m "New package: MaCollective.Maenifold version 1.0.0"
git push origin maenifold-1.0.0
```

Create PR at: https://github.com/microsoft/winget-pkgs/pulls

### 6. PR Review Process

- Microsoft's automated checks will validate manifests
- A maintainer will review (typically 2-7 days)
- Once merged, users can install via: `winget install maenifold`

## Updating to a New Version

For version 1.0.1:

1. Run release script:
   ```bash
   ./scripts/release.sh 1.0.1
   ```

2. Update manifests with new version:
   - Change `PackageVersion: 1.0.1` in all three files
   - Update `InstallerUrl` to point to v1.0.1 release
   - Update `InstallerSha256` (from SHA256SUMS)
   - Update `ReleaseDate`
   - Update `ReleaseNotes` and `ReleaseNotesUrl`

3. Copy to winget-pkgs fork:
   ```bash
   mkdir -p manifests/m/MaCollective/Maenifold/1.0.1
   cp distribution/winget/*.yaml manifests/m/MaCollective/Maenifold/1.0.1/
   ```

4. Submit new PR:
   ```bash
   git checkout -b maenifold-1.0.1
   git add manifests/m/MaCollective/Maenifold/1.0.1/
   git commit -m "Update: MaCollective.Maenifold version 1.0.1"
   git push origin maenifold-1.0.1
   ```

## Testing Installation

After PR is merged, test:
```powershell
winget search maenifold
winget install maenifold
maenifold --tool MemoryStatus --payload '{}'
```

## Common Issues

### Validation Failures

If validation fails, common issues:
- Incorrect SHA256 checksum
- Invalid URL (must be HTTPS)
- Manifest version mismatch
- Missing required fields

### Installation Type

We use `portable` installer type because we distribute a ZIP with a single executable. No MSI/EXE installer needed.

## Resources

- WinGet Manifest Schema: https://github.com/microsoft/winget-pkgs/tree/master/doc/manifest
- Submission Guidelines: https://github.com/microsoft/winget-pkgs/blob/master/CONTRIBUTING.md
