---
title: Setup
sidebar_position: 2
---

# Setup

## Prerequisites

The **.NET 8 SDK** is required and pinned in `global.json`. If multiple SDKs are
installed, `dotnet` resolves to 8.x in this repo regardless of what else is present.

## Commands

```bash
dotnet restore api-back.csproj
dotnet build api-back.csproj --configuration Release
dotnet test api-back.sln --configuration Release
```

## Validation

Run the local build and test flow before opening a pull request.

## Run with Docker

The image is built from the `Dockerfile` at the repository root (`runtime` stage), the
same one CI uses to build and publish `ghcr.io/astrea-eip/api-back`.

### Build

```bash
docker build -t api-back:local .
```

### Run alone

```bash
docker run --rm -p 5217:5217 \
  -e GraphHopper__BaseUrl=http://host.docker.internal:8989 \
  -e MongoDb__ConnectionString=mongodb://host.docker.internal:27017 \
  api-back:local
```

Inside a container, `localhost` refers to the container itself, not your machine —
reach services running on the host via `host.docker.internal` instead.

### Run with MongoDB

`docker-compose.yml` defines an optional `api` service behind the `api` profile, so a
plain `docker compose up -d` still starts only MongoDB:

```bash
docker compose --profile api up -d --build
docker compose --profile api down
```

### Environment variables

| Variable | Purpose | Default in the image | Local example |
|---|---|---|---|
| `ASPNETCORE_HTTP_PORTS` | Port(s) Kestrel listens on | `5217` | `5217` |
| `ASPNETCORE_ENVIRONMENT` | `Production` disables Swagger; `Development` enables it at `/swagger` | `Production` | `Development` |
| `MongoDb__ConnectionString` | MongoDB connection string | placeholder, not usable as-is | `mongodb://mongo:27017` |
| `MongoDb__DatabaseName` | MongoDB database name | `astrea_proto` | `astrea_proto` |
| `GraphHopper__BaseUrl` | GraphHopper base URL, required for `/v0/itinerary` | not set — itinerary requests fail until provided | `http://host.docker.internal:8989` |
| `Nominatim__BaseUrl` | Nominatim geocoding base URL | `https://nominatim.openstreetmap.org` | `https://nominatim.openstreetmap.org` |

GraphHopper and Nominatim come from `core-moteur`'s own `docker-compose.yml` (ports
`8989` and `8991`). That stack downloads the France OSM extract and needs several GB
of RAM, so it is optional when you only want to test the api container itself — the
itinerary endpoint will just return a 500 until those services are reachable.

The engine version baked into the image comes from `engine-version.txt`, exactly like
CI — see [Building the engine locally](../CONTRIBUTING.md) for details.

## Use the local API from the mobile app

The mobile app calls the API running on a developer machine. The API listens on port `5217`
in plain HTTP.

### Start the API so the app can reach it

| Where the app runs | How to start the API |
|---|---|
| Android emulator on the same machine | `dotnet run` (default `http` profile) or Docker |
| Real phone on the same Wi-Fi | `dotnet run --launch-profile lan`, or Docker (`docker compose --profile api up -d --build`) |

The default `http` profile only listens on `localhost`, so a real phone cannot reach it.
The `lan` profile listens on all network interfaces (`http://0.0.0.0:5217`), like the Docker image.

### Base URL to use in the app

| Where the app runs | Base URL |
|---|---|
| Android emulator | `http://10.0.2.2:5217` (`10.0.2.2` is the host machine as seen from the emulator) |
| iOS simulator | `http://localhost:5217` |
| Real phone on the same Wi-Fi | `http://<computer IP>:5217`, e.g. `http://192.168.1.42:5217` |

Find the computer IP with `ipconfig` (Windows, "IPv4 Address") or `ip addr` / `ifconfig`
(Linux, macOS). It can change when you switch networks.

Quick check from the phone browser: `http://<computer IP>:5217/swagger` should open Swagger.

### Troubleshooting

- **The phone cannot connect at all.** Check that the phone and the computer are on the same
  network, and that the firewall allows inbound TCP on port `5217` (on Windows, accept the prompt
  shown the first time, or add an inbound rule for a private network).
- **Requests are blocked by the app.** The local API is plain HTTP. Android (API 28+) and iOS
  block cleartext HTTP by default, so the mobile app must allow it for its development build only.
  This is configured in the mobile repository, not here.
- **Do not use the `https` profile from a phone.** It only listens on `localhost`, and it
  redirects HTTP calls to HTTPS on port `7028`. Use the `lan` profile instead.
- **401 after restarting the API.** Anonymous tokens are kept in memory: get a new one with
  `GET /v0/auth/anonymous` and retry.
- **Simulated GPS position.** Send it as the `start` of the itinerary in the `"lat,lng"` format
  (no geocoding needed). It must be inside the OSM extract loaded in GraphHopper, otherwise the
  itinerary request fails (see issue #87 for clearer error codes).
