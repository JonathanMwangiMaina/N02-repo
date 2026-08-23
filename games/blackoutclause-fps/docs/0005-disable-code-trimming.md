# ADR-0005: Disable Code Trimming for Godot Reflection Compatibility

## Status
Accepted

## Context
.NET 8 supports assembly trimming (PublishTrimmed=true) to reduce publish size by removing unused code. However, Godot 4 C# relies heavily on reflection for:
- Signal connections ([Signal] attribute)
- Export properties ([Export] attribute)
- Node/Resource discovery
- Scene instantiation
- Dynamic method invocation
- Source generators (GodotSharp.SourceGenerators)

Trimming breaks these reflection-based mechanisms, causing runtime crashes or missing functionality.

## Decision
Disable code trimming for the Godot client project (PublishTrimmed=false, TrimMode=partial at most). Keep trimming enabled for the server project where reflection usage is controlled.

## Consequences

### Positive
- Guaranteed compatibility with Godot's reflection-heavy architecture
- No runtime "missing method/property" crashes from over-trimming
- Source generators work correctly
- Zero time debugging trim-related issues
- Simpler build configuration

### Negative
- Larger publish size (~60-100MB vs ~30-50MB for client)
- More memory usage at runtime
- Longer startup time (more assemblies to load)
- Server still benefits from trimming

### Neutral
- Can use [DynamicallyAccessedMembers] annotations selectively if size becomes critical
- Godot 4.4+ may improve trimming support (monitor updates)

## Alternatives Considered
- **Selective trimming with annotations**: Requires annotating hundreds of Godot-related types, maintenance burden
- **TrimMode=partial**: Still breaks some Godot reflection scenarios
- **Custom trimmer root assembly**: Complex, fragile, version-specific

## References
- [.NET Trimming Documentation](https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-options)
- [Godot C# and Trimming Issues](https://github.com/godotengine/godot/issues/73542)
- [GodotSharp Source Generators](https://github.com/godotengine/godot/tree/master/modules/mono/GodotSharp/GodotSharp.SourceGenerators)