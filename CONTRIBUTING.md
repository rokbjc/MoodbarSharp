# Contributing

## Development setup

```bash
dotnet build
dotnet test
dotnet run --project samples/MoodbarSample -- path/to/audio.mp3
```

## Branching model

`main` is always releasable. All work happens on short-lived branches merged back via pull request.

| Branch prefix | Purpose |
|---------------|---------|
| `feat/xxx` | New feature |
| `fix/xxx` | Bug fix |
| `chore/xxx` | Tooling, deps, CI |
| `docs/xxx` | Documentation only |
| `refactor/xxx` | Code restructure without behaviour change |

Rules:
- Branch from `main`; keep branches short-lived (days, not weeks)
- One concern per PR
- Prefer **squash merge** to keep `main` history linear
- Delete branches after merge

## Commit messages — Conventional Commits

All commits (or at minimum the squash-merge title) must follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Types and their effect on the version

| Type | Description | Version bump |
|------|-------------|--------------|
| `feat` | New user-facing feature | minor |
| `fix` | Bug fix | patch |
| `perf` | Performance improvement | patch |
| `docs`, `chore`, `ci`, `test`, `refactor`, `style` | No user impact | none (no release PR) |
| `feat!` / `fix!` / any `!` or `BREAKING CHANGE:` footer | Breaking change | major |

### Examples

```
feat: add GenerateFromInt16Pcm overload
fix: clamp output bytes to [0, 255] correctly
perf: replace List<Rgb> allocation with ArrayPool
feat!: rename MoodbarOptions.Bands to SpectrumBands

BREAKING CHANGE: MoodbarOptions.Bands has been renamed to SpectrumBands.
```

## Release flow (automated)

Releases are fully automated via [release-please](https://github.com/googleapis/release-please):

1. Merge a `feat` or `fix` commit to `main` → release-please opens a **Release PR** that bumps `version.json` and updates `CHANGELOG.md`
2. Review the Release PR; merge it when ready to ship
3. release-please creates the git tag `vX.Y.Z` and a GitHub Release automatically

You never create version tags or edit `CHANGELOG.md` by hand.

## Pull request guidelines

- Open an issue before starting non-trivial changes
- All CI checks must pass before merge
- All contributions must be compatible with the GPLv3 license (see [`COPYING`](COPYING))
- Changes that deviate significantly from the upstream [exaile/moodbar](https://github.com/exaile/moodbar) algorithm should be clearly justified in the PR description
