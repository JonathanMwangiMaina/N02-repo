# ADR-0002: Use ASP.NET Core 8 for Backend API

## Status
Accepted

## Context
We need a backend API for BlackoutClause that provides:
- Authentication and authorization
- Subscription management
- Multiplayer game state synchronization
- Leaderboards, statistics, matchmaking
- Webhook handling for Clerk events
- High performance and scalability

## Decision
Use ASP.NET Core 8 Minimal APIs for the backend server.

## Consequences

### Positive
- Native C# - shared code with Godot client via IndieFps.Shared
- High performance (Kestrel server, ~7M req/sec on minimal hardware)
- Minimal APIs reduce boilerplate, cleaner code
- Native AOT compilation support for smaller, faster containers
- Excellent Docker support with official Microsoft images
- Built-in dependency injection, logging, configuration
- OpenAPI/Swagger generation with Scalar UI
- Health checks for container orchestration
- Rate limiting, authentication middleware built-in
- Long-term support (LTS) until Nov 2026

### Negative
- Windows-focused historically (though cross-platform now)
- Larger memory footprint than Go/Rust microservices
- Less mature ecosystem for game-specific services (matchmaking, realtime)

### Neutral
- Team needs ASP.NET Core expertise
- EF Core for ORM (adds complexity vs Dapper)

## Alternatives Considered
- **Node.js/Express**: JavaScript ecosystem but no code sharing with C# client
- **Go**: Excellent performance but no code sharing, different paradigm
- **Rust/Axum**: Maximum performance but steep learning curve, no code sharing
- **Python/FastAPI**: Rapid development but slower, no code sharing

## References
- [ASP.NET Core 8 Documentation](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-8.0)
- [Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)