# Contributing to `api-back`

`api-back` follows the Astrea-EIP engineering handbook for workflow, versioning, review, and documentation rules.

## Quick reference

Worked example for issue `#123 feat: add wheelchair width to itinerary search`:

| Step | Value |
|---|---|
| Issue title | `feat: add wheelchair width to itinerary search` |
| Branch | `feature/123-itinerary-wheelchair-width` |
| Commit | `feat(itinerary): forward wheelchair width to engine` |
| PR title | `feat(itinerary): forward wheelchair width to engine` |
| PR body | includes `Closes #123` |

## Required references

Read these handbook sections before opening a pull request:

- [`architecture/repositories`](https://github.com/Astrea-EIP/docs/blob/main/docs/architecture/repositories.md)
- [`contribution/branches`](https://github.com/Astrea-EIP/docs/blob/main/docs/contribution/branches.md)
- [`contribution/git-workflow`](https://github.com/Astrea-EIP/docs/blob/main/docs/contribution/git-workflow.md)
- [`contribution/commits`](https://github.com/Astrea-EIP/docs/blob/main/docs/contribution/commits.md)
- [`contribution/pull-requests`](https://github.com/Astrea-EIP/docs/blob/main/docs/contribution/pull-requests.md)
- [`workflows/ci`](https://github.com/Astrea-EIP/docs/blob/main/docs/workflows/ci.md)
- [`workflows/release-flow`](https://github.com/Astrea-EIP/docs/blob/main/docs/workflows/release-flow.md)

The sections below restate those rules with `api-back`-specific detail (scopes, PR title format, automation exceptions) so this file is self-sufficient. Where it adds a precision the handbook doesn't define, it is labelled **api-back-specific**.

## Branch naming

Pattern: `<type>/<issue-id>-<scope>-<short-desc>`.

- Allowed types: `feature`, `fix`, `chore` only.
- Lowercase only, words separated by hyphens, ASCII only — no accents, even though the team writes in French.
- Always branch from an up-to-date `main`. One branch = one issue. Never push directly to `main`.

The branch type comes from the issue template that created the issue:

| Issue template | Issue title prefix | Label | Branch prefix |
|---|---|---|---|
| Feature | `feat:` | `type:feature` | `feature/` |
| Bug | `fix:` | `type:bug` | `fix/` |
| Task | `task:` | `type:task` | `chore/` |

**Trap:** the branch prefix is the full word `feature/`, while the commit and PR type is `feat`. `feat/123-...` is **not** a valid branch name.

Valid examples:

- `feature/123-itinerary-wheelchair-width`
- `fix/248-geocoding-empty-result`
- `chore/301-ci-engine-cache`
- `chore/64-docs-contributing-rules`

Invalid examples:

| Branch | Why it's invalid |
|---|---|
| `my-branch` | no type, no issue id |
| `feature/itinerary-width` | no issue id |
| `feat/123-itinerary-width` | `feat` is a commit type, not a branch prefix |
| `fix/123` | no scope or description |
| `Feature/123_Itinerary` | uppercase and underscore |
| `chore/64-règles` | accented character |

**Exceptions (api-back-specific):** branches created by automation don't carry an issue id and must not be renamed:

- `dependabot/**`, created by Dependabot (`.github/dependabot.yml`)
- `chore/bump-engine-version`, created by the `Check engine version` workflow

### Recommended scopes (api-back-specific)

The same scope is used in the branch name, the commit header, and the PR title. Non-exhaustive:

| Scope | Covers |
|---|---|
| `auth` | `AuthController`, `AuthService`, `AccessTokenMiddleware` |
| `itinerary` | `ItineraryController`, `ItineraryService`, itinerary DTOs |
| `geocoding` | `NominatimGeocodingService` |
| `engine` | AstreaEngine integration, `engine-version.txt` |
| `errors` | `ExceptionHandlingMiddleware`, `ErrorLog`, `Shared/Errors` |
| `mongo` | MongoDB config, repositories, `seed/` |
| `ci` | `.github/workflows` |
| `docker` | `Dockerfile`, `docker-compose.yml` |
| `deps` | NuGet / GitHub Actions dependency bumps |
| `docs` | `docs/`, `README.md`, `CONTRIBUTING.md` |

## Commit messages

Conventional Commits are mandatory: `type(scope): short description`. Breaking changes use `type!:` or `type(scope)!:`.

Allowed types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`. There is no `ci`, `build`, `perf` or `style` type — use `chore(ci): ...`, `chore(deps): ...`, etc.

Valid examples:

- `feat(auth): add refresh token endpoint`
- `fix(web): handle missing profile picture`
- `refactor(engine)!: replace route score contract`

Invalid examples:

- a commit with no type, e.g. `update login`
- vague descriptions, e.g. `fix stuff`
- informal tags, e.g. `WIP`, `fixup!`

**Consequences in this repo (api-back-specific):**

- PRs are merged with **merge commits**, not squashed, so every commit on the branch lands on `main` as-is.
- `.github/workflows/auto-tag.yml` scans every commit subject since the last tag:
  - `type!:` → major bump
  - any `feat` → minor bump
  - any `fix` → patch bump
  - only `docs`, `refactor`, `test`, `chore` → no release
- Because every commit is kept and parsed, no `WIP`, `fixup` or `update` commit may reach `main`. Clean up history (e.g. `git rebase -i`) before requesting review.

## Pull requests

**Title (api-back-specific — the handbook doesn't define a PR title format):** must be a valid Conventional Commit header, `type(scope): description`.

- Use the type of the main change; it must be one of the allowed commit types.
- English, imperative mood, lowercase start, no trailing period, 72 characters max.
- Never copy the issue title as-is — `task:` is not a valid type.
- Why: depending on the repository's merge-commit message setting, the PR title can become the merge commit subject that `auto-tag.yml` parses, and it is what reviewers read in `main`'s history.

Valid examples:

- `feat(itinerary): forward wheelchair width to engine`
- `fix(geocoding): return 400 when address cannot be resolved`
- `docs(contributing): add branch and pull request naming rules`

Invalid examples:

- `task: Docs : Regles de Contribution` — `task` is not a valid type
- `Update CONTRIBUTING` — no Conventional Commit header
- `feat add auth` — missing colon
- `WIP` — not a Conventional Commit header
- `Feat(Auth): Added login.` — uppercase type/scope, trailing period

**Body:** fill in `.github/pull_request_template.md`.

- `Closes #<issue-id>` is mandatory. `.github/workflows/project-automation.yml` reads the closing issue reference to move the issue to "In Review" on the project board when review is requested — without it the board isn't updated and the issue doesn't auto-close on merge.
- Only tick validation boxes for commands you actually ran.

**Rules (from the handbook):**

- One PR = one issue; the scope must not exceed the issue.
- CI must be green: format check, build, test, publish.
- Review by `@Astrea-EIP/astrea-core` (auto-requested via `.github/CODEOWNERS`).
- Merge with a merge commit (no squash).
- Behavior changes need tests or an explicit explanation.
- Use a draft PR for work in progress.

## Code style

- CI runs `dotnet format api-back.csproj --verify-no-changes`. Run `dotnet format api-back.csproj` before pushing — it needs `lib/AstreaEngine.dll`, see [Building the engine locally](#building-the-engine-locally) below.
- Layering: Controllers → `Interfaces/IServices` → `Interfaces/IRepositories`. Each of `Controllers/`, `Services/`, `Repositories/`, `Interfaces/*`, `DTOs/*`, `Models/Entities/`, and `Configurations/` has its own README defining naming suffixes and responsibilities — follow those, don't restate or invent rules beyond what they and CI already enforce.

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
