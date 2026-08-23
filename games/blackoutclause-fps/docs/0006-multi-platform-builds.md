# ADR-0006: Separate Build Pipelines per Target Platform

## Status
Accepted

## Context
BlackoutClause must ship on Windows, macOS, Linux, iOS, and Android. Each platform has:
- Different Godot export templates
- Different .NET runtime identifiers (win-x64, osx-arm64, linux-x64, ios-arm64, android-arm64)
- Different code signing requirements
- Different packaging formats (EXE/MSI, DMG, AppImage, IPA, APK/AAB)
- Different CI runner requirements (Windows, macOS, Linux runners)
- Different testing/validation needs

## Decision
Create separate build jobs/pipelines per platform in CI/CD, each with:
- Dedicated Dockerfile for containerized builds (Linux)
- Platform-specific GitHub Actions runners (windows-latest, macos-latest, ubuntu-latest)
- Platform-specific Godot export templates
- Platform-specific packaging scripts
- Independent success/failure tracking

## Consequences

### Positive
- Clear isolation: Windows build failure doesn't block macOS release
- Platform-specific optimization (native AOT on Windows, AppImage on Linux)
- Parallel execution reduces total CI time
- Easier debugging: logs per platform
- Can add platform-specific steps (notarization on macOS, signing on Windows)
- Supports different Godot export template versions per platform if needed

### Negative
- More CI configuration to maintain
- Duplication of common steps (restore, build shared, test)
- Higher CI minute consumption
- Need multiple runner types (cost on private repos)

### Neutral
- Use composite actions or reusable workflows to reduce duplication
- Artifact retention per platform

## Alternatives Considered
- **Single multi-platform job**: Simpler config but all-or-nothing, harder to debug, slower
- **Matrix strategy**: Good for similar platforms but iOS/Android need macOS runner, different tools
- **Separate repositories per platform**: Overkill, duplicates code, sync issues

## References
- [GitHub Actions Matrix Strategy](https://docs.github.com/actions/using-jobs/using-a-matrix-for-your-jobs)
- [Godot Export Templates](https://docs.godotengine.org/en/stable/tutorials/export/exporting_projects.html)
- [.NET RID Catalog](https://learn.microsoft.com/dotnet/core/rid-catalog)