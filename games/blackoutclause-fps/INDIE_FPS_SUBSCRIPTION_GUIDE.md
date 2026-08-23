# Indie FPS with Subscription Model - Complete SDLC Tutorial

## Overview
This guide walks you through building a desktop indie FPS game with a subscription model:
- **$1.00 initial charge** (trial/activation)
- **$9.99/month recurring** for active subscribers

---

## 1. Technology Stack Decisions

### Game Engine: Godot 4.x (C#)
| Factor | Decision | Rationale |
|--------|----------|-----------|
| **Language** | C# (.NET 8) | Strong typing, great IDE support, shared logic with backend |
| **Engine** | Godot 4.x | Free, open-source, no royalties, excellent 3D, C# first-class |
| **Rendering** | Forward+ / Mobile | Scales from low-end to high-end PCs |
| **Physics** | Jolt (GodotPhysics) | Performant, deterministic for multiplayer later |

### Backend: Minimal API (ASP.NET Core 8)
| Component | Choice | Rationale |
|-----------|--------|-----------|
| **Framework** | ASP.NET Core Minimal APIs | Lightweight, fast, native AOT support |
| **Database** | SQLite (dev) → PostgreSQL (prod) | Zero-config dev, scales to managed cloud |
| **Auth** | JWT + Refresh Tokens | Stateless, works offline-first |
| **Payments** | Stripe Billing | Handles $1 trial + $9.99/mo natively, webhooks, portal |

### Desktop Packaging
| Platform | Tool |
|----------|------|
| Windows | `dotnet publish -r win-x64 --self-contained` + NSIS installer |
| macOS | `dotnet publish -r osx-arm64 --self-contained` + DMG |
| Linux | `dotnet publish -r linux-x64 --self-contained` + AppImage/Flatpak |

### CI/CD
- **GitHub Actions**: Build, test, sign, release on tag push
- **Artifacts**: Signed binaries per platform uploaded to GitHub Releases

---

## 2. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        DESKTOP CLIENT (Godot)                   │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Game Core   │  │ Auth/Sub    │  │ Local Cache (SQLite)    │  │
│  │ (FPS Logic) │◄─│ Manager     │──│ Offline play + sync     │  │
│  └─────────────┘  └──────┬──────┘  └─────────────────────────┘  │
│                         │ HTTPS (REST + WebSocket)               │
└─────────────────────────┼────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                      BACKEND API (ASP.NET Core)                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Auth Endpts │  │ Sub Endpts  │  │ Stripe Webhook Handler  │  │
│  │ (Register,  │  │ (Status,    │  │ (subscription.created,  │  │
│  │  Login,     │  │  Cancel,    │  │  updated, deleted,      │  │
│  │  Refresh)   │  │  Portal)    │  │  invoice.paid)          │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│         │               │                     │                  │
│         └───────────────┼─────────────────────┘                  │
│                         ▼                                        │
│              ┌─────────────────────┐                             │
│              │   PostgreSQL DB     │                             │
│              │ (Users, Subs,       │                             │
│              │  Entitlements)      │                             │
│              └─────────────────────┘                             │
└─────────────────────────────────────────────────────────────────┘
```

### Key Design Principles
1. **Offline-first**: Game plays fully offline; subscription checked on launch + periodic heartbeat
2. **Graceful degradation**: Expired sub → demo mode (limited levels), not hard lockout
3. **Stateless auth**: JWT in memory, refresh token in secure OS keystore (Windows Credential Manager, macOS Keychain, libsecret)
4. **Idempotent webhooks**: Stripe events processed exactly once via event ID dedup

---

## 3. Project Structure

```
IndieFps/
├── src/
│   ├── IndieFps.Client/           # Godot 4 C# project
│   │   ├── IndieFps.Client.csproj
│   │   ├── ProjectSettings.godot
│   │   ├── Scenes/
│   │   │   ├── Main/              # Main menu, loading
│   │   │   ├── Gameplay/          # FPS scenes (Player, Weapons, Enemies)
│   │   │   ├── UI/                # HUD, Menus, Subscription UI
│   │   │   └── Auth/              # Login, Register, Subscription status
│   │   ├── Scripts/
│   │   │   ├── Core/              # GameLoop, Input, Settings
│   │   │   ├── Gameplay/          # PlayerController, Weapon, Health, AI
│   │   │   ├── Networking/        # ApiClient, AuthTokenManager, WebSocket
│   │   │   ├── Subscription/      # SubscriptionManager, EntitlementChecker
│   │   │   └── Storage/           # LocalDb (SQLite), SecureStorage
│   │   └── Resources/             # Models, Textures, Shaders, Audio
│   │
│   ├── IndieFps.Server/           # ASP.NET Core Minimal API
│   │   ├── IndieFps.Server.csproj
│   │   ├── Program.cs
│   │   ├── Configuration/
│   │   │   └── AppSettings.cs
│   │   ├── Domain/
│   │   │   ├── Entities/          # User, Subscription, Entitlement
│   │   │   ├── Events/            # Domain events for webhooks
│   │   │   └── ValueObjects/      # SubscriptionTier, PaymentStatus
│   │   ├── Infrastructure/
│   │   │   ├── Data/              # DbContext, Migrations
│   │   │   ├── Auth/              # JwtService, RefreshTokenStore
│   │   │   ├── Payments/          # StripeService, WebhookVerifier
│   │   │   └── Email/             # Transactional emails (SendGrid/Resend)
│   │   ├── Application/
│   │   │   ├── Commands/          # RegisterUser, CancelSubscription
│   │   │   ├── Queries/           # GetSubscriptionStatus, GetEntitlements
│   │   │   └── Handlers/
│   │   └── Endpoints/
│   │       ├── AuthEndpoints.cs
│   │       ├── SubscriptionEndpoints.cs
│   │       └── WebhookEndpoints.cs
│   │
│   └── IndieFps.Shared/           # Shared DTOs, Enums, Constants
│       ├── IndieFps.Shared.csproj
│       ├── DTOs/
│       ├── Enums/
│       └── Constants/
│
├── infrastructure/
│   ├── docker-compose.yml         # Postgres, Redis (local dev)
│   ├── Dockerfile.server
│   ├── kubernetes/                # K8s manifests (prod)
│   └── terraform/                 # Cloud infra (optional)
│
├── build/
│   ├── build.ps1 / build.sh       # Cross-platform build script
│   ├── sign/                      # Code signing certs (gitignored)
│   └── packaging/                 # NSIS, DMG, AppImage scripts
│
├── tests/
│   ├── IndieFps.Client.Tests/     # Godot unit tests (GUT)
│   ├── IndieFps.Server.Tests/     # xUnit + TestContainers
│   └── IndieFps.Integration.Tests/# Playwright E2E (optional)
│
├── .github/
│   └── workflows/
│       ├── ci.yml                 # Build, test, lint
│       ├── cd.yml                 # Sign, package, release
│       └── security.yml           # Dependabot, codeql
│
├── docs/
│   ├── architecture.md
│   ├── api-contract.md
│   ├── subscription-flow.md
│   └── deployment.md
│
├── IndieFps.sln
├── Directory.Build.props          # Centralized .NET props
├── Directory.Packages.props       # Centralized package versions
├── global.json                    # .NET SDK version pin
├── .editorconfig
├── .gitignore
└── README.md
```

---

## 4. Subscription Flow Design

### Pricing Model
```
┌────────────────────────────────────────────────────────────┐
│                    SUBSCRIPTION TIERS                        │
├──────────────────┬──────────────────┬──────────────────────┤
│      TIER        │   FREE (Demo)    │      PRO ($9.99/mo)  │
├──────────────────┼──────────────────┼──────────────────────┤
│ Initial Charge   │ $0               │ $1.00 (trial auth)   │
│ Recurring        │ N/A              │ $9.99/month          │
│ Levels Access    │ 1 (Tutorial)     │ All (15+)            │
│ Multiplayer      │ ❌               │ ✅                    │
│ Cosmetics        │ ❌               │ ✅                    │
│ Mod Support      │ ❌               │ ✅                    │
│ Cloud Saves      │ ❌               │ ✅                    │
└──────────────────┴──────────────────┴──────────────────────┘
```

### State Machine
```
┌─────────┐     $1 auth      ┌──────────┐   $9.99/mo    ┌──────────┐
│ UNPAID  │ ──────────────► │  TRIAL   │ ────────────► │  ACTIVE  │
│ (Demo)  │   (7 days)      │ (Active) │  (recurring)  │ (Pro)    │
└─────────┘                  └──────────┘               └──────────┘
     ▲                           │                         │
     │                           │ cancel/fail             │ cancel/fail
     │                           ▼                         ▼
     │                    ┌──────────────┐          ┌──────────────┐
     └───────────────────│  PAST_DUE    │◄─────────│  CANCELLED   │
                          │ (Grace 14d)  │          │ (Access till │
                          └──────────────┘          │  period end) │
                             │                      └──────────────┘
                             │ fail
                             ▼
                       ┌──────────────┐
                       │  EXPIRED     │
                       │ (Back to     │
                       │  Demo mode)  │
                       └──────────────┘
```

### Stripe Configuration
```yaml
# Stripe Dashboard → Products → "IndieFps Pro"
Product: "IndieFps Pro"
  Price: $9.99/month (recurring)
  Trial Period: 7 days (handles $1 auth via setup_intent)
  
# Alternative: Two-price approach
Price 1: "IndieFps Activation" - $1.00 one-time (setup_intent)
Price 2: "IndieFps Pro Monthly" - $9.99/month (subscription)
```

---

## 5. Implementation Phases

### Phase 1: Foundation (Week 1-2)
- [ ] Solution structure + shared contracts
- [ ] Godot project setup with C#
- [ ] ASP.NET Core Minimal API + EF Core + PostgreSQL
- [ ] JWT auth (register, login, refresh)
- [ ] Local SQLite cache in client
- [ ] CI: Build both projects on PR

### Phase 2: Core Gameplay (Week 3-5)
- [ ] FPS controller (movement, look, jump, sprint)
- [ ] Weapon system (raycast/projectile, reload, ammo)
- [ ] Health/damage system
- [ ] Basic enemy AI (patrol, chase, attack)
- [ ] Level loading + scene management
- [ ] Settings (graphics, input, audio)

### Phase 3: Subscription Integration (Week 6-7)
- [ ] Stripe account + product/price setup
- [ ] Backend: Subscription endpoints + webhook handler
- [ ] Client: SubscriptionManager + EntitlementChecker
- [ ] Secure token storage (OS keystore)
- [ ] Offline entitlement cache + sync logic
- [ ] Demo mode gating (level access, features)

### Phase 4: Polish & Platform (Week 8-9)
- [ ] Main menu + auth UI + subscription UI
- [ ] Stripe Customer Portal integration (cancel, update payment)
- [ ] Code signing (Windows EV cert, Apple Developer ID)
- [ ] Installers: NSIS (Win), DMG (Mac), AppImage (Linux)
- [ ] Auto-updater (GitHub Releases polling)

### Phase 5: Launch Prep (Week 10)
- [ ] Load testing (k6/Gatling)
- [ ] Monitoring (OpenTelemetry → Grafana Cloud)
- [ ] Error tracking (Sentry)
- [ ] Analytics (custom events, privacy-first)
- [ ] Press kit, store pages (Steam, itch.io, website)
- [ ] Beta test → iterate

---

## 6. Key Code Patterns

### Shared DTOs (IndieFps.Shared)
```csharp
// DTOs/SubscriptionStatusDto.cs
public record SubscriptionStatusDto(
    string UserId,
    SubscriptionTier Tier,           // Free, Pro
    SubscriptionState State,         // Unpaid, Trial, Active, PastDue, Cancelled, Expired
    DateTime? CurrentPeriodEnd,
    DateTime? TrialEndsAt,
    bool HasActiveEntitlement,
    string[] Entitlements            // ["levels.all", "multiplayer", "cosmetics"]
);

// Enums/SubscriptionTier.cs
public enum SubscriptionTier { Free, Pro }

// Enums/SubscriptionState.cs
public enum SubscriptionState { Unpaid, Trial, Active, PastDue, Cancelled, Expired }
```

### Client: Subscription Manager
```csharp
// Scripts/Subscription/SubscriptionManager.cs
public partial class SubscriptionManager : Node
{
    private ApiClient _api;
    private SecureStorage _secureStorage;
    private LocalDb _localDb;
    private SubscriptionStatusDto _cachedStatus;
    private System.Timers.Timer _heartbeatTimer;

    public event Action<SubscriptionStatusDto> OnStatusChanged;

    public override void _Ready()
    {
        _api = GetNode<ApiClient>("/root/ApiClient");
        _secureStorage = GetNode<SecureStorage>("/root/SecureStorage");
        _localDb = GetNode<LocalDb>("/root/LocalDb");
        
        LoadCachedStatus();
        StartHeartbeat();
    }

    public async Task<SubscriptionStatusDto> CheckStatusAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedStatus != null && !IsCacheStale())
            return _cachedStatus;

        try
        {
            var token = await _secureStorage.GetAccessTokenAsync();
            var status = await _api.GetSubscriptionStatusAsync(token);
            _cachedStatus = status;
            await _localDb.UpsertSubscriptionStatusAsync(status);
            OnStatusChanged?.Invoke(status);
            return status;
        }
        catch (Exception ex)
        {
            GD.PushError($"Subscription check failed: {ex.Message}");
            return _cachedStatus ?? GetDemoStatus();
        }
    }

    public bool CanAccess(string entitlement) 
        => _cachedStatus?.Entitlements.Contains(entitlement) ?? false;

    public bool IsPro => _cachedStatus?.Tier == SubscriptionTier.Pro 
                      && _cachedStatus.State is SubscriptionState.Active or SubscriptionState.Trial;
}
```

### Server: Stripe Webhook Handler
```csharp
// Endpoints/WebhookEndpoints.cs
public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/stripe", async (HttpRequest req, StripeService stripe, AppDbContext db, ILogger<Program> log) =>
        {
            var json = await new StreamReader(req.Body).ReadToEndAsync();
            var signature = req.Headers["Stripe-Signature"];

            StripeEvent stripeEvent;
            try 
            {
                stripeEvent = stripe.ConstructEvent(json, signature);
            }
            catch (StripeException ex)
            {
                log.LogWarning(ex, "Invalid Stripe signature");
                return Results.BadRequest();
            }

            // Idempotency: check if already processed
            var exists = await db.ProcessedWebhookEvents.AnyAsync(e => e.StripeEventId == stripeEvent.Id);
            if (exists) return Results.Ok();

            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                await HandleEventAsync(stripeEvent, db, log);
                db.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent { StripeEventId = stripeEvent.Id, ProcessedAt = DateTime.UtcNow });
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Webhook handling failed for {EventId}", stripeEvent.Id);
                await tx.RollbackAsync();
                return Results.StatusCode(500);
            }

            return Results.Ok();
        }).DisableAntiforgery();
    }

    private static async Task HandleEventAsync(StripeEvent evt, AppDbContext db, ILogger log)
    {
        switch (evt.Type)
        {
            case "customer.subscription.created":
            case "customer.subscription.updated":
                await SyncSubscriptionAsync(evt.Data.Object as Subscription, db);
                break;
            case "customer.subscription.deleted":
                await CancelSubscriptionAsync(evt.Data.Object as Subscription, db);
                break;
            case "invoice.payment_succeeded":
                await HandlePaymentSuccessAsync(evt.Data.Object as Invoice, db);
                break;
            case "invoice.payment_failed":
                await HandlePaymentFailedAsync(evt.Data.Object as Invoice, db);
                break;
        }
    }

    private static async Task SyncSubscriptionAsync(Subscription sub, AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == sub.CustomerId);
        if (user == null) return;

        var entitlements = sub.Items.Data
            .Select(i => i.Price.Metadata.GetValueOrDefault("entitlement"))
            .Where(e => !string.IsNullOrEmpty(e))
            .ToArray();

        var status = sub.Status switch
        {
            "trialing" => SubscriptionState.Trial,
            "active" => SubscriptionState.Active,
            "past_due" => SubscriptionState.PastDue,
            "canceled" => SubscriptionState.Cancelled,
            _ => SubscriptionState.Unpaid
        };

        user.Subscription = new UserSubscription
        {
            StripeSubscriptionId = sub.Id,
            Tier = entitlements.Contains("pro") ? SubscriptionTier.Pro : SubscriptionTier.Free,
            State = status,
            CurrentPeriodEnd = DateTimeOffset.FromUnixTimeSeconds(sub.CurrentPeriodEnd).UtcDateTime,
            TrialEndsAt = sub.TrialEnd.HasValue ? DateTimeOffset.FromUnixTimeSeconds(sub.TrialEnd.Value).UtcDateTime : null,
            Entitlements = entitlements
        };

        await db.SaveChangesAsync();
    }
}
```

### Godot: Entitlement Gate
```csharp
// Scripts/Subscription/EntitlementGate.cs
[GlobalClass]
public partial class EntitlementGate : Node
{
    [Export] public string RequiredEntitlement { get; set; } = "";
    [Export] public NodePath TargetNodePath { get; set; }
    [Export] public bool DisableInsteadOfHide { get; set; } = false;

    private SubscriptionManager _subManager;
    private Node _targetNode;

    public override void _Ready()
    {
        _subManager = GetNode<SubscriptionManager>("/root/SubscriptionManager");
        _targetNode = GetNodeOrNull(TargetNodePath) ?? this;
        
        _subManager.OnStatusChanged += OnStatusChanged;
        ApplyGate(_subManager.GetCachedStatus());
    }

    private void OnStatusChanged(SubscriptionStatusDto status)
        => ApplyGate(status);

    private void ApplyGate(SubscriptionStatusDto status)
    {
        var hasAccess = string.IsNullOrEmpty(RequiredEntitlement) 
            || status?.Entitlements.Contains(RequiredEntitlement) == true;

        if (DisableInsteadOfHide)
            _targetNode.Set("disabled", !hasAccess);
        else
            _targetNode.Visible = hasAccess;
    }
}
```

Usage in scene: Attach `EntitlementGate` to a level portal node, set `RequiredEntitlement = "levels.all"`

---

## 7. Security Checklist

| Area | Implementation |
|------|----------------|
| **Auth** | Argon2id password hash, JWT RS256, short access (15m) + long refresh (30d) |
| **Token Storage** | OS keystore (Windows: CredMan, macOS: Keychain, Linux: libsecret) |
| **API** | HTTPS only, rate limiting (100/min), CORS locked to game domain |
| **Stripe** | Webhook signature verification, idempotency keys, secret in env |
| **Game** | No client-side authority on entitlements; server validates on multiplayer join |
| **PII** | Minimal collection (email only), GDPR delete endpoint |
| **Updates** | Signed manifests, signature verification before apply |

---

## 8. Economics & Scaling

### Cost Breakdown (Monthly, 1,000 Subscribers)
| Service | Cost |
|---------|------|
| Stripe Fees (2.9% + $0.30) | ~$330 |
| Server (2x CPU, 4GB RAM, managed PG) | ~$50 |
| CDN / Game Downloads | ~$20 |
| Monitoring (Grafana Cloud free tier) | $0 |
| Error Tracking (Sentry free) | $0 |
| **Total** | **~$400/mo** |

### Break-even
- **$9.99 × 1,000 = $9,990 revenue**
- **~$400 costs = 96% margin**
- Break-even at **~41 subscribers**

### Scaling Triggers
| Metric | Threshold | Action |
|--------|-----------|--------|
| Concurrent players | >500 | Add read replica, Redis cache |
| API latency p99 | >500ms | Horizontal pod autoscaler |
| DB connections | >80% | Connection pooling (PgBouncer) |
| Webhook queue | >100 pending | Background worker (Hangfire) |

---

## 9. Local Development Setup

```bash
# Prerequisites
# - .NET 8 SDK
# - Godot 4.3+ (C# edition)
# - Docker Desktop
# - Stripe CLI (for webhook testing)

# 1. Clone & restore
git clone <repo>
cd IndieFps
dotnet restore

# 2. Start infrastructure
cd infrastructure
docker-compose up -d  # Postgres, Redis

# 3. Configure secrets (User Secrets / .env)
cd ../src/IndieFps.Server
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 64)"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=indiefps;Username=postgres;Password=postgres"

# 4. Run migrations
dotnet ef database update

# 5. Start backend
dotnet run --urls "https://localhost:5001;http://localhost:5000"

# 6. Start Stripe webhook tunnel (separate terminal)
stripe listen --forward-to localhost:5000/webhooks/stripe

# 7. Open Godot project in src/IndieFps.Client and run
```

---

## 10. Deployment Checklist

### Pre-launch
- [ ] Stripe live mode activated, webhook endpoint registered
- [ ] Code signing certificates installed in CI
- [ ] Production database provisioned (managed PG)
- [ ] Domain + TLS (Cloudflare / Let's Encrypt)
- [ ] Sentry DSN configured
- [ ] Grafana dashboards imported
- [ ] Legal: ToS, Privacy Policy, Refund Policy
- [ ] Steam/itch.io store pages ready

### Launch Day
- [ ] Tag release: `git tag v1.0.0 && git push origin v1.0.0`
- [ ] Verify GitHub Action builds + signs all 3 platforms
- [ ] Verify installers launch correctly on clean VMs
- [ ] Monitor Stripe dashboard for first payments
- [ ] Monitor error rates in Sentry
- [ ] Monitor API latency in Grafana

---

## 11. Next Steps for You

1. **Pick your engine**: Godot (recommended) vs Unity vs Unreal
2. **Create Stripe account** → Get test keys
3. **Initialize the solution** with the structure above
4. **Implement Phase 1** - foundation only, no gameplay yet
5. **Test the full loop**: Register → $1 auth → Subscribe → Play → Cancel

Would you like me to:
- **Generate the starter solution** with all projects, shared contracts, and CI?
- **Deep-dive into any specific phase** (e.g., Godot FPS controller, Stripe webhook handling)?
- **Create the Docker/Infrastructure setup** for local dev?

Let me know which piece to build first.