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
  Instanz ist vorgesehen (zweiter DbContext mit `UseNpgsql`).
  Schema entsteht aktuell über `EnsureCreated()`, echte Migrationen stehen aus.
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
  IndustrieDashboard.Infrastructure  EF Core/SQLite, AuditLogService,
                                     KontrolleingriffService, SimulierteMaschinenDatenQuelle
  Modules/                           Dashboard (funktionsfähig) + 3 Platzhaltermodule
  IndustrieDashboard.App             WPF-Shell, Composition Root, Navigation
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

## Konventionen

- Bezeichner und Kommentare auf Deutsch (so ist der bestehende Code geschrieben)
- Keine Geschäftslogik im Code-Behind, alles ins ViewModel
- Nullable aktiviert, `ImplicitUsings` aktiviert

## Offene Punkte

OPC UA / MQTT echt anbinden, SignalR, die drei Platzhaltermodule fachlich
ausbauen, IdentityServer/Active Directory + TLS, echte EF-Core-Migrationen,
CI-Pipeline in Azure DevOps.
