# Contributing to maenifold

## Branch Structure

maenifold uses a two-tier branch protection model:

```
feature-branch → dev → main
```

- **`main`**: Production-ready code, always stable
- **`dev`**: Integration branch for all features and fixes
- **`feature-branch`**: Your work branch

Both `main` and `dev` are protected branches that require pull requests.

## Workflow

### 1. Create a Feature Branch

Branch from `dev`:

```bash
git checkout dev
git pull origin dev
git checkout -b feature/your-feature-name
```

Branch naming conventions:
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring

### 2. Make Your Changes

```bash
# Make your changes
git add .
git commit -m "feat: Add your feature description"

# Push to your feature branch
git push origin feature/your-feature-name
```

### 3. Create Pull Request to `dev`

```bash
gh pr create --base dev --title "feat: Your feature description" --body "Description of changes"
```

Or use the GitHub web interface.

### 4. After Merge: `dev` to `main`

Once features are tested in `dev`, create a PR to `main`:

```bash
gh pr create --base main --head dev --title "Release: Version X.Y.Z" --body "Summary of changes"
```

## Commit Message Format

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `refactor:` - Code refactoring
- `test:` - Test updates
- `chore:` - Build/tooling changes

Examples:
```
feat: Add CLI interface documentation to README
fix: Resolve SQLite permission errors in CI tests
docs: Update tool name references to PascalCase
```

## Merge Conflict Resolution

If your PR has conflicts:

1. Update your branch with the target branch:
   ```bash
   git fetch origin
   git merge origin/dev  # or origin/main
   ```

2. Resolve conflicts in your editor

3. Commit and push:
   ```bash
   git add .
   git commit -m "Merge conflicts resolved"
   git push origin your-branch-name
   ```

## Code Standards

- Follow [.NET runtime coding guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md)
- Allman style braces
- Four spaces for indentation
- PascalCase for tool names
- All configuration in config files, not hardcoded

## Testing

Run tests before submitting PR:

```bash
dotnet test
```

For release builds:

```bash
dotnet build -c Release
```

## Questions?

Open an issue or reach out to the maintainers.
