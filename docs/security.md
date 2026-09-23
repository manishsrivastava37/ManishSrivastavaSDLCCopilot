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
Development identity and local database settings are non-production only. Production configuration must come from environment/secret management, must fail closed when required settings are absent, and must disable seeded demo identities/data.
