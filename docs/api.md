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

## Accounts

Sign-up (`POST /v0/auth/register`) and sign-in (`POST /v0/auth/login`) are implemented. The
`/v0/users/me` routes (account and saved mobility profile) are not implemented yet: their
contract is agreed in advance in `docs/contracts/accounts.openapi.yaml`, explained in
`docs/contracts/accounts.md`, so the apps can build their screens now.

## Auth flow

1. `GET /v0/auth/anonymous` → `202` with `{ "accessToken": "<JWT>" }`.
2. Send that token as the `access-token` header on every other request.
3. The token is a signed JWT that expires after 24 hours. It survives an API restart as long
   as `Jwt__SigningKey` is set (see `docs/setup.md`). If a request gets a `401`, fetch a new
   token and retry the request once before giving up.

With an account, `POST /v0/auth/register` (`201`) or `POST /v0/auth/login` (`200`) returns a
user token in the same `{ "accessToken": "<JWT>" }` shape. It is sent in the same
`access-token` header, works on every route the anonymous token works on, and expires after
7 days: on a `401`, sign in again.

## Endpoints

| Method | Path | Purpose | Status codes |
|---|---|---|---|
| `GET` | `/v0/auth/anonymous` | Get an anonymous access token | `202`, `400`, `500` |
| `POST` | `/v0/auth/register` | Create an account and sign in | `201`, `400`, `409`, `429`, `500` |
| `POST` | `/v0/auth/login` | Sign in | `200`, `400`, `401`, `429`, `500` |
| `POST` | `/v0/itinerary` | Compute an itinerary | `201`, `400`, `401`, `422`, `500` |
| `GET` | `/v0/itinerary` | Compute an itinerary, parameters in the query string | `200`, `400`, `401`, `422`, `500` |
| `GET` | `/v0/wheelchairs` | List the standard wheelchairs and accessibility reference values | `200`, `401`, `500` |

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

## Query parameters (`GET /v0/itinerary`)

Same computation as `POST /v0/itinerary`, for clients that can only send a GET (a
shareable link, a simple fetch). Every field of the table above becomes a query parameter
with the same name, format and rules, and the response body is the same, with `200`
instead of `201`. Encode free-text addresses (`encodeURIComponent`).

```
GET /v0/itinerary?start=48.8566,2.3522&end=48.8606,2.3376&mobility_profile=1&wheelchair_width=0.7
```

## Wheelchair catalogue (`GET /v0/wheelchairs`)

Build the wheelchair selector from this endpoint rather than hardcoding the list: models
are added or corrected in `api-back` without a new app release.

- `categories[]`: `id` (`manual` or `electric`, use it as a translation key), `name`
  (English), `models[]`.
- `models[]`: `id` (stable, store it to remember the user's choice), `brand`, `range`,
  `width` and `length` as `{ "min", "max" }`, and `recommended_wheelchair_width`.
- `accessibility_standards`: `min_passage_width` (0.8), `recommended_corridor_width`
  (0.9), `turning_circle_diameter` (1.5).

All dimensions are in **metres**, like `wheelchair_width`. Send the chosen model's
`recommended_wheelchair_width` (the widest chair of its range) as `wheelchair_width` to
`POST /v0/itinerary`. Only the width affects routing today: the engine does not use
`length` or the accessibility standards yet.

## Error format

- `400` — `{ "error": "<message>" }`: invalid request body, or an address that cannot be found.
- `401` — no body
- `429` — `{ "error": "<message>" }` and a `Retry-After` header (seconds): too many sign-ins or
  sign-ups from the same IP address, or too many failed sign-ins for the same email address.
  Wait for `Retry-After` seconds before trying again; do not retry automatically in a loop.
- `422` — `{ "error": "<message>" }`: a point is outside the area covered by the map (or too
  far from any path), or no route matches the mobility profile. The message is written for
  the end user (in French) and can be shown as is.
- `500` — `{ "error": "<message>", "errorId": "<32 hex chars>" }`. Quote `errorId` to the
  backend team when reporting an issue: it identifies the entry in the `error_logs`
  MongoDB collection.

## Known limitations (current behavior)

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
  DTO, changed response code, etc.) and commit the result. Requires the **.NET 8
  SDK**, pinned in `global.json` — the Swashbuckle CLI ships builds for multiple
  runtimes and must run on the same one as the app.
- CI fails the build if the committed `docs/api.json` doesn't match what the code
  actually generates.
