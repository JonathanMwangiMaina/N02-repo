# Architecture Decision Records (ADR)

This directory contains Architecture Decision Records for the N02-repo game development projects.

## Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [0001](0001-use-godot-for-game-client.md) | Use Godot 4 for Game Client | Accepted | 2026-08-23 |
| [0002](0002-use-aspnet-core-for-backend.md) | Use ASP.NET Core 8 for Backend API | Accepted | 2026-08-23 |
| [0003](0003-use-clerk-for-auth.md) | Use Clerk for Authentication, Multi-tenancy, and Billing | Accepted | 2026-08-23 |
| [0004](0004-shared-csharp-library.md) | Shared C# Library for Client/Server Code Sharing | Accepted | 2026-08-23 |
| [0005](0005-disable-code-trimming.md) | Disable Code Trimming for Godot Reflection Compatibility | Accepted | 2026-08-23 |
| [0006](0006-multi-platform-builds.md) | Separate Build Pipelines per Target Platform | Accepted | 2026-08-23 |
| [0007](0007-app-hardening.md) | App Hardening Against Game Exploits (2025/2026) | Accepted | 2026-08-23 |
| [0008](0008-multi-game-repo-structure.md) | Multi-Game Repository Structure (Simon + BlackoutClause) | Accepted | 2026-08-23 |

## ADR Template

```markdown
# ADR-XXXX: Title

## Status
Proposed | Accepted | Rejected | Deprecated | Superseded

## Context
What is the issue that we're seeing that is motivating this decision or change?

## Decision
What is the change that we're proposing and/or doing?

## Consequences
What becomes easier or more difficult to do because of this change?

### Positive
- 

### Negative
- 

### Neutral
- 

## Alternatives Considered
- Alternative 1: Reason for rejection
- Alternative 2: Reason for rejection

## References
- Links to relevant documentation, issues, or discussions
```