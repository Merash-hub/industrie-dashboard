# Industrie-Dashboard – Prototyp

[![CI](https://github.com/Merash-hub/industrie-dashboard/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Merash-hub/industrie-dashboard/actions/workflows/ci.yml)

Lauffähiger Prototyp der geplanten Windows-Desktop-Anwendung
(Dashboard, Schichtplanung, Maschinenüberwachung, Kontrolleingriffe).
Dient als gemeinsame Arbeitsgrundlage für die weitere, schrittweise
Ausarbeitung – **nicht** als fertiges Produkt. Aktueller Meilenstein: 0.3
(Identität und Rollen), Details im [`CHANGELOG.md`](CHANGELOG.md).

## Was ist bereits enthalten

| Bereich | Stand |
|---|---|
| Solution-/Projektstruktur (modular, s. u.) | ✅ vollständig |
| MVVM-Basis, RelayCommand, EventAggregator | ✅ vollständig, mit Tests |
| Domänenmodell (Maschine, Schicht, Mitarbeiter, Audit-Log, Kontrolleingriff) | ✅ vollständig |
| EF-Core-Datenkontext (SQLite) mit echten Migrationen (`Data/Migrations/`) | ✅ funktionsfähig |
| Modulares Plug-in-System für Fachmodule (`IAppModule`) | ✅ vollständig |
| **Dashboard-Modul** (Kacheln, Live-Chart, Kontrolleingriff-Demo) | ✅ **funktionsfähig** mit simulierten Maschinendaten |
| **Anzeigeverlauf** (gemeinsamer Verlauf für das Live-Chart, bleibt beim Modulwechsel erhalten) | ✅ **funktionsfähig**, im Arbeitsspeicher |
| **Kontrolleingriffe-Modul** (Anfordern, Freigeben, Ablehnen, Zurückziehen, Zeugenpfad, Audit-Trail-Ansicht) | ✅ **funktionsfähig** |
| **Identität und Rollen** (Windows-Anbieter mit Rollen aus Windows-Gruppen, fünf Rollen, Prototyp-Benutzerwechsel nur im Debug-Build) | ✅ **funktionsfähig**, siehe `docs/spec-identitaet-rollen.md` |
| **Audit-Schutz** (Audit-Log nur erweiterbar, im Code und per Datenbank-Trigger) | ✅ **funktionsfähig** |
| Schichtplanung | 🔲 Platzhalter – nächste Ausbauschritte |
| Maschinenüberwachung als Fachmodul (Detailansicht, Grenzwerte, Alarme) | 🔲 Platzhalter – die simulierte Datenerfassung läuft bereits im Hintergrund |
| OPC UA / MQTT / SignalR | 🔲 noch nicht angebunden (siehe unten) |
| TLS, Serverdienst, externe Datenbank (PostgreSQL) | 🔲 noch nicht vorhanden (Spezifikation Teil D) |

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
  IndustrieDashboard.Infrastructure  EF Core (SQLite, Migrationen), Audit-Log, Kontrolleingriff-Service,
                                     Anzeigeverlauf, Prototyp-Benutzer, simulierte Maschinendatenquelle
  IndustrieDashboard.Identity.Windows  Windows-Anbieter: Benutzer per SID, Rollen aus Windows-Gruppen
  Modules/
    IndustrieDashboard.Modules.Dashboard             (funktionsfähig)
    IndustrieDashboard.Modules.Schichtplanung        (Platzhalter)
    IndustrieDashboard.Modules.Maschinenueberwachung (Platzhalter)
    IndustrieDashboard.Modules.Kontrolleingriffe     (funktionsfähig)
  IndustrieDashboard.App             WPF-Shell, Composition Root (DI), Navigation, Wahl des Identitätsanbieters
tests/
  IndustrieDashboard.Tests           xUnit-Tests für Core, Shared, Infrastructure, Identität, Module und Shell
.github/
  workflows/ci.yml                   automatischer Build und Test bei jedem Push
  dependabot.yml                     wöchentliche Vorschläge für Paket- und Aktionsupdates
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
- **Migrationen:** Echte EF-Core-Migrationen (`src/IndustrieDashboard.Infrastructure/Data/Migrations/`).
  `App.xaml.cs` ruft beim Start `Database.Migrate()` auf. Neue Migration:
  `dotnet tool run dotnet-ef migrations add <Name> --project src/IndustrieDashboard.Infrastructure --startup-project src/IndustrieDashboard.Infrastructure --output-dir Data/Migrations`
  (lokales Tool, einmalig `dotnet tool restore`).
- **Identität und Rollen:** Zwei wählbare Anbieter (`Sicherheit:Identitaetsanbieter`
  in `appsettings.json`): `Windows` (Rollen aus Windows-Gruppenmitgliedschaft,
  Standard und einzige in Release-Builds zulässige Option) oder `Prototyp`
  (fester Testbenutzer-Satz mit Benutzerwechsel in der Oberfläche, nur in
  Debug-Builds). Fünf Rollen, zentrale Berechtigungsmatrix, Vier-Augen-Prinzip
  nach Kennung (nicht nach Anzeigename) inkl. Zeugenpfad bei Alleinbesetzung
  der Instandhaltung. Details: `docs/spec-identitaet-rollen.md`.
- **Vier-Augen-Prinzip:** Im Dashboard- und Kontrolleingriffe-Modul nutzbar
  (Maschine auswählen → "Not-Stopp anfordern" → Anforderung landet in
  `KontrolleingriffAnforderungen` + Audit-Log; Freigabe/Ablehnung verweigert
  dieselbe Person, die angefordert hat - geprüft über die stabile Kennung,
  nicht über den Anzeigenamen).

## Noch offen (nächste Ausbauschritte)

- Echte OPC-UA-/MQTT-Anbindung (ersetzt `SimulierteMaschinenDatenQuelle`)
- SignalR für Live-Updates zwischen mehreren Clients
- Schichtplanung: UI + Geschäftslogik, dazu Vertretungen (Spezifikation Teil C)
- Maschinenüberwachung: Detailansicht je Maschine, Grenzwerte/Alarme
- Dauerhafte Messdatenspeicherung in PostgreSQL, Auswertungsansicht mit
  seltener Aktualisierung (Spezifikation Teil D)
- Entra-ID-Anbindung (Abstraktion über `BenutzerKennung` vorbereitet, nicht gebaut)
- TLS und ein Serverdienst für den Betrieb mit mehreren Arbeitsplätzen
- CI-Pipeline in Azure DevOps, falls im Zielbetrieb gewünscht (GitHub Actions
  läuft bereits, siehe „Qualitätsnachweis")

## Qualitätsnachweis

Bei jedem Push auf `main` und bei jedem Pull Request baut GitHub das Projekt
automatisch auf einem frischen Windows-Rechner (Debug und Release), führt alle
Tests aus und prüft die Pakete auf bekannte Schwachstellen
(`.github/workflows/ci.yml`). Das Abzeichen „CI" oben zeigt das Ergebnis des
letzten Laufs.

Dieselbe Prüfung lokal:

```powershell
dotnet build -c Debug
dotnet build -c Release
dotnet test -c Debug
dotnet list package --vulnerable --include-transitive
```

Zusätzlich manuell geprüft: das Grundkonstrukt (0.1) auf Windows 10 und
Windows 11, der aktuelle Stand (0.3) auf Windows 11.

## Sicherheit

**Umgesetzt:** fünf Rollen mit zentraler Berechtigungsmatrix, Rollen im
Betrieb nur aus echter Windows-Gruppenmitgliedschaft, Vier-Augen-Prinzip per
stabiler Benutzerkennung (nicht per Anzeigename) mit Zeugenpfad, Audit-Log nur
erweiterbar (Code und Datenbank-Trigger), jede Verweigerung protokolliert,
„fail closed" (im Zweifel verweigern), Prüfung auf bekannte Schwachstellen
und Dependabot für die Lieferkette.

**Bewusst noch offen:** Netzwerkbetrieb mit TLS, Serverdienst, externe
Datenbank, Vertretungen (Spezifikation Teil C und D).

**Wichtig:** Dies ist ein Prototyp. Er ist **nicht** für sicherheitsgerichtete
Steuerungsfunktionen an echten Anlagen zugelassen oder geprüft.

Mehr dazu: [`SECURITY.md`](SECURITY.md) (auch: wie man eine Lücke meldet),
[`docs/spec-identitaet-rollen.md`](docs/spec-identitaet-rollen.md) und
[`CHANGELOG.md`](CHANGELOG.md).
