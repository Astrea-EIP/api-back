---
title: Testing
sidebar_position: 4
---

# Testing

`api-back` has a dedicated xUnit test project, `tests/api-back.Tests`, added to
`api-back.sln`. CI runs it on every pull request and push to `main`.

## Prerequisites

- `lib/AstreaEngine.dll` — see [Building the engine locally](./setup.md); the test
  project references `api-back.csproj`, which needs the engine DLL to build.
- Docker, running — only needed for the `Database` category (Testcontainers starts a
  real, disposable MongoDB container). Everything else runs without Docker and without
  any network access.

## Running the tests

Everything:

```bash
dotnet test api-back.sln
```

By category, using the `Category` trait set on every test class:

```bash
dotnet test api-back.sln --filter "Category=Unit"
dotnet test api-back.sln --filter "Category=Integration"
dotnet test api-back.sln --filter "Category=Database"

# Everything except the tests that need Docker
dotnet test api-back.sln --filter "Category!=Database"
```

With TRX results and code coverage (what CI collects):

```bash
dotnet test api-back.sln -c Release --logger trx --collect:"XPlat Code Coverage" --results-directory TestResults
```

## Folder layout and naming

```
tests/api-back.Tests/
  Controllers/    one test class per controller
  Services/       one test class per service
  Middlewares/    one test class per middleware
  Repositories/   one test class per repository (Database category)
  Integration/    full HTTP pipeline and engine-integration tests
  Support/        fakes, fixtures, and the culture helper — no tests live here
```

- One test class per production class, named `<ClassUnderTest>Tests`.
- Test methods follow `Method_Scenario_ExpectedResult`, with an Arrange/Act/Assert body.
- Every test class carries `[Trait("Category", "Unit"|"Integration"|"Database")]`:
  - **Unit** — pure, in-memory, no I/O.
  - **Integration** — in-process HTTP (`WebApplicationFactory`) and/or WireMock; no
    Docker, no real network.
  - **Database** — Testcontainers; needs Docker.
- No `Thread.Sleep`. For fire-and-forget work (e.g. the async error-log write in
  `ExceptionHandlingMiddleware`), poll with `Support/Poll.cs`, which has a short timeout.

## No real network, ever

No test may call the real Nominatim, GraphHopper, or any other internet host. Both
GraphHopper and Nominatim URLs come from configuration (`GraphHopper:BaseUrl`,
`Nominatim:BaseUrl`), so tests point them at local fixtures instead:

- **Unit tests** that touch `HttpClient` directly use `Support/StubHttpMessageHandler.cs`
  — a minimal in-memory handler, no sockets at all.
- **Integration tests** that need a more realistic HTTP server (status codes, headers,
  request matching, logged requests) use `Support/WireMockFixture.cs`, a thin wrapper
  around a `WireMockServer` bound to a random local port. `Support/ApiTestContext.cs`
  bundles one of these per GraphHopper/Nominatim together with a fresh
  `Support/ApiFactory.cs` (a `WebApplicationFactory<Program>`) so each end-to-end test
  gets an isolated app instance with its own stubs.
- `Support/ApiFactory.cs` also replaces `IErrorLogRepository` with
  `Support/InMemoryErrorLogRepository.cs`, so the placeholder MongoDB connection string
  in `appsettings.json` is never touched outside the `Database` category.

## Adding a test

**New service or middleware method:** add a test to its existing class (or a new
`<Class>Tests` class under `Services/`/`Middlewares/`) following the naming convention
above. Mock its dependencies with Moq; if it calls out over HTTP, use
`StubHttpMessageHandler`.

**New endpoint, or a change to an existing one:** add a case to
`Integration/ApiEndToEndTests.cs`. Get a token with the `GetAnonymousTokenAsync()`
extension, configure any GraphHopper/Nominatim responses the endpoint needs through
`ctx.GraphHopper`/`ctx.Nominatim`, and assert on the HTTP response. If the endpoint
touches MongoDB through a new repository, add a `Database`-category test under
`Repositories/` using `Support/MongoDbFixture.cs`.

**Characterizing a known bug instead of fixing it:** write the test for the *correct*
behavior, then mark it `[Fact(Skip = "...")]` with a reason that names the problem — see
the skipped tests in `Integration/ApiEndToEndTests.cs` for examples. Don't weaken an
assertion to make a bug pass silently.
