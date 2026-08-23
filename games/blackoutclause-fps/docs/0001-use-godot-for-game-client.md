# ADR-0001: Use Godot 4 for Game Client

## Status
Accepted

## Context
We need a game engine for the BlackoutClause FPS client that supports:
- C#/.NET development for shared code with backend
- Cross-platform deployment (Windows, macOS, Linux, iOS, Android)
- 3D rendering with good performance
- No royalties or licensing costs
- Strong community and documentation

## Decision
Use Godot 4.3 with .NET/C# support as the game engine for the BlackoutClause client.

## Consequences

### Positive
- Free and open-source (MIT license), no royalties
- Native C# support with Godot.NET.Sdk
- Excellent 3D capabilities with Forward+ renderer
- Exports to all target platforms (Windows, macOS, Linux, iOS, Android, Web)
- Shared C# code with ASP.NET Core backend
- Lightweight (~100MB) compared to Unity/Unreal
- No vendor lock-in

### Negative
- Smaller ecosystem than Unity/Unreal
- Fewer third-party assets/plugins
- C# support in Godot 4 is relatively new (may have rough edges)
- No built-in multiplayer networking (need to implement or use third-party)

### Neutral
- Learning curve for team members unfamiliar with Godot
- GDScript vs C# choice (we chose C# for code sharing)

## Alternatives Considered
- **Unity**: Mature ecosystem but licensing costs, larger binary sizes, less favorable terms
- **Unreal Engine 5**: Excellent 3D but C++ primary, Blueprint visual scripting, 5% royalty after $1M
- **Custom Engine**: Maximum control but enormous development effort

## References
- [Godot 4 Documentation](https://docs.godotengine.org/en/stable/)
- [Godot C# Documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)
- [Godot.NET.Sdk NuGet](https://www.nuget.org/packages/Godot.NET.Sdk/)