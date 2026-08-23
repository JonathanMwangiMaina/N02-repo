# ADR-0009: Retain C# / ASP.NET Core for BlackoutClause Backend

## Status
Accepted

## Context
Evaluated Go 1.26 vs .NET 10 for backend rewrite. Go shows 4-8x memory advantage and 2-4x latency improvement at 100K CCU. However, the project has existing C# architecture with Godot 4.3 client.

Evaluated Native AOT for .NET 8 deployment size reduction.

## Decision
**Retain C# / ASP.NET Core 8** with:
- PgBouncer for connection pooling (1000+ concurrent connections)
- Upstash Redis for serverless/global caching (scale-to-zero, <5ms latency)
- Aurora DSQL / Serverless v2 for multi-region PostgreSQL
- Source-generated JSON serialization for trimming readiness
- **Native AOT deferred** until EF Core 10 LTS (Nov 2025) with stable AOT support

## Rationale
1. **Shared Godot↔Server code** (DTOs, validation, game logic) is irreplaceable - would require full rewrite in two languages
2. **Unity exit option** requires C# - Go has zero Godot/Unity integration
3. **Clerk SDK maturity** favors C# over Go community SDKs
4. **Team velocity** in C# > Go learning curve (2-3 months for proficiency)
5. **Infrastructure parity achieved** via PgBouncer + DSQL + Upstash
6. **Native AOT blockers**: EF Core 8 has no AOT support; EF Core 9 experimental with dynamic LINQ limitations, no production guarantee

## Consequences
- Accept 50% memory overhead vs Go (mitigated by PgBouncer + trimming)
- Invest in C#-specific scaling patterns (PgBouncer, DSQL, Upstash)
- Re-evaluate Native AOT at EF Core 10 LTS (Nov 2025)
- Re-evaluate Go at 50K CCU if Go becomes compelling

## References
- [Go 1.26 vs .NET 10 Benchmark](https://furkan-dvlp.medium.com/go-1-26-vs-net-10-vs-net-11-preview)
- [Kubernetes Cost Analysis](https://www.binarybox.org/p/i-benchmarked-5-languages-in-kubernetes)
- [Live Game Backend Scaling Playbook](https://crux.supercraft.host/blog/live-game-backend-scaling-playbook)
- [Upstash Redis Documentation](https://upstash.com/docs/redis/sdks/csharp/gettingstarted)
- [PgBouncer Documentation](https://www.pgbouncer.org/)
- [ASP.NET Core Native AOT](https://learn.microsoft.com/aspnet/core/fundamentals/native-aot)
- [EF Core Native AOT Experimental](https://learn.microsoft.com/ef/core/performance/nativeaot-and-precompiled-queries)