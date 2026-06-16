# TabajarasInterview

A distributed application built with **.NET 10** and **.NET Aspire**. The solution orchestrates a Blazor Server web frontend and a containerized Rust API, wired together through Aspire's app-host model with built-in service discovery, health checks, resilience, and OpenTelemetry observability. The Rust API connects to a MySQL database that runs externally on a dedicated server (outside the Aspire orchestration).

## Table of Contents

- [Architecture](#architecture)
- [Projects](#projects)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Features](#features)
- [Project Structure](#project-structure)
- [Observability](#observability)
- [Contributing](#contributing)

## Architecture

The solution follows the .NET Aspire orchestration pattern. The **AppHost** project is the composition root that defines and launches the resources of the distributed application. MySQL is **not** orchestrated by Aspire — it runs on a separate server and is accessed by the Rust API directly:

```
+-------------------------------------------------------------+
|                  TabajarasInterview.AppHost                 |
|                   (Aspire Orchestrator)                     |
+-------------------------------------------------------------+
				  |                          |
				  v                          v
		+----------------+          +------------------+
		|   Rust API     |          |   Blazor Web     |
		|  (container)   |          |  (web-frontend)  |
		+----------------+          +------------------+
				  |
				  v
		+--------------------------+
		|   MySQL (external server, |
		|   not managed by Aspire)  |
		+--------------------------+
```

- **Rust API** is a container resource exposed over HTTP that connects to the external MySQL database.
- **Blazor Web** is the public-facing frontend that exposes external HTTP endpoints.
- **MySQL** runs on a dedicated server outside the Aspire orchestration and is reached by the Rust API.

## Projects

| Project | Description |
| --- | --- |
| `TabajarasInterview.AppHost` | The Aspire orchestrator. Declares the Rust API container and the Blazor frontend, and wires the dependencies between them. |
| `TabajarasInterview.Web` | The Blazor Server frontend using interactive server-side rendering, output caching, and a typed `HttpClient` for backend communication. |
| `TabajarasInterview.ServiceDefaults` | Shared Aspire defaults applied to every service: OpenTelemetry, health checks, service discovery, and HTTP resilience. |

## Tech Stack

- **.NET 10** (`net10.0`)
- **.NET Aspire** (`Aspire.AppHost.Sdk` 13.4.2)
- **Blazor Server** (interactive server components)
- **MySQL** (external database server, accessed by the Rust API)
- **Rust API** (container resource)
- **OpenTelemetry** (metrics, tracing, logging)
- **Bootstrap** (frontend styling)

## Prerequisites

Make sure the following are installed before running the project:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [.NET Aspire workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)
- A container runtime such as [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [Podman] (required for the Rust API container)
- A built `rust-api` container image available locally

## Getting Started

1. **Clone the repository**

   ```bash
   git clone https://github.com/jud0r/TabajarasInterview.git
   cd TabajarasInterview
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Run the application** through the AppHost:

   ```bash
   dotnet run --project TabajarasInterview.AppHost
   ```

4. **Open the Aspire Dashboard.** When the host starts, the console prints the dashboard URL (for example, `https://localhost:17022`). From there you can inspect every resource, its logs, traces, and metrics.

## Configuration

### AppHost (`AppHost.cs`)

The distributed application is composed in `TabajarasInterview.AppHost/AppHost.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Rust API
builder.AddContainer("rust-api", "rust-api")
	.WithHttpEndpoint(port: 8080, targetPort: 8080);

// Blazor
builder.AddProject<Projects.TabajarasInterview_Web>("web-frontend")
	.WithExternalHttpEndpoints();

builder.Build().Run();
```

> **Note:** Container resources require an explicit `targetPort` when adding an HTTP endpoint, since Aspire cannot infer the internal listening port of an arbitrary container image.
>
> The Rust API is responsible for its own MySQL connection (connection string / host configured on the Rust side), since the database is hosted externally and not managed by Aspire.

### Launch Profiles

The AppHost defines `http` and `https` launch profiles in `Properties/launchSettings.json`, each exposing the application URL and the Aspire Dashboard OTLP/MCP/resource-service endpoints.

## Features

The Blazor frontend ships with the standard Aspire starter pages:

- **Home** (`/`) — Landing page.
- **Counter** (`/counter`) — Interactive counter demonstrating server-side interactivity.
- **Weather** (`/weather`) — Sample data page rendered with streaming and output caching, served through the `WeatherApiClient`.

## Project Structure

```
TabajarasInterview/
├── TabajarasInterview.AppHost/          # Aspire orchestrator
│   ├── AppHost.cs                       # Resource composition
│   └── Properties/launchSettings.json
├── TabajarasInterview.Web/              # Blazor Server frontend
│   ├── Program.cs                       # App startup & DI
│   ├── WeatherApiClient.cs              # Typed HTTP client
│   └── Components/
│       ├── Pages/                       # Home, Counter, Weather, Error
│       └── Layout/                      # MainLayout, NavMenu
└── TabajarasInterview.ServiceDefaults/  # Shared Aspire defaults
	└── Extensions.cs                    # Telemetry, health, discovery
```

## Observability

`TabajarasInterview.ServiceDefaults` centralizes cross-cutting concerns that are applied to each service through `AddServiceDefaults()`:

- **OpenTelemetry** — Metrics (ASP.NET Core, HttpClient, runtime), tracing, and structured logging. When `OTEL_EXPORTER_OTLP_ENDPOINT` is set, telemetry is exported via OTLP.
- **Health Checks** — `/health` (readiness) and `/alive` (liveness) endpoints, available in the Development environment.
- **Service Discovery** — Enabled by default for all `HttpClient` instances.
- **Resilience** — Standard resilience handler (retries, circuit breakers, timeouts) applied to outbound HTTP calls.

## Contributing

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/my-feature`).
3. Commit your changes (`git commit -m 'Add my feature'`).
4. Push the branch (`git push origin feature/my-feature`).
5. Open a Pull Request.

---

_Repository: [github.com/jud0r/TabajarasInterview](https://github.com/jud0r/TabajarasInterview)_
