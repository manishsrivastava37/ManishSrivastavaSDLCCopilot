# Implementation Plan

## Slice 1: foundation and origination
Create solution structure, domain model for applications, SQL persistence, health endpoint, API error handling, development identity boundary, application create/list/detail, lifecycle transitions, audit events, and tests. Deliver a usable React shell if Node is available.

## Slice 2: credit approval
Add recommendation factors and rationale, approval work item, human decision endpoint, role/tenant authorization matrix, optimistic concurrency, and integration tests.

## Slice 3: collateral
Add collateral records, valuations, lien status, review tasks, APIs, UI workflow, and tests for monetary precision and stale updates.

## Slice 4: documents
Add metadata/versioning, verification workflow, object-storage abstraction, upload security contract, reviewer findings, and tests. Integrate malware scanning before production upload release.

## Slice 5: operations and hardening
Add dashboards, configurable task queues, metrics/tracing, rate limits, production OIDC, migrations, CI/CD, dependency scanning, browser tests, backup/restore runbook, and deployment manifests.

## Delivery rule
Each slice is independently compilable and tested before the next slice begins. This repository starts with Slice 1 and leaves no core workflow as a mock success path.
