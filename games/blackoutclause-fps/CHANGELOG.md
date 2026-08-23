# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- BlackoutClause FPS game project structure with Godot 4.3 / C# client and ASP.NET Core 8 server
- GDD (Game Design Document) for BlackoutClause sci-fi mercenary hero shooter
- Shared library for DTOs, enums, and constants between client and server
- JWT-based authentication with Argon2id password hashing and refresh token rotation
- Subscription management system with Clerk integration (auth, multi-tenancy, billing)
- Secure token storage using OS keystore (DPAPI/Keychain/libsecret)
- Offline-first SQLite cache for subscription entitlements
- Automated token refresh and heartbeat subscription sync
- Entitlement gating system for game features
- Cross-platform build scripts (Windows, macOS, Linux)
- Docker infrastructure for server deployment (PostgreSQL, Redis, Mailpit)
- GitHub Actions CI/CD pipeline with CodeQL security scanning
- Platform-specific build configurations for Windows, macOS, Linux, iOS, Android
- App hardening against 2025/2026 game exploits (anti-cheat, input validation, rate limiting)

### Changed
- Renamed project from IndieFps to BlackoutClause across all projects and namespaces
- Replaced Stripe payment integration with Clerk for auth, multi-tenancy, and billing
- Disabled code trimming (PublishTrimmed=false) for Godot reflection compatibility
- Updated CI/CD to build for all target platforms with zero warnings/errors target
- Restructured repository for multi-game support (Simon game + BlackoutClause)

### Security
- Implemented input validation and sanitization on all network endpoints
- Added rate limiting on authentication endpoints
- Enforced HTTPS/TLS 1.2+ in production
- Non-root Docker containers with read-only filesystem where possible
- CodeQL static analysis integrated in CI pipeline
- Dependency vulnerability scanning in CI pipeline

## [1.0.0] - 2026-08-23

### Added
- Initial BlackoutClause project structure
- GDD documentation (blackout-clause-gdd.md)
- Basic client/server architecture with Godot 4.3 and ASP.NET Core 8
- Subscription-based monetization model design