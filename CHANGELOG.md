# Patchverlauf

Alle nennenswerten Änderungen am Industrie-Dashboard, neueste zuerst. Aufbau angelehnt an
„Keep a Changelog": **Hinzugefügt**, **Geändert**, **Behoben**, **Sicherheit**.
Die Kürzel in Klammern sind Git-Commits (`git show <Kürzel>` zeigt Details).
Die Versionsnummern sind Meilensteine des Prototyps, keine veröffentlichten Releases.

Pflege: Nach jedem abgeschlossenen Arbeitspaket einen Eintrag unter „Unveröffentlicht"
ergänzen. Beim Abschluss eines Meilensteins den Block mit Version und Datum versehen.

---

## Unveröffentlicht

Grundlage: `docs/auftrag-ci-readme.md` (Stufe 1b: CI, Lieferkette, README).

### Hinzugefügt
- Auftrag Stufe 1b in `docs/` und dieser Patchverlauf (9dd1763).
- Automatischer Build und Test auf Windows bei jedem Push auf `main` und jedem Pull Request
  (`.github/workflows/ci.yml`): Debug und Release bauen, Tests in Debug, Testbericht als
  Download (b1b640e).
- Dependabot für NuGet-Pakete und GitHub-Aktionen, wöchentlich, Microsoft-Pakete gruppiert
  (8844c85).
- `SECURITY.md`: Sicherheitslücken privat über GitHub melden, unterstützter Stand, Umfang,
  bestehende Schutzmaßnahmen (e753c02).

### Geändert
- `README.md` auf den tatsächlichen Stand gebracht: Statusabzeichen, Tabelle und Strukturbaum
  korrigiert, „Qualitätsnachweis" statt des veralteten Entstehungshinweises, neuer Abschnitt
  „Sicherheit" (8377e7c).
- `CLAUDE.md`: Abschnitt „CI und Lieferkette" mit den Regeln für Workflows (e256c53).
- Paketupdates von Dependabot, jeweils nach grünem CI-Lauf übernommen: Microsoft-Gruppe
  (EF Core, Extensions) 10.0.11 → 10.0.13 und `Microsoft.NET.Test.Sdk` 17.6.0 → 18.11.0 (f04f6e6),
  `xunit` 2.4.2 → 2.9.3 (aee2cbe), `coverlet.collector` 6.0.0 → 10.1.0 (3ccaecf),
  `xunit.runner.visualstudio` 2.4.5 → 4.0.1 (24ca95e).
- Werkzeugmanifest von `dotnet-tools.json` nach `.config/dotnet-tools.json` verschoben, weil
  Dependabot es nur dort findet; `dotnet-ef` 10.0.11 → 10.0.13, passend zu EF Core.
- Dependabot schlägt neue Versionen erst vor, wenn sie mindestens 14 Tage alt sind
  (`cooldown`), für NuGet und GitHub-Aktionen.

### Behoben
- Compiler-Warnung CS0162 (unerreichbarer Code) im Release-Build von `PrototypBenutzerKontext`:
  Die Release-Sperre steht jetzt in einer eigenen Methode, die Wirkung ist unverändert.
- Compiler-Warnung CS0067 (ungenutztes Ereignis) in einem Test-Hilfsobjekt.
- `dotnet test` im Release-Build schlug bei 13 Tests fehl, weil sie den Prototyp-Anbieter
  brauchen. Diese Tests werden in Release jetzt mit Begründung übersprungen. Build ohne Warnungen
  außer den bekannten NU1701.

### Sicherheit
- Neuer Test: Im Release-Build lässt sich der Prototyp-Anbieter nicht erzeugen, und es entsteht
  kein Anmelde-Eintrag.
- Der CI-Lauf schlägt fehl, wenn ein Paket (auch indirekt eingebunden) eine bekannte Schwachstelle
  hat. Lässt sich das Prüfergebnis nicht lesen, schlägt er ebenfalls fehl (b1b640e).
- Workflow mit Mindestrechten: nur Lesen, keine Secrets, kein `pull_request_target`, nur
  GitHub-eigene Aktionen, jede auf einen Commit-Hash festgenagelt (b1b640e).

---

## 0.3 – Identität und Rollen (2026-10-08)

Grundlage: `docs/spec-identitaet-rollen.md` (Teil A und B) und `docs/auftrag-stufe1.md`.

### Hinzugefügt
- Fünf Rollen (Bediener, Maschineneinrichter, Schichtleitung, Instandhaltung, Administration) und
  eine zentrale Berechtigungsmatrix (`RollenBerechtigungen`) als einzige Stelle für Rechte (af347b9).
- Stabile Benutzerkennung (`BenutzerKennung`) statt Anzeigename; erweiterter `IBenutzerKontext` (af347b9).
- Zeugenpfad bei Alleinbesetzung der Instandhaltung: Freigabe mit Zeuge, Pflichtgrund, Bestätigung
  durch eine dritte Person, Ablauf nach 15 Minuten (3de7a25).
- Aktion „Zurückziehen": Die anfordernde Person kann eine eigene, noch offene Anforderung
  zurücknehmen, ohne Vier-Augen-Prüfung, mit Audit-Eintrag (a4dce17).
- Windows-Anbieter (`IndustrieDashboard.Identity.Windows`): Kennung ist die SID, Rollen kommen aus
  Windows-Gruppen, Gruppenauflösung testbar über `IGruppenPruefer` (99bccce).
- Konfiguration `appsettings.json` mit sicherem Standard „Windows"; `appsettings.Development.json`
  („Prototyp") nur im Debug-Build (99bccce).
- Prototyp-Testbenutzer mit festen Kennungen und Rollen, darunter eine zweite Person der
  Instandhaltung (4b468d3).
- Oberfläche: Kopfzeile mit Name und Rollen, deaktivierte Schaltflächen mit Tooltip (Grund der
  Sperre), kompletter Zeugen-Ablauf im Modul Kontrolleingriffe (0da8436).
- Echte EF-Core-Migrationen mit Baseline (78cde10); Werkzeugmanifest für `dotnet-ef`.
- Spezifikation und Auftrag in `docs/` (3f60bfb).
- Audit-Eintrag „Anmeldung" beim Start (Windows) und bei jedem Benutzerwechsel (Prototyp), mit
  vergebenen Rollen und je Rolle dem Ergebnis der Gruppenauflösung (d9b176d).
- Kopfzeile nennt die geprüften Windows-Gruppen, wenn keine einzige Rolle vergeben wurde (d9b176d).
- Anzeigeverlauf (Spezifikation Teil B): langlebiger Dienst `MaschinenVerlaufsDienst` mit Ringpuffer,
  Quelle `IAuswertungsQuelle` (im Prototyp aus dem Arbeitsspeicher), Intervall und Fenster per
  Konfiguration (Standard 5 Minuten / 8 Stunden, in der Entwicklung 5 Sekunden) (8dbbbe1).
- Betriebshinweis in `CLAUDE.md`: Installationsordner nur für Administratoren beschreibbar (d9b176d).

### Geändert
- Vier-Augen-Prinzip vergleicht Kennungen statt Anzeigenamen (3de7a25).
- Freigeben und Ablehnen sind der Instandhaltung vorbehalten; Schichtleitung und Administration
  geben nichts frei (3de7a25).
- Der Dienst ermittelt die handelnde Person selbst aus dem Benutzerkontext, nicht aus einem von der
  Oberfläche übergebenen Namen (3de7a25).
- Ablehnen folgt denselben Vier-Augen-Regeln wie Freigeben; für die Rücknahme gibt es „Zurückziehen"
  (3de7a25, a4dce17).
- Datenbank-Initialisierung per `Database.Migrate()` statt `EnsureCreated()` (78cde10).
- Das Dashboard liest den Verlauf aus dem gemeinsamen Dienst: Nach einem Modulwechsel ist die Kurve
  sofort wieder da und enthält auch die Zeit im anderen Modul (2378666).
- `CLAUDE.md` und `README.md` auf den Stand von Stufe 1 gebracht, mit verbindlichen
  Sicherheitsleitlinien (fail closed, Prüfung im Dienst, Kennung statt Name, Konfiguration
  verleiht keine Rechte) (eb55e39).
- Maschinenüberwachung läuft auf Anwendungsebene: ein Start beim Programmstart, ein Stopp beim
  Beenden (e2541c4).

### Behoben
- Nach dem Beenden gingen Protokollmeldungen verloren, weil der Logger vor dem Entsorgen des
  Dienstcontainers geschlossen wurde (e2541c4).
- Ein kurzlebiges Dashboard-Fenster konnte die gemeinsame Maschinenüberwachung abschalten (e2541c4).
- Eine zurückgezogene Anforderung ließ sich noch freigeben oder ablehnen (a665f34).
- Tooltips auf deaktivierten Schaltflächen erschienen nicht, weil WPF sie dort standardmäßig
  unterdrückt (`ToolTipService.ShowOnDisabled`) (0da8436).

### Sicherheit
- Gleichzeitigkeitsschutz: Ändern zwei Personen gleichzeitig denselben Vorgang, wird die zweite
  Aktion verweigert und protokolliert (a665f34).
- Berechtigung wird vor dem Laden des Vorgangs geprüft, damit niemand über Fehlermeldungen
  herausfinden kann, welche Nummern existieren (a665f34).
- Längenbegrenzung für Beschreibung und Ausnahmegrund (500 Zeichen), Pflichtgrund mindestens
  5 Zeichen (a665f34).
- Jede Verweigerung wird als „ZugriffVerweigert" im Audit-Log gespeichert (3de7a25).
- Der Prototyp-Anbieter verweigert sich technisch in Release-Builds; unbekannte Konfigurationswerte
  verhindern den Start (4b468d3, 99bccce).
- Kennungs- und Zeugenpfad-Felder fallen unter den bestehenden Audit-Schutz; die Trigger stehen
  jetzt in der Migration (78cde10, 7058cae).
- Rollengruppen, die auf eine zu breite Sammelgruppe zeigen (z. B. „Jeder", „Authentifizierte
  Benutzer", „Benutzer", Domänen-Benutzer), vergeben keine Rolle; Warnung im Log (d9b176d).
- Jede Ausnahme bei der Gruppenauflösung führt zu „Rolle nicht vergeben" statt zum Absturz oder
  zum Erlauben (d9b176d).

Abnahme (eb55e39): Nachweis per Suche, dass kein Anzeigename mehr für Sicherheitsentscheidungen
dient; Release-Build mit „Prototyp" verweigert den Start; Release-Build mit „Windows" ohne passende
Gruppen zeigt „Keine Rolle zugewiesen" und sperrt den Not-Stopp.

Stand: Build 0 Fehler, 159 Tests grün.

---

## 0.2 – Kontrolleingriffe und Audit-Schutz (2026-09-19)

### Hinzugefügt
- Modul **Kontrolleingriffe**: Anforderung, Freigabe, Ablehnung nach Vier-Augen-Prinzip mit
  Audit-Trail; alle acht Abnahmekriterien der Spezifikation per UI-Automatisierung geprüft (56424cb).
- Spezifikation `docs/spec-kontrolleingriffe.md`.
- MIT-Lizenz (56e34f6).

### Geändert
- Navigation erzeugt je Modulwechsel einen eigenen DI-Scope, den vorherigen verwirft sie (e976d4d).

### Behoben
- ViewModels wurden nie entsorgt und hielten Abonnements am gemeinsamen Ereignisverteiler; dadurch
  sammelten sich Abonnenten bei jedem Modulwechsel an (e976d4d).

### Sicherheit
- Audit-Trail technisch unveränderlich: init-only-Eigenschaften, Prüfung in `SaveChanges`,
  Lesezugriffe ohne Änderungsverfolgung (1d486f0).
- Schutz auf Datenbankebene: SQLite-Trigger lehnen `UPDATE` und `DELETE` auf dem Audit-Log ab, auch
  bei `ExecuteUpdate`, `ExecuteDelete` und rohem SQL (04d5ae5).

---

## 0.1 – Grundkonstrukt (2026-09-19)

Erster lauffähiger Stand, auf Windows 11 und Windows 10 geprüft (3c75c2e).

### Hinzugefügt
- Modulare Solution: `Core`, `Shared`, `Infrastructure`, vier Fachmodule, WPF-Shell, Testprojekt.
- Dashboard-Modul mit Maschinenkacheln, Live-Diagramm und simulierten Maschinendaten.
- MVVM-Basis, Ereignisverteiler (EventAggregator), Dependency Injection, Serilog, EF Core mit SQLite.
- Platzhalter für Schichtplanung und Maschinenüberwachung.
- Eigenständige EXE ohne installiertes .NET (`EXE-erstellen.cmd`), VS-Code-Startkonfiguration.
- 22 Tests.

### Behoben (während der ersten Windows-Builds)
- Nicht existierende Paketversionen: MaterialDesignThemes 5.3.3 → 5.3.2, Serilog.Sinks.File 8.0.0 → 7.0.0.
- Fehlendes `using Microsoft.EntityFrameworkCore` (`UseSqlite`).
- Ungültiges `Opacity` an einem `Run`-Element; falscher Pfad der Themendatei
  (`MaterialDesign2.Defaults.xaml`); `Run.Text`-Bindung musste `OneWay` sein.

### Geändert
- Zielplattform von .NET 8 auf .NET 10 (LTS bis 11/2028), weil .NET 8 am 10.11.2026 aus dem Support fällt.

---

## Vorgeschichte (2026-09-10 bis 2026-09-19, vor dem ersten Commit)

Prototypen v0.1 bis v0.5 entstanden in einer Cloud-Sitzung ohne Windows und ohne NuGet-Zugang:
Entwurf der Architektur, Wechsel auf .NET 10, Offline-Prüfung der reinen Logik. Erst der lokale Build
auf Windows hat die WPF-Fehler aufgedeckt (siehe 0.1). Git und GitHub wurden am 2026-09-19 eingerichtet.
