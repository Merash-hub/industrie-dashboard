# Spezifikation: Identität, Rollen, Vertretungen und Messdatenspeicherung

Stand 2026-10-08 · Version 4 · Status: **Teil A und B freigabereif, Teil C und D sind Entwurf**
Umsetzung durch Claude Code lokal, in kleinen Commits, nach jedem Schritt Build und Tests.

## Entscheidungen von Steven

1. **Freigabe liegt allein bei der Instandhaltung.** Die Schichtleitung gibt maschinell nichts frei. (08.10.)
2. **Funktionstrennung bleibt:** Die Administration darf nichts freigeben. Es gilt "Richtlinien einhalten". (08.10.)
3. **Zeugenbestätigung:** Ist nur eine Person der Instandhaltung anwesend, darf die Leitung als Zeuge
   bestätigen, aus versicherungstechnischen Gründen. (08.10., Abschnitt A6)
4. **Vertretungen:** Die Schichtleitung kann für eine Vertretung (Schichtleitervertretung,
   Maschineneinrichter bzw. dessen Vertreter) Rechte im Voraus und befristet einstellen. (08.10., Teil C)
5. **Alle Messdaten dauerhaft in einer externen Datenbank, mit möglichst wenig Speicherbelastung.** (Teil D)
6. **Das Hauptprogramm aktualisiert nur alle paar Minuten, um Traffic zu sparen, und zeigt den
   Berechtigten eine übersichtliche Durchschnittsauswertung.** (Teil D5)
7. **Die Sicherheitsrichtlinien aus SC-900 werden durchgehend beachtet** (Abschnitt "SC-900-Zuordnung").

Leitlinie: *so sauber und sicher wie möglich.* Verweigern ist der Standard (fail closed),
Berechtigungen werden **im Dienst** geprüft und nicht nur in der Oberfläche, jede
Verweigerung wird protokolliert.

Die Namen unten sind Vorschläge. Wo der bestehende Code andere Namen hat, die vorhandenen
verwenden und nur die Struktur übernehmen. **Vor dem ersten Edit den aktuellen Stand lesen**
(`IBenutzerKontext`, `KontrolleingriffService`, `AuditLogEintrag`, `AppDbContext`, `App.xaml.cs`).

---

## Teil A – Identität und Rollen

### A1. Ausgangslage und Problem

- `IBenutzerKontext` liefert nur eine Zeichenkette `AktuellerBenutzer` (Anzeigename).
- Das Vier-Augen-Prinzip in `KontrolleingriffService` vergleicht **Anzeigenamen**. Zwei Personen
  mit gleichem Namen, eine Umbenennung oder eine andere Schreibweise brechen die Prüfung.
  Das ist eine echte Sicherheitslücke, kein Schönheitsfehler.
- Es gibt keine Rollen und keine Berechtigungsprüfung. Jeder Nutzer darf alles.

### A2. Ziele

1. Jeder Benutzer hat eine **stabile, unveränderliche Kennung**. Bei Windows ist das die SID.
2. Das Vier-Augen-Prinzip vergleicht **Kennungen**, nie Namen.
3. Es gibt fünf **Rollen**, abgeleitet aus Windows-Gruppen.
4. Aus Rollen folgen **Berechtigungen**, die in den Diensten erzwungen werden.
5. Der Anmeldeanbieter ist **per Konfiguration wählbar** (Windows / Prototyp). Entra ID wird nur
   durch die Abstraktion vorbereitet, nicht gebaut.
6. Verweigerte Zugriffe landen im Audit-Log.

### A3. Nicht-Ziele (bewusst)

Keine Anmeldemaske mit Passwort in der Anwendung. Keine eigene Benutzerverwaltung mit Passwörtern.
Kein Entra-ID-Code. Kein IdentityServer. Die Anwendung erbt die Windows-Anmeldung und speichert
keine Zugangsdaten.

### A4. Typen in `IndustrieDashboard.Core`

```csharp
// Stabile Kennung. Windows: SID als Text. Prototyp: feste Kennung wie "proto:steven".
// Spaeter Entra ID: "entra:{tid}:{oid}". Vergleich immer ordinal.
public readonly record struct BenutzerKennung(string Wert);

public sealed record Benutzer(BenutzerKennung Kennung, string Anzeigename);

public enum Rolle { Bediener, Maschineneinrichter, Schichtleitung, Instandhaltung, Administration }

public enum Berechtigung
{
    MaschinenAnsehen,
    MaschineEinrichten,         // Platzhalter, wird spaeter fachlich verfeinert
    KontrolleingriffAnfordern,
    KontrolleingriffFreigeben,
    KontrolleingriffAblehnen,
    KontrolleingriffAlsZeugeBestaetigen,
    AuswertungAnsehen,          // Durchschnittsanalyse, Teil D5
    AuditLogLesen,
    // Mit dem Modul Schichtplanung (Teil C):
    SchichtplanAnsehen,
    SchichtplanBearbeiten,
    VertretungErnennen,
}
```

`IBenutzerKontext` wird erweitert (bestehende Mitglieder behalten, wo nötig):

```csharp
Benutzer AktuellerBenutzer { get; }                 // vorher: string
IReadOnlySet<Rolle> Rollen { get; }
bool HatBerechtigung(Berechtigung berechtigung);
event EventHandler? BenutzerGewechselt;
```

`RollenBerechtigungen` ist **die eine Stelle** im Code, an der steht, welche Rolle was darf
(statische, schreibgeschützte Zuordnung `Rolle -> Berechtigung[]`). Kein Rollenwissen verstreut
in Services oder Views.

**Berechtigungsmatrix:**

| Berechtigung | Bediener | Maschineneinrichter | Schichtleitung | Instandhaltung | Administration |
|---|---|---|---|---|---|
| MaschinenAnsehen | ja | ja | ja | ja | ja |
| MaschineEinrichten (Platzhalter) | nein | ja | ja | ja | nein |
| KontrolleingriffAnfordern | ja | ja | ja | ja | nein |
| **KontrolleingriffFreigeben** | nein | nein | **nein** | **ja** | nein |
| **KontrolleingriffAblehnen** | nein | nein | **nein** | **ja** | nein |
| KontrolleingriffAlsZeugeBestaetigen | nein | nein | **ja** | nein | nein |
| AuswertungAnsehen | nein | nein | ja | ja | nein |
| AuditLogLesen | nein | nein | ja | nein | ja |
| SchichtplanAnsehen | ja | ja | ja | ja | ja |
| SchichtplanBearbeiten | nein | nein | ja | nein | nein |
| VertretungErnennen | nein | nein | ja | nein | nein |

Hinweise:

- **Neue Rolle `Maschineneinrichter`** (Windows-Gruppe `Industrie-Maschineneinrichter`). Steven (08.10.):
  Der Einrichter darf mehr als der Bediener; Einzelrechte werden später verfeinert. Vorläufig: alle
  Rechte des Bedieners plus der Platzhalter `MaschineEinrichten`. Weitere Rechte bewusst noch nicht
  festlegen; die Matrix ist die eine Stelle zum Erweitern.
- `AuswertungAnsehen` ("für die Berechtigten") und `AuditLogLesen` für die Instandhaltung sind
  Vorschläge, die Steven bestätigt.
- Hat ein Benutzer mehrere Rollen, gilt die **Vereinigung** der Berechtigungen. Hat er keine Rolle,
  gilt: **keine Berechtigung**, auch nicht `MaschinenAnsehen`.
- **Folge der Entscheidung zu Freigaben:** Freigeben dürfen nur Mitglieder der Instandhaltung. Fordert
  eine Person der Instandhaltung selbst an und ist keine zweite Person der Instandhaltung da, gilt
  der Zeugenpfad in A6.
- Funktionstrennung: `Administration` hat bewusst **keine** fachlichen Berechtigungen für Eingriffe.
  Wer Rechte verwaltet, gibt nichts frei und bestätigt nichts.

### A5. Datenmodell-Änderung

- `AuditLogEintrag`: neues `required init`-Feld `BenutzerKennung` (Text) zusätzlich zum Anzeigenamen.
  Der Anzeigename bleibt als Momentaufnahme für die Lesbarkeit.
- `KontrolleingriffAnforderung`: `AngefordertVonKennung`, `FreigegebenVonKennung` (nullable bis zur
  Freigabe), zusätzlich zu den Anzeigenamen.
- Für den Zeugenpfad (A6): `ZeugeKennung`, `ZeugeAnzeigename`, `ZeugeBestaetigtAm`, `AusnahmeGrund`.
- Neue Audit-Ereigniskategorien: `Anmeldung`, `ZugriffVerweigert`, `FreigabeMitZeuge`, `ZeugeBestaetigt`.
- Der bestehende Schutz (init-only, `SaveChanges`-Prüfung, SQLite-Trigger) bleibt und gilt auch
  für die neuen Felder.

**Schema-Änderung = jetzt der richtige Zeitpunkt für echte EF-Core-Migrationen.** Empfehlung:
`EnsureCreated()`/`SicherstellenErstelltMitAuditSchutz` durch eine Baseline-Migration ersetzen.
Die Audit-Trigger kommen per `migrationBuilder.Sql(...)` in die Migration. Es gibt keine
Produktivdaten, die vorhandene lokale Datenbank darf gelöscht werden. Als eigener, vorgezogener
Commit **vor** den Identitätsänderungen.

### A6. Vier-Augen-Prinzip (präzisiert) und Zeugenpfad

**Normalfall.** Freigabe oder Ablehnung ist nur zulässig, wenn **alle** Bedingungen gelten:

1. Aufrufer hat `KontrolleingriffFreigeben` bzw. `KontrolleingriffAblehnen` (also: Rolle Instandhaltung).
2. `AktuellerBenutzer.Kennung != AngefordertVonKennung` (ordinal, Groß-/Kleinschreibung beachten).
3. Anforderung ist im Status `Angefordert` (kein doppeltes Freigeben).
4. Alles in **einer Transaktion** mit dem Audit-Eintrag.

Fordert also ein Bediener an, gibt eine Person der Instandhaltung frei. Fordert die Instandhaltung
selbst an, gibt eine **andere** Person der Instandhaltung frei.

**Zeugenpfad (Alleinbesetzung, Entscheidung Steven).** Ist die anfordernde Person der Instandhaltung
die einzige anwesende, darf sie ihre eigene Anforderung freigeben, **wenn** ein Zeuge bestätigt.
Die Verantwortung bleibt bei der Instandhaltung; der Zeuge bezeugt nur und gibt nichts frei.

1. Ablauf: Instandhaltung fordert an. Sie wählt "Freigabe mit Zeuge" und trägt einen **Pflichtgrund**
   ein (z. B. "Alleinbesetzung"). Status `ZeugeAngefragt`.
2. Die Person mit `KontrolleingriffAlsZeugeBestaetigen` (Schichtleitung oder eine wirksame
   Schichtleitervertretung, siehe Teil C) sieht die offene Anfrage **an ihrem eigenen Arbeitsplatz
   unter ihrem eigenen Konto** und bestätigt. Status `ZeugeBestaetigt`. Die Bestätigung gilt nur für
   diesen einen Vorgang und läuft nach einer konfigurierbaren Zeit ab (Vorschlag 15 Minuten).
3. Erst danach schließt die Instandhaltung die Freigabe ab. Status `Freigegeben`.
4. Der Zeuge muss eine **dritte, andere Person** sein: Kennung des Zeugen ungleich Kennung des
   Anfordernden/Freigebenden. Der Zeuge kann nicht in derselben Sitzung oder demselben Fenster
   handeln (Prototyp: Benutzerwechsel genügt für den Test).
5. Audit: Kategorie `FreigabeMitZeuge` mit allen drei Kennungen, Zeiten und Grund. Der Zeugenpfad ist
   in der Audit-Ansicht **als Ausnahme filterbar**, damit er später nachgeprüft werden kann.
6. Der Zeugenpfad gilt **nur** für Anforderungen der Instandhaltung an sich selbst. Für alle anderen
   gilt der Normalfall.

Offene Bitte: Ob eine Zeugenbestätigung gegenüber Versicherung und Betrieb ausreicht, sollte mit
Arbeitssicherheit bzw. Betrieb geklärt werden (keine Rechtsberatung). Die Anwendung liefert den
Nachweis über Person, Handlung und Zeitpunkt.

Fehlschläge (fehlende Berechtigung, gleiche Kennung, abgelaufener Zeuge, unzulässiger Zeuge) werfen
eine eigene Ausnahme (`NichtBerechtigtException` / `VierAugenVerletzungException`) **und** schreiben
einen Audit-Eintrag `ZugriffVerweigert` (Kennung, Aktion, Grund). Der Eintrag muss auch dann
gespeichert werden, wenn die Aktion abgelehnt wird.

Hinweis: Dies ist ein Prototyp. Er ist **nicht** für sicherheitsgerichtete Steuerungsfunktionen an
echten Anlagen zugelassen oder geprüft.

### A7. Windows-Anbieter

Neues Projekt `IndustrieDashboard.Identity.Windows` (`net10.0-windows`), damit `Infrastructure`
plattformneutral bleibt (sonst Analyzer CA1416).

- `WindowsBenutzerKontext : IBenutzerKontext`
  - `WindowsIdentity.GetCurrent()`: `User.Value` = SID = Kennung; Anzeigename aus `Name`
    (Format `DOMAENE\benutzer`).
  - Rollen: je Rolle eine konfigurierte Gruppe. Die Gruppe wird **einmal beim Start** zu einer SID
    aufgelöst (`NTAccount.Translate`), die Mitgliedschaft per `WindowsPrincipal.IsInRole(SecurityIdentifier)`
    geprüft.
  - Lässt sich eine Gruppe nicht auflösen: Warnung ins Log, diese Rolle wird **nicht** vergeben.
    Niemals "im Zweifel erlauben".
  - Funktioniert auch **ohne Domäne** mit lokalen Gruppen (z. B. `AX18PRO\Industrie-Instandhaltung`).
    So lässt sich alles auf Stevens Laptops testen, ohne Active Directory.
- Rollen werden beim Start einmal ermittelt. Änderungen an der Gruppenmitgliedschaft wirken nach
  Neustart der Anwendung. Das ist zu dokumentieren.

Konfiguration `appsettings.json` (neben der EXE; enthält **keine Geheimnisse**):

```json
{
  "Sicherheit": {
    "Identitaetsanbieter": "Windows",
    "RollenGruppen": {
      "Bediener": "Industrie-Bediener",
      "Maschineneinrichter": "Industrie-Maschineneinrichter",
      "Schichtleitung": "Industrie-Schichtleitung",
      "Instandhaltung": "Industrie-Instandhaltung",
      "Administration": "Industrie-Admin"
    }
  }
}
```

Die Konfiguration kann nur **auswählen und benennen**, nie Rechte verleihen: Die Basisrolle kommt
ausschließlich aus echter Gruppenmitgliedschaft in Windows.

### A8. Prototyp-Anbieter und Fail-closed-Regeln

- `PrototypBenutzerKontext` bleibt, bekommt feste Kennungen (`proto:steven` usw.) und feste Rollen
  je Testbenutzer (Steven = Maschineneinrichter, Anna = Schichtleitung, Thomas = Instandhaltung, ein zweiter
  Instandhaltungs-Testbenutzer, ein Bediener, ein Testadmin). Der zweite Instandhaltungs-Benutzer wird
  gebraucht, damit die Freigabe im Normalfall testbar bleibt; ohne ihn wird der Zeugenpfad getestet.
- **Nur in Debug-Builds zulässig.** Ein Release-Build mit `Identitaetsanbieter = Prototyp` startet
  nicht, zeigt eine verständliche Meldung und schreibt ins Log.
- Standardwert bei fehlender oder unlesbarer Konfiguration: **Windows**.
- Unbekannter Wert in `Identitaetsanbieter`: Start verweigern.
- Die Benutzerwechsel-Oberfläche (`IBenutzerWechsel`) wird nur angezeigt, wenn der Prototyp-Anbieter aktiv ist.

### A9. Vorbereitung Entra ID (nur Abstraktion)

Keine neue Arbeit außer: `BenutzerKennung` ist freier Text mit Präfixkonvention, und
`IBenutzerKontext` kennt keine Windows-Typen. Ein späterer `EntraBenutzerKontext` liefert
`entra:{tid}:{oid}` und Rollen aus Gruppen- oder App-Rollen-Claims. Hybrid (AD + Entra) ist über
dieselbe Abstraktion lösbar, sobald der Anbieter wählbar ist.

### A10. Oberfläche

- Kopfzeile der Shell zeigt Anzeigename und Rollen (Information, keine Schutzfunktion).
- Schaltflächen für nicht erlaubte Aktionen sind deaktiviert **und** haben einen Tooltip mit dem
  Grund. Der Dienst prüft trotzdem selbst.
- Modul Kontrolleingriffe: Eigene Anforderungen zeigen "Freigabe durch andere Person nötig".

### A11. Tests (xUnit, plattformneutral wo möglich)

- `RollenBerechtigungen`: jede Zelle der Matrix; keine Rolle -> keine Berechtigung;
  **Schichtleitung und Administration dürfen nicht freigeben**.
- Vier-Augen: gleiche Kennung abgelehnt; **gleicher Anzeigename, andere Kennung erlaubt**;
  gleiche Kennung, anderer Anzeigename abgelehnt (der Regressionstest für den Fehler).
- Freigabe ohne Berechtigung: Ausnahme **und** `ZugriffVerweigert` im Audit-Log.
- Doppelte Freigabe wird abgelehnt.
- Der Audit-Schutz gilt für die neuen Felder (Update/Delete weiterhin unmöglich, auch per Trigger).
- Konfiguration: unbekannter Anbieter, fehlende Datei, Prototyp im Release-Build.
- Zeugenpfad: ohne Zeuge keine Selbstfreigabe; Zeuge gleich Anfordernder abgelehnt; Zeuge ohne Berechtigung
  (z. B. Bediener, Administration) abgelehnt; abgelaufene Bestätigung abgelehnt; Pflichtgrund fehlt abgelehnt;
  Schichtleitung kann nie selbst freigeben; Audit enthält alle drei Kennungen.
- `WindowsBenutzerKontext`: Gruppenauflösung gegen eine austauschbare Schnittstelle
  (`IGruppenPruefer`) testen, damit die Tests ohne echte Windows-Gruppen laufen.

### A12. Abnahmekriterien

1. Build 0 Fehler, 0 neue Warnungen (außer den bekannten NU1701), alle Tests grün.
2. Start mit `Windows`: Kopfzeile zeigt den echten Windows-Benutzer; ohne Gruppenzugehörigkeit sind
   alle Aktionen verweigert, und die Verweigerung steht im Audit-Log.
3. Start mit lokalem Testbenutzer in der Gruppe `Industrie-Schichtleitung`: **Freigabe ist
   verweigert**. Mit einem Benutzer in `Industrie-Instandhaltung`: Freigabe möglich, aber **nicht**
   für die eigene Anforderung.
4. Selbstfreigabe der Instandhaltung klappt nur mit Zeugenbestätigung einer anderen Person der Schichtleitung.
5. Release-Build mit `Prototyp` startet nicht.
6. Im Audit-Log steht bei jedem Eintrag die Kennung.
7. Kein Anzeigename wird mehr für eine Sicherheitsentscheidung verwendet (per `grep` über den Code nachweisen).

---

## Teil B – Anzeigeverlauf im Hauptprogramm (Arbeitsspeicher)

Der Diagrammverlauf soll einen Modulwechsel überdauern (Entscheidung Steven). Er dient **nur der
Anzeige** und ist unabhängig von der dauerhaften Speicherung in Teil D.

**Geändert durch die Entscheidung "Aktualisierung nur alle paar Minuten":** Die Anzeigepunkte
entstehen im Takt des **Aktualisierungsintervalls** (Vorschlag 5 Minuten, einstellbar von 1 bis 60),
nicht mehr alle 2 Sekunden. Das frühere 30-Minuten-Fenster würde dann nur 6 Punkte zeigen und ist
hinfällig. Neuer Vorschlag: Fenster **8 Stunden** (eine Schicht), bei 5 Minuten sind das 96 Punkte
je Metrik. Beide Werte sind Konfiguration.

- Schnittstelle `IVerlaufsDienst` in `Core` (kein WPF-Bezug): Momentaufnahme als schreibgeschützte
  Kopie, Ereignis `VerlaufAktualisiert`.
- Umsetzung `MaschinenVerlaufsDienst` in `Infrastructure`: Singleton, **Ringpuffer mit fester
  Kapazität**, thread-sicher (Lock), `IDisposable`. Er nimmt **Anzeigepunkte** entgegen (Mittelwert,
  Minimum, Maximum je Intervall), nicht Rohwerte.
- Quelle der Anzeigepunkte hinter der Schnittstelle `IAuswertungsQuelle` (siehe D5). Im Prototyp
  verdichtet eine Umsetzung die simulierten Werte im Arbeitsspeicher; später liefert PostgreSQL sie.
  Für Vorführungen darf das Intervall in der Konfiguration auf wenige Sekunden gestellt werden.
- Lebenszyklus wie die Überwachung: beim Anwendungsstart gestartet, beim Beenden **vor**
  `Log.CloseAndFlush()` entsorgt.
- `DashboardViewModel` liest beim Erzeugen die Momentaufnahme und hängt sich ans Ereignis. Der
  Wechsel auf den UI-Thread geschieht **im ViewModel**, nicht im Dienst. Beim Entsorgen wird das
  Ereignis abgemeldet.

Tests: Kapazität (der N+1-te Punkt verdrängt den ältesten), parallele Schreiber und Leser,
nach `Dispose` keine Ereignisse mehr, zweites ViewModel sieht den Verlauf des ersten ohne
zurückbleibende Abonnenten, Intervall und Fenster werden aus der Konfiguration gelesen.

Abnahme: Mit kurzem Intervall (z. B. 5 s) laufen lassen, ins andere Modul wechseln, zurück: die Kurve
ist durchgehend und enthält auch die Zeit in dem anderen Modul.

---

## Teil C – Vertretungen: Rechte im Voraus und befristet (Entwurf)

**Stevens Wunsch:** Im Reiter Schichtplanung soll die Schichtleitung einstellen können, wer eine
Rolle **vertritt**, zum Beispiel als Schichtleitervertretung oder als Maschineneinrichter bzw.
Einrichter-Vertreter, auch für die Zukunft. Umsetzung erst **zusammen mit dem Modul Schichtplanung**,
weil dafür die Zuordnung Mitarbeiter zu Schicht und zu Benutzerkennung gebraucht wird. In Teil A
wird nur die Voraussetzung geschaffen: `HatBerechtigung` kann zusätzlich zur Rollenmatrix weitere
Quellen (`IBerechtigungsQuelle`) auswerten.

### C1. Grundsatz: Eine Vertretung gibt Rolle auf Zeit, nicht beliebige Rechte

Die **Basisrolle kommt immer aus Windows** (Teil A). Eine Vertretung ist ein Eintrag
"Person X vertritt Rolle R von Datum A bis Datum B". Während der Gültigkeit erhält X die
**vertretbaren Berechtigungen** der Rolle R, und zwar nur diese. Was vertretbar ist, steht je Rolle
als feste Positivliste im Code. Fällt die Basisrolle in Windows weg, ist die Vertretung **sofort
unwirksam**.

| Vertretene Rolle | Vertretbare Berechtigungen (Vorschlag) | Nie vertretbar |
|---|---|---|
| Schichtleitung | SchichtplanBearbeiten, KontrolleingriffAlsZeugeBestaetigen | VertretungErnennen, AuditLogLesen, Freigeben, Ablehnen |
| Maschineneinrichter | MaschineEinrichten (später verfeinert) | Freigeben, Ablehnen, Zeuge |
| Instandhaltung | **keine** Vertretung durch die Anwendung | alles (siehe unten) |

Die Zeugenbefugnis ist bei der Schichtleitervertretung enthalten (Vorschlag), weil der Zeugenpfad
sonst bei Abwesenheit der Leitung ausfällt. Steven bestätigt das.

**Instandhaltung wird nicht über die Anwendung vertreten.** Die Verantwortung für Freigaben bleibt
dort, wo Steven sie festgelegt hat: Wer freigeben darf, wird ausschließlich über die Windows-Gruppe
`Industrie-Instandhaltung` bestimmt, also durch die Administration auf Anweisung des Betriebs. Die
Schichtleitung kann das nicht umgehen.

### C2. Leitplanken (jede ist ein SC-900-Prinzip, siehe unten)

1. **Nur abwärts:** Eine Vertretung kann nur für Personen mit Basisrolle Bediener oder
   Maschineneinrichter eingetragen werden, die zur Schicht der ernennenden Schichtleitung gehören.
2. **Keine Selbsternennung** und **keine Ketten:** Wer vertritt, darf selbst keine Vertretung ernennen.
3. **Befristung ist Pflicht:** `GueltigAb`, `GueltigBis`, konfigurierbare Höchstdauer (Vorschlag
   90 Tage). `GueltigAb` darf in der Zukunft liegen (**Eintragen im Voraus**).
4. **Unveränderlich protokolliert:** Eine Vertretung wird nie überschrieben oder gelöscht. Ein
   Widerruf ist ein neuer Eintrag. Schutz wie beim Audit-Trail (init-only, Prüfung im `DbContext`, Trigger).
5. **Jede Aktion landet im Audit-Log:** Ernennung, Widerruf, Verweigerung, und jede Handlung, die
   *aufgrund einer Vertretung* geschah (mit Hinweis "in Vertretung").
6. **Prüfung zur Laufzeit** gegen die aktuelle UTC-Zeit: Eine zukünftige Vertretung wirkt erst ab
   `GueltigAb`, eine abgelaufene nie mehr.

### C3. Daten

`Vertretung`: Id, `VertreterKennung`, `VertreteneRolle`, `GueltigAb`, `GueltigBis`,
`ErnanntVonKennung`, `ErnanntAm`, `Grund` (optional), für Widerruf ein Verweis auf die widerrufene
Vertretung. Dazu bekommt `Mitarbeiter` eine nullable `BenutzerKennung`.

### C4. Oberfläche

Im Reiter Schichtplanung ein Unterbereich "Vertretungen", sichtbar nur mit `VertretungErnennen`:
Liste der Mitarbeiter der eigenen Schicht mit aktiven, zukünftigen und abgelaufenen Vertretungen
(das ist zugleich die **Überprüfungsansicht** für regelmäßige Rechtekontrolle), Dialog zum
Eintragen und Widerrufen. In der Kopfzeile erscheint "in Vertretung: Schichtleitung" während der
Gültigkeit.

### C5. Offen (Steven)

Die Einzelrechte des Maschineneinrichters werden später verfeinert (Steven, 08.10.); bis dahin gilt der Platzhalter `MaschineEinrichten`. Außerdem hängt "eigene Schicht" an der noch offenen Frage zur
Schichtplanung (vier Schichtarten oder vier Schichtgruppen).

---

## Teil D – Dauerhafte Messdatenspeicherung in externer Datenbank (Entwurf)

**Ziel (Steven):** Alle aufgenommenen Daten in einer externen Datenbank speichern, mit möglichst
wenig Speicherbelastung.

**Annahme:** Externe Datenbank = **PostgreSQL** (so im ursprünglichen Technologiestack vorgesehen).
Die Anwendung greift nur über eine Schnittstelle zu, damit sich der Anbieter tauschen lässt.
Das ersetzt die frühere Einstufung "PostgreSQL nicht in Version 1.0".

### D1. Architektur

- `IMesswertSpeicher` in `Core`: `Task SchreibenAsync(IReadOnlyList<Messwert>, CancellationToken)`.
  Implementierungen: `PostgreSqlMesswertSpeicher` (neues Projekt `Infrastructure.PostgreSql`, Npgsql)
  und `NullMesswertSpeicher` (Standard, wenn keine Datenbank konfiguriert ist).
- Hintergrunddienst `MesswertErfassungsDienst`: hört auf `IMaschinenDatenQuelle`, reduziert die Daten
  (D2), sammelt sie und schreibt sie **gebündelt** (z. B. alle 10 Sekunden oder ab 500 Werten,
  Npgsql-Binärimport `COPY`) statt Einzel-Inserts.
- **Ausfallsicherheit:** Ist die Datenbank nicht erreichbar, puffert eine begrenzte lokale SQLite-
  Warteschlange (Outbox) und sendet nach. Überschreitet sie die Höchstgröße, werden zuerst die
  ältesten Rohwerte verworfen (Aggregate bleiben), mit Warnung im Log. Die Anwendung selbst blockiert nie.
- Das Dashboard liest weiter aus dem Arbeitsspeicher (Teil B), nie direkt aus der Datenbank.

### D2. Speicherbelastung minimieren

1. **Nur Änderungen speichern (Totband):** Ein Wert wird nur geschrieben, wenn er sich um mehr als
   eine Schwelle je Metrik vom zuletzt gespeicherten unterscheidet, **plus ein Lebenszeichen** nach
   spätestens z. B. 60 Sekunden, damit eine Lücke später von einem Ausfall unterscheidbar bleibt.
   Dazwischen gilt "letzter Wert bleibt".
2. **Aggregate für die Langzeit:** Pro Metrik und Minute Minimum, Maximum, Mittelwert, Anzahl.
   Rohwerte nur kurz aufbewahren (Vorschlag 7 Tage), Aggregate lange (Vorschlag mindestens 2 Jahre).
3. **Schmales Schema:** Maschine und Metrik als kleine Ganzzahl-Schlüssel, Wert als `real` (4 Byte),
   keine Textspalten in den Messdatentabellen.
4. **Partitionierung nach Zeit** (monatlich): Altes wird durch Entfernen ganzer Partitionen gelöscht
   (schnell, ohne Aufblähung der Tabelle). Optional später eine Zeitreihen-Erweiterung mit
   Kompression; das ist eine Option, keine Voraussetzung.
5. **Arbeitsspeicher der Anwendung** bleibt begrenzt: feste Kapazität bei Ringpuffer und Sendepuffer.

### D3. Sicherheit der Datenbankanbindung

- **TLS erzwingen** und das Serverzertifikat prüfen (`SSL Mode=VerifyFull`).
- **Keine Zugangsdaten im Quellcode, in der Konfigurationsdatei oder im Repo.** Quelle: Windows
  Credential Manager oder DPAPI-geschützt für den Benutzer; falls möglich Windows-integrierte
  Authentifizierung. Das Passwort erscheint nie im Log.
- **Minimale Rechte:** Das Konto der Anwendung darf in die Messdatentabellen nur schreiben
  (`INSERT`), nicht ändern oder löschen. Aufbewahrung und Partitionswartung laufen unter einem
  getrennten Wartungskonto.
- **Audit-Log in der zentralen Datenbank** nur mit `INSERT` und `SELECT`, `UPDATE`/`DELETE` entzogen,
  zusätzlich die Hash-Kette (geplant). Das lokale SQLite-Audit bleibt als Pufferspeicher.
- Parametrisierte Abfragen (EF Core/Npgsql), keine zusammengesetzten SQL-Texte.

### D4. Offen (Steven, mit Standardwerten)

Wo läuft die Datenbank (eigener Rechner zum Testen, später ein Server)? Wie lange Rohwerte und
Aggregate aufbewahrt werden (Standard 7 Tage / 2 Jahre)? Was bei längerem Datenbankausfall passieren
soll (Standard: begrenzt puffern, dann älteste Rohwerte verwerfen und warnen).
Standard für das Aktualisierungsintervall: 5 Minuten.
Für Tests wird eine lokale PostgreSQL-Instanz auf dem Laptop gebraucht.

### D5. Hauptprogramm: seltene Aktualisierung und Durchschnittsauswertung

**Ziel (Steven):** Das Hauptprogramm aktualisiert sich nur alle paar Minuten, um Traffic zu sparen,
und zeigt den Berechtigten eine klare Durchschnittsauswertung.

**Trennung der Aufgaben:**

- **Erfassung** (nah an den Maschinen, später ein eigener Dienst): schnell, mit Totband (D2),
  schreibt gebündelt in die Datenbank.
- **Hauptprogramm** (Anzeige): holt nur **fertige Minutenaggregate**, nie Rohwerte, im Takt des
  Aktualisierungsintervalls (Vorschlag 5 Minuten, einstellbar 1 bis 60).

**Traffic sparen:**

1. Inkrementelle Abfrage: "alles seit Zeitstempel X". Es werden nur neue Aggregate übertragen, nie der ganze Verlauf.
2. Aktualisierung nur, solange das Fenster sichtbar ist und das Dashboard gezeigt wird (pausiert bei
   minimiertem Fenster oder anderem Modul; beim Zurückkehren ein sofortiger Nachladevorgang).
3. Schaltfläche "Jetzt aktualisieren" für den Bedarfsfall; bei Fehlern wachsende Wartezeit statt Dauerfeuer.
4. Schmale Ergebnisse: nur die angezeigten Maschinen und Metriken, nur die gewählte Auflösung.

**Wichtige Ausnahme (Vorschlag):** Die Minutentaktung gilt für **Messwerte und Auswertung**. Offene
**Kontrolleingriff-Anforderungen** und **Alarme** dürfen nicht minutenlang unbemerkt bleiben, vor allem
bei einer Freigabe oder Zeugenbestätigung. Dafür eine kurze Abfrage (z. B. alle 15 bis 30 Sekunden;
es sind nur wenige Zeilen) oder später SignalR-Push. Steven bestätigt.

**Auswertungsansicht (nur mit `AuswertungAnsehen`):**

- Pro Maschine und Metrik eine Linie für den **Durchschnitt**, dazu ein Band von Minimum bis Maximum.
- Wählbarer Zeitraum: letzte Stunde, aktuelle Schicht, 24 Stunden, 7 Tage (größere Zeiträume nutzen
  gröbere Aggregate, damit die Datenmenge klein bleibt).
- Grenzwerte als feine Hilfslinien; Abweichungen deutlich, aber ruhig hervorgehoben.
- Eine Metrik pro Diagramm, keine überladenen Mehrfachachsen, gut lesbare Farben mit ausreichendem
  Kontrast, zusätzlich Beschriftungen statt reiner Farbcodierung.
- Zusammenfassung oben: Durchschnitt, Minimum, Maximum, Abweichung gegenüber dem Vorzeitraum.

**Schnittstelle:** `IAuswertungsQuelle` in `Core` mit Methoden wie `HoleAggregateSeit(…)` und
`HoleVerlauf(maschine, metrik, von, bis, aufloesung)`. Umsetzungen: im Arbeitsspeicher (Prototyp,
Teil B) und PostgreSQL (Teil D).

**Sicherheitshinweis zur Architektur:** Greift jedes Hauptprogramm direkt auf die Datenbank zu, liegt
das Datenbankkonto auf jedem Arbeitsplatz. Das widerspricht dem Prinzip der geringsten Rechte. Für
den Prototyp genügt ein reines Lesekonto nur für die Aggregat-Sichten, abgelegt im Windows Credential
Manager. **Zielbild:** ein kleiner Serverdienst (ASP.NET Core, SignalR wie im Stack vorgesehen), der
Rollen prüft, an der Datenbank hängt und die Daten an die Programme liefert. Die Programme kennen
dann keine Datenbankzugangsdaten mehr. Dieser Schritt wird als eigene Entscheidung geplant.

---

## SC-900-Zuordnung (Sicherheitsprinzipien in dieser Spezifikation)

Hinweis: SC-900 ist eine Zertifizierung zu Grundlagen, kein Prüfsiegel für Software. Die Tabelle
zeigt, welche Grundprinzipien die Maßnahmen abbilden.

| SC-900-Thema | Umsetzung in diesem Projekt | Stand |
|---|---|---|
| Authentifizierung vs. Autorisierung | Windows authentifiziert, die Anwendung autorisiert über Rollen | Teil A |
| Identitätsanbieter, hybride Identität | Anbieter-Abstraktion; AD jetzt, Entra ID vorbereitet | A7–A9 |
| Rollenbasierte Zugriffskontrolle (RBAC) | Rollen aus Gruppen, zentrale Matrix | A4 |
| Geringste Rechte (Least Privilege) | Standard "keine Berechtigung"; Vertretung nur Positivliste | A4, C2 |
| Funktionstrennung | Administration gibt nichts frei; Anforderer gibt nicht selbst frei | A4, A6 |
| Zero Trust: explizit prüfen | Jeder Dienst prüft selbst, nicht nur die Oberfläche | A6 |
| Zero Trust: Annahme einer Kompromittierung | Audit-Schutz auf drei Ebenen, Hash-Kette geplant, Outbox | A5 |
| Zeitlich begrenzter Zugriff (vergleichbar mit Privileged Identity Management) | Befristete Vertretungen mit Höchstdauer, im Voraus eintragbar | C2 |
| Doppelte Kontrolle bei kritischen Aktionen | Vier-Augen-Prinzip, Zeugenpfad mit drei verschiedenen Personen und Pflichtgrund | A6 |
| Datensparsamkeit, Minimierung der Datenübertragung | Aggregate statt Rohwerte, inkrementelle Abfrage, seltene Aktualisierung | D2, D5 |
| Zugriffsüberprüfung (Access Reviews) | Übersicht aktiver/zukünftiger/abgelaufener Zuweisungen | C4 |
| Verschlüsselung bei der Übertragung | TLS mit Zertifikatsprüfung zur Datenbank | D3 |
| Geheimnisverwaltung | Keine Zugangsdaten im Code oder Repo; Credential Manager/DPAPI | D3 |
| Nachvollziehbarkeit, Protokollierung (Audit) | Unveränderliches Audit-Log inklusive Verweigerungen | A5, A6 |
| Compliance, Datenaufbewahrung | Definierte Aufbewahrungsfristen, Partitions-Löschung | D2 |
| Schutz vor Bedrohungen (Erkennung) | Verweigerungs-Ereignisse als Auswertungsgrundlage; später Anbindung eines SIEM möglich | später |
| Lieferkette, Schwachstellen | Dependabot, CodeQL, Secret Scanning (Roadmap Schritt 7) | Roadmap |

Datenschutz: Das Audit-Log enthält personenbezogene Daten (Kennung, Name). Zweck und Aufbewahrungsdauer
sind zu dokumentieren; vor einem echten Betrieb ist die Mitbestimmung des Betriebsrats zu klären.
Das ist keine Rechtsberatung.

---

## Umsetzungsreihenfolge (jeweils eigener Commit, nach jedem Schritt Build und Tests)

**Stufe 1 (Teil A und B):**

1. Echte EF-Core-Migrationen mit Baseline und Audit-Triggern.
2. Core-Typen (`BenutzerKennung`, `Benutzer`, `Rolle`, `Berechtigung`, `RollenBerechtigungen`) mit Tests.
3. Schema: Kennungsfelder, Migration, angepasste Audit-Erzeugung.
4. Dienste erzwingen Berechtigung und Vier-Augen per Kennung; `ZugriffVerweigert`-Audit; Tests.
5. `PrototypBenutzerKontext` auf Kennungen und Rollen; Debug-only-Regel.
6. Projekt `Identity.Windows`, Konfiguration, Anbieterwahl in `App.xaml.cs`.
7. Oberfläche: Kopfzeile, deaktivierte Schaltflächen mit Tooltip.
8. Teil B als eigene Commits (Dienst, dann ViewModel).

**Stufe 2:** Teil D (Messdatenspeicher, PostgreSQL) als eigene detaillierte Spezifikation.
**Stufe 3:** Schichtplanung-Modul mit Teil C (Vertretungen).
**Stufe 4:** Auswertungsansicht und Aktualisierungsintervall (D5), danach die Entscheidung über den Serverdienst.

## Offene Entscheidungen für Steven

1. ~~Maschineneinrichter~~ erledigt: darf mehr als der Bediener, Einzelrechte werden später verfeinert.
2. Darf die Schichtleitervertretung als Zeuge bestätigen? (Vorschlag: ja.)
3. Wer gilt als "Berechtigte" für die Auswertung (`AuswertungAnsehen`)? Vorschlag: Schichtleitung und
   Instandhaltung. `AuditLogLesen` für die Instandhaltung: Vorschlag nein.
4. Aktualisierungsintervall (Vorschlag 5 Minuten) und Anzeigefenster (Vorschlag 8 Stunden).
5. Kontrolleingriffe und Alarme kürzer aktualisieren als die Messwerte (Vorschlag: ja).
6. Teil D: Datenbankort, Aufbewahrungsfristen, Verhalten bei Ausfall (Standardwerte stehen oben).
7. Gruppennamen (Vorschlag `Industrie-Bediener/-Maschineneinrichter/-Schichtleitung/-Instandhaltung/-Admin`).
