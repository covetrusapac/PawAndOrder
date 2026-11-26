# Paw & Order

In the veterinary system, sick and injured pets are cared for by two separate yet equally important groups: the clinicians who treat them, and the staff who keep the practice running. At Paw & Order, these are their stories.

## Stack

- Backend: `ASP.NET Core Minimal API`
- Frontend: `React + TypeScript + Vite + MUI`
- Database: `SQL Server 2022`
- Local container runtime: `Podman`

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

## Run Locally

1. Start SQL Server:

```powershell
podman compose up -d
```

2. Start the backend:

```powershell
dotnet run --project .\backend\src\PawAndOrder.Api
```

The API uses this local connection string by default:

```text
Server=tcp:127.0.0.1,1433;Database=PawAndOrder;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False
```

3. Start the frontend:

```powershell
cd .\frontend
npm install
npm run dev
```

4. Open:

- API Swagger: `http://localhost:5077/swagger`
- Frontend: `http://localhost:5173`

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
- For the MVP, schema creation uses `EnsureCreated()` rather than migrations.
- The frontend is desktop-oriented and optimized for workflow density over mobile layouts.
