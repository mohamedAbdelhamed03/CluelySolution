# 23. Data Lifecycle & Retention Policy — Cluely

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review |
| **Purpose** | Describe, in **business** terms, the lifecycle of every business object: when it is created, how long it lives, when it expires/archives/deletes, who owns it, and why. This is a **policy**, not a storage design — no databases, schemas, or implementation. |
| **Technology** | Neutral. |

## Table of Contents
1. [Purpose & Principles](#1-purpose--principles)
2. [References](#2-references)
3. [Lifecycle Stages Defined](#3-lifecycle-stages-defined)
4. [Object Lifecycles](#4-object-lifecycles)
5. [Retention Summary Table](#5-retention-summary-table)
6. [Privacy & Future Authentication Impact](#6-privacy--future-authentication-impact)
7. [Revision History](#7-revision-history)

---

## 1. Purpose & Principles

Cluely is a **no-account, transient** product. Its guiding data principles:

- **P1 — Minimalism:** collect and keep only what the business needs; no personal data (NFR-10, INV-P2).
- **P2 — Room-bounded life:** most objects live only as long as their Room ([15](../02-business-analysis/14-lobby-room-lifecycle.md)).
- **P3 — Immutability where fairness requires it:** Board, Key, Dictionary Version, and Game Result are immutable within their scope (INV-B5, INV-D3, INV-O4).
- **P4 — Reproducibility:** enough is retained to explain a match's outcome without PII (QM-18).
- **P5 — Clean reclamation:** expired Rooms release their resources and Room Code (BR-RX-4).

## 2. References
- [07 — Domain Model](../02-business-analysis/06-domain-model.md), [11 — Invariants](../02-business-analysis/10-business-invariants.md)
- [12 — Domain Events](../02-business-analysis/11-domain-events-catalog.md), [14 — Dictionary Management](../02-business-analysis/13-dictionary-management.md)
- [16 — Player Session](../02-business-analysis/15-player-session-reconnection.md), [22 — Quality Metrics](04-quality-metrics.md)

## 3. Lifecycle Stages Defined

| Stage | Meaning |
|-------|---------|
| **Creation** | The business moment the object comes into existence. |
| **Active Lifetime** | The period it is live and mutable/usable per its rules. |
| **Expiration** | The condition/moment it ceases to be active. |
| **Archiving** | Whether a durable, non-active record is kept for reproducibility/audit (business policy; no storage detail). |
| **Deletion** | When it is discarded entirely. |
| **Business Owner** | The role/component responsible for it. |

## 4. Object Lifecycles

### Room
- **Creation:** When a Host creates it (EVT-1). **Active:** From Lobby through InMatch/PostMatch. **Expiration:** Idle timeout, empty, or abandonment (BR-RX-*). **Archiving:** Not archived; only its Game Results may be retained (see Game Result). **Deletion:** On expiry, transient state discarded; Room Code released (BR-RX-4, INV-R2). **Owner:** Room Service.

### Match (Game)
- **Creation:** At valid Start (EVT-11). **Active:** InProgress until terminal. **Expiration:** Win/loss/abandonment (BR-GE-1/5). **Archiving:** The **Game Result** may be archived (below); live match state is not. **Deletion:** Live state discarded with the Room. **Owner:** Game Engine.

### Board (and Key)
- **Creation:** At Board generation (EVT-12). **Active:** For the whole Match; immutable (INV-B5/B8). **Expiration:** Match end. **Archiving:** Final Board snapshot may be part of the Game Result. **Deletion:** With the Match/Room. **Owner:** Game Engine.

### Word Card / Card Ownership
- **Creation:** With the Board. **Active:** Reveal state changes one-way (INV-B7); ownership immutable. **Expiration:** Match end. **Archiving:** Reflected in the Game Result's final board snapshot. **Deletion:** With the Board. **Owner:** Game Engine.

### Clue
- **Creation:** On valid clue submission (EVT-16). **Active:** For its Turn only (one active Clue — INV-G3). **Expiration:** Turn end. **Archiving:** May be retained as part of match history/audit (business option; no PII). **Deletion:** With the Match/Room. **Owner:** Game Engine.

### Guess
- **Creation:** On accepted guess (EVT-17/18). **Active:** Instantaneous (produces a Reveal). **Expiration:** Immediately resolved. **Archiving:** May be retained for auditability (QM-18). **Deletion:** With the Match/Room. **Owner:** Game Engine.

### Turn / Round
- **Creation:** Turn on phase start (EVT-14); Round on pair start (EVT-15). **Active:** Until ended (§8.3/8.5). **Expiration:** Turn/Round end. **Archiving:** Sequence may be retained for audit. **Deletion:** With the Match/Room. **Owner:** Game Engine.

### Game Result
- **Creation:** At Match end (EVT-21). **Active:** Immutable once recorded (INV-O4). **Expiration:** N/A (it is a record). **Archiving:** **May be retained** beyond the Room to support reproducibility/audit **without PII** (P4); retention duration is a Product-Owner policy decision. **Deletion:** Per the chosen retention policy; contains no personal data. **Owner:** Product Owner (policy) / Game Engine (production). **Note:** Nicknames are not personal identifiers but are transient; a retained Result should reference outcome (winning colour, reason) rather than persist Player identities.

### Player Session / Temporary Identity
- **Creation:** On join (EVT-2, token issued). **Active:** While connected or within grace. **Expiration:** Leave, grace expiry, Host removal, or Room expiry (PS-5). **Archiving:** Never archived (transient, PII-free — INV-P2). **Deletion:** Immediately on session end; token invalidated (PS-27). **Owner:** Connection Manager / Identity Abstraction.

### Reconnect Token
- **Creation:** With the Player Session. **Active:** Room-scoped, valid within grace (PS-2/3). **Expiration:** Session end or grace expiry. **Archiving:** Never. **Deletion:** With the session; invalidated on removal. **Owner:** Identity Abstraction.

### Dictionary Version
- **Creation:** On publication (content team). **Active:** From Active through Deprecated (still used by matches that started on it) (DM-L1..L4). **Expiration:** Retirement. **Archiving:** **Retained immutably** for reproducibility (DM-V1, INV-D3) even after retirement, per content policy. **Deletion:** Only by explicit content-governance decision, never while a bound Match could reference it. **Owner:** Content/Localization team.

### Domain Events
- **Creation:** When their business fact occurs ([12](../02-business-analysis/11-domain-events-catalog.md)). **Active:** Transient signals. **Expiration:** After delivery. **Archiving:** **May be retained** (PII-free) to support observability/auditability (QM-12/18); retention window is a Product-Owner policy. **Deletion:** Per that policy. **Owner:** Publishing component / Product Owner (retention policy).

### Business Errors
- **Creation:** On a rejected intent ([13](../02-business-analysis/12-business-error-catalog.md)). **Active:** Returned to the caller. **Expiration:** After the response. **Archiving:** Aggregate counts may be retained for quality monitoring (no PII). **Deletion:** Per monitoring policy. **Owner:** Product Owner / QA.

## 5. Retention Summary Table

| Object | Lives as long as | Archived (durable)? | Contains PII? | Owner |
|--------|------------------|---------------------|---------------|-------|
| Room | Its lifecycle (until expiry) | No | No | Room Service |
| Match | Start → terminal | No (only its Result) | No | Game Engine |
| Board / Key / Cards | The Match | Only in Result snapshot | No | Game Engine |
| Clue / Guess / Turn / Round | The Match | Optional (audit) | No | Game Engine |
| **Game Result** | Recorded permanently (policy) | **Yes (optional, PII-free)** | No | Product Owner |
| Player Session / Identity | Session (join → end) | **No** | No | Connection Manager |
| Reconnect Token | Session + grace | No | No | Identity Abstraction |
| **Dictionary Version** | Until retired | **Yes (immutable)** | No | Content team |
| Domain Events | Transient | Optional (observability) | No | Publisher / PO |
| Business Errors | Per response | Aggregate only | No | PO / QA |

## 6. Privacy & Future Authentication Impact

- **DL-1** No object stores personal data today; identities are transient nicknames only (INV-P2, NFR-10).
- **DL-2** Retained Game Results and Events are **PII-free**; they describe outcomes and lifecycle, not people.
- **DL-3 Future authentication impact:** when accounts are introduced ([Roadmap Phase 2](06-product-roadmap.md), AUTH-1..5):
  - Durable **Accounts** become a **new** object with its own lifecycle and (then) applicable privacy/retention obligations — governed separately at that time.
  - Existing transient objects' lifecycles remain unchanged; only the **identity** an object references may become durable.
  - Retained Results/Events could optionally be associated with Accounts **only** under a future, explicit privacy policy; nothing in this MVP policy presumes or requires it.
- **DL-4** Any future retention of person-linked data must be introduced with an updated policy; this document governs only the current no-account model.

## 7. Revision History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-01 | Initial business data-lifecycle & retention policy for the no-account MVP. |
