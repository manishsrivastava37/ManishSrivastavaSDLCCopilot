# Data Model

## Core entities
- `LoanApplication`: UUID, tenant UUID, application number, borrower legal name, requested amount (`decimal(19,4)`), currency, purpose, lifecycle status, owner, timestamps, row version.
- `WorkflowTask`: UUID, tenant UUID, aggregate type/id, task type, assignee, status, due date, timestamps.
- `CreditRecommendation`: UUID, application UUID, analyst, recommendation, rationale, factor list, created timestamp. Advisory only.
- `CreditDecision`: UUID, application UUID, approver, decision, rationale, decided timestamp. Separate from recommendation.
- `Collateral`: UUID, application UUID, type, description, value (`decimal(19,4)`), currency, valuation date, lien status, review status.
- `Document`: UUID, application UUID, category, version, object-storage key, content hash, verification status, reviewer, findings, timestamps.
- `AuditEvent`: UUID, tenant UUID, actor, action, entity type/id, correlation ID, occurred timestamp, safe metadata JSON.

## Relationships and invariants
An application belongs to one tenant and may have many tasks, recommendations, decisions, collateral records, documents, and audit events. A decision cannot be created without an existing application and authorized approver. Lifecycle transitions are allow-listed and audited. Monetary values are never binary floating point. Document metadata never includes document content or secrets.

## SQL conventions
Use `uniqueidentifier`, `datetime2`, `decimal(19,4)`, bounded `nvarchar`, indexes on tenant/status/owner and application foreign keys, foreign keys with restrictive delete behavior, and optimistic concurrency tokens. Sensitive identifiers are not part of this first model.
