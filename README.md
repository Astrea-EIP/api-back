# api-back

`api-back` is the main backend repository for Astrea-EIP.
It owns API contracts, controllers, services, and persistence-facing behavior.

## What belongs here

This repository owns:

- backend endpoints and request handling
- authentication and authorization logic
- service orchestration and persistence integration
- backend build, test, and publish workflows
- repository-local backend documentation

This repository does not own:

- frontend or mobile UI code
- deployment environment state
- organization-wide engineering rules maintained in `docs`

## Local development

Use the .NET SDK required by the project.

```bash
dotnet restore api-back.csproj
dotnet build api-back.csproj --configuration Release
dotnet test api-back.csproj --configuration Release
```

## Documentation

Repository-specific backend documentation lives under `docs/`.
The shared engineering handbook lives in `Astrea-EIP/docs`.
