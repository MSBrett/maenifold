# Branch Protection Rules

## Branch Strategy

```
feature/* → dev (via PR) → main (via PR)
```

## Main Branch Protection (PRODUCTION)

**Settings to enable in GitHub:**

### General
- [x] Require a pull request before merging
  - [x] Require approvals: **2**
  - [x] Dismiss stale pull request approvals when new commits are pushed
  - [x] Require review from CODEOWNERS
  - [x] Require approval of the most recent reviewable push

### Status Checks
- [x] Require status checks to pass before merging
  - [x] Require branches to be up to date before merging
  - **Required status checks:**
    - `Build / build`
    - `Security / secrets-scan`
    - `Security / dependency-scan`
    - `CodeQL / Analyze (csharp)`
    - `CodeQL / Analyze (javascript)`

### Conversation Resolution
- [x] Require conversation resolution before merging

### Restrictions
- [x] Do not allow bypassing the above settings
- [x] Restrict who can push to matching branches
  - Only allow: `ma-collective/maintainers`

### Force Push
- [x] Do not allow force pushes
- [x] Do not allow deletions

### Additional Rules
- [x] Require signed commits
- [x] Require linear history
- [x] Require deployments to succeed before merging
- [x] Lock branch (no direct pushes at all)

## Dev Branch Protection (STAGING)

**Settings to enable in GitHub:**

### General
- [x] Require a pull request before merging
  - [x] Require approvals: **1**
  - [x] Dismiss stale pull request approvals when new commits are pushed

### Status Checks
- [x] Require status checks to pass before merging
  - **Required status checks:**
    - `Build / build`
    - `Security / secrets-scan`

### Conversation Resolution
- [x] Require conversation resolution before merging

### Force Push
- [x] Do not allow force pushes
- [x] Do not allow deletions

## Feature Branch Convention

All feature branches should follow this naming pattern:
- `feature/description` - New features
- `fix/description` - Bug fixes
- `docs/description` - Documentation updates
- `refactor/description` - Code refactoring
- `test/description` - Test additions/updates
- `perf/description` - Performance improvements
- `security/description` - Security fixes

## Workflow

### 1. Feature Development
```bash
# Create feature branch from dev
git checkout dev
git pull origin dev
git checkout -b feature/my-feature

# Work on feature
# Commit changes
# Push to origin
git push -u origin feature/my-feature
```

### 2. PR to Dev
- Open PR from `feature/*` → `dev`
- Required checks must pass
- 1 approval required
- Merge via "Squash and merge"

### 3. PR to Main (Release)
- Open PR from `dev` → `main`
- Required checks must pass
- 2 approvals required
- Merge via "Create a merge commit" (preserve history)
- Automatically triggers release workflow

## Automated Enforcement

The following GitHub Actions will enforce these rules:

1. **Block Direct Commits** - Workflow fails if direct commit detected
2. **PR Validation** - Ensures PRs follow conventions
3. **Branch Name Validation** - Rejects incorrectly named branches

## Setup Instructions

1. Go to Settings → Branches in the GitHub repository
2. Add branch protection rule for `main`
3. Add branch protection rule for `dev`
4. Configure as specified above
5. Save changes

## CodeQL Security Analysis

The repository includes automated CodeQL security analysis for both C# and JavaScript code. 

### Repository-level Code Scanning
To enable full CodeQL functionality with security alerts in the GitHub UI:
1. Go to Settings → Security → Code security and analysis
2. Enable "Code scanning"
3. The CodeQL workflow will then upload results to GitHub's security dashboard

### Workflow Behavior
- **When code scanning is enabled**: Results are uploaded to GitHub's security dashboard
- **When code scanning is disabled**: Analysis still runs and passes, but results are not uploaded
- The workflow will display informational warnings if upload fails due to disabled code scanning
- Both scenarios allow the workflow to pass required status checks

### Required Status Checks
The following CodeQL checks are required for branch protection:
- `CodeQL / Analyze (csharp)` - Analyzes C# code for security vulnerabilities
- `CodeQL / Analyze (javascript)` - Analyzes JavaScript/TypeScript code for security vulnerabilities

## Emergency Procedures

In case of critical security fixes:
1. Create `security/cve-fix` branch from main
2. Fix the issue
3. Open PR directly to main with "SECURITY" label
4. Requires immediate review from 2 maintainers
5. Backport to dev after merge