# ADR-0007: App Hardening Against Game Exploits (2025/2026)

## Status
Accepted

## Context
As of 2025/2026, common game exploits include:
- **Memory manipulation**: Speed hacks, aimbots, wallhacks via memory scanning/injection
- **Network manipulation**: Packet injection, replay attacks, state desync
- **Input automation**: Bots, trigger bots, recoil control scripts
- **Client authority abuse**: Trusting client for hit detection, movement, economy
- **DLL injection**: Code injection via malicious DLLs
- **Debugger attachment**: Runtime inspection/modification
- **File tampering**: Modified assets, configs, save files
- **Time manipulation**: Speeding up/slowing down game clock
- **API abuse**: Direct backend API calls bypassing game logic
- **Cheat engines**: Cheat Engine, WeMod, custom trainers

## Decision
Implement defense-in-depth hardening across client, server, and network layers:

### Client-Side (Godot)
- **Integrity verification**: Checksum critical assets at startup
- **Anti-debug**: Detect debugger attachment, exit or degrade gracefully
- **Anti-tamper**: Verify assembly signatures, detect DLL injection
- **Memory protection**: Use Godot's built-in bounds checking, avoid unsafe code
- **Input validation**: Sanitize all network-received data before use
- **Obfuscation**: Use .NET obfuscation (not suppression) for release builds
- **Secure storage**: OS keystore for tokens (already implemented)

### Server-Side (ASP.NET Core)
- **Authoritative simulation**: Server is source of truth for movement, combat, economy
- **Input validation**: Validate all client inputs server-side (range, rate, sequence)
- **Rate limiting**: Per-endpoint and per-user limits
- **Anti-replay**: Nonce/timestamp on state-changing requests
- **Encryption**: TLS 1.2+ for all traffic, encrypt sensitive payloads
- **Audit logging**: Security-relevant events (login, purchases, admin actions)

### Network
- **Deterministic lockstep or server-authoritative**: No client trust for gameplay
- **Packet encryption**: DTLS or application-layer encryption for UDP
- **Sequence numbers**: Detect dropped/reordered/replayed packets
- **Heartbeat/time sync**: Detect speed hacks via server time comparison

### Build/Deploy
- **Code signing**: Windows (EV cert), macOS (notarization), iOS (App Store), Android (Play Signing)
- **Reproducible builds**: Verify build artifacts match source
- **Dependency scanning**: CI scans for CVEs (CodeQL, dotnet list package --vulnerable)
- **Minimal attack surface**: Disable unused Godot modules, strip debug symbols in release

## Consequences

### Positive
- Significantly raises bar for cheat developers
- Protects competitive integrity and economy
- Complies with platform store requirements (Apple/Google/Steam)
- Reduces support burden from hacked accounts

### Negative
- Development time for anti-cheat systems
- Potential false positives (legitimate players flagged)
- Cannot prevent all cheating (determined attackers will succeed)
- Obfuscation makes debugging production issues harder
- Performance overhead from validation/encryption

### Neutral
- No silver bullet - layer defenses, assume breach
- Monitor, detect, ban workflow essential
- Community reporting + server-side analytics > client-only anti-cheat

## Alternatives Considered
- **Kernel-level anti-cheat (BattlEye, EAC, Vanguard)**: Effective but invasive, privacy concerns, Linux/macOS support limited, expensive
- **Client-only anti-cheat**: Easily bypassed, security theater
- **No anti-cheat**: Not viable for competitive FPS with economy

## References
- [OWASP Game Security](https://owasp.org/www-project-game-security/)
- [.NET Security Best Practices](https://learn.microsoft.com/dotnet/standard/security/)
- [Godot Security](https://docs.godotengine.org/en/stable/tutorials/security/index.html)
- [Valve Anti-Cheat Research](https://www.valvesoftware.com/en/anti-cheat)