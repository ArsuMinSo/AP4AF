# Launching UTB Minute – Canteen Ordering System

This document describes all prerequisites, first-time setup steps, and how to start the application.

---

## Prerequisites

| Requirement | Version / Notes |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | `net10.0` target framework |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Must be **running** before starting the app |
| [Visual Studio 2022/2026](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) | Optional, any IDE that supports .NET |
| Git | For cloning the repository |

> **Important:** Docker is required because PostgreSQL and Keycloak run as Docker containers managed by .NET Aspire. Ensure Docker Desktop is started before launching the application.

---

## Service Ports (when running via Aspire)

| Service | URL |
|---|---|
| **Web API** | `http://127.0.0.1:8080` |
| **Keycloak** (identity provider) | `http://localhost:8180` |
| **pgAdmin** (database GUI) | assigned by Aspire (see dashboard) |
| **Aspire Dashboard** | `http://localhost:15888` (default) |

---

## First-Time Setup

### 1. Clone the repository

```bash
git clone <repo-url>
cd AP4AF
```

### 2. Restore dependencies

```bash
dotnet restore UTB.Minute.slnx
```

### 3. Start Docker Desktop

Make sure Docker Desktop is running. The app uses Docker containers for:

- **PostgreSQL** – the application database
- **Keycloak** – authentication and identity management

### 4. Build the solution (optional sanity check)

```bash
dotnet build UTB.Minute.slnx
```

---

## Running the Application

### Via .NET Aspire (recommended)

```bash
dotnet run --project src/UTB.Minute.AppHost
```

This starts all services in the correct order:
1. PostgreSQL container (with persistent data volume)
2. Keycloak container (with persistent data volume)
3. `dbmanager` – runs database migrations
4. `webapi` – REST API, exposed at **http://127.0.0.1:8080**
5. `adminclient` – Blazor app for canteen management
6. `canteenclient` – Blazor app for students/cooks

Open the **Aspire Dashboard** (URL printed in the console) to monitor all services and view their URLs.

### Via Visual Studio

1. Open `UTB.Minute.slnx`
2. Set `UTB.Minute.AppHost` as the **startup project**
3. Press **F5** or click **Run**

---

## Accessing the Web API

Once running, the Web API is available at:

```
http://127.0.0.1:8080
```

### OpenAPI (Swagger UI)

In development mode the OpenAPI spec is served at:

```
http://127.0.0.1:8080/openapi/v1.json
```

### Health checks (development only)

- `http://127.0.0.1:8080/health`
- `http://127.0.0.1:8080/alive`

---

## Database Management

The `dbmanager` service exposes an HTTP endpoint to reset and re-seed the database (useful during development). Find its URL in the Aspire Dashboard, then call:

```http
POST http://<dbmanager-url>/db/reset
```

This will:
1. Drop the existing database
2. Re-run all EF Core migrations
3. Seed initial data

---

## Adding EF Core Migrations

```bash
dotnet ef migrations add <MigrationName> \
  --project src/UTB.Minute.Db \
  --startup-project src/UTB.Minute.WebApi
```

---

## Running Tests

```bash
dotnet test src/UTB.Minute.WebApi.Tests/
```

Tests use an in-memory database and do not require Docker or a running Aspire host.

---

## Stopping the Application

Press **Ctrl+C** in the terminal running the AppHost. Docker containers will be stopped but their data volumes are preserved, so data persists across restarts.

To remove all persisted data (start fresh), delete the named Docker volumes:

```bash
docker volume ls          # find volumes named after the Aspire app
docker volume rm <volume> # remove individual volumes
```

---

## Troubleshooting

| Problem | Solution |
|---|---|
| `Docker is not running` error | Start Docker Desktop and try again |
| Port 8080 already in use | Stop whatever is using port 8080, or change the `WithHttpEndpoint(port: 8080 ...)` line in `src/UTB.Minute.AppHost/Program.cs` |
| Port 8180 already in use | Change the Keycloak port in `src/UTB.Minute.AppHost/Program.cs` |
| Database connection errors | Wait for PostgreSQL to be healthy; Aspire retries automatically |
| Keycloak not ready | It can take 30–60 s on first start; Aspire waits for it before starting dependent services |
