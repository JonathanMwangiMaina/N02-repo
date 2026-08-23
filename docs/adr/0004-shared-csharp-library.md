# ADR-0004: Shared C# Library for Client/Server Code Sharing

## Status
Accepted

## Context
We want to share code between the Godot client (C#) and ASP.NET Core server (C#) for:
- DTOs (Data Transfer Objects) for API contracts
- Enums for game types, entitlements, error codes
- Constants for configuration values
- Validation logic
- Game rules/logic that must be identical client and server

## Decision
Create a shared class library (IndieFps.Shared / BlackoutClause.Shared) targeting net8.0, referenced by both client and server projects.

## Consequences

### Positive
- Single source of truth for API contracts - eliminates drift
- Compile-time validation of DTOs/enums across client/server
- Shared validation logic (DRY)
- Game logic can be authoritative on server, predicted on client
- Easy to version and publish as NuGet package if needed
- Net8.0 compatible with both Godot.NET.Sdk and ASP.NET Core 8

### Negative
- Must target lowest common denominator (net8.0)
- Cannot use server-only packages (EF Core, ASP.NET Core) in shared
- Cannot use Godot-specific types (Vector3, Node, etc.) in shared
- Build order dependency (shared must build first)
- Changes require rebuild of both client and server

### Neutral
- Need clear boundaries: shared = contracts/logic only, no framework deps
- Use #if GODOT for client-only code in shared (avoid if possible)

## Alternatives Considered
- **Code generation from OpenAPI**: Generates DTOs but not enums/constants/logic, adds build complexity
- **Protocol Buffers/gRPC**: Excellent for performance but overkill for REST API, Godot C# gRPC support limited
- **Duplicate code**: Simple initially but leads to bugs and drift
- **Shared project (MSBuild)**: Works but less explicit than class library, no NuGet packaging

## References
- [.NET Standard / Target Framework Monikers](https://learn.microsoft.com/dotnet/standard/net-standard)
- [Godot.NET.Sdk Project Structure](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/project_setup.html)