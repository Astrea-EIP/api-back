---
title: Architecture
sidebar_position: 3
---

# Architecture

## Purpose

This page documents repository-local backend architecture.

## Rules

- API contracts and backend services belong here.
- Frontend rendering and deployment state do not belong here.
- Breaking API behavior must be documented and versioned accordingly.

## Mobility profile sent to the engine

`ItineraryService.BuildUserJson` translates the itinerary request into the user JSON expected by
`core-moteur`. The API contract seen by the apps does not change when the engine format evolves:
only this translation is updated.

The engine first applies a preset from `profile`, then the optional overrides.
A field is only sent for the mobility profile it applies to.

| API request | Sent to the engine | Applies to |
| --- | --- | --- |
| `mobility_profile` 0 / 1 / 2 / 3 | `profile`: `walking` / `wheelchair` / `crutches` / `blind` | all |
| `wheelchair_width` | `width` | Wheelchair |
| `can_climb_stairs` | `can_take_stairs` | Crutches |
| `path_preferences` `Stairs` (prefers stairs over slopes) | `avoid_slopes: true` | Pedestrian |
| `path_preferences` `Slopes` (prefers slopes over stairs) | `avoid_stairs: true` | Pedestrian |

Example for a wheelchair user: `{"profile":"wheelchair","width":0.8}`.

`Stairs` and `Slopes` set together contradict each other, so neither is sent.

Not sent yet, because the engine does not handle them (see the TODO in `BuildUserJson`):
`physical_strength`, `is_accompanied`, and the `WidePathways`, `LitStreets` and `Benches`
path preferences. They are still accepted and validated by the API.

The engine only uses this JSON from the release that includes core-moteur PR #23
(profile personalisation). Earlier engine versions ignore it.

## Stored data

| Collection | Entity | Indexes | Notes |
| --- | --- | --- | --- |
| `error_logs` | `ErrorLog` | none | One entry per unexpected error, found with the `errorId` returned by a `500`. |
| `users` | `User` | `email`, unique | One document per account. The email address is stored trimmed and in lower case; the password only as a hash (`passwordHash`, ASP.NET Core Identity PBKDF2 format). |
| `user_profiles` | `UserProfile` | `userId`, unique | One mobility profile per account. Health data (GDPR): saved only with the consent, deleted with the account or when the consent is withdrawn. |

`UserProfile` uses the fields of `POST /v0/itinerary`, so a saved profile can be sent as an
itinerary request as is. `UserProfileRepository.SaveAsync` creates or replaces the profile in a
single operation: `createdAt` and `consentGivenAt` keep their first values, and the optional
fields left empty are removed from the document instead of being stored as `null`.

Repositories create their indexes the first time a collection is used (`MongoIndexes`), not at
startup, so the API and the tests that replace the repositories never connect to MongoDB for
nothing.
