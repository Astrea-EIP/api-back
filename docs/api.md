---
title: API contract
sidebar_position: 4
---

# API contract

This page is for frontend developers consuming `api-back`.

## Where the contract lives

- **`docs/api.json`** — the generated OpenAPI 3.1 document, the source of truth.
  - On `main`: `https://raw.githubusercontent.com/Astrea-EIP/api-back/main/docs/api.json`
  - Per release tag: `https://raw.githubusercontent.com/Astrea-EIP/api-back/<tag>/docs/api.json`

    Use the contract from the tag that is actually deployed to the environment you're
    targeting (see `deploy-orchestration` for which tag is where) — `main` can be ahead
    of what's running.
- **Swagger UI** at `http://localhost:5217/swagger` when running the API locally in
  `Development`. Lets you try requests directly from the browser, including
  authorizing with a token.

## Auth flow

1. `GET /v0/auth/anonymous` → `202` with `{ "accessToken": "<32 hex chars>" }`.
2. Send that token as the `access-token` header on every other request.
3. Tokens are kept in memory and are **lost when the API restarts**. If a request gets
   a `401`, fetch a new token and retry the request once before giving up.

## Endpoints

| Method | Path | Purpose | Status codes |
|---|---|---|---|
| `GET` | `/v0/auth/anonymous` | Get an anonymous access token | `202`, `400`, `500` |
| `POST` | `/v0/itinerary` | Compute an itinerary | `201`, `400`, `401`, `500` |

## Request fields (`POST /v0/itinerary`)

| Field | Format | Required |
|---|---|---|
| `start` | `"lat,lng"` (`.` as decimal separator, e.g. `48.8566,2.3522`) or a free-text address | yes |
| `end` | same formats as `start` | yes |
| `mobility_profile` | integer, `0`=Pedestrian, `1`=Wheelchair, `2`=Crutches, `3`=Blind | yes |
| `wheelchair_width` | number, metres, `> 0` and `<= 2` | Wheelchair only |
| `physical_strength` | integer, `0`=Low, `1`=Medium, `2`=High | Wheelchair only |
| `is_accompanied` | boolean | Wheelchair and Blind |
| `can_climb_stairs` | boolean | Crutches only |
| `path_preferences` | integer bitmask, see below | Pedestrian only |

`path_preferences` bits: `Stairs=1`, `Slopes=2`, `WidePathways=4`, `LitStreets=8`,
`Benches=16`. Combine with bitwise OR — e.g. `LitStreets | Benches` = `24`.

Full field descriptions, examples, and the exact schema are in `docs/api.json` /
Swagger UI — this table is a quick reference, not the contract itself.

## Error format

- `400` — `{ "error": "<message>" }`
- `401` — no body
- `500` — `{ "error": "<message>", "errorId": "<32 hex chars>" }`. Quote `errorId` to the
  backend team when reporting an issue: it identifies the entry in the `error_logs`
  MongoDB collection.

## Known limitations (current behavior)

- An address that can't be geocoded returns `500`, not `400`.
- A missing `mobility_profile` is currently accepted (defaults to Pedestrian) even
  though the schema marks it required.
- Engine `v0.2.2` does not yet change the computed route based on the mobility
  profile — it always uses its internal "wheelchair" profile.

## Changes

Breaking API changes land as `type!:` commits (see `CONTRIBUTING.md`), which produce a
major version tag. Watch for those when bumping the pinned tag you consume.

## For backend developers

- `docs/api.json` is **generated** — never edit it by hand.
- Run `scripts/update-api-contract.sh` after any API change (new endpoint, changed
  DTO, changed response code, etc.) and commit the result.
- CI fails the build if the committed `docs/api.json` doesn't match what the code
  actually generates.
