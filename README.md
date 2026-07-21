# DayScribe

A local-first, privacy-preserving desktop activity tracker with AI-powered daily digests.

Tracks active window usage, monitors browser activity via a Chrome extension, and generates intelligent daily summaries using local LLMs (Ollama) or OpenAI.

## Features

- **Active Window Tracking** — Logs foreground application and window title at 2s intervals with idle detection
- **Browser Extension** — Chrome extension reports active URLs, tab titles, and time spent per page (including SPA navigation)
- **AI Daily Digest** — Summarizes your day: top apps, browsed domains, and article takeaways
- **Article Summarization** — Automatically fetches and summarizes long-form articles (via SmartReader) every 15 minutes
- **Dashboard** — Visual breakdown of time spent across apps and domains with live updates
- **AI Chat** — Ask natural-language questions about your day
- **100% Local** — No telemetry, no cloud dependency. All data stays on your machine
- **System Tray** — Minimizes to tray, continues tracking in background

## Quick Start — Standalone .exe (no .NET needed)

Build a self-contained single-file executable that runs on any Windows 10/11 machine:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

Output: `bin/Release/net10.0-windows/win-x64/publish/DayScribe.exe` (~180 MB)

Double-click `DayScribe.exe` — no .NET runtime or SDK required.

## Prerequisites for Development

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (pre-installed on Windows 10/11)
- Chrome/Edge browser (for the extension)

## Getting Started

### 1. Clone and build

```bash
git clone https://github.com/sujeet-kumar-shah/dailscribe.git
cd dailscribe
dotnet build
```

### 2. Configure (optional)

Edit `appsettings.json` to set the API port and AI provider:

```json
{
  "AppConfig": {
    "LocalApiPort": 9103,
    "Ollama": {
      "Endpoint": "http://localhost:11434/api/generate",
      "Model": "llama3.2:1b"
    },
    "OpenAI": {
      "ApiKey": ""
    }
  }
}
```

- **Ollama** (default) — works out of the box if Ollama is running locally
- **OpenAI** — set `AppConfig:OpenAI:ApiKey` for GPT-4o-mini fallback

### 3. Run

```bash
dotnet run
```

A WPF window opens with the Blazor dashboard served from `http://localhost:9103`.

### 4. Load the Chrome extension

1. Open Chrome → `chrome://extensions`
2. Enable **Developer mode**
3. Click **Load unpacked** → select the `Extension/` folder
4. Pin the DayScribe extension icon

The extension starts reporting active URLs and time spent to the local API.

## Architecture

```
DayScribe/
├── App.xaml / App.xaml.cs          # WPF application entry point
├── MainWindow.xaml / .cs           # WPF window with WebView2 hosting Blazor UI
├── Program.cs                      # ASP.NET Core host + Blazor + API setup
├── Components/
│   ├── App.razor                   # Blazor root component (HTML shell)
│   ├── Routes.razor                # Page router
│   ├── Layout/MainLayout.razor     # Sidebar + nav layout
│   └── Pages/
│       ├── Dashboard.razor         # Home — stats, charts, AI chat
│       ├── DailyDigest.razor       # AI digest + article summarization
│       └── ActivityLog.razor       # Raw activity log viewer
├── Services/
│   ├── ActivityTrackerService.cs   # Background window tracker (P/Invoke)
│   ├── DailyDigestService.cs       # Stats aggregation + LLM digest generation
│   ├── ArticleSummarizerService.cs # URL fetch + LLM summarization (auto-runs every 15 min)
│   └── IActivityTracker.cs         # Tracker interface
├── Database/
│   ├── DayScribeDbContext.cs        # EF Core SQLite context
│   └── Models/
│       ├── ActivityLogEntry.cs      # Active window log
│       ├── BrowserEvent.cs          # Browser tab event
│       └── ArticleSummary.cs        # Article + AI summary
├── Extension/
│   ├── manifest.json               # Chrome extension v3 manifest
│   ├── content.js                  # Content script (tracks tabs, SPA navigation)
│   └── background.js               # Extension service worker
├── website/                         # Standalone marketing website (separate .csproj)
│   ├── Pages/                       # Razor Pages (Index, Features, Pricing, Download, Blog, Contact)
│   ├── Services/                    # BlogService, EmailSender
│   └── wwwroot/                    # CSS, JS, download binaries
├── Migrations/                     # EF Core migrations
└── wwwroot/
    └── css/site.css                # Custom Blazor UI styles
```

## API

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/browser/activity` | Receive browser activity from the extension |

## Tech Stack

- **.NET 10** — ASP.NET Core + Blazor Interactive Server
- **WPF** — Desktop shell with WebView2
- **Entity Framework Core** + **SQLite** — Local data storage
- **SmartReader** — Readability/article extraction
- **Ollama / OpenAI** — AI summarization
- **Tailwind CSS** (via CDN) — Blazor UI styling
- **H.NotifyIcon.Wpf** — System tray integration
- **Bootstrap 5** — Marketing website UI

## Project Structure

| Directory | Description |
|-----------|-------------|
| `DayScribe/` | Main desktop WPF application |
| `DayScribe/website/` | Marketing website (Razor Pages, separate build) |
| `DayScribe/Extension/` | Chrome extension for browser tracking |

The website under `website/` has its own `.csproj` and is excluded from the main app build. Build it separately:

```bash
cd website
dotnet build
dotnet run
```
