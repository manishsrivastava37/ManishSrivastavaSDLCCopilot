# Commercial Lending Workflow Platform

Enterprise-oriented workflow foundation for commercial lending operations.

## Current status
Slice 1 foundation and loan origination lifecycle are implemented in `src/LoanOps.Api`. Design and delivery contracts are in `docs/`.

## Run
Requires .NET 10 SDK. From the repository root:

```powershell
dotnet restore
dotnet run --project src/LoanOps.Api
```

The API exposes `/health/live`, `/health/ready`, and `/swagger` in development. Database configuration is documented in `docs/architecture.md`; the current development profile uses a local SQLite database only when explicitly configured for local execution, while production targets SQL Server.
