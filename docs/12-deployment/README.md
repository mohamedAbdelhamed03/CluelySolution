# 12 — Deployment

| | |
|---|---|
| **Phase** | Deployment |
| **Status** | RC1 deployment baseline documented. |
| **Why this phase exists** | To document how the system is delivered to environments — deployment guides, environment/config, and release process. |
| **Owner** | DevOps Lead. |

## Purpose
Deliver the tested system to running environments reliably and repeatably.

## Scope
Deployment and release documentation; not application design, not operations runbooks (phase 13).

## Documents

- [Deployment Guide](01-deployment-guide.md)
- [Environments and Configuration](02-environments-and-configuration.md)
- [Migration Guide](03-migration-guide.md)
- [OpenAPI Usage](04-openapi-usage.md)

## Dependencies
- **Input:** [11 Testing](../11-testing/README.md) sign-off; architecture/design for topology.
- **Output:** deployable, released system consumed by Operations.

## Entry / Exit Criteria
- **Entry:** tested build. **Exit:** repeatable deployment/release documented and validated.

## Related Phases & Next
- Follows **11**; precedes **13 Operations**.
→ [13 — Operations](../13-operations/README.md)
