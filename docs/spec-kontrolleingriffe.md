# Spezifikation: Modul Kontrolleingriffe

Ziel: Das Platzhaltermodul `IndustrieDashboard.Modules.Kontrolleingriffe` wird zu
einem funktionsfähigen Modul ausgebaut. Freigabe kritischer Eingriffe nach dem
Vier-Augen-Prinzip plus Einsicht in den Audit-Trail.

**Wichtige Randbedingung:** Die Shell (`IndustrieDashboard.App`) darf dabei
NICHT verändert werden, mit einer einzigen erlaubten Ausnahme (siehe Punkt 1).
Wenn dafür Änderungen an der Shell nötig erscheinen, ist das ein Hinweis auf
einen Architekturfehler. In dem Fall bitte melden statt umbauen.

Was bereits existiert und wiederverwendet wird: `KontrolleingriffAnforderung`,
`KontrolleingriffStatus`, `AuditLogEintrag`, `IKontrolleingriffService` mit
`KontrolleingriffService`, `IAuditLogService` mit `AuditLogService`,
`IEventAggregator` und `KontrolleingriffStatusGeaendertEvent`.

---

## 1. Neue Abstraktion: Benutzerkontext

Heute steht der Benutzername im Dashboard fest verdrahtet als
`"Steven (Bediener)"`. Für das Vier-Augen-Prinzip ist das ein Problem: Wer
anfordert, darf nicht freigeben, also könnte man mit nur einem Benutzer nie
etwas freigeben und das Modul wäre nicht vorführbar.

In `IndustrieDashboard.Core/Interfaces/IBenutzerKontext.cs`:

```csharp
public interface IBenutzerKontext
{
    string AktuellerBenutzer { get; }
    event EventHandler? BenutzerGewechselt;
}
```

Bewusst minimal gehalten, weil diese Schnittstelle später von Active Directory
bzw. IdentityServer bedient wird. Dort gibt es kein Umschalten.

Das Umschalten ist reine Prototyp-Funktionalität und gehört deshalb in eine
eigene, getrennte Schnittstelle, ebenfalls in Core:

```csharp
public interface IBenutzerWechsel
{
    IReadOnlyList<string> VerfuegbareBenutzer { get; }
    void Wechsle(string benutzer);
}
```

Implementierung in `IndustrieDashboard.Infrastructure`, Klasse
`PrototypBenutzerKontext`, die beide Schnittstellen erfüllt. Feste Testliste:
`"Steven Katzer (Bediener)"`, `"Anna Weber (Schichtleitung)"`,
`"Thomas Krause (Instandhaltung)"`. Startwert ist der erste Eintrag.
`Wechsle` löst `BenutzerGewechselt` aus.

Registrierung in `InfrastructureServiceCollectionExtensions.AddInfrastruktur`:
als **Singleton**, und beide Schnittstellen müssen dieselbe Instanz liefern
(erst die Instanz registrieren, dann beide Interfaces darauf abbilden).

**Die einzige erlaubte Änderung außerhalb dieses Moduls:** In
`DashboardViewModel` wird das fest verdrahtete `"Steven (Bediener)"` durch
`_benutzerKontext.AktuellerBenutzer` ersetzt. `IBenutzerKontext` dazu per
Konstruktor injizieren.

## 2. Erweiterungen an IKontrolleingriffService

Ergänzen, jeweils inklusive Audit-Log-Eintrag:

```csharp
Task<KontrolleingriffAnforderung> AblehnenAsync(
    int anforderungId, string abgelehntVon, string? begruendung, CancellationToken ct = default);

Task<IReadOnlyList<KontrolleingriffAnforderung>> GetAlleAnforderungenAsync(
    int maxAnzahl = 100, CancellationToken ct = default);
```

Regeln:

- **Freigeben** verlangt zwingend eine andere Person als die anfordernde. Diese
  Prüfung existiert bereits in `FreigebenAsync` und bleibt unverändert.
- **Ablehnen** darf jede Person, auch die anfordernde. Eine Ablehnung durch die
  anfordernde Person ist fachlich eine Rücknahme und völlig legitim. Der
  Audit-Log-Eintrag hält fest, wer abgelehnt hat, damit der Unterschied
  nachvollziehbar bleibt.
- `GetAlleAnforderungenAsync` liefert absteigend nach `AngefordertAm`.

## 3. ViewModel

`KontrolleingriffeViewModel` in `ViewModels/`, abgeleitet von `ViewModelBase`.
Injiziert werden `IKontrolleingriffService`, `IAuditLogService`,
`IBenutzerKontext`, `IBenutzerWechsel` und `IEventAggregator`.

Eigenschaften:

- `ObservableCollection<AnforderungViewModel> OffeneAnforderungen`
- `ObservableCollection<AuditLogEintrag> AuditEintraege`
- `AnforderungViewModel? AusgewaehlteAnforderung`
- `string AktuellerBenutzer` (aus dem Benutzerkontext)
- `IReadOnlyList<string> VerfuegbareBenutzer`
- `string StatusMeldung`

Kommandos: `FreigebenCommand`, `AblehnenCommand`, `AktualisierenCommand`
(alle `AsyncRelayCommand`).

`AnforderungViewModel` kapselt eine `KontrolleingriffAnforderung` und ergänzt:

- `bool DarfFreigeben` = `AngefordertVon != aktuellerBenutzer`
- `string FreigabeHinweis` = bei `false` der Text
  "Vier-Augen-Prinzip: Eine Anforderung darf nicht von der Person freigegeben
  werden, die sie gestellt hat."

Das ist der didaktisch wichtigste Teil der Oberfläche: Der Knopf ist sichtbar,
aber deaktiviert, und der Tooltip erklärt warum. Nicht einfach ausblenden.

Beim Wechsel des Benutzers (`BenutzerGewechselt`) müssen alle
`DarfFreigeben`-Werte neu berechnet und gemeldet werden.

## 4. View

`Views/KontrolleingriffeView.xaml`, im Stil der bestehenden Views mit
MaterialDesign. Drei Bereiche untereinander:

**Kopfzeile:** "Angemeldet als:" plus `ComboBox` mit `VerfuegbareBenutzer`.
Daneben klein und kursiv der Hinweis "Prototyp: Benutzerwechsel ersetzt später
die Anmeldung über Active Directory."

**Offene Anforderungen:** Eine `materialDesign:Card` je Anforderung in einem
`ItemsControl`. Angezeigt werden Maschine, Beschreibung, angefordert von und
Zeitpunkt. Rechts zwei Knöpfe, "Freigeben" (an `DarfFreigeben` gebunden, mit
`ToolTip` aus `FreigabeHinweis`) und "Ablehnen". Wenn die Liste leer ist, ein
ruhiger Hinweistext statt einer leeren Fläche.

**Audit-Trail:** `DataGrid`, schreibgeschützt, Spalten Zeitstempel, Benutzer,
Aktion, Zielobjekt, Begründung. Die letzten 100 Einträge, neueste oben.

Code-Behind nur `InitializeComponent()`, `DataContext` per Konstruktor, und im
`Loaded`-Ereignis das Laden anstoßen. Keine Logik im Code-Behind.

## 5. Verdrahtung über den EventAggregator

Hier soll sichtbar werden, dass die Module lose gekoppelt zusammenarbeiten:

- Das ViewModel abonniert `KontrolleingriffStatusGeaendertEvent` und lädt bei
  Eintreffen die Listen neu. Damit erscheint eine im Dashboard ausgelöste
  Anforderung hier ohne manuelles Aktualisieren.
- Nach erfolgreichem Freigeben oder Ablehnen wird dasselbe Ereignis
  veröffentlicht.
- **Wichtig:** Das Abonnement im `Dispose` wieder abmelden, sonst sammeln sich
  bei jedem Moduswechsel tote Abonnenten an. `IDisposable` implementieren.

## 6. Modulregistrierung

In `KontrolleingriffeModule.RegisterServices` den `PlatzhalterViewModel` durch
`KontrolleingriffeViewModel` und `KontrolleingriffeView` ersetzen, beide als
`AddTransient`. `ErzeugeStartView` liefert die neue View. `AnzeigeName`,
`IconKind` und `Reihenfolge` bleiben unverändert.

## 7. Tests

In `IndustrieDashboard.Tests` ergänzen. Für den `AppDbContext` SQLite im
Arbeitsspeicher verwenden (`Data Source=:memory:` mit offen gehaltener
Verbindung). Damit braucht es kein zusätzliches NuGet-Paket und das Verhalten
entspricht dem echten Anbieter.

1. `FreigebenAsync` durch dieselbe Person wirft `InvalidOperationException`.
2. `FreigebenAsync` durch eine andere Person setzt Status auf `Freigegeben`,
   füllt `FreigegebenVon` und `FreigegebenAm` und schreibt einen Audit-Eintrag.
3. `AblehnenAsync` setzt den Status auf `Abgelehnt` und schreibt einen
   Audit-Eintrag, auch wenn dieselbe Person ablehnt.
4. `GetOffeneAnforderungenAsync` liefert ausschließlich Einträge mit Status
   `Angefordert`.
5. `PrototypBenutzerKontext.Wechsle` ändert `AktuellerBenutzer` und löst
   `BenutzerGewechselt` aus.

## 8. Abnahmekriterien

Erst erfüllt, wenn alles davon zutrifft:

- `dotnet build` ohne Fehler, `dotnet test` ohne fehlgeschlagene Tests
- Die Anwendung startet, "Kontrolleingriffe" zeigt die neue Oberfläche
- Im Dashboard eine Maschine wählen, "Not-Stopp anfordern", dann zu
  Kontrolleingriffe wechseln: Die Anforderung steht dort, ohne Aktualisieren
- "Freigeben" ist deaktiviert, solange derselbe Benutzer angemeldet ist, und der
  Tooltip erklärt den Grund
- Nach Wechsel auf einen anderen Benutzer wird "Freigeben" aktiv
- Nach dem Freigeben verschwindet die Anforderung aus den offenen und der
  Audit-Trail enthält zwei Einträge: Anforderung und Freigabe, mit
  unterschiedlichen Benutzern
- Die Anwendung beenden und neu starten: Der Audit-Trail ist noch da
  (SQLite-Persistenz)
- Am Schluss ein Git-Commit mit aussagekräftiger Nachricht
