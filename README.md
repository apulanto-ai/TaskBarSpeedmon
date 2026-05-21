# TaskBarSpeedmon

A lightweight Windows system tray app that displays real-time network download and upload speeds directly in the taskbar notification area.

![Windows 11](https://img.shields.io/badge/Windows-11-0078d4?logo=windows)
![.NET Framework 4](https://img.shields.io/badge/.NET%20Framework-4-512BD4)
![C#](https://img.shields.io/badge/C%23-5.0-239120?logo=csharp)

## Features

- **Live tray icon** — green `↓` for download, orange `↑` for upload, updates every second
- **Tooltip** — shows both speeds at a glance: `↓ 1.2 MB/s  ↑ 0.3 MB/s`
- **Right-click menu** — live speed readout, settings, autostart toggle, exit
- **Settings dialog** — customize colors, font size, icon size with live preview
- **Persistent settings** — saved to `%APPDATA%\TaskBarSpeedmon\settings.ini`
- **Autostart** — optional registry entry under `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`

## Requirements

- Windows 10 / 11
- .NET Framework 4 (pre-installed on Windows 10+)

## Build

No SDK or NuGet required. Uses only the .NET Framework bundled with Windows:

```powershell
.\build.ps1
```

This calls `csc.exe` from `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\` directly.

## Usage

Run `TaskBarSpeedmon.exe`. The icon appears in the system tray. Right-click for options.

To build a release binary, run `build.ps1` — output is a single ~21 KB `.exe`.

## Resource usage

- RAM: ~30 MB
- CPU: negligible (one timer tick per second)
- No background services, no installers, no dependencies

---

> **Disclaimer — Vibe Coded**
>
> This project was built through an iterative, conversational process with an AI assistant (Claude). The code works and has been tested on Windows 11, but it was not written by a human developer following a formal design process. It may contain unconventional patterns, non-idiomatic C#, or areas that could be improved by a code review. Use at your own discretion. Contributions and cleanups are welcome.
