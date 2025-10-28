# NPM Publishing Guide for maenifold

This guide explains how to set up npm publishing for the first time and trigger releases.

## Prerequisites

### 1. Create npm Account

1. Go to https://www.npmjs.com/signup
2. Create account with username, email, and password
3. Verify your email address
4. Enable 2FA (two-factor authentication) - **Required for publishing to @scopes**

### 2. Create npm Organization (Optional but Recommended)

Since the package uses `@ma-collective/` scope:

**Option A: Create organization on npmjs.com**
1. Go to https://www.npmjs.com/org/create
2. Create organization named `ma-collective`
3. Choose plan (free plan is fine)
4. Add yourself as owner

**Option B: Use your personal scope**
- You can publish to your personal scope instead
- Update all package names from `@ma-collective/maenifold` to `@yourusername/maenifold`
- Update `package.json` and all platform package files

### 3. Generate npm Access Token

1. Log in to https://www.npmjs.com
2. Click your profile icon → "Access Tokens"
3. Click "Generate New Token"
4. Choose "Automation" token type (required for CI/CD)
5. Copy the token (you won't see it again!)

### 4. Add Token to GitHub Secrets

1. Go to your GitHub repository
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Name: `NPM_TOKEN`
5. Value: Paste your npm token
6. Click "Add secret"

## Package Structure

The maenifold npm distribution uses an **optimized optionalDependencies pattern**:

### Main Package
- **Name**: `@ma-collective/maenifold`
- **Size**: ~2MB (just wrapper scripts and docs)
- **Contains**: CLI wrapper, postinstall script, documentation
- **Depends on**: Platform-specific packages via optionalDependencies

### Platform Packages (6 total)
Each contains only the binary for one platform:
- `@ma-collective/maenifold-linux-x64` (~30-40MB)
- `@ma-collective/maenifold-linux-arm64` (~30-40MB)
- `@ma-collective/maenifold-darwin-x64` (~30-40MB)
- `@ma-collective/maenifold-darwin-arm64` (~30-40MB)
- `@ma-collective/maenifold-win32-x64` (~30-40MB)
- `@ma-collective/maenifold-win32-arm64` (~30-40MB)

### How It Works
When a user runs `npm install -g @ma-collective/maenifold`:
1. npm downloads the main package (~2MB)
2. npm checks `optionalDependencies` for platform-specific packages
3. npm **automatically** downloads only the matching platform package (~30-40MB)
4. Total download: ~32-42MB instead of ~200MB for all platforms

## Release Process

### Automatic Release (Recommended)

The GitHub Actions workflow automatically builds and publishes when you create a git tag:

```bash
# Make sure all changes are committed
git add .
git commit -m "Release v1.0.0"

# Create and push tag
git tag v1.0.0
git push origin v1.0.0
```

This will:
1. Build binaries for all 6 platforms
2. Create GitHub Release with downloadable archives
3. Publish all 6 platform packages to npm
4. Publish main package to npm

### Manual Workflow Trigger

You can also trigger the workflow manually from GitHub:

1. Go to Actions tab in your repository
2. Click "Release" workflow
3. Click "Run workflow"
4. Enter version number (e.g., `1.0.0`)
5. Click "Run workflow"

### Local Testing (Before First Release)

Test the build process locally:

```bash
# Build all platforms (this will take a few minutes)
npm run build:all

# Create platform package structures
./scripts/create-platform-packages.sh

# Verify packages were created
ls -la packages/

# Test main package locally
npm pack
```

## First Release Checklist

- [ ] npm account created and email verified
- [ ] 2FA enabled on npm account
- [ ] Organization `ma-collective` created (or package names updated)
- [ ] npm automation token generated
- [ ] `NPM_TOKEN` secret added to GitHub repository
- [ ] All code committed to main branch
- [ ] Version in `package.json` and `src/maenifold.csproj` is correct
- [ ] README.md is up to date
- [ ] Documentation website is deployed
- [ ] Create git tag: `git tag v1.0.0 && git push origin v1.0.0`

## Updating Versions

For subsequent releases:

1. Update version in both files:
   - `package.json`: `"version": "1.0.1"`
   - `src/maenifold.csproj`: `<Version>1.0.1</Version>`

2. Update optionalDependencies in `package.json`:
   ```json
   "optionalDependencies": {
     "@ma-collective/maenifold-linux-x64": "1.0.1",
     "@ma-collective/maenifold-linux-arm64": "1.0.1",
     ...
   }
   ```

3. Commit and tag:
   ```bash
   git add .
   git commit -m "Bump version to 1.0.1"
   git tag v1.0.1
   git push origin main --tags
   ```

## Troubleshooting

### "You must sign up for private packages"
- Your npm account needs 2FA enabled
- Make sure `publishConfig.access` is set to `"public"` in package.json

### "You do not have permission to publish @ma-collective/maenifold"
- You need to be an owner/member of the `@ma-collective` organization
- Or change the package scope to your personal account

### "Package already published: @ma-collective/maenifold@1.0.0"
- You can't republish the same version
- Bump the version number and try again
- Or use `npm unpublish` within 72 hours (not recommended)

### Platform package not found when installing
- Make sure all platform packages were published successfully
- Check npm registry: https://www.npmjs.com/package/@ma-collective/maenifold-linux-x64
- Version numbers must match exactly between main and platform packages

### GitHub Actions workflow fails on npm publish
- Check that `NPM_TOKEN` secret is set correctly
- Verify token hasn't expired (automation tokens don't expire by default)
- Check workflow logs for specific error messages

## Post-Release

After successful release:

1. **Verify packages on npm**:
   - https://www.npmjs.com/package/@ma-collective/maenifold
   - Check all 6 platform packages are listed

2. **Test installation**:
   ```bash
   npm install -g @ma-collective/maenifold@1.0.0
   maenifold --version
   ```

3. **Update documentation**:
   - Update website with new version number
   - Update README if needed
   - Announce release

4. **Monitor**:
   - Watch npm download stats
   - Monitor GitHub issues for installation problems
   - Check for platform-specific issues

## Security Notes

- **Never commit npm tokens** to the repository
- **Use automation tokens** for CI/CD (not classic or granular tokens)
- **Enable 2FA** on your npm account
- **Audit dependencies** regularly with `npm audit`
- **Keep secrets secure** - npm tokens have full publish access

## Support

If you encounter issues:
- Check GitHub Actions logs for workflow failures
- Verify npm token permissions
- Ensure all prerequisites are met
- Open an issue on GitHub for help
