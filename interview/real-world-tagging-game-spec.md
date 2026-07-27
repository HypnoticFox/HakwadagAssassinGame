---
sessionID: ses_060b5a895ffe9RCgcJsAd4zH77
baseMessageCount: 0
updatedAt: 2026-07-26T17:35:31.654Z
version: 1.0
date_created: 2026-07-26
owner: agent
tags: [spec, diagnostic]
---

# I want to build an online real-world tagging game. Players are assigned targets from within a group and must complete a tag under the specific conditions described by the assignment. The app should support mobile-first play, temporary game state, and push notifications.
@AGENTS.md already contains some information on how I want the project to be build.

## Current spec

This document specifies an online real-world tagging game ("Hakwadag"). Players are assigned targets from within a group and must complete a tag under specific behavioral conditions. The game is confirmation-based: the hunter submits a kill under a chosen circumstance, the target receives a notification and confirms or denies. The game follows a points-and-leaderboard model within a fixed duration. The primary use case is a scouting camp (~20 players, 3-7 days), but creator-configurable parameters support flexible scale. The app is mobile-first, PWA-based, uses temporary state (Redis), and sends push notifications.

## 1. Purpose & Scope

**Audience**: Developers building the Hakwadag Assassin Game frontend (Vue 3 / TypeScript) and backend (.NET clean architecture).

**Boundaries**: Covers core game loop: game creation, joining, target assignment, tag submission/confirmation, scoring, leaderboard, admin actions, push notifications. Does not cover chat, social features, monetization, or native apps.

**Assumptions**: Good-faith play, temporary Redis state, mobile-first UI, email OTP auth.

## 2. Definitions

Hunter (player who hunts), Target (player being hunted), Tag/Kill (successful completion), Assignment (hunter-target pairing with conditions), Circumstance (specific condition), Game Creator (admin), Co-admin (delegated admin), Condition Type (template for circumstance), Safe Time Block (restricted hours), Auto-resolution (timeout-based confirmation).

## 3. Requirements, Constraints & Guidelines

**REQ-001**: Create game with configurable name, duration, max players, base points, confirmation timeout, per-condition-type bonuses.
**REQ-002**: Invite code/link joining.
**REQ-003**: Random target assignment at game start.
**REQ-004**: New random target after successful tag.
**REQ-005**: Hunter sees target identity and conditions.
**REQ-006**: Hunter submits tag by selecting fulfilled circumstance.
**REQ-007**: Target gets push notification and can confirm/deny within timeout.
**REQ-008**: Auto-confirm on timeout.
**REQ-009**: Base points + condition bonus (creator sets per type).
**REQ-010**: Live leaderboard.
**REQ-011**: Creator can end game early.
**REQ-012**: Leave game → hunter gets new target.
**REQ-013**: Admin dispute resolution.
**REQ-014**: Safe time blocks.
**REQ-015**: Push notifications for all key events.

Condition types (built-in): WithSpecificPerson (at assignment time), Alone, WithXPeople, MundaneAction, Custom.

Security: HTTPS, email OTP, role-based access, hunter-only submission, target-only confirmation.

Constraints: Vue 3 + TS/PWA frontend, .NET clean architecture, Redis state, Web Push API, SignalR, Docker Compose.

## 4. Interfaces & Data Contracts

Core domain models: Game, Player, GamePlayer, Assignment, Condition (abstract with subtypes), TagSubmission.

API endpoints: auth (send-otp, verify-otp), games CRUD + start/end, join via invite code, assignments/me, tag submit/confirm/deny/void, leave, admin management, safe times CRUD, leaderboard, conditions CRUD.

SignalR hub with events: ScoreUpdated, TagSubmitted, TagResolved, GameStarted, GameEnded, AssignmentChanged, PlayerLeft.

JSON shapes for Game response and Assignment (hunter's view) included.

## 5. Acceptance Criteria

12 ACs covering: game creation, joining, start assignment, tag submission, confirm, timeout auto-confirm, deny, admin void, leaderboard updates, leave game, safe time blocks, game end.

## 6. Test Automation Strategy

xUnit v3 (backend), Vitest (frontend). Domain layer pure unit tests. Application layer mocked use-case tests. Infrastructure Redis integration tests via Testcontainers. API integration via WebApplicationFactory. Frontend component tests with vue-test-utils.

## 7. Rationale & Context

Confirmation-based tagging (no GPS/QR): privacy, simplicity, good-faith social play. Points & leaderboard over elimination: keeps all players engaged full duration. Email OTP: no password management. Random + reassignment: equal opportunity, dynamic. Multiple conditions per assignment: flexibility for hunter. Creator-configurable conditions: reusable across events. Redis-only: temporary by design. PWA: no app store. Auto-resolution: prevents stuck games.

## 8. Dependencies & External Integrations

EXT-001: SMTP/email service for OTP. EXT-002: Web Push API (PWA). EXT-003: Redis. EXT-004: SignalR. EXT-005: Docker Compose.

## 9. Examples & Edge Cases

Assignment flow example with Charlie/Diana/Frank. Edge cases: target leaves mid-game, mutual targets, only 2 players remain, bad-faith denial, simultaneous submissions, creator leaves, OTP rate limit. Leaderboard table example.

## 10. Validation Criteria

Pre-game: min 2 players, NotStarted status. Tag submission: correct hunter, outside safe times, valid condition, no pending tag. Confirmation: correct target, Pending status, within timeout. Admin: role enforcement. Scoring: base + bonus, non-negative.

## 11. Related Specifications / Further Reading

AGENTS.md, Vue 3 docs, SignalR docs, Web Push API spec.

## Q&A history

No answers yet.
