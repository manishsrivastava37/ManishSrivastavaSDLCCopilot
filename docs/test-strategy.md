# Test Strategy

## Test levels
- Unit: domain transition rules, validation, recommendation explainability, authorization decisions, money handling.
- Integration: API plus SQL provider, tenant isolation, transactions, audit records, health probes, and problem details.
- Contract: request/response schemas and status codes against `docs/api-contract.md`.
- UI: component and workflow tests for loading, validation, error, empty, and authorized states; browser smoke tests for critical paths.
- Security: dependency scanning, static analysis, authorization matrix, upload abuse cases, and secret scanning.

## Definition of done per vertical slice
Format, lint, typecheck, unit tests, relevant integration tests, and a clean build pass. Tests use generated synthetic data only. Time-sensitive assertions use UTC and injected clocks where needed. No test logs sensitive values.

## Initial acceptance paths
Create an application; verify validation and audit; transition through allowed statuses; submit an explainable recommendation; require an authorized human decision; add collateral; register and verify document metadata; reject cross-tenant and unauthorized requests; return safe errors.
