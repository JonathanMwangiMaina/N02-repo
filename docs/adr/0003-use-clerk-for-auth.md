# ADR-0003: Use Clerk for Authentication, Multi-tenancy, and Billing

## Status
Accepted

## Context
We need authentication, multi-tenancy, and billing for BlackoutClause with:
- User registration, login, password reset, MFA
- Organization/team support (multi-tenancy for clans/guilds)
- Subscription billing with trials, upgrades, downgrades, cancellations
- Customer portal for self-service
- Webhooks for real-time subscription state sync
- SOC 2 compliance, GDPR ready
- Minimal custom auth code to maintain

## Decision
Use Clerk as the identity and billing platform, replacing the previous Stripe-only approach.

## Consequences

### Positive
- Complete auth solution (signup, signin, MFA, passwordless, social auth)
- Built-in multi-tenancy (organizations) for clans/guilds
- Integrated billing with Stripe backend (no direct Stripe integration needed)
- Pre-built UI components (React, Next.js, HTML) - adaptable for Godot via web views
- Webhooks for real-time events (user.created, subscription.updated, etc.)
- User management dashboard for support
- Session management with JWT tokens
- Device fingerprinting, bot detection
- Free tier generous for indie development
- Reduces custom auth code by ~80%

### Negative
- Vendor lock-in (though data export available)
- Monthly cost at scale (but cheaper than building/maintaining)
- Web view authentication in Godot requires custom implementation
- Less control over auth flows vs custom implementation
- Dependency on external service availability

### Neutral
- Need to implement Clerk webhook handlers in ASP.NET Core
- Godot client needs HTTP client for Clerk API calls
- Migration from custom JWT/Stripe requires careful transition

## Alternatives Considered
- **Custom JWT + Stripe**: Full control but months of development (previous approach)
- **Auth0**: Enterprise-focused, expensive, no built-in billing
- **Firebase Auth**: Good auth but no billing, Google lock-in
- **Supabase Auth**: Good but billing requires separate Stripe integration
- **AWS Cognito**: Complex setup, no built-in billing or multi-tenancy UI

## References
- [Clerk Documentation](https://clerk.com/docs)
- [Clerk Billing](https://clerk.com/docs/billing/overview)
- [Clerk Organizations (Multi-tenancy)](https://clerk.com/docs/organizations/overview)
- [Clerk Webhooks](https://clerk.com/docs/webhooks/overview)