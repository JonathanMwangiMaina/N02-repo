# ADR-0008: Multi-Game Repository Structure (Simon + BlackoutClause)

## Status
Accepted

## Context
N02-repo hosts multiple game projects:
- **Simon Game**: Memory pattern game (previously existed, now archived/referenced)
- **BlackoutClause**: Sci-fi mercenary hero shooter (current focus)

Need a repository structure that:
- Isolates each game's code, assets, builds
- Shares common infrastructure (CI/CD patterns, Docker base images, docs)
- Allows independent versioning and releases
- Supports future game additions
- Clear ownership and navigation

## Decision
Adopt a **monorepo with per-game directories** structure:

```
N02-repo/
├── .github/workflows/          # Shared CI/CD workflows (reusable)
├── docs/                       # Shared documentation (ADR, architecture)
├── infrastructure/             # Shared Docker base images, docker-compose templates
├── games/
│   ├── simon-game/             # Simon game (archived/maintained)
│   │   ├── src/                # Client + Server (if any)
│   │   ├── assets/
│   │   ├── build/              # Platform-specific builds
│   │   ├── Dockerfile.*
│   │   └── README.md
│   └── blackoutclause-fps/     # BlackoutClause FPS
│       ├── src/
│       │   ├── BlackoutClause.Shared/
│       │   ├── BlackoutClause.Server/
│       │   └── BlackoutClause.Client/
│       ├── assets/
│       ├── build/              # Platform-specific builds
│       │   ├── windows/
│       │   ├── macos/
│       │   ├── linux/
│       │   ├── ios/
│       │   └── android/
│       ├── Dockerfile.server
│       ├── Dockerfile.client.windows
│       ├── Dockerfile.client.macos
│       ├── Dockerfile.client.linux
│       ├── Dockerfile.client.ios
│       ├── Dockerfile.client.android
│       └── README.md
├── tools/                      # Shared build/deploy tools
├── CHANGELOG.md                # Root changelog (links to per-game)
├── README.md                   # Root README (index of games)
└── global.json                 # Shared .NET SDK version
```

Each game is a self-contained .NET solution with its own:
- Solution file (BlackoutClause.sln)
- Directory.Build.props / Directory.Packages.props
- CI/CD workflow (extends shared reusable workflows)
- Version number
- Release artifacts

## Consequences

### Positive
- Clear isolation: changes to BlackoutClause don't affect Simon
- Shared tooling/infrastructure reduces duplication
- Easy to add new games following same pattern
- Single PR can span multiple games if needed (engine upgrades)
- Git history per game via directory filtering
- Shared .NET SDK version via global.json

### Negative
- Larger repo size over time
- CI runs for all games on root changes (mitigate with path filters)
- Need discipline to not create cross-game dependencies
- IDE may load all projects (use solution filtering)

### Neutral
- Git submodules considered but rejected (complexity, sync issues)
- Separate repos considered but rejected (duplicated CI, harder shared tooling)

## Alternatives Considered
- **Separate repositories**: Clean isolation but duplicates CI/CD, Dockerfiles, docs, tooling
- **Git submodules**: Version locking but painful workflow, nested clones
- **Single flat solution**: All projects in one .sln - coupling, build noise, version conflicts

## References
- [Monorepo vs Polyrepo](https://monorepo.tools/)
- [GitHub Actions Path Filters](https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions#onpushpaths)
- [.NET Solution Filter](https://learn.microsoft.com/visualstudio/extensibility/solution-filter)