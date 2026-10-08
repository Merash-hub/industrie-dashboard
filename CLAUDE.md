# Projektkontext für Claude

Diese Datei wird von Claude Code beim Start im Projektordner automatisch gelesen.

## Projektziel

Windows-Desktop-Anwendung für den Industrieeinsatz mit vier Modulen: Dashboard,
Schichtplanung, Maschinenüberwachung, Kontrolleingriffe. Das Projekt wächst
schrittweise; der aktuelle Stand ist ein funktionsfähiger Prototyp, kein
fertiges Produkt.

## Technologiestack und getroffene Entscheidungen

- **C# / .NET 10** (LTS bis 11/2028). Ursprünglich .NET 8, bewusst gewechselt,
  weil .NET 8 am 10.11.2026 aus dem Support fällt. Nicht zurück auf 8 wechseln.
- **WPF** (nicht WinUI 3), MVVM, Dependency Injection, EventAggregator
- **UI-Komponenten**: MaterialDesignInXamlToolkit 5.3.2 und LiveCharts2 2.0.5,
  beide kostenlos. DevExpress/Telerik sind im Zielbild vorgesehen, aber bewusst
  noch nicht eingebunden (Lizenzkosten). Ein späterer Wechsel betrifft nur die
  Views, nicht die ViewModels.
- **Daten**: EF Core 10.0.11 mit SQLite lokal; PostgreSQL für die zentrale
  Instanz ist vorgesehen (zweiter DbContext mit `UseNpgsql`), für Messdaten
  als zweite, externe Datenbank geplant (Spezifikation Teil D, noch nicht
  umgesetzt). Schema über echte EF-Core-Migrationen (`Data/Migrations/` in
  `IndustrieDashboard.Infrastructure`), `App.xaml.cs` ruft beim Start
  `Database.Migrate()` auf. Neue Migration: `dotnet tool run dotnet-ef
  migrations add <Name> --project src/IndustrieDashboard.Infrastructure
  --startup-project src/IndustrieDashboard.Infrastructure --output-dir
  Data/Migrations` (lokales Tool, siehe `dotnet-tools.json`).
- **Logging**: Serilog (Konsole + Datei)
- **Tests**: xUnit. Die Testpakete sind noch alt (`xunit` 2.4.2,
  `Microsoft.NET.Test.Sdk` 17.6.0) und sollten bei Gelegenheit gehoben werden.
- Paketversionen müssen vor dem Eintragen gegen die NuGet-API auf Existenz als
  stabile Version geprüft werden (nicht nur gegen Release Notes/Wissen aus dem
  Training, da Pakete zurückgezogen oder umbenannt werden können).

## Architektur

Abhängigkeitsrichtung: `App` → `Modules.*` → `Shared` + `Core`; `Infrastructure` → `Core`.

```
src/
  IndustrieDashboard.Core            Domänenmodelle + Interfaces, keine Abhängigkeiten
  IndustrieDashboard.Shared          ViewModelBase, RelayCommand, EventAggregator, IAppModule
  IndustrieDashboard.Infrastructure  EF Core/SQLite, AuditLogService, KontrolleingriffService,
                                     SimulierteMaschinenDatenQuelle, Anzeigeverlauf (Teil B),
                                     PrototypBenutzerKontext
  IndustrieDashboard.Identity.Windows  WindowsBenutzerKontext, Gruppenauflösung (net10.0-windows,
                                     eigenes Projekt, damit Infrastructure plattformneutral bleibt)
  Modules/                           Dashboard + Kontrolleingriffe funktionsfähig,
                                     Schichtplanung + Maschinenüberwachung Platzhalter
  IndustrieDashboard.App             WPF-Shell, Composition Root, Navigation, Identitätsanbieter-Wahl
tests/IndustrieDashboard.Tests       xUnit
```

Wichtiges Prinzip: Module kennen nur Interfaces aus `Core`, nie konkrete
Infrastruktur. `SimulierteMaschinenDatenQuelle` ist absichtlich die einzige
Stelle, die später durch echte OPC-UA-/MQTT-Clients ersetzt wird.

Neues Modul: `IAppModule` implementieren und in `App.xaml.cs` in die Liste
`_alleModule` eintragen. Die Shell braucht dafür keine Änderung.

### Audit-Trail (Vier-Augen-Prinzip)

`AuditLogEintrag` ist absichtlich unveränderlich: alle Felder außer `Id` sind
`init`-only, `AppDbContext.SaveChanges(Async)` blockiert jeden Versuch, einen
bestehenden Eintrag zu ändern oder zu löschen (`EntityState.Modified`/
`Deleted` löst eine `InvalidOperationException` aus), und `IAuditLogService`
bietet ausschließlich Anlegen (`ProtokolliereAsync`) und Lesen
(`GetEintraegeAsync`) an, keine Update-/Delete-Methoden. Diese Eigenschaft ist
für sicherheitsrelevante Aktionen (insbesondere Kontrolleingriffe) verbindlich
und darf beim Erweitern nicht aufgeweicht werden.

### Identität und Rollen

Siehe `docs/spec-identitaet-rollen.md` für die vollständige Spezifikation.
Kurzfassung des aktuellen Stands (Teil A, Stufe 1 abgeschlossen):

- Jeder Benutzer hat eine stabile `BenutzerKennung` (Windows: SID; Prototyp:
  `proto:name`) **und** einen `Anzeigename`. Nur die Kennung zählt für
  Sicherheitsentscheidungen (Vier-Augen-Prinzip, Berechtigungsprüfung); der
  Anzeigename ist ausschließlich Anzeige/Protokoll-Momentaufnahme.
- `RollenBerechtigungen` (`IndustrieDashboard.Core.Autorisierung`) ist die
  **einzige** Stelle im Code, die festlegt, welche der fünf Rollen (Bediener,
  Maschineneinrichter, Schichtleitung, Instandhaltung, Administration) welche
  Berechtigung hat. Kein Rollenwissen sonst irgendwo verstreuen.
- Anbieter per Konfiguration wählbar (`Sicherheit:Identitaetsanbieter` in
  `appsettings.json`): `Windows` (Rollen aus Windows-Gruppenmitgliedschaft,
  `IndustrieDashboard.Identity.Windows`) oder `Prototyp` (feste Testbenutzer,
  **nur in Debug-Builds**, technisch erzwungen im Konstruktor von
  `PrototypBenutzerKontext`). Fehlende/unlesbare Konfiguration → `Windows`;
  unbekannter Wert oder `Prototyp` im Release-Build → Start verweigert.
- `KontrolleingriffService` erzwingt das Vier-Augen-Prinzip und den
  Zeugenpfad bei Alleinbesetzung der Instandhaltung (Spezifikation A6)
  serverseitig; die Oberfläche deaktiviert Schaltflächen nur zusätzlich.
- Jede Anmeldung (Windows-Start, Prototyp-Benutzerwechsel) und jede
  Verweigerung schreibt einen Audit-Eintrag (Kategorien `Anmeldung` bzw.
  `ZugriffVerweigert`).

### Sicherheitsleitlinien (verbindlich, bei jeder Erweiterung beachten)

1. **Fail closed.** Verweigern ist der Standard. Keine Rolle → keine
   Berechtigung, auch nicht Lesen. Eine Windows-Gruppe, die sich nicht
   auflösen lässt (Tippfehler, Gruppe existiert nicht, jede sonstige
   Ausnahme bei der Auflösung), vergibt die Rolle **nicht** - nur eine
   Warnung ins Log, nie "im Zweifel erlauben". Eine Rollengruppe, die auf
   eine zu breite Sammelgruppe auflöst (Everyone, Authentifizierte Benutzer,
   BUILTIN\Users/Guests, Domänen-Benutzer/-Gäste), wird ebenso abgelehnt
   (`BreiteSammelgruppenErkennung`).
2. **Prüfung im Dienst, nicht nur in der Oberfläche.** Jeder Dienst
   (insbesondere `KontrolleingriffService`) prüft Berechtigung und Vier-
   Augen-Prinzip selbst und unabhängig von dem, was die UI anzeigt oder
   deaktiviert. Berechtigung wird außerdem **vor** dem Laden einer
   Anforderung per Id geprüft, damit niemand über Fehlermeldungen vorhandene
   Anforderungsnummern erraten kann.
3. **Kennung statt Name.** Niemals einen Anzeigenamen für eine
   Sicherheitsentscheidung vergleichen (Regressionsgefahr: zwei Personen mit
   gleichem Namen, Umbenennung, Tippfehler). Vergleiche über
   `BenutzerKennung.Wert` mit `StringComparison.Ordinal`. Nachweis per
   `grep` möglich: keine sicherheitsrelevante Prüfung greift auf
   `.Anzeigename` zu (einzige zulässige Ausnahme: die Auswahl in der
   Prototyp-Benutzerwechsel-Dropdown-Liste - das ist eine reine
   UI-Auswahl, keine Sicherheitsentscheidung).
4. **Konfiguration verleiht keine Rechte.** `appsettings.json` kann nur
   auswählen und benennen (welcher Anbieter, welche Gruppe heißt wie), nie
   selbst eine Rolle vergeben - die Basisrolle kommt ausschließlich aus
   echter Windows-Gruppenmitgliedschaft. Deshalb darf der
   Installationsordner nur für Administratoren beschreibbar sein (siehe
   unten).

### Betriebshinweis: Installationsordner

`appsettings.json` neben der EXE enthält die Zuordnung Rolle → Windows-Gruppe
(`Sicherheit:RollenGruppen`). Die Konfiguration kann nur auswählen und
benennen, nie Rechte verleihen (die Rolle kommt ausschließlich aus echter
Windows-Gruppenmitgliedschaft) - trotzdem darf der Installationsordner **nur
für Administratoren beschreibbar** sein. Wer die Datei ändern kann, kann sonst
z. B. `Instandhaltung` versehentlich oder absichtlich auf eine falsche Gruppe
umbiegen. Normale Benutzer dürfen die Datei lesen, aber nicht schreiben.

## Konventionen

- Bezeichner und Kommentare auf Deutsch (so ist der bestehende Code geschrieben)
- Keine Geschäftslogik im Code-Behind, alles ins ViewModel
- Nullable aktiviert, `ImplicitUsings` aktiviert

## Offene Punkte

OPC UA / MQTT echt anbinden, SignalR, Schichtplanung und Maschinenüberwachung
fachlich ausbauen, Vertretungen im Schichtplanung-Modul (Spezifikation Teil
C), dauerhafte Messdatenspeicherung in PostgreSQL mit Totband/Aggregaten/TLS
(Teil D), Auswertungsansicht mit seltener Aktualisierung (Teil D5), Entra-ID-
Anbindung (Abstraktion ist vorbereitet, siehe `BenutzerKennung`), CI-Pipeline
in Azure DevOps. Kein IdentityServer vorgesehen (die Anwendung erbt bewusst
nur die Windows-Anmeldung, siehe `docs/spec-identitaet-rollen.md` Abschnitt A3).
