# Security

## Threat model
Protect borrower and financial workflow data from unauthorized tenant access, privilege escalation, tampering, malicious uploads, replayed commands, injection, and sensitive-data leakage.

## Controls
- Production OIDC/OAuth2 bearer validation with issuer, audience, expiry, signature, and scope checks.
- Role-based authorization plus tenant predicates on every query and command.
- Server-side validation, parameterized EF queries, bounded input sizes, and restrictive CORS.
- TLS in transit, encrypted SQL/object storage at rest, managed secrets, key rotation, and backups with tested restore.
- Append-only audit events for sign-in context, data changes, decisions, verification, and permission failures.
- Correlation IDs; log actor, action, entity type/id, and outcome, never tokens, passwords, document content, government IDs, bank details, or sensitive monetary values.
- Production document pipeline: allow-listed MIME types, size limits, malware scanning, quarantine, content-disposition controls, and signed short-lived download URLs.
- Rate limits on authentication and mutation endpoints, anti-forgery protections where cookie auth is used, and security headers.
- Generic client errors; detailed diagnostics only in protected server telemetry.

## Demo versus production
Development identity and local database settings are non-production only. The current development API uses the explicit `X-Demo-User`, `X-Demo-Tenant`, and `X-Demo-Roles` headers to exercise the authorization boundary; these headers are not an authentication mechanism and must be disabled outside Development. Production configuration must use OIDC/OAuth2 bearer validation from environment/secret management, fail closed when required settings are absent, and disable seeded demo identities/data.

All API routes except liveness require an authenticated principal. Development routes filter applications by the tenant claim and reject cross-tenant application, workspace, collateral, and document access. The API still accepts identity fields in development request DTOs for compatibility, but production commands must derive actor, owner, verifier, and approver identities from authenticated claims.

Mutation endpoints also require role claims: relationship managers create and submit intake, credit analysts submit analysis and recommendations, credit verifiers perform second-level verification, credit approvers record decisions, collateral specialists update collateral, and document verification analysts manage document verification. The Development UI derives its demo identity header from the operation actor only to exercise these boundaries locally; this handler is disabled outside Development.
