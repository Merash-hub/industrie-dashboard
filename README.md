# Industrie-Dashboard – Prototyp v0.1

Erster lauffähiger Prototyp der geplanten Windows-Desktop-Anwendung
(Dashboard, Schichtplanung, Maschinenüberwachung, Kontrolleingriffe).
Dient als gemeinsame Arbeitsgrundlage für die weitere, schrittweise
Ausarbeitung – **nicht** als fertiges Produkt.

## Was ist bereits enthalten

| Bereich | Stand |
|---|---|
| Solution-/Projektstruktur (modular, s. u.) | ✅ vollständig |
| MVVM-Basis, RelayCommand, EventAggregator | ✅ vollständig, mit Tests |
| Domänenmodell (Maschine, Schicht, Mitarbeiter, Audit-Log, Kontrolleingriff) | ✅ vollständig |
| EF-Core-Datenkontext (SQLite) | ✅ vollständig (Schema per `EnsureCreated()`, echte Migrationen folgen) |
| Modulares Plug-in-System für Fachmodule (`IAppModule`) | ✅ vollständig |
| **Dashboard-Modul** (Kacheln, Live-Chart, Kontrolleingriff-Demo) | ✅ **lauffähig** mit simulierten Maschinendaten |
| Schichtplanung / Maschinenüberwachung / Kontrolleingriffe | 🔲 Grundgerüst/Platzhalter – nächste Ausbauschritte |
| OPC UA / MQTT / SignalR | 🔲 noch nicht angebunden (siehe unten) |
| IdentityServer/Active Directory, TLS | 🔲 noch nicht angebunden |

Die simulierte Maschinenanbindung (`SimulierteMaschinenDatenQuelle`) ist
bewusst die **einzige** Stelle, die später durch echte OPC-UA-/MQTT-Clients
ersetzt werden muss – das Dashboard-Modul kennt nur die Abstraktion
`IMaschinenDatenQuelle` und muss dafür nicht verändert werden.

## Voraussetzungen zum Ausführen

- **Windows 10/11** (WPF läuft nur unter Windows)
- **.NET 10 SDK** – zwingend das *SDK*, nicht nur die Runtime. Prüfen mit
  `dotnet --list-sdks`; kommt dort keine Zeile, ist kein SDK installiert.
- Ein Editor: **VS Code** mit der C#-Erweiterung genügt, **Visual Studio 2022**
  bietet zusätzlich den XAML-Designer.
- Internetzugang beim ersten Build (NuGet-Pakete werden automatisch
  wiederhergestellt – DevExpress/Telerik werden bewusst **nicht** verwendet,
  siehe unten)

## Erste Schritte

SDK installieren (PowerShell):

```powershell
winget install Microsoft.DotNet.SDK.10
```

**Danach das Terminal schließen und neu öffnen**, sonst ist `dotnet` noch nicht
im PATH. Prüfen mit `dotnet --list-sdks` – dort muss eine 10.x-Zeile stehen.

Dann im Projektordner (dort, wo `IndustrieDashboard.sln` liegt):

```powershell
dotnet build
dotnet run --project src\IndustrieDashboard.App
```

In **VS Code**: den Ordner öffnen, der die `IndustrieDashboard.sln` enthält
(nicht den übergeordneten Entpack-Ordner), und beim Öffnen die Rückfrage
"Do you trust the authors?" mit **Ja** beantworten. Ohne dieses Vertrauen
startet die C#-Erweiterung nur im eingeschränkten Modus und lädt keine Projekte.

Danach startet **F5** die Anwendung direkt. Die passende Startkonfiguration
liegt bereits im Ordner `.vscode/` bei. Meldet VS Code beim Drücken von F5
stattdessen etwas über eine fehlende Erweiterung für "Plain Text", dann wurde
der falsche Ordner geöffnet: `.vscode/` und `IndustrieDashboard.sln` müssen
direkt im geöffneten Ordner liegen.

In **Visual Studio 2022**: `IndustrieDashboard.sln` öffnen,
`IndustrieDashboard.App` als Startprojekt setzen, F5. Im Visual Studio Installer
muss die Workload **".NET-Desktopentwicklung"** angehakt sein.

Die SQLite-Datenbank und Logs werden automatisch angelegt unter
`%LOCALAPPDATA%\IndustrieDashboard\`.

## Projektstruktur

```
IndustrieDashboard.sln
src/
  IndustrieDashboard.Core            Domänenmodelle & Interfaces (keine Abhängigkeiten)
  IndustrieDashboard.Shared          MVVM-Basis, EventAggregator, IAppModule-Vertrag
  IndustrieDashboard.Infrastructure  EF Core (SQLite), Audit-Log, Kontrolleingriff-Service,
                                     simulierte Maschinendatenquelle
  Modules/
    IndustrieDashboard.Modules.Dashboard             (funktionsfähig)
    IndustrieDashboard.Modules.Schichtplanung        (Platzhalter)
    IndustrieDashboard.Modules.Maschinenueberwachung (Platzhalter)
    IndustrieDashboard.Modules.Kontrolleingriffe     (Platzhalter)
  IndustrieDashboard.App             WPF-Shell, Composition Root (DI), Navigation
tests/
  IndustrieDashboard.Tests           xUnit-Tests für Shared & Infrastructure
```

**Abhängigkeitsrichtung:** `App` → `Modules.*` → `Shared` + `Core`.
`Infrastructure` → `Core`. Module kennen nur Interfaces aus `Core`
(z. B. `IMaschinenDatenQuelle`), nie konkrete Infrastruktur-Implementierungen –
das hält die Architektur austauschbar (z. B. SQLite → PostgreSQL, Simulation
→ echtes OPC UA, ohne Module anfassen zu müssen).

## Ein neues Modul hinzufügen

1. Neues Class-Library-Projekt unter `src/Modules/` anlegen
   (`net10.0-windows`, `UseWPF=true`, Referenz auf `Core` + `Shared`).
2. `IAppModule` implementieren (Anzeigename, Icon, `RegisterServices`,
   `ErzeugeStartView`).
3. In `IndustrieDashboard.App/App.xaml.cs` zur Liste `_alleModule` hinzufügen.

Das war's – Navigation und DI-Wiring übernimmt die Shell automatisch.

## Bewusste Entscheidungen für diesen Prototyp

- **UI-Komponenten:** [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
  (kostenlos) statt DevExpress/Telerik, damit sofort ohne Lizenz gearbeitet
  werden kann. Umstieg auf DevExpress/Telerik ist ein reiner UI-Austausch in
  den Views, sobald eine Lizenz vorliegt – Architektur/ViewModels bleiben
  unverändert.
- **Charts:** [LiveCharts2](https://livecharts.dev/) (kostenlos, MIT-Lizenz).
- **.NET-Version:** .NET 10 (LTS, Support bis November 2028). Der erste Entwurf
  lief noch auf .NET 8, das aber am 10.11.2026 aus dem Support fällt – für eine
  Anwendung, die über Jahre wachsen soll, wäre das von Anfang an eine Altlast
  gewesen.
- **Datenzugriff:** EF Core + SQLite lokal; der Wechsel/die Ergänzung um
  PostgreSQL für die zentrale Instanz ist ein zweiter `DbContext` mit
  `UseNpgsql(...)` statt `UseSqlite(...)` gegen dasselbe Modell.
- **Migrationen:** Für den Prototyp erzeugt `EnsureCreated()` das Schema
  direkt aus dem Modell. Sobald sich das Modell stabilisiert, sollte auf
  echte EF-Core-Migrationen umgestellt werden: `dotnet ef migrations add InitialCreate --project src/IndustrieDashboard.Infrastructure --startup-project src/IndustrieDashboard.App`.
- **Vier-Augen-Prinzip:** Im Dashboard-Modul bereits als End-to-End-Demo
  nutzbar (Maschine auswählen → "Not-Stopp anfordern" → Anforderung landet
  in `KontrolleingriffAnforderungen` + Audit-Log; `KontrolleingriffService.FreigebenAsync`
  verweigert die Freigabe durch dieselbe Person, die angefordert hat).

## Noch offen (nächste Ausbauschritte)

- Echte OPC-UA-/MQTT-Anbindung (ersetzt `SimulierteMaschinenDatenQuelle`)
- SignalR für Live-Updates zwischen mehreren Clients
- Schichtplanung: UI + Geschäftslogik
- Maschinenüberwachung: Detailansicht je Maschine, Grenzwerte/Alarme
- Kontrolleingriffe: UI für Freigabe-Liste + Audit-Log-Ansicht
- Authentifizierung/Autorisierung (IdentityServer/Active Directory), TLS
- Echte EF-Core-Migrationen statt `EnsureCreated()`
- CI-Pipeline (Azure DevOps), wie im Technologiestack vorgesehen

## Hinweis zur Entstehung dieses Prototyps

Dieser Prototyp wurde in einer Linux-Sandbox ohne Zugriff auf nuget.org
erstellt. Was dort tatsächlich gegen den echten .NET-10-Compiler gebaut und
geprüft wurde: `Core` vollständig, dazu die Logik von `Shared`, der simulierten
Maschinendatenquelle und aller xUnit-Tests (über eine Offline-Prüfung mit
Framework-Referenzen). Jeweils null Fehler und null Warnungen.

Nicht gebaut werden konnten die WPF-Projekte (`App`, `Modules.*`), weil dafür
der Windows-Desktop-Workload nötig ist, den es unter Linux nicht gibt. Deren
C#- und XAML-Code wurde von Hand geprüft. Sämtliche NuGet-Paketversionen in
den `.csproj`-Dateien wurden einzeln gegen die NuGet-API auf Existenz geprüft,
damit `dotnet restore` nicht an einer erfundenen Versionsnummer scheitert.

Falls beim ersten Build unter Windows trotzdem etwas hakt: die Fehlermeldung
als Text schicken, dann wird das gezielt nachgebessert.
