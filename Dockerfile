# Builds the pinned AstreaEngine.dll from core-moteur (same logic as CI, see engine-version.txt)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY api-back.csproj engine-version.txt ./
RUN apt-get update && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*

RUN git clone --depth 1 --branch "$(cat engine-version.txt)" \
    https://github.com/Astrea-EIP/core-moteur.git /tmp/core-moteur \
    && dotnet build /tmp/core-moteur/lib -c Release -o /tmp/engine-build

COPY . .
RUN mkdir -p lib && cp /tmp/engine-build/AstreaEngine.dll lib/AstreaEngine.dll

RUN dotnet restore api-back.csproj
RUN dotnet publish api-back.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5217
EXPOSE 5217

ENTRYPOINT ["dotnet", "api-back.dll"]
