#!/usr/bin/env bash
# Regenerates docs/api.json from the built app. Run after any API change; CI fails the
# build if the committed file is out of date (see .github/workflows/ci.yml).
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

dotnet build api-back.csproj -c Release
dotnet tool restore
dotnet swagger tofile --openapiversion 3.1 --output docs/api.json bin/Release/net8.0/api-back.dll v1
