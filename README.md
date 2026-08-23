# N02-repo — Game Development Projects

A monorepo hosting multiple indie game projects built with modern .NET and Godot.

## Projects

| Game | Genre | Engine | Status | Description |
|------|-------|--------|--------|-------------|
| **[BlackoutClause](./games/blackoutclause-fps/)** | Sci-Fi Mercenary Hero Shooter (FPS) | Godot 4.3 / C# + ASP.NET Core 8 | Active Development | Multiplayer FPS with 9 unique mercenaries, faction warfare, subscription model via Clerk |
| **[Simon Game](./games/simon-game/)** | Memory Pattern Game | Godot 4.3 / C# | Archived / Reference | Classic Simon memory game - reference implementation for Godot/C# patterns |

---

## BlackoutClause — Sci-Fi Mercenary Hero Shooter

> **"The fighting is designed to never resolve. The corporations need it that way."**

### Overview
BlackoutClause is a competitive multiplayer FPS set in the Kepler Drift, where two megacorporations (Vantage-Halcyon and Solari Combine) fight proxy wars through private military contractors. Players choose from 9 distinct mercenaries across 3 roles (Offense, Defense, Support) in objective-based matches.

### Key Features
- **9 Unique Mercenaries**: Vex, Marrow, Cinder, Breach, Bulwark, Forge, Suture, Reveille, Wraith
- **Two Factions**: Meridian Tactical Solutions (Vantage-Halcyon) vs Obsidian Reach (Solari Combine)
- **Symmetric Balance**: Same 9 roles per faction, reskinned not redesigned
- **Subscription Model**: Free demo → $1 activation (7-day trial) → $9.99/month Pro via Clerk
- **Cross-Platform**: Windows, macOS, Linux, iOS, Android
- **Secure by Design**: Server-authoritative, anti-cheat hardened, Clerk auth

### Tech Stack

| Layer | Technology |
|-------|------------|
| **Game Engine** | Godot 4.3 (C# / .NET 8) |
| **Backend** | ASP.NET Core 8 Minimal APIs |
| **Database** | PostgreSQL (prod) / SQLite (dev) |
| **Auth & Billing** | Clerk (Auth, Multi-tenancy/Organizations, Billing) |
| **Real-time** | WebRTC / WebSocket (planned) |
| **CI/CD** | GitHub Actions (multi-platform matrix) |
| **Containerization** | Docker (per-platform client + server) |
| **Packaging** | NSIS (Windows), DMG (macOS), AppImage (Linux), IPA (iOS), AAB/APK (Android) |

### Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                      BLACKOUTCLAUSE CLIENT (Godot 4 / C#)           │
│  ┌────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────────┐  │
│  │ Game Core  │ │ Auth Manager│ │ Sub Manager │ │ Secure Storage│  │
│  └────────────┘ └──────┬──────┘ └──────┬──────┘ └───────────────┘  │
└────────────────────────┼────────────────────────────────────────────┘
                         │ HTTPS (REST) + WSS (WebSocket)
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    BACKEND API (ASP.NET Core 8)                     │
│  ┌──────────┐ ┌────────────┐ ┌────────────┐ ┌──────────────────┐  │
│  │ Auth     │ │ Subscription│ │ Matchmaking │ │ Game State      │  │
│  │ Endpoints│ │ Endpoints  │ │ Service    │ │ Sync (WebSocket) │  │
│  └──────────┘ └────────────┘ └────────────┘ └──────────────────┘  │
│  ┌──────────┐ ┌────────────┐ ┌────────────┐ ┌──────────────────┐  │
│  │ Clerk    │ │ PostgreSQL │ │ Redis      │ │ Health Checks    │  │
│  │ Webhooks │ │ (Users/Subs)│ │ (Cache)    │ │ (K8s Ready)      │  │
│  └──────────┘ └────────────┘ └────────────┘ └──────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

### Quick Start

#### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (pinned via `global.json`)
- [Godot 4.3+](https://godotengine.org/download) (.NET edition)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Clerk Account](https://clerk.com) (for auth/billing)

#### 1. Clone & Configure
```bash
git clone https://github.com/your-org/N02-repo.git
cd N02-repo/games/blackoutclause-fps

# Start infrastructure (PostgreSQL, Redis, Mailpit)
cd infrastructure
docker-compose up -d

# Configure Clerk secrets (Server)
cd ../src/BlackoutClause.Server
dotnet user-secrets init
dotnet user-secrets set "Clerk:PublishableKey" "pk_test_..."
dotnet user-secrets set "Clerk:SecretKey" "sk_test_..."
dotnet user-secrets set "Clerk:WebhookSecret" "whsec_..."
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 64)"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=blackoutclause;Username=postgres;Password=postgres"

# Configure Clerk (Client - Godot ProjectSettings)
# Add Clerk Publishable Key to ProjectSettings.godot or via environment
```

#### 2. Run Backend
```bash
cd src/BlackoutClause.Server
dotnet run --urls "https://localhost:5001;http://localhost:5000"
```

#### 3. Test Clerk Webhooks (separate terminal)
```bash
# Using Clerk CLI or ngrok
clerk webhooks forward --url http://localhost:5000/api/v1/webhooks/clerk
```

#### 4. Run Client
Open `src/BlackoutClause.Client` in Godot 4.3 (.NET edition) and press **F5**.

### Project Structure (BlackoutClause)

```
games/blackoutclause-fps/
├── src/
│   ├── BlackoutClause.Shared/      # Shared DTOs, Enums, Constants (net8.0)
│   ├── BlackoutClause.Server/      # ASP.NET Core 8 Minimal API
│   │   ├── Configuration/          # AppSettings, ClerkSettings, JwtSettings
│   │   ├── Domain/Entities/        # User, Subscription, Organization, Match
│   │   ├── Infrastructure/         # Auth, Payments(Clerk), Data (EF Core)
│   │   └── Endpoints/              # Auth, Subscription, Matchmaking, Webhooks
│   └── BlackoutClause.Client/      # Godot 4 C# Project
│       ├── Scripts/Core/           # Global, Settings, Audio, Input
│       ├── Scripts/Networking/     # ApiClient, AuthTokenManager, WebSocketClient
│       ├── Scripts/Storage/        # SecureStorage (OS keystore), LocalDb (SQLite)
│       ├── Scripts/Subscription/   # SubscriptionManager, EntitlementGate
│       ├── Scripts/Gameplay/       # PlayerController, Weapons, Abilities, Match
│       ├── Scenes/                 # MainMenu, Gameplay, Auth, UI, HUD
│       └── ProjectSettings.godot   # Godot project config
├── build/                          # Platform-specific packaging scripts
│   ├── windows/                    # NSIS installer scripts
│   ├── macos/                      # DMG creation scripts
│   ├── linux/                      # AppImage scripts
│   ├── ios/                        # Xcode project + export config
│   └── android/                    # Gradle + export config
├── infrastructure/
│   ├── docker-compose.yml          # Postgres, Redis, Mailpit
│   ├── Dockerfile.server           # Server production image
│   ├── Dockerfile.client.windows   # Windows client build image
│   ├── Dockerfile.client.macos     # macOS client build image
│   ├── Dockerfile.client.linux     # Linux client build image
│   ├── Dockerfile.client.ios       # iOS client build image (requires macOS)
│   └── Dockerfile.client.android   # Android client build image
├── .github/workflows/              # CI/CD (extends shared reusable workflows)
├── docs/                           # Game-specific docs (GDD, API, Deployment)
└── README.md                       # This file
```

### Subscription Model (via Clerk)

| Tier | Initial | Monthly | Access |
|------|---------|---------|--------|
| **Free (Demo)** | $0 | - | Tutorial only, no multiplayer |
| **Pro** | $1.00 (7-day trial) | $9.99 | All maps, multiplayer, cosmetics, mods, cloud saves, clan/organization support |

### Development Workflow

#### Adding a New Entitlement
1. Add to `Entitlement` enum in `BlackoutClause.Shared`
2. Add to `EntitlementConstants.ProEntitlements`
3. Configure in Clerk Dashboard (Product → Entitlements metadata)
4. Use `EntitlementGate` node in Godot scenes with `RequiredEntitlement`

#### Database Migrations
```bash
cd src/BlackoutClause.Server
dotnet ef migrations add MigrationName
dotnet ef database update
```

#### Running Tests
```bash
# All tests
dotnet test BlackoutClause.sln

# Specific project
dotnet test tests/BlackoutClause.Server.Tests/
```

### Platform Builds

Each platform has a dedicated build configuration in `build/{platform}/` and Dockerfile in `infrastructure/`.

```bash
# Windows (requires Windows runner)
dotnet publish src/BlackoutClause.Client -c Release -r win-x64 --self-contained -o ./dist/win

# macOS (requires macOS runner)
dotnet publish src/BlackoutClause.Client -c Release -r osx-arm64 --self-contained -o ./dist/mac

# Linux
dotnet publish src/BlackoutClause.Client -c Release -r linux-x64 --self-contained -o ./dist/linux

# iOS (requires macOS + Xcode)
# Uses Godot iOS export template + Xcode build

# Android
# Uses Godot Android export template + Gradle
```

### CI/CD

Push a tag to trigger multi-platform release:
```bash
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions will:
1. Run CI (build, test, lint, security scan) on all platforms
2. Build server Docker image
3. Build client for Windows, macOS, Linux, iOS, Android
4. Code sign (if certificates configured)
5. Package installers (NSIS, DMG, AppImage, IPA, AAB)
6. Create GitHub Release with all artifacts

### Security

- **Server-authoritative gameplay** — No client trust for movement, combat, economy
- **Clerk-managed auth** — MFA, passwordless, bot detection, device fingerprinting
- **Short-lived JWTs** (15 min) + **Clerk session management**
- **Argon2id** password hashing (via Clerk)
- **Clerk webhook signature verification** + **idempotency keys**
- **Code signing** required for production releases
- **HTTPS only** in production (TLS 1.2+)
- **Rate limiting** on all endpoints
- **Input validation** on all network boundaries
- **CodeQL** static analysis in CI
- **Dependency vulnerability scanning** in CI

### App Hardening (2025/2026 Threat Model)

- Anti-debug detection (client)
- Assembly integrity verification (client)
- Memory manipulation detection (client)
- Deterministic simulation / server reconciliation (network)
- Packet encryption + sequence numbers (network)
- Replay attack prevention (server)
- Cheat detection via server-side heuristics (server)

### Documentation

- [Game Design Document (GDD)](./docs/blackout-clause-gdd.md)
- [Architecture Decision Records (ADR)](../docs/adr/)
- [Changelog](../CHANGELOG.md)
- [API Documentation](https://api.blackoutclause.dev) (when deployed)

---

## Simon Game (Archived)

Classic Simon memory pattern game built as a reference implementation for Godot 4 + C# patterns.

**Location**: `games/simon-game/`

**Status**: Archived — kept for reference patterns (scene management, input handling, audio, UI)

---

## Repository Structure

```
N02-repo/
├── .github/
│   └── workflows/              # Shared reusable CI/CD workflows
├── docs/
│   └── adr/                    # Architecture Decision Records
├── infrastructure/             # Shared Docker base images, compose templates
├── games/
│   ├── blackoutclause-fps/     # BlackoutClause FPS (active)
│   └── simon-game/             # Simon Game (archived)
├── tools/                      # Shared build/deploy scripts
├── CHANGELOG.md                # Root changelog
├── README.md                   # This file
├── global.json                 # .NET SDK version pinning
├── Directory.Build.props       # Shared MSBuild properties
└── Directory.Packages.props    # Centralized package versions
```

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards
- `dotnet format` — Enforced in CI
- `TreatWarningsAsErrors=true` — Zero warnings policy
- Centralized package versions via `Directory.Packages.props`
- ADR required for architectural decisions

---

## License

MIT License — See [LICENSE](LICENSE) for details.

---

## Resources

- [Godot 4 Documentation](https://docs.godotengine.org/en/stable/)
- [ASP.NET Core 8 Documentation](https://learn.microsoft.com/aspnet/core/)
- [Clerk Documentation](https://clerk.com/docs)
- [.NET 8 Documentation](https://learn.microsoft.com/dotnet/)
- [Keep a Changelog](https://keepachangelog.com/)
- [Architecture Decision Records](https://adr.github.io/)