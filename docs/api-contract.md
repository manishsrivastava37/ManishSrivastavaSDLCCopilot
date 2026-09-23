# API Contract

Base path: `/api/v1`. JSON uses camelCase. All timestamps are ISO-8601 UTC. Errors use `application/problem+json` with a generic title/detail and `traceId`.

## Health
- `GET /health/live` -> 200 when the process is alive.
- `GET /health/ready` -> 200 when dependencies are ready; 503 otherwise.

## Applications
- `GET /applications?status=&ownerId=&page=&pageSize=` -> paged summaries.
- `POST /applications` -> 201 with application; validates borrower name, positive decimal amount, and ISO currency.
- `GET /applications/{id}` -> application detail with tasks/documents/collateral summaries.
- `POST /applications/{id}/transitions` body `{ "targetStatus": "...", "reason": "..." }` -> 200; transition is audited and allow-listed.

## Workflow and controls
- `GET /applications/{id}/tasks` -> authorized work items.
- `POST /applications/{id}/recommendations` -> advisory recommendation with rationale and factors; never changes approval state.
- `POST /applications/{id}/decisions` -> authorized human decision with rationale; requires a current recommendation and records approver identity.
- `POST /applications/{id}/credit-verification` -> second-level verification with outcome and rationale; required before approval submission for INR applications above 25,000.
- `POST /applications/{id}/collateral` -> collateral metadata.
- `POST /applications/{id}/documents` -> document metadata registration.
- `POST /documents/{id}/verification` -> reviewer result and findings.

Authentication is bearer OIDC in production. Authorization is role and tenant scoped. The API never returns secrets, raw document content, tokens, or sensitive financial values in logs.
