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
dotnet test api-back.csproj --configuration Release
```

## Validation

Run the local build and test flow before opening a pull request.
