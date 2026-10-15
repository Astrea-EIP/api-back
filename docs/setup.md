---
title: Setup
sidebar_position: 2
---

# Setup

## Commands

```bash
dotnet restore api-back.csproj
dotnet build api-back.csproj --configuration Release
dotnet test api-back.csproj --configuration Release
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
