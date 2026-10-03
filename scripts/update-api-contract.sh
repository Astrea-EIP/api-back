#!/usr/bin/env bash
# Regenerates docs/api.json from the built app. Run after any API change; CI fails the
# build if the committed file is out of date (see .github/workflows/ci.yml).
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

# The Swashbuckle CLI must run on the .NET 8 runtime (pinned in global.json): it ships
# net8.0/net9.0/net10.0 builds, picks the one matching the SDK that launches it, then
# relaunches itself against the app's net8.0 runtimeconfig — a newer build can't load there.
dotnet_version=$(dotnet --version)
if [[ "$dotnet_version" != 8.* ]]; then
  echo "This script needs the .NET 8 SDK (pinned in global.json). Install it from https://dotnet.microsoft.com/download/dotnet/8.0"
  exit 1
fi

dotnet build api-back.csproj -c Release
dotnet tool restore
dotnet swagger tofile --openapiversion 3.1 --output docs/api.json bin/Release/net8.0/api-back.dll v1
