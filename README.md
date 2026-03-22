# FireOps CRM (MaX)

Vertical **service + AMC + sales** CRM for fire safety companies (tasks 2 & 3). Stack: **React (Vite) + .NET 10 Web API + PostgreSQL + EF Core**.

## Layout

| Folder | Contents |
|--------|----------|
| `be/` | `CRM.Api`, `CRM.Domain`, `CRM.Application`, `CRM.Infrastructure` |
| `fe/` | React + TypeScript SPA |

## PostgreSQL

1. Install PostgreSQL locally or use Docker, e.g.:

   `docker run --name fireops-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=fireops_crm_dev -p 5432:5432 -d postgres:16`

2. Connection strings (see `be/CRM.Api/appsettings.json` and `appsettings.Development.json`):

   - **Default (production template):** `Host=localhost;Port=5432;Database=fireops_crm;Username=postgres;Password=postgres`
   - **Development:** uses database `fireops_crm_dev` (same host/user/password by default)

3. Override without editing files:

   ```powershell
   $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=fireops_crm_dev;Username=postgres;Password=YOUR_PASSWORD"
   ```

4. Apply schema (migrations run automatically on startup):

   ```powershell
   cd d:\CRM
   dotnet run --project be/CRM.Api --urls http://localhost:5254
   ```

   Or create migration manually after model changes:

   ```powershell
   dotnet ef migrations add Name --project be/CRM.Infrastructure --startup-project be/CRM.Api --output-dir Persistence/Migrations
   dotnet ef database update --project be/CRM.Infrastructure --startup-project be/CRM.Api
   ```

5. Set a strong **`Jwt:Key`** in configuration for production.

## Run API

```powershell
cd d:\CRM
dotnet run --project be/CRM.Api --urls http://localhost:5254
```

- Swagger: `http://localhost:5254/swagger`

## Run frontend

```powershell
cd d:\CRM\fe
npm install
npm run dev
```

Open `http://localhost:5173` (proxies `/api` to the API).

## Seed logins

After first run against an empty database:

- **admin@fireops.local** / **Admin123!** (Admin)
- **tech@fireops.local** / **Tech123!** (Technician)

Sample data includes **Shah Fire Safety**, sites, AMC contract, visits, service request, installation, ops task, and products.

## Modules (MVP)

- Leads & enquiries (requirement, location, assignment)
- Customers & sites (compliance per site)
- Products, quotations + line items
- Installations (technician, schedule, checklist/photos)
- AMC contracts & visits
- Service requests
- Ops tasks (technician)
- Dashboard (AMC revenue, open services, pending quotes, etc.)

PDF quotes, WhatsApp, and email alerts are natural follow-ups.
