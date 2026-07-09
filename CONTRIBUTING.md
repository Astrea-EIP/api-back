# Contributing to `api-back`

`api-back` follows the Astrea-EIP engineering handbook for workflow, versioning, review, and documentation rules.

## Required references

Read these handbook sections before opening a pull request:

- `architecture/repositories`
- `contribution/git-workflow`
- `contribution/commits`
- `contribution/pull-requests`
- `workflows/ci`
- `workflows/release-flow`

## Local rules

- Keep backend API and service concerns in this repository.
- Document API-facing changes in local docs when behavior changes.
- Keep one pull request scoped to one issue.
- Ensure the local build and test flow passes before review.

## Building the engine locally

`api-back` links against `AstreaEngine.dll`, built from `core-moteur`. The
version consumed is pinned in `engine-version.txt` and CI always builds
against that exact tag — never against a floating branch, to avoid api-back
silently compiling against a different engine than what `preprod`/`prod`
declare in `deploy-orchestration`.

To build it locally, matching what CI does:

```bash
git clone --branch "$(cat engine-version.txt)" --depth 1 \
  https://github.com/Astrea-EIP/core-moteur.git /tmp/core-moteur
dotnet build /tmp/core-moteur/lib -c Release -o /tmp/engine-build
mkdir -p lib
cp /tmp/engine-build/AstreaEngine.dll lib/AstreaEngine.dll
```

`lib/AstreaEngine.dll` is gitignored — it is always generated, never committed.

Bumping `engine-version.txt` to a newer `core-moteur` tag is proposed
automatically by the `Check engine version` workflow (weekly, opens a PR) —
review and merge it like any other PR. You can also edit the file by hand if
you need a specific version sooner.

## Local development data

Run a local, disposable MongoDB with:

```bash
docker compose up -d
```

This starts MongoDB seeded from `seed/init.js`. It is entirely separate from
`preprod`/`prod` — break it, reset it, or reload it at any time with:

```bash
docker compose down -v && docker compose up -d
```

Update `appsettings.Development.json` (gitignored, not committed) with a
connection string pointing at `mongodb://localhost:27017`.
