# Cluely — Implementation Decision Log

## Purpose

Not every engineering decision deserves an ADR.

This document records implementation-level decisions that help future maintainers understand why the code looks the way it does.

---

## Template

### Date

### Component

### Decision

### Reason

### Alternatives Considered

### Trade-offs

### References

### Future Reassessment

---

## Decisions

### 2026-07-11: Vertical Slice Architecture in Application Layer
- **Component**: Cluely.Application
- **Decision**: Organize Application layer into vertical slices (Rooms, Gameplay) with each slice containing Command, Handler, Result, and Validator.
- **Reason**: Aligns with user preference for vertical slices; keeps related code together; avoids cross-slice coupling.
- **Alternatives Considered**: N-Layer (Controllers → Services → Repositories), Feature Folders.
- **Trade-offs**: None significant for current scope.
- **References**: User preferences, project memory.

### 2026-07-11: No AutoMapper (Manual Mapping)
- **Component**: Cluely.Application
- **Decision**: Use manual mapping instead of AutoMapper.
- **Reason**: Simplicity, explicitness, avoids magic; follows Simplicity Principles.
- **Alternatives Considered**: AutoMapper, Mapster.
- **Trade-offs**: Slightly more boilerplate, but more maintainable and explicit.
- **References**: [Simplicity Principles](simplicity-principles.md).

### 2026-07-11: Result Pattern with Specific Error Types
- **Component**: Cluely.Application
- **Decision**: Implement a Result pattern with specific error types instead of throwing exceptions for expected errors.
- **Reason**: Explicit error handling, avoids exception overhead for control flow, follows Simplicity Principles.
- **Alternatives Considered**: Throwing exceptions, Either monad.
- **Trade-offs**: More boilerplate, but clearer error flow.
- **References**: [Simplicity Principles](simplicity-principles.md).

### 2026-07-11: AggregateRoot Base Class
- **Component**: Cluely.Domain
- **Decision**: Extract a base AggregateRoot class to unify domain event management and versioning.
- **Reason**: Reduces duplication, ensures consistent event handling across aggregates.
- **Alternatives Considered**: Duplicating event/version logic in each aggregate.
- **Trade-offs**: Adds a base class, but justified by multiple consumers (Room aggregate, future aggregates).
- **References**: [Simplicity Principles](simplicity-principles.md).

### 2026-07-11: No MediatR
- **Component**: Cluely.Application
- **Decision**: Do not use MediatR; handlers are directly registered and invoked.
- **Reason**: Simplicity, avoids unnecessary framework, follows Simplicity Principles; architecture tests enforce this.
- **Alternatives Considered**: MediatR.
- **Trade-offs**: None for current scope.
- **References**: [Simplicity Principles](simplicity-principles.md), Architecture Tests.

### 2026-07-11: Opaque Snapshot Custody Model
- **Component**: Cluely.Infrastructure (SqlRoomCustody)
- **Decision**: Persist the Room aggregate as an opaque JSON snapshot payload plus an append-only sequenced event tail. Do not decompose Participant/Board/Card/Turn into relational tables.
- **Reason**: Aligns with [09.02 Persistence & Data Model Design](../09-technical-design/02-persistence-and-data-model-design.md) and ADR-005; SQL holds state but never becomes the authority.
- **Alternatives Considered**: EF owned-entity decomposition (rejected — violates approved persistence model).
- **Trade-offs**: Snapshot payload is opaque to SQL (no relational querying inside the aggregate); acceptable because custody is recovery-only.
- **References**: ADR-005, 09.02 §4–§8.
- **Future Reassessment**: If snapshot cadence becomes periodic instead of per-commit, add tail replay in Infrastructure.

### 2026-07-11: Per-Commit Snapshot with Tail Validation
- **Component**: Cluely.Infrastructure (SqlRoomCustody)
- **Decision**: Write the snapshot at every committed version during this phase. Recovery deserializes the snapshot and rejects custody where tail events exist beyond the snapshot version.
- **Reason**: Domain is frozen (no event-replay apply methods); per-commit snapshots guarantee correct recovery today while preserving the tail for audit and future replay.
- **Alternatives Considered**: Full event-sourced replay in Infrastructure (deferred — requires domain replay seam).
- **Trade-offs**: Slightly larger write volume; simpler and correct for current scope.
- **References**: ADR-005, Implementation Phase 04.01 scope.

### 2026-07-11: RoomCustodyException for Infrastructure Failures
- **Component**: Cluely.Infrastructure
- **Decision**: Wrap persistence failures in `RoomCustodyException` instead of leaking SQL/EF exceptions to Application.
- **Reason**: Clear infrastructure boundary; meaningful failure surface without exposing storage internals.
- **Alternatives Considered**: Raw exception propagation.
- **Trade-offs**: Application must catch/handle custody failures explicitly when needed.
- **References**: Global Engineering Implementation Standard (Infrastructure section).

### 2026-07-11: SignalR Hub in Infrastructure, Mapped from Api Host
- **Component**: Cluely.Infrastructure / Cluely.Api
- **Decision**: Place `GameHub` in Infrastructure; Api host maps `/hubs/game` via `MapHub<GameHub>()`.
- **Reason**: Hub requires `IHubContext<GameHub>` in dispatcher; Infrastructure cannot reference Api. Hub remains a thin transport carrier delegating to `IGameConnectionService`.
- **Alternatives Considered**: Hub in Api with string-based hub context (rejected — weaker typing).
- **Trade-offs**: Hub class lives outside Api project despite being a transport endpoint.
- **References**: 09.01 Technical Design Foundation, ADR-004.

### 2026-07-11: Per-Connection Role-Filtered Delivery
- **Component**: Cluely.Infrastructure (SignalRDeliveryDispatcher)
- **Decision**: Broadcast updates by iterating room connections and sending individually filtered projections; do not use a single group payload.
- **Reason**: ADR-006 requires different payloads per role; group broadcast would leak or over-redact.
- **Alternatives Considered**: Separate SignalR groups per role (deferred — more moving parts for MVP).
- **Trade-offs**: O(participants) sends per commit; acceptable for small rooms.
- **References**: ADR-004, ADR-006.

### 2026-07-11: Internal Projection Before Visibility Filter
- **Component**: Cluely.Infrastructure.Delivery
- **Decision**: `ProjectionBuilder` produces a full internal projection (including key); `VisibilityFilter` is the sole component that exposes ownership in transport DTOs.
- **Reason**: Whitelist-by-inclusion per ADR-006; key never enters non-Spymaster DTOs structurally.
- **Alternatives Considered**: Filter during build (rejected — mixes concerns).
- **Trade-offs**: Full projection exists transiently in memory during delivery.
- **References**: ADR-006, INV-B9.

### 2026-07-11: IRoomDomainEvent Compile-Time Contract
- **Component**: Cluely.Domain / Cluely.Infrastructure
- **Decision**: Introduce `IRoomDomainEvent` with `RoomId` property; all room events implement it. Replace reflection-based RoomId extraction in publisher and event serialization.
- **Reason**: Compile-time safety for event infrastructure; eliminates `GetProperty("RoomId")` at runtime.
- **Alternatives Considered**: Keep reflection convention (rejected — fragile, untestable).
- **Trade-offs**: All future room events must implement the interface.
- **References**: Phase 04.02.1 Hardening Review.

### 2026-07-11: Compile-Time Room Event Serializer
- **Component**: Cluely.Infrastructure (RoomEventSerializer)
- **Decision**: Serialize domain events via exhaustive pattern match instead of `GetType().Name` + runtime generic serialization.
- **Reason**: Ensures every event type is explicitly handled; architecture test verifies parity with source-gen context.
- **Alternatives Considered**: Keep runtime serialization (rejected — silent omissions possible).
- **Trade-offs**: Adding a new event requires updating serializer switch and JsonSerializable attributes.
- **References**: Phase 04.02.1 Hardening Review.

### 2026-07-11: Single Projection Build Per Broadcast
- **Component**: Cluely.Infrastructure (SignalRDeliveryDispatcher)
- **Decision**: Build internal projection once per broadcast; filter per connection from shared projection.
- **Reason**: Reduces redundant mapping/allocation during multi-connection delivery.
- **Alternatives Considered**: Per-connection full rebuild (previous behavior).
- **Trade-offs**: Internal projection held briefly in memory during broadcast loop.
- **References**: Phase 04.02.1 Performance Review.

### 2026-07-11: Thin REST Controllers with Centralized ProblemDetails
- **Component**: Cluely.Api
- **Decision**: Use capability-based controllers (Rooms, Gameplay, Projections, Health); map request DTOs to Application commands/queries manually; convert `Result` failures via `ApiResultMapper` only.
- **Reason**: ADR-010 transport-as-carrier; keeps API free of business logic and duplicate error shaping.
- **Alternatives Considered**: Minimal APIs (rejected — project already wired for controllers); exposing Application commands directly (rejected).
- **Trade-offs**: Manual mapping boilerplate; predictable and explicit.
- **References**: Phase 05.01, ADR-010.

### 2026-07-11: Query Handlers via IRoomReadModelProvider Port
- **Component**: Cluely.Application / Cluely.Infrastructure
- **Decision**: Add read-only query handlers in Application backed by `IRoomReadModelProvider` implemented in Infrastructure using existing projection + visibility pipeline.
- **Reason**: ADR-010 query separation; API must not call Infrastructure projection types directly.
- **Alternatives Considered**: Query logic in API (rejected); duplicating projection in Application (rejected).
- **Trade-offs**: Application read models parallel transport DTOs; mapping at API boundary.
- **References**: ADR-006, ADR-010, 09.03 §5.

### 2026-07-11: Identity Bounded Context with Participant Binding Seam
- **Component**: Cluely.Infrastructure.Identity / Cluely.Application.Auth
- **Decision**: Persist users, refresh tokens, and `(userId, roomId) → participantId` bindings in a separate `IdentityDbContext`. Authenticated API and SignalR endpoints resolve participant IDs from bindings; Domain and Room aggregate remain unchanged.
- **Reason**: ADR-009 participant continuity complements authenticated identity without merging User into the Room aggregate; authentication identifies who, Domain decides whether.
- **Alternatives Considered**: Client-supplied participant IDs with JWT (rejected — spoofable); embedding User in Room aggregate (rejected — violates bounded context).
- **Trade-offs**: Extra persistence tables and binding writes on create/join; explicit seam between Identity and gameplay.
- **References**: Phase 05.02, ADR-009, ADR-010.

### 2026-07-11: Refresh Token Rotation with Hashed Storage
- **Component**: Cluely.Infrastructure.Identity.Security
- **Decision**: Store SHA-256 hashes of opaque refresh tokens; rotate on refresh by revoking the previous record and issuing a new token.
- **Reason**: Limits replay window; avoids persisting secrets in plaintext.
- **Alternatives Considered**: Long-lived access tokens only (rejected); plaintext refresh storage (rejected).
- **Trade-offs**: Requires DB write on each refresh; acceptable for MVP session continuity.
- **References**: Phase 05.02 security requirements.

---

## Phase 05.02 — Technical Debt Report (2026-07-11)

| ID | Item | Severity | Notes |
|----|------|----------|-------|
| TD-0502-01 | Symmetric JWT signing key in configuration | Medium | Acceptable for MVP; production should use rotated secrets via vault/KMS. |
| TD-0502-02 | Participant binding not removed on leave | Low | Bindings persist after leave; Domain still enforces membership. Rejoin uses existing binding. |
| TD-0502-03 | `ParticipantContext` throws for missing binding | Low | Maps to 500 today for unbound authenticated users on gameplay routes; consider 403 ProblemDetails. |
| TD-0502-04 | Identity and Room share one SQL connection string | Low | Separate DbContexts maintain bounded contexts; physical DB split deferred. |
| TD-0502-05 | No account lockout / rate limiting on login | Medium | Out of MVP scope; add at API gateway or middleware before production exposure. |
| TD-0502-06 | Email verification absent | Low | Explicitly out of scope; usernames are trusted on registration. |

No architectural debt introduced. Gameplay invariants and Room aggregate unchanged.

---

## Phase 05.03 — Technical Debt Resolution (2026-07-11)

| ID | Item | Decision | Notes |
|----|------|----------|-------|
| TD-0502-03 | Missing binding → 500 | **Fixed** | Returns 403 with `ParticipantBindingNotFound` code |
| TD-0502-02 | Binding persists after leave | **Accept for MVP** | Supports ADR-009 continuity; Domain enforces rules |
| TD-0502-01 | JWT signing key in config | **Accept for MVP** | Use secret manager in production |
| TD-0502-04 | Shared SQL connection string | **Accept for MVP** | Logical separation via DbContexts |
| TD-0502-05 | No login rate limiting | **Future Release** | Documented for API gateway layer |
| TD-0502-06 | No email verification | **Accept for MVP** | Out of scope |

---

## Rules

Do not record architectural decisions here.

Architectural decisions belong in ADRs.

This log exists only for implementation choices.
