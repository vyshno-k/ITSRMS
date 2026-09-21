# IT Service Request Management System 

This is the merged single web application. The existing UI is retained, with the Service Catalog and Assignment Management pages replaced by the corresponding IT-Service-Management-Final-aish page designs. Dashboard & Reports is added.

## Structure

- `frontend/` - Angular application
- `backend/` - ASP.NET Core API
- `database/` - SQLite database files

**## Architecture**

  

## Functional flow

Create Request -> New -> Assign Active Engineer -> Assigned -> In Progress -> Resolve -> Resolved -> Confirm & Close -> Closed

Alternative: Resolved -> Reopen -> Reopened -> In Progress

## Dashboard & Reports

- Open / In Progress / Resolved / Closed counts
- SLA-breached tickets
- Tickets by priority and category
- Engineer workload
- Basic filtering

## Run backend

From `backend`:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile http
```

The HTTP profile uses `http://localhost:5103`.

## Run frontend

From `frontend`:

```powershell
npm install --legacy-peer-deps
npm start
```

Open the Angular URL shown by the CLI, normally `http://localhost:4200`.

Use a supported Node.js version for the Angular CLI version specified in `frontend/package.json`.

## Main routes

- `/dashboard`
- `/employees`
- `/service-catalog`
- `/service-requests`
- `/service-requests/new`
- `/service-requests/:id`
- `/sla`
- `/assignment`
- `/resolution-closure`

