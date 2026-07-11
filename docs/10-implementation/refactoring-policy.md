# Cluely — Refactoring Policy

## Purpose

Refactoring is encouraged, but it must remain controlled and reviewable.

---

## Rules

Only refactor code that is:

* being modified
* blocking the current implementation
* required to maintain architectural integrity

---

## Avoid

Do not:

* rewrite unrelated modules
* rename large portions of the codebase without reason
* combine multiple unrelated refactors into one change
* mix architecture changes with feature implementation

---

## Pull Request Scope

Each implementation should have one primary purpose.

Examples:

* Add Room Aggregate
* Implement JoinRoom
* Add SignalR Delivery

Avoid combining unrelated improvements.

---

## Goal

Every change should remain understandable, reviewable, and reversible.
