Title: <type>: <short summary>

Checklist
- [ ] Base branch is correct:
  - Into `dev` from any branch other than `dev` or `main` (same-repo), or from a fork
  - Into `main` only from `dev` (release PR)
- [ ] All status checks are green (including "Branch Policy / policy")
- [ ] Scope is minimal; linked issue(s) referenced if applicable
- [ ] Changelog/Docs updated if needed

Notes
- Direct PRs into `main` are blocked; raise a release PR from `dev`.
- For work-in-progress, open against `dev` from a `feature/*` branch.
- Forks may open PRs into `dev` directly.
