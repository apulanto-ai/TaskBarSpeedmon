# TaskBarSpeedmon

Eine schlanke Windows-App, die die aktuelle Download- und Upload-Geschwindigkeit **direkt auf der Taskleiste** anzeigt, groß und gut lesbar, links neben dem Infobereich.

![Windows 11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078d4?logo=windows)
![.NET Framework 4](https://img.shields.io/badge/.NET%20Framework-4-512BD4)
![C#](https://img.shields.io/badge/C%23-5.0-239120?logo=csharp)

```
                                   ↓   12 MB/s
  [Start] [Apps …]                 ↑  843 KB/s    ^  🔊  14:32
```

## Funktionen

- **Anzeige auf der Taskleiste**: zwei Zeilen (oder eine) mit echter Schriftgröße statt eines winzigen 16×16-Tray-Icons, mit transparentem Hintergrund und passend zur Taskleiste
- **Hell/Dunkel automatisch**: folgt dem Windows-Design, lässt sich aber auch fest einstellen
- **Verlaufsgrafik**: optional dezent im Hintergrund der Anzeige (letzte 60 Sekunden)
- **Detail-Fenster per Klick**: große Werte, Verlaufsdiagramm, Spitzenwerte und übertragene Datenmenge seit Start
- **Verschiebbar**: einfach mit der Maus entlang der Taskleiste ziehen
- **Einstellungen mit Live-Vorschau**: Anordnung, Schriftgröße, fett/normal, Einheit (MB/s oder Mbit/s), Farben, Position
- **Blendet sich bei Vollbild aus** (Spiele, Präsentationen, Videos)
- **Autostart** über `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
- Optional zusätzlich ein Symbol im Infobereich
- Misst IPv4 **und** IPv6 und hat keine Ausreißer, wenn Netzwerkadapter wechseln

## Bedienung

| Aktion | Wirkung |
|---|---|
| Linksklick auf die Anzeige | Detail-Fenster öffnen/schließen |
| Rechtsklick | Menü (Einstellungen, Autostart, Beenden …) |
| Ziehen mit der linken Maustaste | Anzeige entlang der Taskleiste verschieben |

Einstellungen werden in `%APPDATA%\TaskBarSpeedmon\settings.ini` gespeichert.

## Voraussetzungen

- Windows 10 / 11
- .NET Framework 4 (bei Windows 10/11 bereits vorhanden)

## Build

Kein SDK und kein NuGet nötig. Es wird nur der Compiler verwendet, der mit Windows ausgeliefert wird:

```powershell
.\build.ps1
```

Das ruft `csc.exe` aus `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\` auf und erzeugt eine einzelne `TaskBarSpeedmon.exe`.

## Aufbau

| Datei | Inhalt |
|---|---|
| `Program.cs` | Einstiegspunkt, verhindert Mehrfachstart |
| `SpeedmonApp.cs` | Verbindet alles: Timer, Menü, Tray-Symbol, Autostart |
| `TaskbarWidget.cs` | Transparentes Fenster auf der Taskleiste (per-Pixel-Alpha) |
| `SpeedFlyout.cs` | Detail-Fenster mit Diagramm |
| `SettingsForm.cs` | Einstellungsdialog mit Live-Vorschau |
| `NetworkMonitor.cs` | Messung pro Adapter, Verlauf, Summen |
| `SpeedFormatter.cs` | Formatierung von Raten und Datenmengen |
| `AppSettings.cs` / `Theme.cs` | Einstellungen und Farbschemata |
| `NativeMethods.cs` | Win32-Aufrufe |

## Hinweise

- Die Anzeige liegt als randloses Fenster über der Taskleiste. Windows 11 erlaubt keine echten Taskleisten-Erweiterungen mehr; Tools wie TrafficMonitor nutzen denselben Ansatz.
- Ist die Taskleiste auf „automatisch ausblenden“ gestellt, verschwindet die Anzeige zusammen mit ihr.
- Falls die Anzeige mit Taskleisten-Symbolen überlappt: einfach mit der Maus verschieben oder in den Einstellungen den Abstand anpassen.

---

> **Disclaimer: Vibe Coded**
>
> Dieses Projekt ist in einem iterativen Dialog mit einem KI-Assistenten (Claude) entstanden. Der Code wurde nicht von einem menschlichen Entwickler nach einem formalen Designprozess geschrieben und kann unkonventionelle Muster enthalten. Nutzung auf eigene Verantwortung, Beiträge und Verbesserungen sind willkommen.
