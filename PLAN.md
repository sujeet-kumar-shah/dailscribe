# DayScribe — Scaling Plan

## Target
- Both individual knowledge workers AND teams/businesses (freemium + paid tiers)
- Hybrid privacy model: local-first core with optional cloud sync, AI, and team features
- Platforms: Desktop (Windows, Mac, Linux) + Mobile (iOS, Android)

---

## Current Architecture Limitations

| Area | Current | Problem |
|------|---------|---------|
| Platform | WPF + P/Invoke (`user32.dll`) | Windows-only, blocks Mac/Linux/mobile |
| UI | Blazor Interactive Server | Requires constant SignalR — poor for mobile |
| Data | Local SQLite only | No sync, no team access |
| Auth | None | No multi-user support |
| AI | Ollama/OpenAI on device | Slower, limited by local hardware |
| Distribution | Manual build | No CI/CD, no auto-update |

---

## Recommended Evolution

### 1. Cross-Platform Client — MAUI

Replace WPF with **.NET MAUI** to target Windows, macOS, iOS, and Android from a single codebase.

```
Current:                  Proposed:
WPF + WebView2            MAUI Blazor Hybrid (WebView + native)
├── Blazor UI             ├── Blazor UI (reuse Components/)
├── P/Invoke tracking     ├── IActivityTracker abstraction
└── Windows only          ├── Win: current P/Invoke
                          ├── Mac: NSWorkspace / CGWindowList
                          ├── Linux: X11/D-Bus
                          └── Mobile: Screen Time / UsageStats
```

**Reuse path:** All Blazor components, Services, and EF Core code can be directly carried over to MAUI Blazor Hybrid.

### 2. Cloud Backend — ASP.NET Core API

```
┌─────────────┐     ┌──────────────┐     ┌───────────┐
│ Desktop App  │────▶│ Cloud API    │────▶│ PostgreSQL│
│ (MAUI)       │     │ (ASP.NET     │     ├───────────┤
├─────────────┤     │  Core +      │     │ Redis     │
│ Mobile App   │────▶│  SignalR)    │     └───────────┘
├─────────────┤     └──────┬───────┘
│ Browser Ext │────▶       │
└─────────────┘           │ sync + auth + team features
                    ┌──────▼───────┐
                    │ Auth0 /      │
                    │ Firebase Auth│
                    └──────────────┘
```

### 3. Data Sync Model (Local-First)

```
Device                 Cloud
┌──────────────────┐   ┌─────────────────┐
│ SQLite (primary) │   │ PostgreSQL       │
│                  │   │                  │
│ Append-only log  │──▶│ User account     │
│ ActivityEvents   │   │ ActivityEvents   │
│                  │◀──│ Shared team data │
│ Synced cache     │   │ Team digests     │
└──────────────────┘   └─────────────────┘
```

- Always write locally first (no network dependency)
- Background sync when online (changeset-based, not full DB)
- Conflict resolution: per-row last-write-wins with server timestamp
- Encryption: client-side encrypt sensitive fields (URLs, titles) before sync

### 4. Platform-Specific Tracking

| Platform | API | Notes |
|----------|-----|-------|
| Windows | `user32.dll` (keep current) | Already implemented |
| macOS | `NSWorkspace`, `CGWindowListCreate` | Requires native interop |
| Linux | X11 `_NET_ACTIVE_WINDOW`, wayland protocols | `libX11` interop |
| iOS | `familyActivity` / Screen Time API | Native Swift via MAUI |
| Android | `UsageStatsManager`, `AccessibilityService` | Android SDK via MAUI |

### 5. AI Evolution

```
Phase 1: On-device (current) → Ollama / local LLM
Phase 2: Cloud AI (optional upgrade) → GPT-4o / Claude via cloud API
Phase 3: Fine-tuned models → Product-specific small model for summarization
```

---

## Implementation Phases

### Phase 1: Cross-Platform Foundation (3-4 months)

- Migrate WPF → MAUI Blazor Hybrid (reuse all Razor components)
- Extract `IActivityTracker` into platform-specific implementations (Win/Mac/Linux)
- Switch from `AddDbContextFactory` + manual config to proper DI with `DbContextOptions`
- Set up CI/CD (GitHub Actions for Windows, Mac, Linux builds)
- Implement auto-update (Sparkle for Mac, Squirrel for Windows)

### Phase 2: Cloud Platform (2-3 months)

- ASP.NET Core Web API project
- PostgreSQL + Entity Framework Core
- User auth: Auth0 or Firebase Authentication
- Sync protocol: changeset-based REST endpoints
- Browser extension points at cloud API (auth with API key)
- Team/workspace data model

### Phase 3: Team & Collaboration Features (2-3 months)

- Organization CRUD + member management
- Team dashboard (aggregated activity)
- Shared daily digests
- Role-based access (admin, member, viewer)
- Stripe billing integration

### Phase 4: Mobile (2-3 months)

- MAUI iOS + Android projects
- Mobile-specific tracking (screen time / usage stats)
- Push notifications
- Offline-first with cloud sync

### Phase 5: Scale (ongoing)

- Product analytics (PostHog or self-hosted Plausible)
- Marketing site + docs
- Enterprise: on-premise deployment option
- GDPR/DPA compliance

---

## Key Technical Decisions

| Decision | Recommendation | Rationale |
|----------|---------------|-----------|
| Client framework | MAUI Blazor Hybrid | Reuses existing Blazor components and C# code |
| Cloud DB | PostgreSQL | Relational + JSON support |
| Auth | Auth0 | Social login, SSO, good .NET SDK |
| Sync strategy | Local-first CRDT-like | Offline-capable by default |
| Payments | Stripe | Standard for SaaS |
| Hosting | Azure App Service / AWS ECS | .NET-native platform |

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Mac/Linux tracking APIs limited | Start with per-process polling, improve later |
| Mobile tracking restrictive (iOS) | Screen Time API, fall back to manual logging |
| MAUI maturity concerns | Blazor UI portable; MAUI is just the host shell |
| Privacy perception | Open-source core, E2E encryption for synced data |
| SQLite sync complexity | Append-only event log with server watermark |
