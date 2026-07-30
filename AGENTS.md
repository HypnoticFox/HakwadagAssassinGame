# Hakwadag Assassin Game

## Overview

Hakwadag Assassin Game is an online real-world tagging game. Players are assigned targets from within a group and must complete a tag under the specific conditions described by the assignment. The app should support mobile-first play, temporary game state, and push notifications.

## Project Structure

The repository contains two main parts:

* **Frontend**: Vue 3 application written in **TypeScript**
* **Backend**: .NET application

Each part has its own directory. Both directories use:

* `src/` for application code
* `tests/` for test code

## Goals

* Prioritize a smooth **mobile experience**
* Support **PWA** behavior where practical
* Use **temporary state** instead of long-lived persistence whenever possible
* Keep the codebase simple, maintainable, and easy to run locally

## General Working Principles

* Make the smallest change that solves the problem.
* Prefer clarity over cleverness.
* Do not introduce new dependencies unless they clearly improve the project.
* Preserve existing patterns unless there is a strong reason to change them.
* If requirements are ambiguous or a change may be risky, stop and ask before proceeding.
* Keep code and tests aligned.

## Frontend

* Use **Vue 3**
* Use **TypeScript**
* Design for **mobile users first**
* Implement the frontend as a **PWA**
* Support push notifications where feasible and appropriate
* Favor responsive layouts, touch-friendly controls, and simple navigation

### Frontend expectations

* Keep screens usable on small displays
* Avoid desktop-first layouts
* Keep state management straightforward
* Prefer components that are reusable and easy to test

### Frontend formatting

* Format code with **ESLint + Prettier**
* Keep linting and formatting rules consistent across the frontend codebase

## Backend

The backend uses **clean architecture** with four layers:

* **Core**
* **Application**
* **Infrastructure**
* **Web**

### API style

* Use **Minimal APIs** for the .NET backend

### Layer responsibilities

* **Core**: domain entities, value objects, and business rules
* **Application**: use cases, orchestration, and application services
* **Infrastructure**: external integrations, storage, caching, and implementation details
* **Web**: HTTP endpoints, request/response models, and API composition

### Architecture rules

* Keep dependencies pointing inward
* Domain and application code should not depend on infrastructure concerns
* Web should stay thin and delegate business logic to application services
* Infrastructure should implement abstractions defined in inner layers

### Backend formatting

* Format code with the **.NET formatter**
* Keep formatting and code style consistent across the backend codebase

## Shared Utilities

The **Utils** project is reserved for shared utility code that is genuinely reusable across multiple projects.

Use it sparingly:

* Only add code there when it is broadly useful
* Do not move domain logic into Utils just to share it
* Prefer local, explicit code over generic helper bloat

## State and Persistence

The game is temporary by design.

### Preferred approach

* Use **Redis** as much as possible for transient game state
* Store temporary data with appropriate expiry/cleanup behavior where needed

### Database policy

* Use **Postgres** only when durable persistence is truly required
* Do not add a database by default
* If Postgres is introduced, keep its purpose limited and well-justified

## Testing

The backend uses **xUnit v3** for unit and integration tests. The frontend uses **Vitest** for unit tests and **Playwright with TypeScript** for end-to-end tests.

### Testing requirements

* **New features require new tests.** Do not merge feature code without corresponding test coverage.
* **Bug fixes require a regression test.** Before fixing a bug, write a test that reproduces the bug (and fails). Then fix the bug so the test passes. This ensures the bug doesn't regress.
* **Changes to existing behavior require updating the corresponding tests.** If a test fails because behavior changed intentionally, update the test to reflect the new expected behavior. Do not leave failing tests or delete them to make the build pass.

### Test levels

Write tests at three levels:

* **Unit tests** — Test individual classes, methods, and business rules in isolation. Fast, focused, and numerous. Cover domain logic, value objects, and application use cases.
* **Integration tests** — Test how components work together. Cover infrastructure concerns (Redis, Postgres), API endpoints, and external integrations. Use real dependencies where practical, test containers or mocks where not.
* **End-to-end (E2E) tests** — Test complete user flows from frontend to backend using **Playwright with TypeScript**. Cover critical paths like game creation, joining, tagging, and notifications. Run against a real or realistic environment.

### Testing expectations

* Prefer tests at the appropriate layer:
  * Unit tests for business rules and domain logic
  * Integration tests for use cases, infrastructure, and API behavior
  * E2E tests for critical user journeys
* Keep tests readable and focused on behavior
* Name tests clearly: what is being tested and what the expected outcome is
* One assertion per test when practical; multiple assertions only when they test the same logical behavior

## Local Development

The backend should include:

* a **Dockerfile** for building a production container image (the backend runs as a container in production)
* a **docker compose** file for starting backend dependencies (e.g. Redis, Postgres)

The docker compose file only manages dependencies — the backend itself is run directly (e.g. via `dotnet run` or `dotnet watch`), not containerized through compose.

Local setup should support:

* running the backend
* starting required dependencies
* running tests
* iterating without manual environment setup beyond what is necessary

### Remote Development Scripts

When the user asks to start, stop, or restart remote services, call the corresponding PowerShell script:

* **Start**: `.\start-remote-services.ps1 -Detach`
* **Stop**: `.\stop-remote-services.ps1`
* **Restart**: `.\restart-remote-services.ps1`

These scripts manage Redis, backend, frontend, and zrok2 tunnels for remote access.

**Important**: Always use `-Detach` when starting services so the script exits after starting everything in the background.

#### Script Parameters

**start-remote-services.ps1**
- `-SkipDependencies`: Skip starting Redis (useful if already running)
- `-SkipCleanup`: Don't register cleanup handlers (services won't stop on Ctrl+C)
- `-Detach`: Start services in background and exit immediately (required for agent use)

**stop-remote-services.ps1**
- `-ExcludeDependencies`: Skip stopping dependencies like Redis (default: stops them)
- `-DeleteReservedNames`: Delete zrok2 reserved names (URLs will change on next start)

**restart-remote-services.ps1**
- `-BackendOnly`: Only restart the backend (leave frontend running)
- `-FrontendOnly`: Only restart the frontend (leave backend running)

Examples:
- Start all services: `.\start-remote-services.ps1 -Detach`
- Stop everything: `.\stop-remote-services.ps1`
- Stop without stopping Redis: `.\stop-remote-services.ps1 -ExcludeDependencies`
- Restart only backend: `.\restart-remote-services.ps1 -BackendOnly`
- Restart only frontend: `.\restart-remote-services.ps1 -FrontendOnly`

### Debugging with Log Files

When services are started with `-Detach`, output is redirected to log files in the `.logs/` directory:

- `backend-stdout.log` / `backend-stderr.log` - Backend build output, runtime logs, and dotnet watch messages
- `frontend-stdout.log` / `frontend-stderr.log` - Vite dev server output and npm messages
- `zrok-api-stdout.log` / `zrok-api-stderr.log` - API tunnel status
- `zrok-app-stdout.log` / `zrok-app-stderr.log` - Frontend tunnel status

Use these logs to debug issues when services aren't responding or behaving unexpectedly. The `.logs/` directory is gitignored.

## API and Implementation Guidance

* Keep API contracts clean and predictable
* Avoid unnecessary complexity in request and response shapes
* Use names that reflect the domain clearly
* Prefer explicit code over hidden magic
* Keep error handling consistent and understandable

## When Making Changes

Before implementing a change, consider:

* Does this belong in the right layer?
* Is this temporary state, or should it be persisted?
* Does this affect mobile usability?
* Should there be a test for this?
* Does this introduce unnecessary complexity?

## Style

* Use clear, concise language in code and documentation
* Keep functions and classes focused
* Prefer small, composable pieces
* Avoid overengineering
* Use consistent naming across the codebase


<!-- graymatter:instructions:begin — managed by `graymatter init`; edits inside this block are overwritten -->
## Memory (GrayMatter)

This project has persistent agent memory via the `graymatter` MCP tools:

- `memory_search` (`agent_id`, `query`) — call at the **start of a task** when prior context might matter.
- `memory_add` (`agent_id`, `text`) — call whenever you learn something **durable**: user preferences, decisions, conventions, gotchas.
- `memory_reflect` (`action`, `agent`, `text`/`target`) — update or forget stale facts. ⚠ takes `agent`, not `agent_id`.
- `checkpoint_save` / `checkpoint_resume` (`agent_id`) — snapshot/restore session state before major refactors or across restarts.

Use a stable `agent_id` of the form `<project>-<role>` (e.g. `myapp-backend`). Store conclusions, not conversation logs. Err on the side of remembering.
<!-- graymatter:instructions:end -->
