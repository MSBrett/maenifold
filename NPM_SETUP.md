# NPM Publishing Guide

## Prerequisites

- NPM account with access to `@ma-collective` organization
- `.NET 9.0 SDK` installed
- Node.js 18+ installed

## Publishing Process

### 1. Version Update

Update version in `package.json`:
```json
{
  "version": "1.0.0"
}
```

### 2. Build All Platforms

Build self-contained binaries for all supported platforms:
```bash
npm run build:all
```

This creates binaries in:
- `bin/linux-x64/Maenifold`
- `bin/osx-arm64/Maenifold`
- `bin/osx-x64/Maenifold`
- `bin/win-x64/Maenifold.exe`

### 3. Test Installation Locally

Test the package locally before publishing:
```bash
npm pack
npm install -g ma-collective-maenifold-1.0.0.tgz
maenifold --tool MemoryStatus --payload '{}'
```

### 4. Publish to NPM

Ensure you're logged in:
```bash
npm login
```

Publish the package:
```bash
npm publish --access public
```

### 5. Verify Publication

Check the package on NPM:
- https://www.npmjs.com/package/@ma-collective/maenifold

Test installation from NPM:
```bash
npm install -g @ma-collective/maenifold
maenifold --tool MemoryStatus --payload '{}'
```

## Distribution Files

The NPM package includes:
- Cross-platform binaries (Linux, macOS ARM64/x64, Windows)
- Wrapper script (`scripts/maenifold.js`) for platform detection
- Installation script (`scripts/postinstall.js`)
- Documentation (README.md, docs/DEVELOPMENT.md, DISTRIBUTION.md)

## Troubleshooting

### Build Errors

If platform builds fail, ensure .NET 9.0 SDK is installed:
```bash
dotnet --version
```

### Binary Not Found After Install

Check that the wrapper script is executable:
```bash
chmod +x ./scripts/maenifold.js
```

### Platform Detection Issues

The wrapper script auto-detects platform. If it fails, set the binary path manually or check Node.js version:
```bash
node --version  # Should be 18+
```
