# ADR-004 — Real-Time Communication & Delivery Architecture

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-02 |
| **Decision owner** | Lead Software Architect |
| **Answers** | *How are committed, versioned, role-filtered projections delivered from the server to participants while preserving correctness, ordering, determinism, and hidden-information guarantees?* |
| **Complies with** | [ADR-000 Vocabulary](ADR-000-architecture-vocabulary.md), [ADR-001 Style](ADR-001-overall-architecture-style.md), [ADR-002 Authoritative Game State](ADR-002-authoritative-game-state.md), [ADR-003 Coordination](ADR-003-per-room-coordination-model.md), [ADR-006 Visibility](ADR-006-role-based-information-visibility.md). Does not redefine authority, projections, or coordination. |
| **Scope note** | Defines the **delivery architecture** only. It chooses **no** transport (no WebSockets/SignalR/SSE/gRPC/MQTT/brokers), serialization, compression, encryption, auth, persistence, deployment, or libraries ([§18 Non-Goals](#18-non-goals)). |

---

## 1. Executive Summary

Delivery in Cluely is a **read-only, versioned, per-room, push-oriented distribution of committed
role-filtered projections and domain events** from the server to participants. It is performed by the
**Delivery Boundary** — a pure **Reader** ([ADR-000](ADR-000-architecture-vocabulary.md)) that
**transports** what the Authority committed (ADR-003) and the Projection function produced (ADR-006),
**after commit**, in **per-room order**, tagged with a **monotonic per-room version**.

The chosen model is **Server Push of committed, versioned projections, with Snapshot-on-(re)connect +
Incremental version-tagged updates, and client-driven resynchronization on a detected gap** (a
publish/subscribe-per-room distribution shape). Delivery is **idempotent** and **replay-safe**:
duplicates are no-ops, out-of-order/late messages are ignored because clients apply **only newer
versions**, and a client that detects a gap requests a **fresh snapshot** (the current committed
projection for its role).

Delivery is deliberately **separate** from authority and from projection generation:
- **Separate from authority** because it must never decide outcomes or own state — mixing them would
  reintroduce the races ADR-003 eliminated and risk a second source of truth ([ADR-002](ADR-002-authoritative-game-state.md)).
- **Separate from projections** because *deciding what a role may see* (ADR-006) is a secrecy concern
  that must live in one auditable place; delivery only *carries* the already-filtered result.
- **Never owns state** because the single authoritative aggregate is owned solely by the room Authority;
  delivery holds only disposable copies.

Correctness, ordering, determinism, and hidden-information integrity are preserved **because delivery
adds nothing and decides nothing** — it faithfully carries committed, versioned, already-filtered
artifacts, and the version discipline makes stale/duplicate/reordered delivery harmless.

> One-line statement: **push committed, role-filtered, per-room-versioned projections; apply-newest-only;
> snapshot on (re)connect; resync on gap; deliver only, never decide.**

---

## 2. Problem Statement

**Why multiplayer delivery is difficult.** Many participants must see a consistent, timely view of one
evolving room over networks that lose, duplicate, delay, and reorder messages, while some data is secret
to some participants.

**Why unreliable networks matter.** Mobile links drop and vary; naïve delivery would show contradictory
or stale boards, double-apply actions, or miss the terminal event — corrupting the *perceived* game even
when the *authoritative* game is correct.

**Why ordering matters.** Turns, reveals, and results are meaningful only in order; showing a reveal
before its turn, or an old turn after a new one, breaks comprehension and trust ([Rule Precedence](../../02-business-analysis/16-rule-precedence.md)).

**Why delivery cannot become authoritative.** If a delivered/cached view were treated as truth, two
truths would exist → drift, contradictions, and — worst — a path for the key to become canonical
([ADR-002 AI-STATE-8](ADR-002-authoritative-game-state.md#14-architectural-invariants-introducedaffirmed)).

**Why correctness > latency.** A fast wrong/leaky view is worthless; Cluely's value is a fair, correct
game ([AP-04/05](../../06-architecture-governance/01-architecture-principles.md)). Delivery optimizes
latency **only within** correctness and secrecy guarantees.

Relevant risks: [ENG-RT-01/02/03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)
(loss/dup/reorder, slow clients, reconnect resync), ENG-FP-01 (leak via any path), ENG-SC-03 (fan-out).

---

## 3. Delivery Philosophy

| Principle | Meaning | Why |
|-----------|---------|-----|
| **Delivery is Read-Only** | Delivery observes committed state/projections and transports them. | It is a Reader; never a writer ([ADR-000](ADR-000-architecture-vocabulary.md)). |
| **Authority Owns State** | Only the room Authority owns/mutates the aggregate. | [ADR-001/002](ADR-002-authoritative-game-state.md). |
| **Commit Before Delivery** | Nothing is delivered before it is committed. | No partial/uncommitted exposure ([ADR-003](ADR-003-per-room-coordination-model.md)). |
| **Projection Before Delivery** | Only already-role-filtered projections are delivered. | Secrecy decided once, in ADR-006; delivery carries the result. |
| **No Client Trust** | Clients cannot influence what/whether/when/ordering of authoritative delivery. | Clients are modifiable ([AAP-02](../../06-architecture-governance/02-architecture-anti-principles.md)). |
| **Delivery Never Mutates State** | Delivery cannot change the aggregate or projections. | Single source of truth. |
| **Deterministic Publication** | The same commit yields the same events/projections in the same order. | Reproducibility/testing ([AP-06](../../06-architecture-governance/01-architecture-principles.md)). |
| **Versioned Communication** | Every delivered artifact carries a monotonic per-room version. | Enables stale/dup/gap detection. |
| **Idempotent Delivery** | Re-delivering an artifact has no additional effect. | Replay/duplicate safety ([CR-4/CR-9](ADR-003-per-room-coordination-model.md#3-coordination-requirements)). |
| **Replay Safety** | Old artifacts can be re-sent without harm. | They carry only already-visible, older-version data. |
| **Room Isolation** | Delivery is per-room; no cross-room traffic couples rooms. | [AAP-08](../../06-architecture-governance/02-architecture-anti-principles.md), SCAL-1. |
| **Least Knowledge** | Delivery knows only how to route committed, filtered artifacts — not rules or secrets. | Minimizes coupling and leak surface. |

---

## 4. Candidate Delivery Models

Evaluated at the per-room, real-time, hidden-information scope. None dismissed without reasoning.

### DM1 — Request/Response Polling
- **Overview:** Clients periodically request the current projection.
- **Correctness/Ordering:** Fine (server-authoritative), but staleness between polls; ordering via
  version.
- **Latency:** Poor (poll interval) — bad for a real-time party game.
- **Complexity/Recovery/Testing:** Low/Simple/Easy.
- **Scalability:** Wasteful (constant polling) but simple.
- **Verdict:** Correct but **too laggy**; rejected as the primary model (usable only as a degraded
  fallback).

### DM2 — Long Polling
- **Overview:** Client holds a request open until new state or timeout.
- **Latency:** Better than DM1; still request-cycle overhead.
- **Complexity:** Medium; connection churn.
- **Verdict:** A **transport tactic**, not an architecture; acceptable as an implementation fallback
  under the chosen architecture, not the model itself.

### DM3 — Server Push (RECOMMENDED base)
- **Overview:** The server pushes committed, versioned projections/events to subscribed participants as
  they occur.
- **Correctness/Ordering:** Excellent with per-room versioning; commit-before-delivery preserved.
- **Latency:** Best — immediate on commit.
- **Complexity:** Medium (subscription + gap handling).
- **Recovery:** Good with snapshot-on-connect + resync.
- **Scalability:** Good — per-room fan-out; rooms isolated.
- **Testing/DX/Evolution:** Good.
- **Verdict:** **Selected base** — matches real-time feel and the commit-then-broadcast model.

### DM4 — Publish/Subscribe (per room)
- **Overview:** Participants subscribe to their room's stream; the Authority/delivery publishes committed
  artifacts to that room's subscribers with role-appropriate projections.
- **Advantages:** Natural per-room isolation; clean fan-out; conceptually simple ownership (publisher =
  delivery of committed state; consumers = participants).
- **Disadvantages:** Must ensure a subscriber receives only its **role** projection (enforced by ADR-006,
  not by delivery deciding).
- **Verdict:** **Selected as the distribution shape** for DM3 — publish/subscribe **per room** is how
  push is organized. (This is an architectural shape, not a broker/technology choice.)

### DM5 — Event Stream (append-only ordered stream per room)
- **Overview:** Deliver an ordered stream of committed domain events; clients fold events onto a base
  snapshot.
- **Advantages:** Natural ordering/versioning; small incremental messages; good for replay/observability.
- **Disadvantages:** Clients must correctly fold; a missed event needs gap handling (→ snapshot).
- **Verdict:** **Adopted as the incremental-update mechanism** *combined with* snapshots (DM8): snapshot
  establishes a base, events (version-tagged) advance it, gaps trigger re-snapshot. The stream carries
  **projected/filtered** events (ADR-006), never the key.

### DM6 — Message Queue (durable broker-mediated)
- **Overview:** Route delivery through a durable queue/broker.
- **Advantages:** Buffering, decoupling, potential durability.
- **Disadvantages:** Introduces broker infrastructure/semantics (a technology choice — out of scope here);
  risks treating queued messages as truth; heavier than needed for per-room, in-order, room-lifetime
  delivery ([AP-12](../../06-architecture-governance/01-architecture-principles.md), [AAP-05/12](../../06-architecture-governance/02-architecture-anti-principles.md)).
- **Verdict:** **Not an architectural requirement.** A broker may be an *implementation* choice for fan-out
  at scale (Technical Design), but the **architecture** does not depend on one and must not make delivered
  messages authoritative.

### DM7 — Periodic Synchronization (full state on an interval)
- **Overview:** Periodically push the full projection regardless of change.
- **Advantages:** Self-healing (bounded staleness).
- **Disadvantages:** Laggy and wasteful as a primary model.
- **Verdict:** Rejected as primary; a **low-frequency safety net** (heartbeat/keepalive with version) is
  acceptable to detect gaps.

### DM8 — Snapshot + Incremental Updates (RECOMMENDED completion)
- **Overview:** On (re)connect, deliver a **snapshot** (current role projection at the latest version);
  thereafter deliver **incremental** version-tagged updates/events; on gap, re-snapshot.
- **Advantages:** Best of both — fast steady-state (increments) and robust recovery (snapshots); precise
  version catch-up; leak-safe (snapshot and increments are both role projections).
- **Disadvantages:** Requires version bookkeeping and gap detection (well-understood).
- **Verdict:** **Selected** as the synchronization strategy layered on DM3/DM4/DM5.

### Evaluation summary

| Criterion | DM1 Poll | DM2 LongPoll | **DM3/4/5/8 (chosen)** | DM6 Queue | DM7 Periodic |
|-----------|:--------:|:------------:|:----------------------:|:---------:|:------------:|
| Correctness | 4 | 4 | **5** | 4 | 4 |
| Ordering | 4 | 4 | **5** | 4 | 3 |
| Latency | 1 | 3 | **5** | 4 | 2 |
| Complexity (5=lowest) | 5 | 4 | **3** | 2 | 4 |
| Recovery | 3 | 3 | **5** | 4 | 4 |
| Scalability | 2 | 3 | **4** | 4 | 2 |
| Operational (5=lowest) | 5 | 4 | **4** | 2 | 4 |
| Debuggability | 4 | 3 | **4** | 3 | 4 |
| Maintainability | 4 | 3 | **4** | 3 | 4 |
| Developer Experience | 3 | 3 | **4** | 3 | 3 |
| Future Evolution | 2 | 2 | **5** | 4 | 2 |
| Testing | 4 | 3 | **4** | 3 | 4 |

---

## 5. Final Delivery Model

**Adopt Server Push (DM3), organized as per-room Publish/Subscribe (DM4), delivering committed
role-filtered projections and version-tagged domain events (DM5), synchronized via Snapshot-on-
(re)connect + Incremental updates with gap-triggered re-snapshot (DM8), and a low-frequency versioned
heartbeat as a gap-detection safety net (DM7-lite).** Clients **apply newest version only**; delivery is
**idempotent** and **replay-safe**.

- **Why it fits Cluely:** immediate real-time feel with robust recovery, at per-room scale, with secrecy
  untouched.
- **Why it aligns with ADR-003:** delivery happens strictly **after commit**, in **per-room order** — it
  consumes the single ordered writer's output and never reorders or races it.
- **Why it aligns with ADR-006:** delivery carries **already-filtered** projections; it never filters or
  generates them, so it cannot leak.
- **Why it protects business invariants:** version discipline + apply-newest-only preserve turn/reveal/
  result ordering and finality ([INV-G2/G7/O1](../../02-business-analysis/10-business-invariants.md)); the
  key is never in a non-Spymaster projection ([INV-B9](../../02-business-analysis/10-business-invariants.md)).
- **Why it preserves determinism:** publication is a deterministic function of the commit sequence.
- **Why it preserves room isolation:** subscriptions and streams are **per room**; no cross-room delivery.

---

## 6. Delivery Lifecycle

Technology-neutral; strictly **after** the ADR-003 commit and ADR-006 projection.

```
Committed State (ADR-003) → Projection Generation (ADR-006)
   → Version Assignment (monotonic per-room) → Publication (per-room, to subscribers by role)
   → Delivery (read-only push) → Acknowledgement (conceptual: client's highest applied version)
   → Expiration (superseded by higher version) → Synchronization (client applies newest / requests snapshot on gap)
   → Reconnect Recovery (fresh role snapshot at latest version, then increments)
```

| Stage | What happens |
|-------|--------------|
| **Committed State** | The only thing delivery may read (commit-before-delivery). |
| **Projection Generation** | ADR-006 produces the role-filtered view/events (delivery does not do this). |
| **Version Assignment** | A monotonic **per-room version** is attached to the artifact (see §7). |
| **Publication** | The artifact is published to the room's subscribers, each receiving **their role's** projection. |
| **Delivery** | Read-only push to participants. |
| **Acknowledgement (conceptual)** | Conceptually, a client tracks its **highest applied version**; the server may use this to tailor increments vs snapshot. No business outcome depends on acknowledgement (delivery is best-effort + resync). |
| **Expiration** | An artifact is superseded when a higher version exists; older artifacts are ignored. |
| **Synchronization** | Client applies only newer versions; on a detected gap it requests a snapshot (§11). |
| **Reconnect Recovery** | On (re)connect, deliver a fresh role snapshot at the latest version, then resume increments. |

---

## 7. Versioning Strategy

- **What is a version?** A **monotonically increasing per-room sequence** stamped at each committed
  change. It orders everything delivered for that room.
- **Who creates it?** The **Authority** at commit (delivery merely carries it) — so versioning is bound to
  the single ordered writer (ADR-003), guaranteeing a total order per room.
- **What is versioned?** The **committed room state transition** (the commit). Projections and events for
  that commit **inherit that version**. Thus: *commit is versioned; projection/event carry it.* (Room is
  the scope; state is the subject; projection/event are the carriers.)
- **Stale detection:** an artifact whose version ≤ the client's highest applied version is **stale** →
  ignored.
- **Duplicate handling:** a re-delivered version equal to one already applied is a **no-op** (idempotent).
- **Missing versions (gap):** if a client observes a version jump (e.g., applied N, receives N+2), it
  detects a **gap** and requests a **snapshot** at the latest version (it does not attempt to guess N+1).
- **Conflicting versions:** conflicts cannot arise within a room because a single writer assigns a total
  order; if two artifacts claim the same version with different content, that is a **defect** (fitness
  function FF-1/§13) — the architecture guarantees one artifact set per version.

> Versions are **per-room and independent across rooms**; there is no global version (room isolation).

---

## 8. Ordering Guarantees

| Context | Guarantee |
|---------|-----------|
| **Within a room** | **Total order** by per-room version, derived from the single ordered writer (ADR-003). Clients present state in version order. |
| **Across rooms** | **No ordering relationship** — rooms are independent (isolation). |
| **After reconnect** | The client receives a **snapshot at the latest version**, re-establishing order; increments resume from there. |
| **After recovery** | Order is re-established from the recovered committed state's version; delivery never replays terminal effects ([ADR-002 §12](ADR-002-authoritative-game-state.md#12-recovery-model)). |
| **During retries** | Retried (duplicate) artifacts carry their original version → ignored/no-op; order unaffected. |
| **During duplicate delivery** | Same as retries — idempotent by version. |
| **During late delivery** | A late artifact with a version ≤ applied is ignored; it can never replace newer state. |
| **During concurrent publication** | Publication is per-room and version-ordered; even if artifacts are transmitted concurrently, **application order is by version**, not arrival. |

**Key property:** clients **apply newest-version-only**; arrival order is irrelevant to correctness.

---

## 9. Delivery Responsibilities

| Party | Responsibility | May it own state? | May it decide visibility? |
|-------|----------------|:-----------------:|:--------------------------:|
| **Authority** (Room Entity) | Commits state; assigns versions; is the origin of all delivered artifacts. | **Yes (sole owner)** | Produces the committed truth; visibility is applied by ADR-006. |
| **Projection Policy** (ADR-006) | Produces role-filtered projections/events from committed state. | No | **Yes (the only place)** |
| **Delivery Boundary** | Publishes/pushes committed, versioned, already-filtered artifacts to per-room subscribers; handles subscribe/unsubscribe, snapshots, increments, gap→snapshot. | **No** | **No** (carries filtered result) |
| **Participants** (clients) | Subscribe; apply newest version; request snapshot on gap; render. | No | No |
| **Observers / future Analytics** | Consume **events/metrics** (PII-free, key-free), never participant projections. | No | No |
| **Future Replay** | May consume the versioned committed-event history (if ADR-005 records it) — never becomes authoritative. | No | No |
| **Future Match History** | Derives from recorded results/events (PII-free) — a read-only consumer. | No | No |

Ownership rule: **origin = Authority; secrecy = Projection Policy; transport = Delivery.** These three are
distinct and never merged.

---

## 10. Delivery Failure Analysis

| Failure | Expected behavior | Recovery | Business impact if mishandled | Consistency guarantee |
|---------|-------------------|----------|-------------------------------|-----------------------|
| **Packet loss** | Missed artifact → client detects gap (version jump or heartbeat) → snapshot. | Snapshot at latest version. | Stale view (temporary). | Converges to latest committed. |
| **Duplicate delivery** | Version already applied → no-op. | None needed. | None. | Idempotent. |
| **Late delivery** | Version ≤ applied → ignored. | None. | None. | Never regresses. |
| **Out-of-order delivery** | Apply-newest-only; buffer or request snapshot if a needed earlier version is absent. | Snapshot on gap. | None. | Version order preserved. |
| **Temporary disconnect** | Presence marked disconnected (ADR-009); on return, snapshot + increments. | Reconnect recovery. | Possible pause if essential (ADR-003). | Resumes at latest committed. |
| **Long disconnect (> grace)** | Treated as departure (ADR-009); returns as fresh join → lobby/next-match projection. | Fresh join. | Seat released per rules. | No stale authoritative view. |
| **Reconnect** | Fresh **role** snapshot at latest version, then increments ([CB-08](../06-consistency-boundaries.md)). | Built-in. | Leak if wrong role — prevented by ADR-006. | Latest committed, role-correct. |
| **Partial delivery** | Incomplete artifact is not applied (version not advanced) → gap → snapshot. | Snapshot. | None. | No partial state shown. |
| **Server restart** | Delivery is stateless-of-truth; on recovery of authoritative state (ADR-005), clients re-snapshot. | Re-snapshot post-recovery. | Temporary disconnect. | Latest committed after recovery. |
| **Room shutdown / expiry** | Delivery stops; participants notified (room closed). | N/A. | None. | Terminal. |
| **Player timeout** | Presence/timeout handled by ADR-009; delivery ceases to that participant. | Reconnect if within grace. | None. | — |
| **Network partition** | Clients on the far side see stale state until reconnection; **never** contradictory authoritative state (they simply lag). | Snapshot on rejoin. | Temporary staleness only. | Converges; no split-truth (single Authority). |

**Invariant across all failures:** delivery failures cause at worst **temporary staleness**, never
**incorrect authoritative state**, **reordering of applied versions**, or **leaks**.

---

## 11. Synchronization Strategy

| Mode | When used |
|------|-----------|
| **Full synchronization (Snapshot)** | On initial connect and on **(re)connect**; and whenever a client detects an unrecoverable **gap**. Delivers the current **role** projection at the latest version. |
| **Incremental synchronization** | Steady state: version-tagged updates/events advance the client from its applied version. |
| **Snapshot synchronization** | The mechanism of full sync — a consistent role projection at a commit version ([ADR-006 §9](ADR-006-role-based-information-visibility.md#9-projection-lifecycle)). |
| **Resynchronization** | Triggered by gap detection (version jump, failed fold, or heartbeat mismatch) → request a fresh snapshot. |
| **Version catch-up** | If the client is only slightly behind and increments are available, deliver the missing increments; otherwise snapshot. (Choice between catch-up vs snapshot is an implementation detail; both yield the same latest committed state.) |
| **Recovery synchronization** | After server-side recovery (ADR-005), clients re-snapshot from the recovered committed state; no terminal replay. |

Default bias: **prefer a snapshot when in doubt** — it is always correct and leak-safe; increments are an
optimization.

---

## 12. Architectural Invariants (AI-DEL-*)

Extend prior invariants:

- **AI-DEL-1:** **Delivery never mutates authoritative state** (or projections).
- **AI-DEL-2:** **Delivery never generates projections** (only ADR-006 does).
- **AI-DEL-3:** **Delivery never filters visibility** (it carries already-filtered projections).
- **AI-DEL-4:** **Delivery never changes ordering** — application order is by per-room version.
- **AI-DEL-5:** **Delivery always references committed versions**; nothing uncommitted is delivered.
- **AI-DEL-6:** **Delivered artifacts are disposable** and **non-authoritative**.
- **AI-DEL-7:** **Delivery is reproducible** — the same commit sequence yields the same artifacts/versions.
- **AI-DEL-8:** **Delivery cannot bypass the Authority** — all artifacts originate from committed state.
- **AI-DEL-9:** **Delivery cannot bypass the Projection Policy** — participant artifacts are role-filtered.
- **AI-DEL-10:** **Every delivered artifact originates from a single committed version** and is traceable to it.
- **AI-DEL-11:** **Delivery is per-room**; no artifact crosses room boundaries.
- **AI-DEL-12:** **Clients apply newest-version-only**; stale/duplicate/late artifacts never regress state.

---

## 13. Architecture Fitness Functions (FF-DEL-*)

Measurable (future architecture tests):

- **FF-1:** **Every delivery references exactly one committed version**, and for a given version the
  artifact set is unique/consistent.
- **FF-2:** **No delivery step modifies authoritative state or projections** (read-only check).
- **FF-3:** **Duplicate delivery never changes correctness** (apply an artifact twice ⇒ identical client state).
- **FF-4:** **Late delivery never replaces newer state** (version ≤ applied ⇒ ignored).
- **FF-5:** **Reconnect always receives the latest committed role projection** (snapshot == recompute at latest version).
- **FF-6:** **Delivery is deterministic** — identical commit sequence ⇒ identical delivered sequence.
- **FF-7:** **Every projection/event is version-traceable** to its commit.
- **FF-8:** **Every publication originates from committed state** (no pre-commit emission).
- **FF-9:** **No hidden information appears in transport** — no delivered artifact to a non-Spymaster
  contains the key (composes with ADR-006 FF-1/9).
- **FF-10:** **No cross-room artifact** is ever delivered (room-isolation check).

Map to [Success Metrics ASM-05/06](../../06-architecture-governance/04-architecture-success-metrics.md),
[QS-01/05/08/10](../09-quality-attribute-scenarios.md).

---

## 14. Security Analysis

Separating **architectural guarantees** (by design) from **operational controls** (deferred to
Technical Design / Security / Ops).

| Threat | Architectural guarantee | Operational control (deferred) |
|--------|-------------------------|--------------------------------|
| **Replay attacks** | Re-sent artifacts carry old versions → ignored; only already-visible data (AI-DEL-12). | Transport-level anti-replay if desired. |
| **Version forgery** | Versions are assigned by the **Authority** at commit; clients cannot mint authoritative versions; a client-claimed version only reports *its* applied position and cannot advance server truth (AI-DEL-8). | Authenticated/encrypted transport (future). |
| **Duplicate injection** | Idempotent by version (FF-3). | Transport dedup. |
| **Client impersonation** | Delivery targets subscribers by **authoritative** identity/role (ADR-006/009), not client claim. | AuthN/AuthZ (future identity seam). |
| **Unauthorized subscriptions** | Subscription yields only the **role** projection derived from authoritative role; subscribing "as Spymaster" without the role yields an Operative projection (no key). | Access control on subscription (future). |
| **Projection substitution** | Delivery carries server-produced projections; a substituted (client-supplied) projection is never treated as truth (AI-DEL-1/6). | Integrity/signing (future). |
| **Delivery interception** | Even if intercepted, a non-Spymaster stream contains **no key** (ADR-006); interception reveals only what that role may already see. | Encryption in transit (future). |
| **Ordering attacks** | Application order is by version, not arrival; injecting/reordering packets cannot reorder applied state (AI-DEL-4/12). | Transport ordering/authenticity. |
| **Timing attacks** | Reveals are public post-commit; unrevealed-card projections are shape/size data-independent (ADR-006 FF-9) → no timing oracle for the key. | — |
| **Metadata leakage** | Delivered metadata (versions, room id) carries no ownership/secret; per-room isolation limits inference. | Minimize transport metadata (future). |
| **Transport metadata exposure** | Architecture stores no secret in delivery metadata. | Transport hardening (future). |
| **Future authentication / encryption** | The architecture is **transport-agnostic**; adding AuthN/encryption strengthens delivery without changing the model (AI-DEL-*). | Chosen in Technical Design. |

**Bottom line:** the architecture ensures that **even a fully compromised transport cannot leak the key
or corrupt authoritative order/state** — because non-Spymaster streams never contain the key, delivery
never owns truth, and order is by committed version.

---

## 15. Trade-off Analysis

- **Latency:** Server push gives the best steady-state latency; snapshots add occasional larger messages
  (accepted for correctness/recovery).
- **Correctness:** Maximized — delivery adds nothing and can only lag, never corrupt.
- **Memory:** Bounded — delivery holds disposable, per-room, versioned artifacts and (optionally) a small
  recent buffer for increments/catch-up.
- **Complexity:** Moderate (subscriptions, versioning, gap→snapshot) — justified; the alternative (polling)
  is simpler but too laggy.
- **Scalability:** Per-room fan-out scales with rooms (isolation); hot-room fan-out is bounded by small
  room sizes ([ENG-SC-03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md)).
- **Developer Experience:** Clear mental model — "apply newest version; snapshot on gap."
- **Recovery:** Excellent — snapshot re-establishes correctness after any disruption.
- **Testing:** Strong — determinism + versioning make loss/dup/reorder/gap tests straightforward.
- **Operational Cost:** Modest; a broker (if ever used for fan-out) is an implementation option, not a
  requirement.
- **Future Distribution:** The per-room, version-tagged, snapshot+increment model maps cleanly onto
  distributed per-room ownership (ADR-007) without change.
- **Future Replay/Analytics:** The versioned committed-event stream is a natural (read-only) source for
  replay/analytics later — without making delivery authoritative.

---

## 16. Risks

| Risk | Type | Mitigation |
|------|------|------------|
| Client treats a delivered/cached projection as truth | Architecture | AI-DEL-6; clients render, never adjudicate; server re-snapshot authoritative. |
| Delivery accidentally re-filters or generates projections | Architecture/Security | AI-DEL-2/3; delivery carries ADR-006 output only; fitness FF-9. |
| Version reuse/inconsistency (two artifacts, same version, different content) | Correctness | Single writer assigns versions (ADR-003); FF-1 detects violations. |
| Gap storms (many clients re-snapshot at once) | Operational/Scalability | Prefer increments/catch-up when slightly behind; snapshot only on real gaps; bounded room sizes; heartbeat tuning. |
| Hidden info in transport/telemetry | Security (critical) | AI-DEL-9/FF-9; non-Spymaster streams never contain the key; telemetry key-free (carried to ops). |
| Delivery outlives Authority (delivering after room gone) | Architecture | Delivery originates from committed state of a live Authority; on room end, delivery stops (AI-DEL-8/11). |
| Cross-room interference | Isolation | AI-DEL-11/FF-10; per-room subscriptions/streams. |
| Over-reliance on acknowledgements for correctness | Architecture | No business outcome depends on ack; correctness rests on version + snapshot, not delivery receipts. |

---

## 17. Assumptions

| # | Assumption | Confidence | If invalid |
|---|-----------|-----------|-------------|
| AS-1 | A **monotonic per-room version** can be assigned at commit (single writer, ADR-003). *(Effectively entailed.)* | Very High | Without it, ordering/gap detection would need another mechanism — but ADR-003 guarantees it. |
| AS-2 | Clients can **detect gaps** (via version jumps/heartbeat) and request snapshots. | High | If not, rely on periodic full sync (DM7) as a fallback (higher cost). |
| AS-3 | **Snapshots are affordable** for small per-room state. | Very High | If state were large (not for a 25-card game), increments-only with careful catch-up would be needed. |
| AS-4 | **Delivery need not be durable** — room-lifetime, best-effort + resync suffices (durability, if any, is ADR-005’s recorded event history, not delivery). | High | If guaranteed delivery were required, add durable buffering (implementation) without changing the model. |
| AS-5 | **Best-effort delivery + apply-newest + snapshot-on-gap** yields correct eventual views for all participants. | Very High | This is the crux; validated by loss/dup/reorder/chaos testing (§13). |

---

## 18. Non-Goals

This ADR does **not** decide: transport protocol, **SignalR / WebSockets / SSE / gRPC / MQTT**, message
brokers, serialization, compression, encryption, authentication/authorization mechanics, persistence,
deployment, database, infrastructure, or libraries/frameworks. It defines **only** the delivery
architecture; the above belong to Technical Design / later ADRs.

---

## 19. Impact on Future ADRs

| ADR / Phase | Constraint imposed by ADR-004 |
|-------------|-------------------------------|
| **ADR-005 State Recovery & Resilience** | After recovery, clients **re-snapshot** at the recovered version; delivery never replays terminals; the versioned committed-event history (if recorded) is a read-only source, never authoritative via delivery. |
| **ADR-007 Room Isolation & Distribution** | Delivery is per-room and version-tagged; distribution must preserve per-room ordering, single-version-authority, and isolation (AI-DEL-4/10/11). |
| **ADR-008 Dictionary Architecture** | Board words arrive inside projections; delivery is agnostic to content; dictionary never influences delivery. |
| **ADR-009 Session & Reconnection** | Reconnect uses snapshot-then-increments for the **restored role**; presence/timeout drive subscribe/unsubscribe; single active connection governs delivery target. |
| **ADR-010 Command/Query Strategy** | Query results are delivered as projections/snapshots; commands do not receive authoritative results by "read-back" but via delivered events; delivery carries outcomes, never decides them. |
| **Software Architecture / Technical Design** | Names the **Delivery Boundary** component; transport/protocol/serialization chosen here must satisfy AI-DEL-*; a broker is optional and non-authoritative. |
| **Implementation** | No client-side ordering/authority; apply-newest-only; no projection generation/filtering in delivery. |
| **Testing** | Chaos/loss/dup/reorder tests, reconnect-snapshot correctness, version-monotonicity, no-key-in-transport (FF-DEL-*). |
| **Operations** | Monitor version monotonicity, gap/snapshot rates, fan-out latency ([QS-08](../09-quality-attribute-scenarios.md)); ensure telemetry carries no key. |

---

## 20. Traceability

| Dimension | References |
|-----------|-----------|
| **Business Rules** | Ordering/finality of turns/reveals/results: [BR-TO/TE, BR-GV, BR-WIN/LOSE/ASN, BR-GE](../../02-business-analysis/02-business-rules.md); waiting-member no-data [BR-JR-6a](../../02-business-analysis/02-business-rules.md). |
| **Business Invariants** | [INV-B9](../../02-business-analysis/10-business-invariants.md) (no leak), [INV-B7](../../02-business-analysis/10-business-invariants.md) (one-way reveal), [INV-G2/G7](../../02-business-analysis/10-business-invariants.md) (one turn/finality), [INV-O1/O4](../../02-business-analysis/10-business-invariants.md) (single/immutable result), [INV-P4](../../02-business-analysis/10-business-invariants.md) (one connection). |
| **Quality Attribute Scenarios** | [QS-01](../09-quality-attribute-scenarios.md) (no leak), [QS-05](../09-quality-attribute-scenarios.md) (availability), [QS-08](../09-quality-attribute-scenarios.md) (propagation latency), [QS-10](../09-quality-attribute-scenarios.md) (concurrent rooms), [QS-07](../09-quality-attribute-scenarios.md) (reconnect). |
| **Engineering Challenges** | [ENG-RT-01/02/03](../../04-engineering-analysis/01-engineering-challenges-risk-analysis.md), ENG-FP-01, ENG-SC-03. |
| **Architecture Discovery** | [Interactions I-08](../08-interaction-discovery.md) (commit-then-broadcast), [Responsibilities R-11](../03-system-responsibilities.md) (delivery), [Consistency CB-08](../06-consistency-boundaries.md). |
| **ADR-000** | *Delivery Boundary, Reader, Projection, Domain Event, Snapshot, Determinism, Idempotency, Room Isolation, Publisher/Consumer* used as defined. |
| **ADR-001/002/003/006** | Delivery = read-only boundary in the monolith; carries ADR-002 aggregate projections; strictly post-ADR-003 commit; carries ADR-006 role-filtered output. |
| **Governance** | [AP-03/04/05/06/07/18](../../06-architecture-governance/01-architecture-principles.md); [AAP-02/05/08/09/12](../../06-architecture-governance/02-architecture-anti-principles.md); [Success Metrics](../../06-architecture-governance/04-architecture-success-metrics.md). |

---

## 21. Architecture Review

- **Decision:** Server push of committed, per-room-versioned, role-filtered projections/events, organized
  as per-room publish/subscribe, synchronized via snapshot-on-(re)connect + incremental updates with
  gap-triggered re-snapshot and a versioned heartbeat safety net; clients apply newest-version-only;
  delivery is read-only, idempotent, replay-safe, and non-authoritative.
- **Confidence:** **High** — entailed by ADR-002/003/006 (committed, ordered, filtered artifacts) and by
  the real-time requirement; alternatives are either too laggy (polling) or introduce unnecessary
  infrastructure/authority risk (broker) or are merely transport tactics.
- **Remaining risks:** gap/snapshot storms under mass reconnection (bounded by small rooms + catch-up);
  telemetry-no-key discipline (carried to ops); version-uniqueness enforcement (guaranteed by single
  writer, checked by FF-1).
- **Open questions (delegated, non-blocking):** increment-vs-snapshot catch-up threshold (Technical
  Design); heartbeat cadence; whether a broker aids fan-out at scale (implementation, non-authoritative);
  durable delivery buffering if ever required (ADR-005/implementation).
- **Review triggers:** a transport choice that cannot preserve per-room ordering; a requirement for
  guaranteed (durable) delivery; very large per-room state (not expected); multi-region delivery; adding
  authentication/encryption (should slot in without model change).
- **Readiness for ADR-005:** **READY.** Delivery now has a precise version/snapshot model; ADR-005 can
  define recovery of authoritative state knowing that clients re-snapshot at the recovered version and
  that delivery never replays terminals or holds truth.

---

## 22. Adversarial Architecture Review — "Attempt to Break the Design"

For each attack: **Attack · Expected Outcome · Architectural Protection · Residual Risk · Mitigation.**

1. **Can an older projection overwrite a newer one?**
   - *Expected:* No. *Protection:* apply-newest-version-only (AI-DEL-12/FF-4); older versions ignored.
   - *Residual:* client bug applying stale. *Mitigation:* version check is a client fitness test; server
     re-snapshot corrects.
2. **Can duplicate delivery change game state?**
   - *Expected:* No. *Protection:* idempotent-by-version (FF-3); delivery never mutates truth (AI-DEL-1).
   - *Residual:* none architecturally. *Mitigation:* —
3. **Can reconnect receive stale information?**
   - *Expected:* No. *Protection:* reconnect delivers a **fresh** role snapshot at the **latest** version
     (FF-5, [CB-08](../06-consistency-boundaries.md)). *Residual:* brief lag until snapshot arrives.
     *Mitigation:* snapshot is prioritized on (re)connect.
4. **Can delivery reorder turns?**
   - *Expected:* No. *Protection:* application order is by committed version, not arrival (AI-DEL-4);
     turns are committed in order by the single writer (ADR-003). *Residual:* none. *Mitigation:* —
5. **Can transport reveal hidden information?**
   - *Expected:* No. *Protection:* non-Spymaster streams **never contain the key** (ADR-006 + AI-DEL-9/FF-9);
     even a compromised transport sees only role-permitted data. *Residual:* Spymaster stream interception
     (a transport-security concern). *Mitigation:* encryption in transit (Technical Design/ops).
6. **Can a delayed packet reveal obsolete secrets?**
   - *Expected:* No. *Protection:* a delayed non-Spymaster artifact still contains no key; and it is stale
     by version → ignored. *Residual:* none. *Mitigation:* —
7. **Can delivery bypass the projection policy?**
   - *Expected:* No. *Protection:* delivery carries **only** ADR-006 output; it cannot generate/filter
     (AI-DEL-2/3/9). *Residual:* a design regression merging filtering into delivery. *Mitigation:*
     fitness FF-9 + review against [AAP-09](../../06-architecture-governance/02-architecture-anti-principles.md).
8. **Can delivery become a second source of truth?**
   - *Expected:* No. *Protection:* delivered artifacts are disposable/non-authoritative (AI-DEL-6);
     clients render, re-snapshot from committed truth. *Residual:* client treating cache as truth.
     *Mitigation:* client fitness test; authoritative re-snapshot.
9. **Can clients influence ordering?**
   - *Expected:* No. *Protection:* ordering is server-assigned per-room version (AI-DEL-4/8); client
     "acks" only report applied position and cannot advance/reorder server truth. *Residual:* none.
     *Mitigation:* —
10. **Can delivery continue after room Authority is gone?**
    - *Expected:* No. *Protection:* artifacts originate from a live Authority's committed state (AI-DEL-8);
      on room end/expiry delivery stops and participants are notified. *Residual:* in-flight artifact
      arriving just after end → harmless (stale, room-closed). *Mitigation:* room-closed handling.
11. **Can cross-room traffic ever interfere / violate isolation?**
    - *Expected:* No. *Protection:* per-room subscriptions/streams; no cross-room artifact (AI-DEL-11/FF-10).
      *Residual:* routing misconfiguration. *Mitigation:* isolation fitness test.
12. **Can replay resurrect obsolete projections (as truth)?**
    - *Expected:* No. *Protection:* replays are old versions (ignored) and non-authoritative regardless;
      recovery restores **authoritative state**, not projections (ADR-002/006). *Residual:* none.
      *Mitigation:* —

**Conclusion:** the delivery architecture **cannot corrupt authoritative order/state and cannot leak the
key by construction** — because it *only transports* committed, versioned, already-filtered artifacts,
clients *apply newest-only* and *re-snapshot on doubt*, and truth is never held by delivery. The sole
residual exposures (interception of a **Spymaster** stream; client misuse of a cache) are **transport-
security** and **client-discipline** concerns delegated to Technical Design/ops and guarded by fitness
functions — not weaknesses of the architecture.

---

## Final Deliverable — Answers

- **What is delivered?** Committed, per-room-**versioned**, **role-filtered projections** and **domain
  events** (snapshots on (re)connect; incremental updates thereafter).
- **Who delivers it?** The **Delivery Boundary** — a pure Reader — publishing per-room to subscribers; the
  **Authority** is the origin, the **Projection Policy** (ADR-006) is the filter.
- **When is it delivered?** **After commit** (commit-before-delivery), in **per-room version order**, and
  on (re)connect/gap as a snapshot.
- **What guarantees exist?** Per-room total ordering by version; apply-newest-only; idempotent, replay-safe
  delivery; eventual convergence to the latest committed role projection; no hidden information in
  non-Spymaster streams; room isolation.
- **What guarantees do NOT exist?** Guaranteed/durable delivery of every intermediate artifact, exactly-once
  *transport*, cross-room ordering, and any correctness dependence on acknowledgements — none are needed
  (correctness rests on version + snapshot).
- **How is ordering preserved?** By a **monotonic per-room version** assigned at commit by the single
  writer; clients present/apply in version order regardless of arrival.
- **How are stale deliveries prevented?** Stale = version ≤ applied ⇒ ignored; gaps ⇒ snapshot; late/dup ⇒
  no-op.
- **Why can delivery never become authoritative?** It never owns or mutates state; delivered artifacts are
  disposable; truth is the Authority's committed aggregate; clients re-snapshot from it (AI-DEL-1/6/8).
- **Why can delivery never leak hidden information?** It carries **only** ADR-006 role-filtered
  projections; non-Spymaster streams **structurally lack the key** (AI-DEL-9, ADR-006 FF-1/9); even a
  compromised transport exposes only role-permitted data.
- **How does this enable future transport technologies without changing the architecture?** The model is
  defined in terms of **committed versions, role projections, snapshots, and apply-newest** — all
  transport-agnostic. Any transport (or broker) that can push/pull ordered, role-scoped, versioned
  artifacts per room satisfies AI-DEL-*; choosing or swapping one is a Technical-Design change beneath an
  unchanged architecture.

---

## Revision History
| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | Initial decision: server push of committed, per-room-versioned, role-filtered projections; snapshot+incremental synchronization; ordering/versioning guarantees; invariants, fitness functions, security & adversarial review, verdict. |
