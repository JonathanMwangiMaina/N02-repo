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
git clone https://github.com/JonathanMwangiMaina/N02-repo.git
cd N02-repo/games/blackoutclause-fps

# Start infrastructure (PostgreSQL, Redis, PgBouncer, Mailpit)
docker-compose -f infrastructure/docker-compose.yml up -d

# Configure Clerk secrets (Server)
cd src/BlackoutClause.Server
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
├── .github/workflows/              # CI/CD (ci-blackoutclause.yml, cd-blackoutclause.yml)
├── build/                          # Platform-specific packaging scripts
│   ├── windows/                    # NSIS installer scripts
│   ├── macos/                      # DMG creation scripts
│   ├── linux/                      # AppImage scripts
│   ├── ios/                        # Xcode project + export config
│   └── android/                    # Gradle + export config
├── docs/                           # Game-specific documentation
│   ├── adr/                        # Architecture Decision Records
│   │   ├── 0001-use-godot-for-game-client.md
│   │   ├── 0002-use-aspnet-core-for-backend.md
│   │   ├── 0003-use-clerk-for-auth.md
│   │   ├── 0004-shared-csharp-library.md
│   │   ├── 0005-disable-code-trimming.md
│   │   ├── 0006-multi-platform-builds.md
│   │   ├── 0007-app-hardening.md
│   │   ├── 0008-multi-game-repo-structure.md
│   │   └── 0009-retain-csharp-backend.md
│   └── README.md                   # ADR index
├── infrastructure/                 # Docker & infrastructure
│   ├── docker-compose.yml          # Postgres, Redis, PgBouncer, Mailpit
│   └── Dockerfile.server           # Server production image
├── src/                            # Source code
│   ├── BlackoutClause.Shared/      # Shared DTOs, Enums, Constants (net8.0)
│   ├── BlackoutClause.Server/      # ASP.NET Core 8 Minimal API
│   │   ├── Configuration/          # AppSettings, ClerkSettings
│   │   ├── Domain/Entities/        # User, Subscription, ProcessedClerkWebhookEvent
│   │   ├── Infrastructure/         # Clerk (Auth, Billing), Data (EF Core), Redis
│   │   └── Endpoints/              # Auth, Game, Clerk Webhooks
│   └── BlackoutClause.Client/      # Godot 4 C# Project
│       ├── Scripts/Core/           # Global, Settings, Audio, Input
│       ├── Scripts/Networking/     # ApiClient, AuthTokenManager, WebSocketClient
│       ├── Scripts/Storage/        # SecureStorage (OS keystore), LocalDb (SQLite)
│       ├── Scripts/Subscription/   # SubscriptionManager, EntitlementGate
│       ├── Scripts/Gameplay/       # PlayerController, Weapons, Abilities, Match
│       ├── Scenes/                 # MainMenu, Gameplay, Auth, UI, HUD
│       └── ProjectSettings.godot   # Godot project config
├── BlackoutClause.sln              # Solution file
├── Directory.Build.props           # Shared MSBuild properties
├── Directory.Packages.props        # Centralized package versions
├── global.json                     # .NET SDK version pinning
├── CHANGELOG.md                    # Keep a Changelog format
├── blackout-clause-gdd.md          # Game Design Document
├── export_presets.cfg              # Godot export configurations
├── icon.svg                        # Game icon
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
| **Containerization | Docker (per-platform client + server) |
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

- [Game Design Document (GDD)](blackout-clause-gdd.md)
- [Architecture Decision Records (ADR)](docs/adr/)
- [Changelog](CHANGELOG.md)

## License

MIT License — See [LICENSE](../LICENSE) for details.

---

## Business Analysis & Market Research (VC Evaluation)

### 📊 TAM / SAM / SOM Analysis (2026)

| Metric | Value | Source & Methodology |
|--------|-------|---------------------|
| **TAM (Total Addressable Market)** | **$5.54B** | Global indie game market 2026 (Mordor Intelligence) |
| **SAM (Serviceable Addressable Market)** | **$687M** | PC indie FPS segment: TAM × 12.4% (PC share) × ~15% (FPS share of PC) |
| **SOM (Serviceable Obtainable Market)** | **$2.7M - $13.5M** | Year 1-3 realistic capture: 0.4-2% of SAM for differentiated indie FPS |

**FPS Genre Specifics (Steam 2024 cohort, 6+ months):**
- Median copies sold: **290** (vs 390 all genres)
- **32.2%** reach ≥1,000 copies | **12.7%** reach ≥10,000 | **5.7%** reach ≥100,000
- Higher *hit rate* at upper thresholds vs other genres despite lower median
- Key differentiators: **puzzle elements, psychedelic visuals, rich storytelling** → higher median revenue

**Subscription Revenue Potential (BlackoutClause Model):**
| Scenario | Subscribers | Monthly Revenue | Annual Revenue |
|----------|-------------|-----------------|----------------|
| Conservative | 500 | $4,995 | $59,940 |
| Base Case | 2,500 | $24,975 | $299,700 |
| Breakout | 10,000 | $99,900 | $1,198,800 |

---

### 💰 Development Cost Breakdown (2026 Estimates)

| Phase | Budget Range | Timeline | Notes |
|-------|-------------|----------|-------|
| **Vertical Slice / Prototype** | $20K - $50K | 2-4 months | Core loop, 1 map, 2 characters, basic netcode |
| **Pre-Production** | $50K - $100K | 3-4 months | Concept art, GDD finalization, tech stack validation |
| **Production (MVP - Early Access)** | $150K - $300K | 8-12 months | 4 maps, 4 characters, 6 weapons, matchmaking |
| **Full Launch (1.0)** | $100K - $200K | 4-6 months | Polish, 9 characters, 6 maps, all game modes |
| **LiveOps Year 1** | $80K - $150K | 12 months | Seasonal content, balance, server scaling |
| **Total (Solo/Small Team)** | **$400K - $800K** | **18-24 months** | Excludes marketing; uses Godot (no engine royalties) |

**Major Cost Drivers:**
- **Art & Animation (35-45%)**: 9 characters × $3K-8K = $27K-72K; environments × $2K-15K
- **Netcode/Backend (20-25%)**: Custom authoritative server, matchmaking, anti-cheat = $80K-150K
- **Engineering (20-25%)**: Gameplay systems, UI, tools, QA automation
- **Audio/Music (5-8%)**: SFX, voice lines, adaptive soundtrack
- **Marketing (100-200% of dev cost)**: $400K-1.6M for launch + Year 1 UA

**Godot Advantage**: No engine royalties vs Unity Pro ($2,040/seat/yr) or Unreal 5% >$1M → saves $20K-100K+ over project lifetime.

---

### 🎯 CAC / LTV Analysis (Break-Even Modeling)

**Assumptions (Conservative Indie FPS):**
- **Price**: $9.99/mo subscription (Pro) + $1 activation (7-day trial)
- **Churn**: 5% monthly (industry avg for sub games with good retention)
- **LTV** = (ARPU × Gross Margin) / Churn = ($9.99 × 0.7) / 0.05 = **$139.86**
- **Target CAC** = LTV / 3 = **$46.62** (3:1 LTV:CAC ratio for sustainable growth)

| Channel | Estimated CAC | Volume Potential | Notes |
|---------|---------------|------------------|-------|
| Steam Wishlist → Sale | $8-15 | High | 0.125 wishlist-to-sale conversion (2024) |
| Influencer / Streamer | $25-50 | Medium | Micro-influencers (10K-50K) best ROI |
| Paid Social (Meta/TikTok) | $30-80 | Scalable | Creative-dependent; test aggressively |
| Discord/Community | $5-10 | Organic | Lowest CAC, slowest scale |
| Referral / Viral | $0-5 | Organic | Built-in clan/org system drives this |

**Break-Even Scenarios:**
| Monthly Subs | Monthly Rev | Annual Rev | CAC Budget (30% Rev) | Payback Period |
|--------------|-------------|------------|---------------------|----------------|
| 500 | $4,995 | $59,940 | $1,498/mo | 31 months |
| 1,500 | $14,985 | $179,820 | $4,495/mo | 10 months |
| 3,000 | $29,970 | $359,640 | $8,991/mo | 6 months |
| 5,000 | $49,950 | $599,400 | $14,985/mo | 4 months |

**Key Insight**: At **~1,500 active subscribers**, the project covers ongoing dev + LiveOps + modest marketing. At **3,000+**, it becomes meaningfully profitable.

---

### 🚀 Go-to-Market Strategy

#### Phase 1: Stealth Build & Community Seeding (Months 1-6)
- [ ] Vertical slice playable (1 map, 2 characters, core gunplay)
- [ ] Private Discord (target: 500 members pre-EA)
- [ ] Devlogs on Twitter/X, Reddit (r/indiegames, r/godot), TIGSource
- [ ] Steam page live with "Coming Soon" (target: 2,000 wishlists before EA)
- [ ] Closed alpha weekends with content creators (10-20 micro-influencers)

#### Phase 2: Early Access Launch (Months 7-12)
- [ ] **Steam Early Access** at $9.99 (discounted from $14.99 launch)
- [ ] Target: **10,000 wishlists** at launch → ~$40K-100K first month
- [ ] 4 maps, 4 characters, ranked + casual, clan system
- [ ] Weekly patches, monthly content drops
- [ ] Review score target: **≥85% "Very Positive"**

#### Phase 3: 1.0 Launch & Scale (Months 13-18)
- [ ] Full 9 characters, 6 maps, all modes
- [ ] **Price increase to $14.99/mo** (grandfather EA players)
- [ ] Console ports evaluation (Switch first, then PS/Xbox)
- [ ] Paid UA scaling: target $15K-30K/mo spend at 3:1 ROAS
- [ ] Tournament support: $5K-10K prize pools quarterly

#### Phase 4: Live Service Maturity (Months 19-36)
- [ ] Seasonal battle passes (cosmetic only, no gameplay)
- [ ] Mod SDK release → UGC extends retention
- [ ] Regional server expansion (SA, SEA, MENA)
- [ ] Cross-play investigation (Godot 4.4+ WebRTC improvements)

---

### 📈 Execution Roadmap & Asset Production Phases

#### Phase 0: Foundation (Months 1-3) ✅ *Current*
- [x] Core architecture (Godot 4.3 / C#, ASP.NET Core 8, Clerk auth)
- [x] Server authoritative netcode, matchmaking, PgBouncer, Upstash Redis
- [x] Subscription system (Clerk), entitlement gating, secure storage
- [x] CI/CD (multi-platform), Docker, ADR documentation
- [ ] **Character controller** (movement, slide, vault, lean)
- [ ] **Weapon system** (ballistics, recoil patterns, attachment slots)
- [ ] **Ability framework** (cooldowns, resources, VFX hooks)

#### Phase 1: Vertical Slice - "The Arena" (Months 4-6)
| Asset Category | Target | Est. Cost | Priority |
|----------------|--------|-----------|----------|
| **Characters** | 2 (Vex, Breach) - blockout → high-poly → game-ready | $6K-16K | P0 |
| **Weapons** | 3 (AR, Shotgun, Sniper) - modeled, rigged, animated | $3K-9K | P0 |
| **Map** | 1 competitive map (Arena) - greybox → art pass | $8K-25K | P0 |
| **VFX** | Muzzle flashes, impacts, ability tells | $2K-5K | P1 |
| **Animations** | Locomotion set (walk, run, crouch, slide, vault, aim) | $4K-12K | P0 |
| **Audio** | Weapon SFX, footstep surfaces, UI | $1.5K-4K | P1 |

**Exit Criteria**: 2v2 deathmatch playable, 60fps on GTX 1060, <100ms server tick

#### Phase 2: Early Access Content (Months 7-12)
| Asset Category | Target | Est. Cost | Priority |
|----------------|--------|-----------|----------|
| **Characters** | +2 (Marrow, Bulwark) = 4 total | $6K-16K | P0 |
| **Weapons** | +3 (SMG, LMG, Pistol) = 6 total | $3K-9K | P0 |
| **Maps** | +3 (Control, Payload, Extraction) = 4 total | $24K-75K | P0 |
| **Game Modes** | Team Deathmatch, Control, Payload, Ranked | Engineering | P0 |
| **Clan System** | Org management, clan wars, shared progression | Engineering | P0 |
| **Day/Night Cycle** | Dynamic lighting per map (baked + realtime) | $3K-8K | P2 |
| **Weather** | Rain, fog, sandstorm (gameplay-affecting) | $5K-12K | P2 |
| **Cutscenes** | Faction intro (2 min), character select (30s each) | $8K-20K | P3 |

#### Phase 3: 1.0 Launch Polish (Months 13-18)
| Asset Category | Target | Est. Cost | Priority |
|----------------|--------|-----------|----------|
| **Characters** | +5 (Cinder, Forge, Suture, Reveille, Wraith) = 9 total | $15K-40K | P0 |
| **Maps** | +2 (Assault, Hybrid) = 6 total | $16K-50K | P0 |
| **Fight Scenes** | "Play of the Game" camera, killcams, highlight reel | $5K-15K | P1 |
| **Cutscenes** | Seasonal narrative (3-5 min per season) | $15K-35K | P2 |
| **Skins/Cosmetics** | Launch set: 9×3 weapon skins, 9×2 character skins | $10K-30K | P1 |
| **Animations** | Emotes, executions, idle variations, emoji wheel | $5K-15K | P2 |
| **Advanced VFX** | Ability ultimates, environmental destruction | $8K-20K | P2 |
| **Accessibility** | Colorblind, remapping, text-to-speech, UI scaling | Engineering | P1 |

#### Phase 4: LiveOps Year 1 (Months 19-30)
| Content | Cadence | Est. Cost/Season |
|---------|---------|------------------|
| **New Map** | Quarterly | $8K-20K |
| **New Character** | Bi-annual | $3K-8K |
| **Battle Pass** (cosmetics only) | Quarterly | $5K-15K |
| **Balance Patches** | Bi-weekly | Engineering |
| **Seasonal Events** | Quarterly (Holiday, Anniversary) | $3K-10K |
| **Map Variants** (Night/Rain versions) | Per season | $2K-5K |

---

### ⚠️ Remaining Technical Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Netcode desync at scale** | High | Critical | Server-authoritative + rollback; integrate Reactor/Coherence if custom fails |
| **Art pipeline bottleneck** | High | High | Lock asset manifest Month 3; modular kits; AI-assisted texturing |
| **Scope creep (feature bloat)** | Very High | Critical | Fixed scope per phase; "no" by default; vertical slice gates |
| **Godot 4.x C# limitations** | Medium | High | Track upstream; maintain C# abstraction layer for Unity pivot option |
| **Anti-cheat effectiveness** | High | Critical | Server-authoritative + heuristic detection; kernel-level only if necessary |
| **Server cost at scale** | Medium | High | Upstash Redis + PgBouncer + Aurora DSQL; bandwidth optimization |
| **Clerk dependency / pricing** | Low | Medium | Export user data monthly; maintain auth abstraction for migration |

---

### 📋 Feature Completeness Checklist (Project Readiness)

| System | Status | Notes |
|--------|--------|-------|
| **Core Architecture** | ✅ Done | Godot 4.3 C#, ASP.NET Core 8, Clerk, Docker |
| **Auth/Subscriptions** | ✅ Done | JWT, Clerk webhooks, entitlement gating, offline cache |
| **Netcode Framework** | 🟡 In Progress | Authoritative server, interpolation, lag compensation |
| **Matchmaking** | 🟡 In Progress | Skill-based, region-aware, party support |
| **Character Controller** | ⏳ Planned | Month 4-5 |
| **Weapon/Ballistics** | ⏳ Planned | Month 4-5 |
| **Ability System** | ⏳ Planned | Month 5 |
| **Map Pipeline** | ⏳ Planned | Greybox → art pass workflow |
| **Animation Pipeline** | ⏳ Planned | Mixamo base → custom layers |
| **VFX Pipeline** | ⏳ Planned | Godot particles + custom shaders |
| **Audio Pipeline** | ⏳ Planned | Wwise evaluation vs built-in |
| **Anti-Cheat** | ⏳ Planned | Server heuristics first; kernel last resort |
| **Mod SDK** | 📋 Backlog | Phase 4 |
| **Cross-Play** | 📋 Backlog | Godot 4.4+ / Console eval |

---

*Last updated: August 2026 | Based on VC-grade market research, indie dev cost surveys (StudioKrew, JuegoStudio, Pixune, Fyros), Steam revenue analysis (Steam Page Analyzer, Game Oracle, Profitable), and 2024-2026 FPS genre performance data.*