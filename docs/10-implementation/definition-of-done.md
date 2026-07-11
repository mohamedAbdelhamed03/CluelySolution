# Cluely — Definition of Done (DoD)

## Purpose

This document defines the minimum completion criteria for every implementation task, regardless of size.

A feature is **not** considered complete because it compiles or because the tests pass.

It is complete only when every item in this checklist is satisfied.

---

## Functional

* Feature behaves according to the approved documentation.
* Business rules are implemented exactly as specified.
* Existing behavior has not changed unintentionally.
* Edge cases have been considered.
* Failure scenarios have been implemented.

---

## Architecture

* All approved ADRs are respected.
* Aggregate boundaries remain unchanged.
* Dependency directions remain unchanged.
* No architectural drift has been introduced.
* No framework leakage into Domain.
* No business logic moved into Infrastructure or API.

---

## Code Quality

* No compiler warnings.
* No analyzer warnings.
* Nullable reference rules satisfied.
* No duplicated code.
* No dead code.
* No commented-out code.
* No unnecessary abstractions.
* No TODO or FIXME comments left behind.

---

## Testing

* Unit tests added where appropriate.
* Existing unit tests pass.
* Architecture tests pass.
* New invariants are covered.
* Failure paths are tested.
* Regression tests added when fixing defects.

---

## Documentation

* Public API changes documented.
* Engineering Decision Log updated when applicable.
* Technical debt documented if intentionally accepted.
* README updated if user-facing behavior changed.

---

## Review

* Self-review completed.
* Previous review findings checked.
* No known regressions introduced.
* Engineering checklist completed.

---

## Final Approval

A task is complete only when another engineer could confidently review and merge it into the main branch without requesting additional engineering work.
