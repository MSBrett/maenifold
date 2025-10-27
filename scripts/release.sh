#!/bin/bash
# Release script for maenifold
# Builds all platforms, generates checksums, and prepares distribution files

set -e  # Exit on error

VERSION=${1:-"1.0.0"}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

echo "==================================="
echo "Maenifold Release Builder"
echo "Version: $VERSION"
echo "==================================="
echo ""

# Step 1: Clean previous builds
echo "Step 1: Cleaning previous builds..."
rm -rf "$ROOT_DIR/bin/linux-x64"
rm -rf "$ROOT_DIR/bin/osx-arm64"
rm -rf "$ROOT_DIR/bin/osx-x64"
rm -rf "$ROOT_DIR/bin/win-x64"
rm -f "$ROOT_DIR"/*.tar.gz
rm -f "$ROOT_DIR"/*.zip
rm -f "$ROOT_DIR/SHA256SUMS"
echo "✓ Clean complete"
echo ""

# Step 2: Run tests
echo "Step 2: Running tests..."
cd "$ROOT_DIR"
dotnet test --configuration Release
echo "✓ Tests passed"
echo ""

# Step 3: Build all platforms
echo "Step 3: Building all platforms..."
echo "  - Building Linux x64..."
dotnet publish src/Maenifold.csproj -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o bin/linux-x64

echo "  - Building macOS ARM64..."
dotnet publish src/Maenifold.csproj -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o bin/osx-arm64

echo "  - Building macOS x64..."
dotnet publish src/Maenifold.csproj -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -o bin/osx-x64

echo "  - Building Windows x64..."
dotnet publish src/Maenifold.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o bin/win-x64

echo "✓ Build complete"
echo ""

# Step 4: Package binaries
echo "Step 4: Packaging binaries..."

cd "$ROOT_DIR/bin/linux-x64"
tar -czf "../../maenifold-linux-x64.tar.gz" Maenifold
echo "  ✓ Linux tarball created"

cd "$ROOT_DIR/bin/osx-arm64"
tar -czf "../../maenifold-osx-arm64.tar.gz" Maenifold
echo "  ✓ macOS ARM64 tarball created"

cd "$ROOT_DIR/bin/osx-x64"
tar -czf "../../maenifold-osx-x64.tar.gz" Maenifold
echo "  ✓ macOS x64 tarball created"

cd "$ROOT_DIR/bin/win-x64"
zip -r "../../maenifold-win-x64.zip" Maenifold.exe
echo "  ✓ Windows zip created"

cd "$ROOT_DIR"
echo ""

# Step 5: Generate checksums
echo "Step 5: Generating SHA256 checksums..."
shasum -a 256 maenifold-*.tar.gz maenifold-*.zip > SHA256SUMS
echo "✓ Checksums generated"
echo ""

# Step 6: Display checksums
echo "SHA256 Checksums:"
echo "===================="
cat SHA256SUMS
echo ""

# Step 7: Update distribution files with checksums
echo "Step 6: Updating distribution files with checksums..."

LINUX_SHA=$(shasum -a 256 maenifold-linux-x64.tar.gz | awk '{print $1}')
OSX_ARM64_SHA=$(shasum -a 256 maenifold-osx-arm64.tar.gz | awk '{print $1}')
OSX_X64_SHA=$(shasum -a 256 maenifold-osx-x64.tar.gz | awk '{print $1}')
WIN_SHA=$(shasum -a 256 maenifold-win-x64.zip | awk '{print $1}')

echo "Checksums extracted:"
echo "  Linux x64:     $LINUX_SHA"
echo "  macOS ARM64:   $OSX_ARM64_SHA"
echo "  macOS x64:     $OSX_X64_SHA"
echo "  Windows x64:   $WIN_SHA"
echo ""

# Update Homebrew formula
echo "Updating Homebrew formula..."
sed -i.bak "s/PLACEHOLDER_ARM64_SHA256/$OSX_ARM64_SHA/g" distribution/homebrew/maenifold.rb
sed -i.bak "s/PLACEHOLDER_X64_SHA256/$OSX_X64_SHA/g" distribution/homebrew/maenifold.rb
sed -i.bak "s/PLACEHOLDER_LINUX_SHA256/$LINUX_SHA/g" distribution/homebrew/maenifold.rb
rm distribution/homebrew/maenifold.rb.bak
echo "  ✓ Homebrew formula updated"

# Update WinGet installer manifest
echo "Updating WinGet installer manifest..."
sed -i.bak "s/PLACEHOLDER_WIN_SHA256/$WIN_SHA/g" distribution/winget/MaCollective.Maenifold.installer.yaml
rm distribution/winget/MaCollective.Maenifold.installer.yaml.bak
echo "  ✓ WinGet manifest updated"
echo ""

# Step 8: Summary
echo "==================================="
echo "Release Build Complete!"
echo "==================================="
echo ""
echo "Generated files:"
echo "  - maenifold-linux-x64.tar.gz"
echo "  - maenifold-osx-arm64.tar.gz"
echo "  - maenifold-osx-x64.tar.gz"
echo "  - maenifold-win-x64.zip"
echo "  - SHA256SUMS"
echo ""
echo "Distribution files updated:"
echo "  - distribution/homebrew/maenifold.rb"
echo "  - distribution/winget/*.yaml"
echo ""
echo "Next steps:"
echo "  1. Create git tag:       git tag -a v$VERSION -m 'Release v$VERSION'"
echo "  2. Push tag:             git push origin v$VERSION"
echo "  3. Create GitHub Release and upload archives + SHA256SUMS"
echo "  4. Publish to NPM:       npm publish --access public"
echo "  5. Submit Homebrew tap:  Copy maenifold.rb to homebrew-tap repo"
echo "  6. Submit WinGet PR:     Fork microsoft/winget-pkgs and submit manifests"
echo ""
