# ADR-006 — Role-Based Information Visibility & Projection Architecture

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *How can one authoritative state safely produce different views for different participants while guaranteeing that hidden information is never exposed?* |
| **Complies with** | [ADR-000 Vocabulary](ADR-000-architecture-vocabulary.md), [ADR-001 Style](ADR-001-overall-architecture-style.md), [ADR-002 Authoritative Game State](ADR-002-authoritative-game-state.md), [ADR-003 Coordination](ADR-003-per-room-coordination-model.md). Does not redefine business rules, authority, or the projections established by ADR-002. |
| **Protects** | **Hidden-Information Integrity** — the single most valuable architectural property of Cluely. |
| **Scope note** | Defines the **visibility/projection architecture** only. Not networking, transport, SignalR, APIs, serialization, authentication, authorization mechanics, persistence, or recovery implementation ([§18 Non-Goals](#18-non-goals)). |

---

## 1. Executive Summary

Cluely produces every participant-facing view as a **projection**: a **read-only, deterministically
computed, role-scoped view derived from committed authoritative state** ([ADR-002](ADR-002-authoritative-game-state.md)).
Projections are generated **server-side**, at the **delivery boundary**, **after commit**
([ADR-003](ADR-003-per-room-coordination-model.md)), by a dedicated, pure **Projection function**.

The decision is a **Per-(Role, Team) Projection built by inclusion (whitelist / least-information)**:
each projection is assembled by **adding only the fields that role-and-team is permitted to see** —
never by copying the whole aggregate and redacting. The **key** (unrevealed card ownership) exists
only inside the authoritative aggregate and inside the **Spymaster** projection; it is **structurally
absent** from every Operative/lobby/spectator projection, so there is nothing to leak, mask, or
accidentally forget to remove.

Projections are **never authoritative**, **never written back**, **idempotent** (identical committed
state ⇒ identical projection), and **disposable** (regenerated from committed state on change,
reconnect, or recovery). Clients are never trusted; no filtering happens on the client. This design
does not merely *describe* leak prevention — it makes leaks **impossible by construction** (whitelist
inclusion + server authority + commit-before-visibility), which [§Adversarial Review](#22-adversarial-security-review--attempt-to-break-the-design) then stress-tests.

> One-line statement: **build each role's view by including only what that role may see, from
> committed state, on the server, after commit — the key never enters a non-Spymaster projection.**

---

## 2. Problem Statement

### Why hidden information is architecturally difficult
Cluely's single authoritative state contains a **secret** (the key) that must be visible to some
participants (Spymasters) and invisible to others (Operatives) **in the same room, at the same time**,
while everyone shares the same board words and sees the same public reveals. The system must serve
*different truths-about-visibility* from *one truth-about-state* — continuously, under concurrency,
reconnection, and recovery.

### Why normal CRUD systems don't have this problem
Typical CRUD systems have per-record access control where a user either may or may not read a record;
they rarely must serve **the same live object** with **field-level, role-dependent secrecy** where a
leak of one field to one role **destroys the product**. Cluely's secrecy is intra-object, intra-room,
real-time, and adversarial (a motivated Operative benefits from cheating).

### Failures if visibility is incorrect
- **Gameplay:** an Operative who learns the key wins trivially → the game is pointless.
- **Business:** loss of trust; the core promise (a fair Codenames) is broken; reputational damage.
- **Security:** information disclosure of the single protected secret; potential to industrialize
  cheating.

This is the existential risk [ENG-FP-01](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)
and quality scenario [QS-01](../09-quality-attribute-scenarios.md); invariant
[INV-B9](../../02-business-analysis/10-business-invariants.md) makes non-disclosure non-negotiable.

---

## 3. Visibility Philosophy

| Principle | Meaning | Why |
|-----------|---------|-----|
| **Need-to-Know** | A projection contains only what its audience is entitled to see. | Minimizes leak surface. |
| **Least Information (whitelist by inclusion)** | Build views by **adding permitted fields**, never by copying-all-then-redacting. | A forgotten redaction leaks; a forgotten inclusion merely omits (safe by default). |
| **Projection over Duplication** | Views are **derived** from the one aggregate, not separately stored truths. | Prevents drift and second sources of truth ([ADR-002](ADR-002-authoritative-game-state.md)). |
| **Role Isolation** | Each role's projection is independent; one role's view never contains another role's secrets. | Contains blast radius. |
| **Projection Immutability** | Once produced for a commit, a projection is read-only and disposable. | No in-place edits that could mix data. |
| **Derived Views** | Projections are computed, not authoritative. | [AI-STATE-8](ADR-002-authoritative-game-state.md#14-architectural-invariants-introducedaffirmed). |
| **No Client Trust** | No visibility decision is delegated to clients; no client-side filtering. | Clients are modifiable ([AP-03](../../06-architecture-governance/01-architecture-principles.md), [AAP-02](../../06-architecture-governance/02-architecture-anti-principles.md)). |
| **Server Authority** | Projections are produced only by the server (Authority/delivery boundary). | Single trusted producer. |
| **Commit Before Visibility** | Nothing is projected/delivered before it is committed. | No partial/uncommitted leaks ([ADR-003](ADR-003-per-room-coordination-model.md)). |
| **Projection Determinism** | Identical committed state + role ⇒ identical projection. | Testable, reproducible, no timing/shape variance. |

---

## 4. Candidate Visibility Models

Evaluated for the intra-object, real-time secrecy problem. None dismissed without reasoning.

### VM1 — Single Projection, filter on client
- **Overview:** Server sends the full aggregate (incl. key); clients hide what the role shouldn't see.
- **Correctness/Leak prevention:** **Fails** — the key reaches every client; hiding is cosmetic
  ([AAP-02/03](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Complexity:** Low server, but catastrophic.
- **Verdict:** **Disqualified.** Included only to be explicitly rejected.

### VM2 — Per-Role Projection (RECOMMENDED base)
- **Overview:** The server produces one projection **per role** (Spymaster projection includes the key;
  Operative projection excludes it), parameterized by **team** where team-relative info matters.
- **Advantages:** The key is present only where allowed; other roles' projections are built without it
  (whitelist); simple, deterministic, testable; few distinct projection shapes.
- **Disadvantages:** Must correctly parameterize by team (both Spymasters see the full key — a business
  fact, [BR-BG-6](../../02-business-analysis/02-business-rules.md) — so team doesn't change the key
  view, but does change "which team am I / whose turn relative to me").
- **Correctness/Leak prevention:** High — structural absence of the key in non-Spymaster projections.
- **Complexity/Maintainability/Testing:** Low/High/High.
- **Performance/Scalability:** Excellent — a small number of projection shapes per room; compute per
  change, fan out.
- **Recovery/Evolution:** Excellent — regenerate from committed state; new roles add a new projection.
- **Verdict:** **Selected** (refined by VM3/VM6 below).

### VM3 — Per-Player Projection
- **Overview:** A distinct projection per individual player.
- **Advantages:** Maximal personalization (e.g., "you are the host", "it is your turn").
- **Disadvantages:** More projections to produce; the **security-relevant** axis is (role, team), not
  identity — per-player adds cost without adding secrecy. Personalization is a thin, non-secret overlay.
- **Verdict:** **Partially adopted** — use Per-(Role, Team) for the **secret-bearing** content, plus a
  thin, **non-secret personalization overlay** (identity-relative labels) computed from already-visible
  data. Full per-player projections are unnecessary for secrecy.

### VM4 — Dynamic Filtering (compute per request/recipient)
- **Overview:** Filter the aggregate on demand for each recipient.
- **Advantages:** Always current; no stored views.
- **Disadvantages:** If implemented as copy-then-redact, it is leak-prone (VM1's risk per request);
  repeated per-recipient computation can be wasteful.
- **Verdict:** Acceptable **only** if it is whitelist-by-inclusion and role-keyed (then it is VM2
  computed lazily). The *filtering-by-redaction* variant is rejected.

### VM5 — Materialized Views (precompute & store per role)
- **Overview:** Maintain stored per-role views updated on each commit.
- **Advantages:** Fast reads.
- **Disadvantages:** A stored view risks being treated as a **second source of truth** ([ADR-002 AI-STATE-8](ADR-002-authoritative-game-state.md#14-architectural-invariants-introducedaffirmed));
  staleness/consistency burden; more leak surface at rest (a stored Spymaster view could be mis-served).
- **Verdict:** Rejected as the model; a short-lived cache of a projection is permissible only as a
  **non-authoritative, role-keyed, disposable** optimization (VM6), never as a maintained truth.

### VM6 — Rule-Based Projection Pipeline (RECOMMENDED mechanism)
- **Overview:** A **declarative visibility ruleset** (which fields each role may include) drives a
  **pure Projection function**: `project(committedState, role, team) → view`, assembled by inclusion.
- **Advantages:** Visibility rules are **explicit, centralized, reviewable, and testable**; the pipeline
  is deterministic and idempotent; adding a role = adding a rule; the whitelist is auditable in one
  place (supports the fitness functions §13 and the adversarial review §22).
- **Disadvantages:** Requires disciplined rule maintenance (mitigated by tests/fitness functions).
- **Verdict:** **Selected as the mechanism** realizing VM2/VM3-overlay.

### VM7 — Hybrid
- **Overview:** VM2 (per-role, team-parameterized) **via** VM6 (rule-based inclusion pipeline), **plus**
  a thin non-secret personalization overlay (from VM3), **optionally** with a short-lived,
  non-authoritative, role-keyed cache of the produced projection.
- **Verdict:** **This is the final decision** — it captures the correctness of VM2, the auditability of
  VM6, the UX of VM3, and permits a safe cache without any second source of truth.

### Evaluation summary

| Criterion | VM1 | **VM2/VM6/VM7 (chosen)** | VM3 | VM4(redact) | VM5 |
|-----------|:---:|:------------------------:|:---:|:-----------:|:---:|
| Leak prevention | ✗ | **Highest (whitelist)** | High | Low | Medium |
| Correctness | 1 | **5** | 4 | 2 | 3 |
| Complexity (5=lowest) | 5 | **4** | 2 | 3 | 2 |
| Maintainability | 1 | **5** | 3 | 2 | 2 |
| Performance | 5 | **4** | 3 | 3 | 5 |
| Scalability | 4 | **4** | 3 | 3 | 4 |
| Testing | 2 | **5** | 3 | 2 | 3 |
| Recovery | 2 | **5** | 4 | 4 | 3 |
| Future evolution | 1 | **5** | 4 | 3 | 3 |

---

## 5. Final Decision

**Adopt VM7:** per-**(Role, Team)** projections produced **server-side at the delivery boundary, after
commit**, by a **pure, rule-based Projection function that assembles each view by inclusion
(whitelist / least-information)** from committed authoritative state — with a thin **non-secret
personalization overlay** and an **optional short-lived, non-authoritative, role-keyed cache**.

**Why:** it makes the key **structurally absent** from every non-Spymaster view (nothing to leak), is
deterministic/idempotent (testable and reproducible), never creates a second source of truth, and adds
new roles by adding a rule. **Rejections:** VM1 (client filtering) leaks by design; VM4-by-redaction is
leak-prone; VM5 risks a maintained second truth; VM3-full is unnecessary cost for secrecy. It aligns
with ADR-002 (projections derived, never authoritative), ADR-003 (commit-before-visibility), and the
governance no-client-trust / server-authority principles.

---

## 6. Projection Architecture

| Question | Answer |
|----------|--------|
| **What is a projection?** | A read-only, role-scoped view **derived** from committed authoritative state (ADR-000 *Projection*); never authoritative. |
| **Who creates it?** | The server, at the **delivery boundary**, via the pure Projection function `project(committedState, role, team)`. |
| **Who owns it?** | It is **owned by no one as truth**; it is a transient artifact of delivery. The **authoritative state** it derives from is owned by the room Authority. |
| **Who consumes it?** | Participants (their clients). Observability consumes **events**, not participant projections, and never the key. |
| **Who may cache it?** | Optionally the delivery layer or client, as a **non-authoritative, role-keyed, disposable** copy; a Spymaster projection is **never** cached under an Operative key (§10, §14). |
| **Who may never modify it?** | Everyone — projections are immutable and read-only; **no write-back to authoritative state** ([AI-STATE-3](ADR-002-authoritative-game-state.md#14-architectural-invariants-introducedaffirmed)). |
| **When is it created?** | After a **commit** that affects a room, and on **(re)connection** / **recovery** (a snapshot projection). |
| **When is it destroyed?** | When superseded by a newer commit, on disconnect, on role change (invalidate), on room expiry. |
| **When is it regenerated?** | Deterministically from committed state whenever needed (change, reconnect, recovery, cache miss/invalidation). |

---

## 7. Visibility Matrix

Authoritative state elements (from [ADR-002 §6](ADR-002-authoritative-game-state.md#6-state-inventory)) ×
role. Values: **V** visible · **H** hidden (absent from projection) · **M** masked (presence known,
value hidden) · **D** derived-and-visible · **C** conditional · **N** never visible to anyone but the
owning server. Roles: **Host** (a player role, orthogonal to game role), **Spymaster**, **Operative**,
**Disconnected** (within grace), **Spectator (future)**, **Observer/System**.

| State element | Host | Spymaster | Operative | Disconnected (in grace) | Spectator (future) | System |
|---------------|:----:|:---------:|:---------:|:-----------------------:|:------------------:|:------:|
| Room identity / code | V | V | V | V | C (join-gated) | V |
| Room/Game lifecycle status | V | V | V | V | V | V |
| Membership (nicknames, teams, roles) | V | V | V | V | V | V |
| Host designation | V | V | V | V | V | V |
| Board words & layout | V | V | V | V | V | V |
| **Key (unrevealed card ownership)** | **H** (unless also Spymaster) | **V** | **H** | restore to prior role | **H** | N (server-internal) |
| Card **reveal flags** + revealed ownership | V | V | V | V | V | V |
| Starting team / turn order | V | V | V | V | V | V |
| Current turn (phase, active team) | V | V | V | V | V | V |
| Current clue (word + number, once given) | V | V | V | V | V | V |
| Guess allowance / used | V | V | V | V | V | V |
| Remaining-agents per team | D | D | D | D | D | D |
| Win/Loss / result (after end) | V | V | V | V | V | V |
| Presence (others' connected status) | V | V | V | V | C | V |
| **Own** session/identity token | own only | own only | own only | own only | own only | N |
| **Others'** session/reconnect tokens | **N** | **N** | **N** | **N** | **N** | N |
| Pause overlay (play paused, awaited role) | V | V | V | V | V | V |
| Authoritative deadlines (grace/idle) | M (that a timer exists) | M | M | M | M | V (server) |
| Pending administrative actions | C (host sees own) | H | H | H | H | V |
| Internal/recovery metadata | N | N | N | N | N | V |

Notes:
- **Host is a player role**; a Host who is *not* a Spymaster does **not** see the key (Host ≠ key
  access). A Host who *is* a Spymaster sees it *because* of the Spymaster role.
- **Both** Spymasters see the **full** key (business fact [BR-BG-6](../../02-business-analysis/02-business-rules.md)); team does not restrict the key view — it only personalizes team-relative labels.
- **Remaining-agents counts are public** in Codenames (derived, visible to all) — this is **not** a leak;
  per-card *ownership* of unrevealed cards is the secret.
- **Tokens** are visible only to their owner and never broadcast; recovery/internal metadata is
  server-only.

---

## 8. Hidden Information Analysis

Every element that must remain hidden, and why:

| Hidden element | Why it must remain hidden |
|----------------|---------------------------|
| **Key (unrevealed card ownership)** | The core secret; knowing it lets Operatives win trivially ([INV-B9](../../02-business-analysis/10-business-invariants.md)). Present only in the aggregate + Spymaster projection. |
| **Ownership of not-yet-revealed cards (any encoding)** | Any signal that distinguishes an unrevealed card's ownership (counts-per-card, hints, ordering, shape) is a leak. Projections encode unrevealed cards **identically** regardless of ownership. |
| **"Future" turn/board decisions** | There are none stored — outcomes are computed at commit; there is no precomputed future to leak. |
| **Reconnect tokens (others')** | A stolen token could hijack a seat/role; visible only to the owner ([INV-P2/P4](../../02-business-analysis/10-business-invariants.md)). |
| **Internal metadata / recovery data** | Could reveal state structure or the key indirectly; server-only. |
| **Pending administrative actions (others')** | Could reveal intent/host operations; scoped to the actor/host. |
| **Authoritative deadlines' exact internal values** | The *fact* of a pause/timer is public; exact internal scheduling is server-only (masked). |

The decisive property: **the key never enters a non-Spymaster projection**, and unrevealed cards are
**indistinguishable by ownership** in those projections.

---

## 9. Projection Lifecycle

Technology-neutral; sits **after** the ADR-003 commit.

```
Commit (ADR-003) → Projection Generation (pure project(state, role, team))
   → Role Filtering (inclusion by visibility ruleset) → [optional role-keyed cache]
   → Delivery (read-only) → Expiration (on next commit / disconnect / role change / expiry)
   → Regeneration (deterministically from committed state, on demand)
```

| Stage | What happens |
|-------|--------------|
| **Commit** | Authoritative state becomes truth (only source projections may read). |
| **Projection Generation** | For each affected (role, team), the pure function assembles a view by **including** permitted fields from committed state. |
| **Role Filtering** | Is intrinsic to generation (inclusion), not a separate redaction step — the ruleset *is* the filter. |
| **[Cache]** | Optionally store the produced view under a **(room, commit-version, role, team)** key; disposable; never authoritative. |
| **Delivery** | The view is transported read-only to entitled participants (mechanism = ADR-004; delivery never mutates). |
| **Expiration** | Superseded by the next commit's projections; invalidated on role change/disconnect/expiry. |
| **Regeneration** | On reconnect/recovery/cache-miss, recompute from the latest committed state — identical inputs ⇒ identical output. |

---

## 10. Projection Consistency

- **When regenerated?** On every committed change affecting the room, and on (re)connection/recovery.
- **Can projections become stale?** A *cached* projection can lag, but it is keyed by **commit version**;
  a stale-versioned projection is never delivered as current and **never** promoted to truth
  ([FF-6](#13-architecture-fitness-functions)). Clients apply only newer versions ([ADR-004] handles
  ordering/resync).
- **Consistency guarantees:** every projection corresponds to **some committed state version**; it never
  reflects uncommitted/partial state (commit-before-visibility); it is **deterministic** for that
  version+role.
- **After reconnect:** the returning participant receives a **fresh projection for their restored role**
  at the latest committed version ([CB-08](../06-consistency-boundaries.md)); no residual view from a
  previous role/connection is reused.
- **After recovery:** projections are **recomputed** from the recovered authoritative state
  ([ADR-002 §12](ADR-002-authoritative-game-state.md#12-recovery-model)); **no projection is restored as
  truth**, so a stale/pre-crash projection can never resurface as authoritative.

---

## 11. Failure Analysis

| Failure | Expected behavior / guarantee |
|---------|-------------------------------|
| **Wrong projection (role mismatch)** | Prevented by keying generation on the **authoritative** (role, team) from committed state, never on client claims; a mismatch is a defect caught by fitness functions/tests (§13), not a runtime "redaction miss". |
| **Duplicate projection** | Idempotent — identical committed version+role ⇒ identical view; duplicates are harmless. |
| **Late projection** | Version-tagged; a late/older-version projection is ignored by clients and never treated as current (§10). |
| **Replay** | Re-sending an old projection yields an old version → ignored; it carries only already-committed, already-visible data (no new leak). |
| **Reconnect** | Fresh projection for the **restored** role at latest version; single active connection ([INV-P4](../../02-business-analysis/10-business-invariants.md)); no cross-role reuse. |
| **Host migration** | Host designation changes in state → projections regenerate; **key visibility is unaffected** (Host ≠ key access); no leak. |
| **Recovery** | Projections recomputed from recovered committed state; none restored as truth (§10). |
| **Role change (between matches only)** | Roles are **frozen during a match** ([INV-T5](../../02-business-analysis/10-business-invariants.md)); on a new match, prior projections/caches are **invalidated** so an ex-Spymaster (now Operative) cannot reuse a key-bearing view (§14 AI-VIS-9). |
| **Dictionary update** | Board words are immutable per match ([INV-D3](../../02-business-analysis/10-business-invariants.md)); projections for the running match are unaffected; new matches project new words. |
| **Future spectator mode** | Constrained now: a spectator projection **must** be an Operative-equivalent (no key), produced by the same pipeline (§14 AI-VIS-8). |
| **Network interruption** | Client resyncs to the latest committed-version projection; partial/dropped delivery never yields uncommitted or cross-role data. |

---

## 12. Architectural Invariants

Extend ADR-002/003 invariants with visibility invariants **AI-VIS-***:

- **AI-VIS-1:** **No projection is authoritative.**
- **AI-VIS-2:** **Every projection derives solely from committed state.**
- **AI-VIS-3:** **No projection writes** to authoritative state (or anything else).
- **AI-VIS-4:** **No projection bypasses the visibility ruleset** — all views are produced by the one
  Projection function.
- **AI-VIS-5:** **Hidden information never leaves the Authority** except into projections **explicitly
  permitted** to contain it (the key → Spymaster projection only).
- **AI-VIS-6:** **Role filtering is deterministic** — identical committed state + (role, team) ⇒
  identical projection.
- **AI-VIS-7:** **Projection generation is idempotent** — regenerating yields the same result; no
  side effects.
- **AI-VIS-8:** **Every participant receives only their permitted information** — built by inclusion, so
  omission is the default.
- **AI-VIS-9:** **A projection is bound to the authoritative (role, team) at its commit version**; a view
  for one role is never delivered to, cached for, or reused by another role.
- **AI-VIS-10:** **Unrevealed cards are indistinguishable by ownership** across a non-Spymaster
  projection (no per-card ownership signal in any form: value, count, order, shape, or size).

---

## 13. Architecture Fitness Functions

Measurable (future architecture tests), derived from §12:

- **FF-1:** **No Operative/lobby/spectator projection contains unrevealed ownership** (the key) — in any
  field, count, ordering, or shape. *(Exhaustive negative test across all non-Spymaster projections.)*
- **FF-2:** **Every projection reproduces exactly from committed state** (recompute ⇒ byte-for-byte-equal
  at the model level).
- **FF-3:** **Projection regeneration is deterministic** and **idempotent** (no variance across runs).
- **FF-4:** **Projection output is identical for identical (state version, role, team).**
- **FF-5:** **No projection mutation** (immutability check) and **no write-back** to authoritative state.
- **FF-6:** **No transport/delivery step modifies a projection**; delivery is read-only.
- **FF-7:** **Every projection is traceable** to a committed state version and the (role, team) it was
  produced for.
- **FF-8:** **Two projections of different roles for the same state are consistent on shared/public
  fields** and differ **only** by permitted (role-specific) content (e.g., the key appears **only** in
  the Spymaster projection).
- **FF-9:** **Structural indistinguishability:** for a fixed set of unrevealed cards, a non-Spymaster
  projection is identical regardless of those cards' true ownership (guards against inference/shape/size
  leaks).

Map to [Success Metrics ASM-05](../../06-architecture-governance/04-architecture-success-metrics.md) and
[QS-01](../09-quality-attribute-scenarios.md).

---

## 14. Security Analysis

| Threat | Analysis & control |
|--------|--------------------|
| **Information disclosure** | Primary threat. Controlled by whitelist inclusion + structural absence of the key from non-Spymaster projections (AI-VIS-5/8/10; FF-1/9). |
| **Replay attacks** | Old projections are version-tagged and carry only already-visible data; replay reveals nothing new (§11). |
| **Projection forgery** | Projections are produced only by the server; clients cannot fabricate an authoritative view; consumers never treat a client-supplied view as truth (AI-VIS-1, no client trust). |
| **Client manipulation** | Visibility is decided server-side from authoritative (role, team); a modified client cannot request a role it doesn't hold; the server ignores client-declared role (AAP-02). |
| **Role escalation** | Role is read from authoritative state, not client input; roles are frozen mid-match; claiming Spymaster is a coordinated Intent subject to one-Spymaster rules (ADR-003, [INV-T3](../../02-business-analysis/10-business-invariants.md)). |
| **Timing leaks** | Because outcomes are **public once revealed**, reveal timing is not a secret. Pre-reveal, projection generation is **data-independent in shape/size** for unrevealed cards (FF-9) and adjudication does not branch observably on ownership → no timing oracle for the key. (Property to preserve, not merely asserted.) |
| **Cache leaks** | Caches are role-keyed and versioned; a Spymaster view is never stored under, or served for, a non-Spymaster key (AI-VIS-9); caches are non-authoritative and disposable. |
| **Logging leaks** | Logs/observability consume **events and metrics**, are PII-free, and **must never contain the key or unrevealed ownership** (constraint carried to ADR-004/ops; FF checks telemetry). |
| **Monitoring leaks** | Same as logging — monitoring signals are derived from public/operational data, never the key. |
| **Memory inspection** | Out of this ADR's software scope (a host-compromise threat); mitigated architecturally by keeping the key **only** in the aggregate + Spymaster projection (minimal footprint) — noted for the security-design phase. |
| **Future authentication** | The additive identity seam does not change visibility rules; roles still come from authoritative state; auth may later strengthen (not weaken) role assurance. |

---

## 15. Trade-offs

- **Performance:** Producing a few projections per commit is cheap; whitelist inclusion touches only
  permitted fields. Slightly more work than "send everything" (VM1) — but VM1 is disqualified. Optional
  caching offsets repeated generation.
- **Memory:** A handful of small projections per room per commit; caches are bounded and disposable.
- **Complexity:** A centralized visibility ruleset adds a little structure but concentrates all secrecy
  logic in one auditable place (a net simplicity win for correctness).
- **Caching:** Permitted but constrained (role-keyed, versioned, non-authoritative); the main risk
  (mis-keyed cache) is guarded by AI-VIS-9/FF-1.
- **Recovery:** Trivial — projections are recomputed, never restored.
- **Testing:** Excellent — determinism + inclusion make exhaustive leak tests (FF-1/9) feasible.
- **Scalability:** Per-room, per-commit projection scales with rooms (isolation); no cross-room concern.
- **Operational impact:** Must ensure telemetry never carries the key (an ops/logging discipline).
- **Developer experience:** Developers add a **visibility rule** for new data, not ad-hoc redactions —
  safer and clearer.
- **Future evolution:** New roles (e.g., spectator) = new rule; the pipeline is stable.

---

## 16. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| A new field added to state but **not** to the ruleset accidentally appears in a projection | Architecture/Security | **Whitelist by inclusion**: new fields are **omitted by default** (safe); tests assert only ruleset fields appear; FF-1/8. |
| Mis-keyed cache serves a Spymaster view to an Operative | Security (critical) | AI-VIS-9; role+version cache keys; FF-1 over cache outputs; prefer no cache if in doubt. |
| Telemetry/logging captures the key | Security (critical) | Ruleset forbids the key outside Spymaster projection; ops guidance; fitness test over emitted signals. |
| Inference via projection shape/size/timing | Security | FF-9 structural indistinguishability; data-independent generation; public-once-revealed model. |
| Client filtering creeps in (regression to VM1) | Architecture | AI-VIS-4/8; review against [AAP-02](../../06-architecture-governance/02-architecture-anti-principles.md); server-only production. |
| Future spectator mode leaks the key | Evolution | AI-VIS-8 constrains spectator to Operative-equivalent projection via the same pipeline. |
| Role change reuse across matches | Security | AI-VIS-9 invalidation on role change; roles frozen mid-match ([INV-T5](../../02-business-analysis/10-business-invariants.md)). |
| Testing can't cover all leak paths | Testing | Centralized pipeline + inclusion makes paths enumerable; property-based + negative tests (FF-1/9). |

---

## 17. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| AS-1 | Visibility is fully determined by **(game role, team)** plus non-secret identity personalization. *(Matches business rules.)* | Very High | If a future feature needed per-player secrets, add a per-player rule branch (pipeline supports it). |
| AS-2 | **Both Spymasters may see the full key** (business fact [BR-BG-6](../../02-business-analysis/02-business-rules.md)). | Fact | — |
| AS-3 | **Remaining-agent counts are public** (Codenames fact). | Very High | If ever secret, move from D to M — but this contradicts the reference game. |
| AS-4 | The Projection function can be **pure and deterministic** over committed state. | Very High | If nondeterminism crept in (e.g., time-based fields), isolate/normalize them; FF-3/4 would catch it. |
| AS-5 | Host-level memory/host compromise is **out of software scope** here (belongs to security-design/ops). | Medium | If in-scope later, add process/enclave-level controls; architecture already minimizes key footprint. |

---

## 18. Non-Goals

This ADR does **not** decide: transport / **SignalR** / protocols (ADR-004); serialization/encoding
(technical design); authentication & authorization mechanics (future ADRs / identity seam);
persistence & recovery implementation (ADR-005); deployment/topology (ADR-007/ops); database/storage.
It defines **only** the visibility/projection architecture.

---

## 19. Impact on Future ADRs

| ADR / Phase | Constraint imposed by ADR-006 |
|-------------|-------------------------------|
| **ADR-004 Real-Time Communication** | Delivers **role-filtered projections** version-tagged; delivery never mutates or re-filters; must not carry the key to non-Spymasters; ordering/resync operate on projection versions. |
| **ADR-005 State Recovery** | Recovery restores **authoritative state**; projections are **recomputed**, never restored; no pre-crash projection may resurface as truth. |
| **ADR-007 Room Isolation / Distribution** | Projections are per-room; no cross-room view; distribution must preserve per-room projection production and AI-VIS-* invariants. |
| **ADR-008 Dictionary Architecture** | Board words appear in projections; the key derives from generation and lives only in aggregate + Spymaster projection; dictionary content never influences visibility rules. |
| **ADR-009 Session & Reconnection** | Reconnect delivers a fresh projection for the **restored** role; role change invalidates prior projections/caches; tokens are owner-only. |
| **ADR-010 Command/Query Strategy** | Queries return **projections** (never authoritative state); any read model is a projection subject to AI-VIS-*. |
| **Software Architecture / Technical Design** | The **delivery boundary** hosts the pure Projection function + visibility ruleset; it is the single, auditable place for secrecy. |
| **Implementation** | No client-side filtering; whitelist inclusion only; no write-back; role from authoritative state. |
| **Testing** | Mandatory leak tests (FF-1/9), determinism/idempotency (FF-3/4), cache-key safety, telemetry-no-key. |
| **Operations** | Telemetry/logging must never contain the key/unrevealed ownership; monitor cache-key correctness. |

---

## 20. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | [BR-BG-6](../../02-business-analysis/02-business-rules.md) (both Spymasters see key), [BR-CO-3/4](../../02-business-analysis/02-business-rules.md) (ownership public only on reveal), [BR-JR-6a](../../02-business-analysis/02-business-rules.md) (waiting members get no board data). |
| **Business Invariants** | [INV-B9](../../02-business-analysis/10-business-invariants.md) (no leak), [INV-B7](../../02-business-analysis/10-business-invariants.md) (reveal one-way), [INV-P2/P4](../../02-business-analysis/10-business-invariants.md) (transient identity, one connection), [INV-T3/T5](../../02-business-analysis/10-business-invariants.md) (one Spymaster, frozen roles). |
| **Rule Precedence** | [16 Rule Precedence](../../02-business-analysis/16-rule-precedence.md) (visibility applies to committed outcomes only). |
| **Quality Attributes** | [QS-01](../09-quality-attribute-scenarios.md) (no leak — non-waivable), [QS-07](../09-quality-attribute-scenarios.md) (leak-free reconnect), [QS-14](../09-quality-attribute-scenarios.md) (testability). |
| **State Ownership** | [05 State Ownership](../05-state-ownership.md) (readers vs owner; key is Spymaster-only). |
| **Consistency Boundaries** | [06 Consistency](../06-consistency-boundaries.md) (CB-08 reconnect snapshot). |
| **ADR-000** | *Projection, Role Filtering, Hidden Information, Reader, Snapshot, Delivery Boundary, Determinism* used as defined. |
| **ADR-001/002/003** | Delivery boundary in the modular monolith; projections derive from the ADR-002 aggregate; produced after ADR-003 commit. |
| **Governance** | [AP-03/05/11/14](../../06-architecture-governance/01-architecture-principles.md); [AAP-02/03/09](../../06-architecture-governance/02-architecture-anti-principles.md); [Success Metrics ASM-05](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 21. Architecture Review

- **Decision:** Per-(Role, Team) projections, produced server-side at the delivery boundary after
  commit, by a pure rule-based Projection function assembling views **by inclusion (whitelist)**, with a
  non-secret personalization overlay and an optional non-authoritative role-keyed cache (VM7).
- **Confidence:** **High** — the whitelist-by-inclusion property makes leaks a *default-impossible*
  rather than a *must-remember-to-prevent* condition, and it is entailed by ADR-002 (projections
  derived) and ADR-003 (commit-before-visibility).
- **Remaining risks:** operational discipline that **telemetry/logging never carries the key**; safe
  **cache keying** (both guarded by fitness functions and carried to ADR-004/ops); host-memory
  compromise is out of scope (security-design phase).
- **Open questions (delegated, non-blocking):** projection delivery ordering/versioning specifics
  (ADR-004); exact cache lifetime (technical design); spectator projection details **if** a future mode
  is approved (constrained here to Operative-equivalent).
- **Review triggers:** introduction of any new role or spectator/observer mode; any per-player secret
  requirement; addition of authentication (should strengthen, not alter, visibility); any proposal to
  cache or persist projections beyond disposable role-keyed copies.
- **Readiness for ADR-004:** **READY.** With *what* is authoritative (ADR-002), *how* it is coordinated
  (ADR-003), and *how* it is safely projected per role (this ADR) all fixed, ADR-004 can define
  real-time delivery of these **version-tagged, role-filtered projections** without touching secrecy.

---

## 22. Adversarial Security Review — "Attempt to Break the Design"

Treated as a hostile review. For each attack, the question is: *can hidden information leak?*

1. **Can an Operative ever infer the Key?**
   - *Attempt:* read the projection; probe field shapes/counts; watch updates.
   - *Result:* **No.** The key is **structurally absent** (whitelist inclusion, AI-VIS-5/10); unrevealed
     cards are **indistinguishable by ownership** (FF-9); remaining-agent counts are public in Codenames
     and reveal nothing per-card. Reveals are public *after* they happen, which is by design.
2. **Can logs expose hidden information?**
   - *Attempt:* read server logs/telemetry.
   - *Result:* **Not by design.** The ruleset forbids the key outside the Spymaster projection; telemetry
     consumes events/metrics that never include unrevealed ownership; a fitness test scans emitted
     signals (FF over telemetry). *Residual:* requires operational discipline (carried to ADR-004/ops).
3. **Can cached projections leak information?**
   - *Attempt:* obtain a cached Spymaster view as an Operative.
   - *Result:* **No.** Caches are keyed by (room, version, **role, team**) and are non-authoritative; a
     Spymaster view is never stored under or served for a non-Spymaster key (AI-VIS-9, FF-1 over caches).
4. **Can reconnect reveal unauthorized state?**
   - *Attempt:* reconnect and receive a snapshot for a higher-privileged role.
   - *Result:* **No.** The snapshot is a **fresh projection for the authoritative restored role** at the
     latest version; role comes from authoritative state, not client claim; single active connection
     ([INV-P4](../../02-business-analysis/10-business-invariants.md)).
5. **Can a future spectator mode violate isolation?**
   - *Attempt:* add spectators who "watch everything."
   - *Result:* **Constrained now.** AI-VIS-8 mandates any spectator projection be **Operative-equivalent
     (no key)**, produced by the same pipeline. A key-bearing spectator view would violate this ADR and
     require reopening it (and the business).
6. **Can replay expose hidden information?**
   - *Attempt:* replay old projections/events.
   - *Result:* **No.** Replays carry only already-committed, already-visible data at an old version;
     nothing new is revealed; clients ignore older versions.
7. **Can role changes leak information?**
   - *Attempt:* be Spymaster in match 1, Operative in match 2, reuse the old key view.
   - *Result:* **No.** Roles are **frozen during a match** ([INV-T5](../../02-business-analysis/10-business-invariants.md));
     on a new match, prior projections/caches are **invalidated** (AI-VIS-9); a new match has a **new
     board and key** anyway.
8. **Can recovery expose stale projections?**
   - *Attempt:* after a crash, resurface a pre-crash projection as current/authoritative.
   - *Result:* **No.** Recovery restores **authoritative state**; projections are **recomputed** and are
     never authoritative (AI-VIS-1/2; [ADR-002 §12](ADR-002-authoritative-game-state.md#12-recovery-model)).
9. **Can timing differences reveal hidden state?**
   - *Attempt:* measure server response times to infer ownership before reveal.
   - *Result:* **No oracle by design.** Projection generation is shape/size **data-independent** for
     unrevealed cards (FF-9); adjudication does not branch observably on ownership; outcomes are only
     revealed publicly at commit. *Property to preserve in design/testing, not merely asserted.*
10. **Can client manipulation or forged role claims escalate visibility?**
    - *Attempt:* a modified client asserts "I am Spymaster."
    - *Result:* **No.** Visibility derives from **authoritative (role, team)**, never client input
      (AAP-02, AP-03); role assignment is a coordinated Intent bound by one-Spymaster rules.

**Conclusion of the adversarial review:** under the software-architecture threat model, **hidden
information cannot leak by construction** — because non-Spymaster projections are **built by inclusion
and never contain the key in any form**, are produced **server-side after commit**, are **never
authoritative**, and are **recomputed (never restored)**. The two residual, non-architectural exposures
— **telemetry/logging discipline** and **host-memory compromise** — are explicitly delegated
(ADR-004/ops and the security-design phase) and are guarded by fitness functions where they touch the
software boundary. The design therefore **proves leak-prevention by design**, satisfying the additional
review requirement.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: per-(role,team) whitelist-inclusion projections at the delivery boundary; visibility matrix, invariants, fitness functions, security & adversarial review, verdict. |
