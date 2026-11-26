# Paw & Order

In the veterinary system, sick and injured pets are cared for by two separate yet equally important groups: the clinicians who treat them, and the staff who keep the practice running. At Paw & Order, these are their stories.

## Stack

- Backend: `.NET 10 ASP.NET Core Minimal API`
- Frontend: `React + TypeScript + Vite + MUI`
- Database: `SQL Server 2022`
- Local orchestration: `.NET Aspire`
- Local container runtime: `Podman` (or Docker)

## What Is Included

- Clients and patients
- Clinician-based appointment scheduling
- Structured consults
- Direct invoicing with Australian GST at `10%`
- Manual payment recording
- Inventory with batch and expiry tracking
- Purchase orders and receiving
- Vaccination due dates
- Basic operational reporting
- Seeded demo data

## Prerequisites

- .NET 10 SDK
- Node.js and npm
- Podman or Docker, running

## Run Locally

Start everything with Aspire:

```powershell
dotnet run --project .\PawAndOrder.AppHost
```

The AppHost starts the SQL Server container (with a persistent data volume), then the API once SQL is ready, then the Vite frontend once the API passes its `/health` check. It also wires up the connection string and CORS origin, so there is nothing to configure.

Open the Aspire dashboard URL printed in the console to see each resource, its logs and traces, and the links to the frontend and API (Swagger at `/swagger`).

If you use Podman and Aspire does not detect it, set `ASPIRE_CONTAINER_RUNTIME=podman` first.

### Without Aspire

You can still run each part by hand:

```powershell
podman compose up -d                                   # SQL Server on 1433
dotnet run --project .\backend\src\PawAndOrder.Api     # http://localhost:5077/swagger
cd .\frontend; npm install; npm run dev                # http://localhost:5173
```

Without Aspire, the API uses this connection string:

```text
Server=tcp:127.0.0.1,1433;Database=PawAndOrder;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False
```

## Demo Login

- Username: `admin`
- Password: `PawAndOrder!`

Other seeded users include:

- `drharper`
- `drnguyen`
- `nurseamy`
- `nurseraj`
- `frontdesk`

All use the same seeded password.

## Notes

- The API seeds the database automatically on startup.
- Pricing is stored `ex GST` and invoice totals are shown `inc GST`.
- For the MVP, schema creation uses `EnsureCreated()` rather than migrations. After changing entities, delete the SQL data volume so the database is recreated.
- The frontend is desktop-oriented and optimized for workflow density over mobile layouts.
