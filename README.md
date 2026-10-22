# TabajarasInterview

A recruitment / interview management application built with **.NET** and **.NET Aspire**. The solution orchestrates an **ASP.NET Core minimal API** (EF Core + SQL Server) and a **Blazor Server** frontend, wired together through Aspire's app-host model with built-in service discovery, health checks, resilience, and OpenTelemetry observability.

## Table of Contents

- [Architecture](#architecture)
- [Projects](#projects)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API](#api)
- [Database](#database)
- [Authentication](#authentication)
- [Features](#features)
- [Tests](#tests)
- [Project Structure](#project-structure)
- [Observability](#observability)
- [Contributing](#contributing)

## Architecture

The **AppHost** project is the composition root that launches the API and the Blazor frontend. SQL Server is **not** orchestrated by Aspire; it runs separately and is reached by the API through the `DefaultConnection` connection string.

```
+-------------------------------------------------------------+
|                  TabajarasInterview.AppHost                 |
|                   (Aspire Orchestrator)                     |
+-------------------------------------------------------------+
			  |                              |
			  v                              v
   +----------------------+       +--------------------+
   | TabajarasInterview.  |<------|  Blazor Web        |
   | Api (minimal API)    |  HTTP |  (web-frontend)    |
   +----------------------+       +--------------------+
			  |
			  v
   +----------------------+
   | SQL Server           |
   | (external, EF Core)  |
   +----------------------+
```

- **API** exposes REST endpoints under `/api/*`, secured with JWT bearer tokens.
- **Blazor Web** is the public-facing frontend. It calls the API through typed HTTP clients; the API address is injected by the AppHost (`Api__BaseUrl`).
- **SQL Server** stores all data and is managed with EF Core migrations.

## Projects

| Project | Description |
| --- | --- |
| `TabajarasInterview.AppHost` | Aspire orchestrator. Declares the API and the Blazor frontend, injects the shared JWT secret and the API base URL. |
| `TabajarasInterview.Api` | ASP.NET Core minimal API (`net11.0`) with EF Core, SQL Server, JWT authentication and OpenAPI/Swagger UI. |
| `TabajarasInterview.Web` | Blazor Server frontend (`net10.0`) using interactive server rendering, MudBlazor and JWT cookie authentication, with typed API clients. |
| `TabajarasInterview.Web.Tests` | xUnit tests for the Web project. |
| `TabajarasInterview.ServiceDefaults` | Shared Aspire defaults: OpenTelemetry, health checks, service discovery and HTTP resilience. |

## Tech Stack

- **.NET 10** (Web, AppHost) and **.NET 11 preview** (API)
- **.NET Aspire** (`Aspire.AppHost.Sdk` 13.4.2)
- **ASP.NET Core minimal APIs** with OpenAPI and Swagger UI
- **Entity Framework Core** with **SQL Server**
- **JWT (HS256)** authentication, **BCrypt** password hashing
- **Blazor Server** (interactive server components) and **MudBlazor**
- **OpenTelemetry** (metrics, tracing, logging)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) and a .NET 11 preview SDK (the API targets `net11.0`)
- [.NET Aspire tooling](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)
- A reachable **SQL Server** instance (local SQL Server, Express, or a container)

## Getting Started

1. **Clone the repository**

   ```bash
   git clone https://github.com/jud0r/TabajarasInterview.git
   cd TabajarasInterview
   ```

2. **Configure the database connection** (see [Database](#database)).

3. **Set the JWT signing secret** (see [Authentication](#authentication)).

4. **Restore and run** through the AppHost:

   ```bash
   dotnet restore
   dotnet run --project TabajarasInterview.AppHost
   ```

5. **Open the Aspire Dashboard.** The console prints its URL (for example `https://localhost:17022`). From there you can open the frontend and the API, and inspect logs, traces and metrics.

The API also exposes Swagger UI at `/swagger` (OpenAPI document at `/api-docs/openapi.json`).

## Configuration

### AppHost (`AppHost.cs`)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var jwtSecret = builder.AddParameter("jwt-secret", secret: true);

var api = builder.AddProject<Projects.TabajarasInterview_Api>("tabajarasinterview-api")
	.WithEnvironment("Jwt__Secret", jwtSecret);

builder.AddProject<Projects.TabajarasInterview_Web>("web-frontend")
	.WithEnvironment("Api__BaseUrl", api.GetEndpoint("http"))
	.WithEnvironment("Jwt__Secret", jwtSecret)
	.WaitFor(api)
	.WithExternalHttpEndpoints();

builder.Build().Run();
```

### API settings

| Key | Description | Default |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string (required). | none |
| `Jwt:Secret` | HS256 signing key (required). | none |
| `Jwt:ExpirationInMinutes` | Access token lifetime. | `60` |
| `Jwt:RefreshTokenDays` | Refresh token lifetime. | `30` |

### Web settings

| Key | Description |
| --- | --- |
| `Api:BaseUrl` | API base URL. Injected by the AppHost; set it manually when running the Web project standalone. |
| `Jwt:Secret` | Must be the **same** value as the API's. The frontend fails fast at startup if it is missing or shorter than 16 bytes. |

> **Note:** in the Development environment the API does not redirect HTTP to HTTPS, because the Web app calls the API over `http` and an HTTP client drops the `Authorization` header when following a redirect. In other environments `UseHttpsRedirection()` is enabled; point `Api:BaseUrl` to an `https://` address there.

## API

All routes are prefixed with `/api`. JSON uses **camelCase** property names and camelCase string enums. Endpoints other than login, refresh and user creation require `Authorization: Bearer <access token>`.

| Group | Endpoints |
| --- | --- |
| `/api/auth` | `POST /login`, `POST /refresh`, `POST /logout` |
| `/api/users` | `POST /create`, `GET /get` (current user), `GET /getAll`, `PUT /update`, `PUT /password`, `DELETE /delete` |
| `/api/candidates` | `GET /getAll`, `POST /create`, `GET /get/{id}`, `PUT /update/{id}`, `DELETE /delete/{id}`, `GET /get/{id}/positions` |
| `/api/positions` | `GET /getAll`, `POST /create`, `GET /get/{id}`, `PUT /update/{id}`, `DELETE /delete/{id}`, `GET /get/{id}/candidates`, `GET`/`POST`/`DELETE /get/{id}/stacks[/{stackId}]`, `GET`/`POST`/`DELETE /get/{id}/locations[/{locationId}]` |
| `/api/stacks` | `GET /getAll`, `POST /create`, `GET /get/{id}`, `PUT /update/{id}`, `DELETE /delete/{id}` |
| `/api/questions` | `GET /getAll`, `GET /byStack/{stackId}`, `POST /create`, `GET /get/{id}`, `PUT /update/{id}`, `DELETE /delete/{id}` |
| `/api/locations` | `GET /getAll` |
| `/api/candidateApplications` | `GET /getAll`, `GET /byCandidate/{candidateId}`, `GET /byPosition/{positionId}`, `POST /create`, `GET /get/{id}`, `PUT /update/{id}`, `DELETE /delete/{id}` |
| `/api/interviews` | `GET /getAll`, `POST /create`, `GET /get/{id}`, `PUT /update/{id}`, `DELETE /delete/{id}` |
| `/api/interviewQuestions` | `GET /getAll`, `POST /create`, `GET /get/{id}`, `PUT /update/{id}`, `DELETE /delete/{id}` |
| `/api/interviewReviewers` | `GET /getAll`, `POST /create`, `GET /get/{id}`, `PUT /update/{id}`, `DELETE /delete/{id}` |
| `/api/positionStacks` | `GET /getAll`, `POST /create`, `GET /get/{positionId}/{stackId}`, `DELETE /delete/{positionId}/{stackId}` |

Errors are returned through a shared filter (`ApiErrorFilter`) using `ApiException`; request validation uses camelCase field names.

> The Blazor dashboard page calls `GET /api/dashboard/overview`, which the API does not implement yet, so the dashboard shows no data until that endpoint is added.

## Database

- **Engine:** SQL Server, accessed with **EF Core** (`AppDbContext`, `UseSqlServer` with retry on failure).
- **Connection string:** `ConnectionStrings:DefaultConnection`. A development value is in `TabajarasInterview.Api/appsettings.Development.json`. Do not commit real credentials; prefer user-secrets or environment variables:

  ```bash
  dotnet user-secrets --project TabajarasInterview.Api set "ConnectionStrings:DefaultConnection" "<connection-string>"
  ```

- **Startup:** the API waits for the database (retrying every 2 seconds) before it starts serving requests.
- **Entities:** `User`, `RefreshToken`, `Candidate`, `Position`, `Location`, `PositionLocation`, `Stack`, `PositionStack`, `Question`, `CandidateApplication`, `Interview`, `InterviewQuestion`, `InterviewReviewer`. Entities derive from base types that provide `CreatedAt`/`UpdatedAt` timestamps and soft delete (`DeletedAt`).
- **Schema:** table and column names are kept as they are in the existing database. Changing C# or JSON naming does not change the schema.
- **Migrations** live in `TabajarasInterview.Api/Data/Migrations`:

  ```bash
  dotnet ef migrations add <Name> --project TabajarasInterview.Api --output-dir Data/Migrations
  dotnet ef database update --project TabajarasInterview.Api
  ```

## Authentication

1. `POST /api/auth/login` returns `accessToken`, `expiresIn`, `refreshToken` and the `user`.
2. The Blazor app stores both tokens in browser cookies (`tabajarasAccessToken`, `tabajarasRefreshToken`) and keeps them in memory for the current session.
3. Every protected API call sends `Authorization: Bearer <accessToken>`. If no access token is available, the Web app tries `POST /api/auth/refresh` with the refresh token.
4. `POST /api/auth/logout` revokes the refresh token (stored hashed in the `RefreshTokens` table); the Web app then clears its cookies.

Tokens are HS256 JWTs carrying `sub` (email), `id` and `tokenType` (`access` or `refresh`). The API and the Web app must share the same secret. The AppHost exposes it as the `jwt-secret` parameter and injects it as `Jwt__Secret` into both:

```bash
dotnet user-secrets --project TabajarasInterview.AppHost set "Parameters:jwt-secret" "<a-long-random-secret>"
```

To run the Web project standalone:

```bash
dotnet user-secrets --project TabajarasInterview.Web set "Jwt:Secret" "<same-secret>"
```

## Features

The Blazor frontend (MudBlazor UI) provides:

- **Home** (`/`), **Login** (`/login`) and **Register** (`/register`).
- **Dashboard** (`/dashboard`): recruitment KPIs and charts.
- **Candidates** (`/candidates`, `/candidates/{id}`): candidate CRUD and profile.
- **Positions** (`/positions`): position CRUD with stack assignment.
- **Applications** (`/applications`): candidate applications across positions.
- **Stacks** (`/stacks`) and **Questions** (`/questions`): CRUD for technology stacks and interview questions.
- **Users** (`/users`): list of registered users.
- Account menu with the current user's name and email, and logout.

## Tests

```bash
dotnet test TabajarasInterview.Web.Tests
```

The `TabajarasInterview.Web.Tests` project (xUnit) covers the API response parser and the authentication state provider.

## Project Structure

```
TabajarasInterview/
|-- TabajarasInterview.AppHost/          # Aspire orchestrator
|   `-- AppHost.cs                       # Resource composition
|-- TabajarasInterview.Api/              # ASP.NET Core minimal API
|   |-- Program.cs                       # Startup, JSON options, endpoint mapping
|   |-- Auth/                            # JWT service, bearer handler, claims
|   |-- Configuration/                   # Auth, database and OpenAPI setup
|   |-- Data/                            # AppDbContext, configurations, migrations
|   |-- Models/                          # EF Core entities
|   |-- Dtos/                            # Request/response contracts
|   |-- Endpoints/                       # One file per endpoint group
|   `-- Common/                          # Error filter, exceptions, validation
|-- TabajarasInterview.Web/              # Blazor Server frontend
|   |-- Program.cs                       # Startup, DI, JWT cookie authentication
|   |-- Services/Api/                    # Typed API clients + response parser
|   |-- Services/Auth/                   # Cookie/JWT auth services
|   |-- DTOs/                            # API request/response models
|   `-- Components/                      # Pages, Shared, Layout
|-- TabajarasInterview.Web.Tests/        # xUnit tests
`-- TabajarasInterview.ServiceDefaults/  # Shared Aspire defaults
```

## Observability

`TabajarasInterview.ServiceDefaults` applies these through `AddServiceDefaults()`:

- **OpenTelemetry**: metrics, tracing and structured logging. When `OTEL_EXPORTER_OTLP_ENDPOINT` is set, telemetry is exported via OTLP.
- **Health checks**: `/health` (readiness) and `/alive` (liveness) in Development.
- **Service discovery**: enabled for all `HttpClient` instances.
- **Resilience**: standard resilience handler (retries, circuit breakers, timeouts) on outbound HTTP calls.

## Contributing

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/my-feature`).
3. Commit your changes (`git commit -m 'Add my feature'`).
4. Push the branch (`git push origin feature/my-feature`).
5. Open a Pull Request.

---

_Repository: [github.com/jud0r/TabajarasInterview](https://github.com/jud0r/TabajarasInterview)_
