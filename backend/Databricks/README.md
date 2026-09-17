# Databricks integration (backend)

The ASP.NET Core API remains the operational application and SQLite remains the local transactional database. The API snapshots employee/catalog data to a Unity Catalog Volume, triggers the Databricks job through the Jobs API, polls the run and returns the PySpark result to Angular.

Configure `Databricks:Host`, `Databricks:Token`, `Databricks:JobId` and `Databricks:VolumePath` with ASP.NET Core environment variables. Never put the token in Angular or source control.

Endpoints:
- `GET /api/databricks/status`
- `POST /api/databricks/prepare`
- `POST /api/databricks/process`
- `GET /api/databricks/runs/{runId}`
