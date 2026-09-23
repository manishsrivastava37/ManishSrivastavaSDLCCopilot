# Commercial Lending Workflow Platform Requirements

## Scope
A production-oriented workflow platform for commercial lending teams supporting loan origination, credit approval, collateral management, and document verification.

## Actors
- Relationship manager: creates and maintains applications.
- Credit analyst: prepares analysis and submits recommendations.
- Credit approver: makes an authorized, human decision.
- Collateral analyst: records and reviews collateral.
- Document reviewer: verifies required evidence.
- Operations administrator: configures workflow and monitors exceptions.

## Functional requirements
1. Create, update, search, and track commercial loan applications through intake, analysis, approval, closing, and decline states.
2. Maintain immutable workflow history and assigned work items with due dates and ownership.
3. Capture explainable credit recommendation factors and require a separate authorized human approval decision.
4. Maintain collateral records, valuation metadata, lien/perfection status, and review tasks.
5. Register documents, classify them, track verification status, reviewer, findings, and version history.
6. Enforce role-based authorization and tenant isolation at every API boundary.
7. Expose audit events for security-sensitive and business-critical actions.
8. Provide operational health, correlation IDs, safe errors, and structured logs.

## Non-functional requirements
- UTC timestamps and generated UUIDs.
- Decimal-safe monetary persistence and calculation.
- No secrets or sensitive customer data in source, logs, or demo fixtures.
- Generic client errors with safe server diagnostics.
- Accessible responsive React UI with keyboard support.
- Automated unit, integration, and API contract coverage.

## Explicit assumptions
- Initial deployment is a single institution with tenant-ready data boundaries; tenant ID is still required on owned records.
- Authentication is represented by a development identity provider abstraction; production uses OIDC/OAuth2 and an enterprise IdP.
- SQL Server is the target relational database; local development may use an in-memory/test provider only for tests.
- File binaries are stored in object storage, not in SQL; this first slice stores document metadata only.
- Credit recommendations are advisory calculations/rules and never auto-approve, decline, or bind a human approver.
- Second-level credit verification is required when `RequestedAmount > 25000` and `Currency == INR`; the exact boundary is intentionally strict greater-than, and non-INR applications do not use an unapproved direct numeric comparison.
- Demo configuration is clearly separated from production configuration and must not be used for real data.

## Out of scope for the initial release
Underwriting policy authoring UI, external bureau integrations, e-signature, disbursement, production document malware scanning, and full enterprise SSO setup.
