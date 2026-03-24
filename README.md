# OpenDriverUpdater

<p align="center">
  <img src="img/banner_odu.jpg" alt="OpenDriverUpdater Banner" width="800" />
</p>

### Safe. Open. Transparent.
The Driver Updater Windows Actually Needs.

---

**OpenDriverUpdater** is a free, open-source Windows driver management tool designed with a focus on safety, transparency, and user control. It scans your hardware, finds official driver updates from trusted sources like the Microsoft Catalog, and lets you install them with confidence.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3-0078D4.svg)](https://learn.microsoft.com/en-us/windows/apps/winui/)

---

## ✨ Features

* 🔍 **Automatic Hardware Detection**: Scans devices using WMI and PnP APIs.
* 🧠 **Smart Driver Matching**: Precise matching based on Hardware IDs.
* 🛡️ **Safe Recommendations**: Multi-layer trust scoring for every candidate.
* 🌐 **Official Sources**: Queries Microsoft Update Catalog, Intel, AMD, and NVIDIA.
* 🖥️ **Modern UI**: Native WinUI 3 interface with a premium dark theme.
* 🧰 **CLI Support**: Full terminal interface for advanced users and automation.
* 🚫 **Zero Bloat**: No ads, no tracking, no bundled software.

---

## 🧱 Architecture

```
OpenDriverUpdater.sln
├── src/
│   ├── OpenDriverUpdater.Core          # Pure business logic (Models, Interfaces, Services, Rules)
│   ├── OpenDriverUpdater.Infrastructure # Windows API, Networking, Storage, Security
│   ├── OpenDriverUpdater.App           # WinUI 3 Desktop Interface (MVVM)
│   └── OpenDriverUpdater.Cli           # Console Interface
└── tests/
    ├── OpenDriverUpdater.Core.Tests
    └── OpenDriverUpdater.Infrastructure.Tests
```

### Layer Breakdown

| Layer | Purpose | Dependencies |
|-------|---------|-------------|
| **Core** | Business logic, models, and interfaces | None |
| **Infrastructure** | Platform-specific implementations (WMI, PnP, SQLite) | Core |
| **App** | Modern desktop UI using WinUI 3 | Core, Infrastructure |
| **CLI** | Terminal tool using System.CommandLine | Core, Infrastructure |

---

## 🚀 Quick Start

```powershell
# Build the Core, Infrastructure, and CLI
dotnet build OpenDriverUpdater.sln

# Run the CLI
dotnet run --project src/OpenDriverUpdater.Cli -- scan

# Run Unit Tests
dotnet test
```

> [!IMPORTANT]
> **WinUI 3 App**: To build and run the desktop application (`OpenDriverUpdater.App`), use **Visual Studio 2022** with the **Windows app development** workload (Windows App SDK) installed. The standard .NET CLI does not natively support building WinUI 3 projects without additional SDK components.


## 🛠️ CLI Commands

| Command | Description |
|---------|-------------|
| `scan` | Scan system for devices |
| `list-devices` | List detected hardware with details |
| `check-updates` | Search for available driver updates |
| `download <id>` | Download a specific driver package |
| `install <path>` | Install a driver using pnputil |
| `export-json` | Export scan results to JSON |

---

## 🗺️ MVP Roadmap

### Phase 1 ✅
- Device scanning (WMI + PnP)
- Hardware ID extraction & matching
- Microsoft Catalog integration
- Native WinUI 3 & CLI interfaces

### Phase 2
- Secure download manager with SHA256 verification
- Authenticode signature validation
- Advanced Trust Scoring engine
- JSON metadata caching

### Phase 3
- Automated installation via `pnputil`
- System restore point integration
- OEM source expansion (Intel, AMD, NVIDIA)
- SQLite local database for history

---

## 🤝 Contributing

We welcome contributions! Please feel free to open issues or submit pull requests.

## 📜 License

MIT © OpenDriverUpdater Contributors
