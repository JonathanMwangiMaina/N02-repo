# BlackoutClause FPS Game

A sci-fi mercenary hero shooter built with Godot 4.3 (C#) and ASP.NET Core 8.

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Godot 4.3+](https://godotengine.org/download) (.NET edition)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Clerk Account](https://clerk.com) (for auth/billing)

### 1. Clone & Configure
```bash
git clone https://github.com/your-org/N02-repo.git
cd N02-repo/games/blackoutclause-fps

# Start infrastructure (PostgreSQL, Redis, Mailpit)
cd infrastructure
docker-compose up -d

# Configure Clerk secrets (Server)
cd ../src/BlackoutClause.Server
dotnet user-secrets init
dotnet user-secrets set "Clerk:Domain" "your-instance.clerk.accounts.dev"
dotnet user-secrets set "Clerk:PublishableKey" "pk_test_..."
dotnet user-secrets set "Clerk:SecretKey" "sk_test_..."
dotnet user-secrets set "Clerk:WebhookSecret" "whsec_..."
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=blackoutclause;Username=postgres;Password=postgres"
```

### 2. Run Backend
```bash
cd src/BlackoutClause.Server
dotnet run --urls "https://localhost:5001;http://localhost:5000"
```

### 3. Test Clerk Webhooks (separate terminal)
```bash
# Using Clerk CLI or ngrok
clerk webhooks forward --url http://localhost:5000/api/v1/webhooks/clerk
```

### 4. Run Client
Open `src/BlackoutClause.Client` in Godot 4.3 (.NET edition) and press **F5**.

## Platform Builds

### Windows (requires Windows runner)
```bash
dotnet publish src/BlackoutClause.Client -c Release -r win-x64 --self-contained -o ./dist/win
```

### macOS (requires macOS runner)
```bash
dotnet publish src/BlackoutClause.Client -c Release -r osx-arm64 --self-contained -o ./dist/mac
```

### Linux
```bash
dotnet publish src/BlackoutClause.Client -c Release -r linux-x64 --self-contained -o ./dist/linux
```

### iOS (requires macOS + Xcode)
```bash
# Uses Godot iOS export template + Xcode build
godot --headless --export-release "iOS" build/ios/BlackoutClause.xcodeproj
```

### Android
```bash
# Uses Godot Android export template + Gradle
godot --headless --export-release "Android" build/android/BlackoutClause.apk
```

## CI/CD

Push a tag to trigger multi-platform release:
```bash
git tag blackoutclause-v1.0.0
git push origin blackoutclause-v1.0.0
```

GitHub Actions will:
1. Run CI (build, test, lint, security scan) on all platforms
2. Build server Docker image
3. Build client for Windows, macOS, Linux, iOS, Android
4. Code sign (if certificates configured)
5. Package installers (NSIS, DMG, AppImage, APK, Xcode project)
6. Create GitHub Release with all artifacts

## Project Structure

```
games/blackoutclause-fps/
├── src/
│   ├── BlackoutClause.Shared/      # Shared DTOs, Enums, Constants (net8.0)
│   ├── BlackoutClause.Server/      # ASP.NET Core 8 Minimal API
│   │   ├── Configuration/          # AppSettings, ClerkSettings
│   │   ├── Domain/Entities/        # User, Subscription, ProcessedClerkWebhookEvent
│   │   ├── Infrastructure/         # Clerk (Auth, Billing), Data (EF Core)
│   │   └── Endpoints/              # Auth, Game, Clerk Webhooks
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
├── export_presets.cfg              # Godot export configurations
├── icon.svg                        # Game icon
├── .github/workflows/              # CI/CD (ci-blackoutclause.yml, cd-blackoutclause.yml)
└── README.md                       # This file
```

## Subscription Model (via Clerk)

| Tier | Initial | Monthly | Access |
|------|---------|---------|--------|
| **Free (Demo)** | $0 | - | Tutorial only, no multiplayer |
| **Pro** | $1.00 (7-day trial) | $9.99 | All maps, multiplayer, cosmetics, mods, cloud saves, clan/organization support |

## Tech Stack

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

## Security

- **Server-authoritative gameplay** — No client trust for movement, combat, economy
- **Clerk-managed auth** — MFA, passwordless, bot detection, device fingerprinting
- **JWT validation** — Clerk-issued tokens validated via JWKS
- **Clerk webhook signature verification** — Svix HMAC-SHA256
- **Code signing** required for production releases
- **HTTPS only** in production (TLS 1.2+)
- **Rate limiting** on all endpoints
- **Input validation** on all network boundaries
- **CodeQL** static analysis in CI
- **Dependency vulnerability scanning** in CI

## Documentation

- [Game Design Document (GDD)](../../blackout-clause-gdd.md)
- [Architecture Decision Records (ADR)](../../docs/adr/)
- [Changelog](../../CHANGELOG.md)

## License

MIT License — See [LICENSE](../../LICENSE) for details.