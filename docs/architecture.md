# Architecture

## System shape
- `src/LoanOps.Api`: ASP.NET Core REST API, domain/application services, EF Core persistence, authentication/authorization, audit and health endpoints.
- `src/LoanOps.Web`: React + TypeScript client, feature-oriented screens, API client, accessible workflow views.
- `tests/LoanOps.Api.Tests`: unit and integration tests.
- SQL Server: relational system of record. Object storage holds document binaries in production.

## Boundaries
HTTP endpoints translate requests to application commands. Application services own business transitions and authorization checks. Domain entities enforce invariants. Persistence is behind EF Core repositories/DbContext. Audit writes are append-only and occur in the same transaction as protected changes where practical.

## Deployment
Containerized API and web assets behind a TLS-terminating reverse proxy; managed SQL Server and object storage; centralized secrets manager; centralized logs/metrics/traces. CI runs restore, format check, build, lint, typecheck, unit tests, integration tests, and dependency/security scans.

## Technology decisions
.NET 10 minimal APIs, C# nullable reference types, EF Core SQL Server, FluentValidation-equivalent explicit validation for the first slice, xUnit, and React TypeScript/Vite when Node is available. A maintained equivalent may be selected and recorded if a requested package is unavailable.

## Reliability
Stateless API instances, health/readiness probes, correlation IDs, optimistic concurrency for mutable workflow records, idempotency keys on future externally retried commands, and UTC timestamps.
