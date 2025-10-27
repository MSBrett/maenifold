# Homebrew Tap Setup

This directory contains the Homebrew formula for maenifold.

## Creating the Homebrew Tap

### 1. Create the tap repository

Create a new GitHub repository named `homebrew-tap`:
```bash
# On GitHub, create: ma-collective/homebrew-tap
# Then locally:
git clone https://github.com/ma-collective/homebrew-tap.git
cd homebrew-tap
```

### 2. Add the formula

Copy the formula to the tap:
```bash
cp /path/to/maenifold/distribution/homebrew/maenifold.rb Formula/maenifold.rb
git add Formula/maenifold.rb
git commit -m "Add maenifold formula"
git push origin main
```

### 3. Users can now install

```bash
brew tap ma-collective/tap
brew install maenifold
```

Or in one command:
```bash
brew install ma-collective/tap/maenifold
```

## Updating the Formula

When releasing a new version:

1. Run the release script to update checksums:
   ```bash
   ./scripts/release.sh 1.0.1
   ```

2. Copy updated formula to tap:
   ```bash
   cp distribution/homebrew/maenifold.rb ../homebrew-tap/Formula/maenifold.rb
   cd ../homebrew-tap
   git add Formula/maenifold.rb
   git commit -m "Update maenifold to 1.0.1"
   git push origin main
   ```

3. Users upgrade:
   ```bash
   brew update
   brew upgrade maenifold
   ```

## Testing the Formula

Test locally before publishing:
```bash
brew install --build-from-source ./maenifold.rb
brew test maenifold
brew audit --strict maenifold.rb
```
