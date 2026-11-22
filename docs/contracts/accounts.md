---
title: Accounts contract
sidebar_position: 5
---

# Accounts contract

This page is for the web and mobile developers who build the sign-up, sign-in and mobility
profile screens. It explains the contract in `docs/contracts/accounts.openapi.yaml`, agreed
**before** the implementation so the apps can plug in their screens now.

- Status: proposed, to be validated by the web and mobile developers in the pull request.
- Implemented by #110 (register, login), #50 (user token), #46 (profile model) and #111
  (`/v0/users/me`).
- When an endpoint is implemented, it appears in `docs/api.json` like every other endpoint.
  Once all of them are there, `accounts.openapi.yaml` is removed: `docs/api.json` stays the
  only source of truth.

## Endpoints

| Method | Path | Purpose | Status codes |
|---|---|---|---|
| `POST` | `/v0/auth/register` | Create an account and sign in | `201`, `400`, `409`, `500` |
| `POST` | `/v0/auth/login` | Sign in | `200`, `400`, `401`, `500` |
| `GET` | `/v0/users/me` | Get the account | `200`, `401`, `500` |
| `DELETE` | `/v0/users/me` | Delete the account and the profile | `204`, `401`, `500` |
| `GET` | `/v0/users/me/profile` | Get the mobility profile | `200`, `401`, `404`, `500` |
| `PUT` | `/v0/users/me/profile` | Create or replace the mobility profile | `200`, `400`, `401`, `500` |
| `DELETE` | `/v0/users/me/profile` | Delete the profile and withdraw the consent, keep the account | `204`, `401`, `500` |

## Decisions

- **Token.** Register and login return `{ "accessToken": "<jwt>" }`, the same shape as
  `GET /v0/auth/anonymous`, so the apps keep one parser. The token is sent in the same
  `access-token` header. The expiry is in the `exp` claim of the token: on a `401`, sign in again.
- **Anonymous mode stays.** The anonymous token still works for the itinerary and wheelchair
  endpoints. The `/v0/users/me` routes require a user token
  and answer `401` to an anonymous token.
- **Token after deletion.** Tokens are stateless and are not revoked. Every `/v0/users/me`
  route checks that the account still exists and answers `401` otherwise, so after
  `DELETE /v0/users/me` the token gives no access to personal data. Until it expires, it can
  still call the anonymous routes, like an anonymous token. The apps delete the token on their
  side after the deletion.
- **Field names.** New fields are in `snake_case`, like the itinerary and wheelchair endpoints.
  `accessToken` keeps its current name so the existing token parsing does not change.
- **Consent (GDPR).** The mobility profile can reveal a disability, which makes it health data.
  The consent is asked only when the profile is saved: `consent: true` is required on
  `PUT /v0/users/me/profile`, otherwise the API answers `400`. Sign-up asks for no consent,
  because the account holds no health data. The API stores when the consent was first given
  (`consent_given_at`).
- **Withdrawal and erasure (GDPR).** `DELETE /v0/users/me/profile` withdraws the consent: the
  profile and `consent_given_at` are deleted for good and the account is kept. Saving a profile
  again asks for the consent again. `DELETE /v0/users/me` deletes the account and the profile
  for good.
- **One profile model.** The profile uses the fields of `POST /v0/itinerary`, with the same names,
  values and rules. A saved profile can therefore be sent as is to compute an itinerary, and the
  API ignores the fields that do not apply to the chosen `mobility_profile`.
- **Wheelchair width.** The apps build the model list from `GET /v0/wheelchairs` and send the
  chosen model's `id` as `wheelchair_model_id` and its `recommended_wheelchair_width` as
  `wheelchair_width`. Widths are in metres.
- **App choice.** `app_profile` is a free value the API stores and returns without using it, so
  an app can restore its own form when several of its choices share the same `mobility_profile`
  (for example `stroller` and `pedestrian` are both `0`).
- **Login errors.** A wrong email and a wrong password both give the same `401` message, so the
  endpoint cannot be used to find out whether an email address has an account.

## From the web models

`src/app/auth/account.models.ts` and `src/app/profile/mobility-profile.models.ts` in `app-web`.

| Web | Contract |
|---|---|
| `RegisterRequest.name`, `email`, `password` | `name`, `email`, `password` |
| `RegisterRequest.birthDate` | `birth_date` |
| `RegisterRequest.consent` | not sent any more: the consent moves to the profile screen, `consent` on `PUT /v0/users/me/profile` |
| `AccountSession.accessToken` | `accessToken` |
| `AccountErrorCode` `email-already-used` / `invalid-credentials` | `409` on register / `401` on login |
| `profile` `manual-wheelchair`, `electric-wheelchair` | `mobility_profile: 1` |
| `profile` `assisted-walking` | `mobility_profile: 2` |
| `profile` `pedestrian`, `stroller` | `mobility_profile: 0` (keep the choice in `app_profile`) |
| `profile` `visually-impaired` | `mobility_profile: 3` |
| `wheelchairModel` (free text) | `wheelchair_model_id`, from `GET /v0/wheelchairs` |
| `wheelchairWidthM` (fixed 0.70) | `wheelchair_width`, the model's `recommended_wheelchair_width` |
| `strength` `low` / `medium` / `high` | `physical_strength` `0` / `1` / `2` |
| `accompanied` | `is_accompanied` |
| `canClimbStairs` | `can_climb_stairs` |
| `stairsOrSlopes` `stairs` / `slopes` | `path_preferences` bit `Stairs` (1) / `Slopes` (2) |
| `preferWidePaths` / `preferLitStreets` / `requireBenches` | `path_preferences` bits `WidePathways` (4) / `LitStreets` (8) / `Benches` (16) |
| `consentGivenAt` | `consent_given_at`, set by the API |

## From the mobile models

`lib/features/onboarding/data/models/accessibility_profile.dart` in `app-mobile`.

| Mobile | Contract |
|---|---|
| `MobilityType.manualWheelchair`, `electricWheelchair` | `mobility_profile: 1` |
| `MobilityType.walkingWithAssistance` | `mobility_profile: 2` |
| `MobilityType.pedestrian`, `stroller` | `mobility_profile: 0` (keep the choice in `app_profile`) |
| no blind or visually impaired choice yet | `mobility_profile: 3` |
| `WheelchairDimensions.widthCm` | `wheelchair_width`, in **metres** (`widthCm / 100`) |
| `RoutePreferences.avoidStairs` (walking with assistance) | `can_climb_stairs: !avoidStairs` |
| `RoutePreferences.avoidStairs` (pedestrian) | `path_preferences` bit `Slopes` (2), slopes preferred over stairs |
| `RoutePreferences.avoidSteepSlopes` (pedestrian) | `path_preferences` bit `Stairs` (1), stairs preferred over slopes |

Not in the contract, because the routing engine does not use them: `lengthCm`,
`maxStepHeightCm`, `avoidUnevenSurfaces`, `preferElevators` and `dogFriendly`. They can be
added to the itinerary request and to the profile together, once the engine supports them.
