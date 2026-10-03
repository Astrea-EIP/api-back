# syntax=docker/dockerfile:1

# engine: AstreaEngine.dll at the version pinned in engine-version.txt (same as CI)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS engine
WORKDIR /engine
COPY engine-version.txt ./
# git is not bundled in the sdk image; only needed here, not in the build stage
RUN apt-get update && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*
RUN git clone --depth 1 --branch "$(tr -d '[:space:]' < engine-version.txt)" \
      https://github.com/Astrea-EIP/core-moteur.git src \
 && dotnet build src/lib -c Release -o /engine/out

# build: restore is cached on the csproj alone, then publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY api-back.csproj ./
RUN dotnet restore api-back.csproj
COPY . .
COPY --from=engine /engine/out/AstreaEngine.dll lib/AstreaEngine.dll
RUN dotnet publish api-back.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# ASPNETCORE_HTTP_PORTS (not ASPNETCORE_URLS) matches what the base image already expects;
# ASPNETCORE_URLS triggers an "Overriding HTTP_PORTS" startup warning.
ENV ASPNETCORE_HTTP_PORTS=5217
EXPOSE 5217

# base image provides the non-root "app" user (uid 1654) via $APP_UID
USER $APP_UID
ENTRYPOINT ["dotnet", "api-back.dll"]
