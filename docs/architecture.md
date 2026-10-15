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
