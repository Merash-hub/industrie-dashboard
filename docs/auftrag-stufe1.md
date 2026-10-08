# Auftrag an Claude Code: Stufe 1 (Identität, Rollen, Anzeigeverlauf)

Du arbeitest im Repo `C:\Projekte\IndustrieDashboard`. Steven Katzer ist kein Berufsentwickler:
Erkläre auf Deutsch, einfach und ohne unnötigen Jargon.

## Zuerst lesen

1. `CLAUDE.md`
2. `docs/spec-identitaet-rollen.md` – **verbindlich**. Umzusetzen sind **nur Teil A und Teil B**.
   Teil C (Vertretungen) und Teil D (PostgreSQL, Auswertungsansicht) **nicht** umsetzen.
3. `git log --oneline -10` und `git status`

## Leitlinie

Steven will das Programm **so sauber und sicher wie möglich**. Verweigern ist der Standard
(fail closed). Berechtigungen werden im Dienst erzwungen, nicht nur in der Oberfläche. Jede
Verweigerung wird im Audit-Log protokolliert.

## Arbeitsregeln

- Nach **jedem Schritt**: `dotnet build` (0 Fehler, keine neuen Warnungen außer den bekannten NU1701)
  und `dotnet test` (alles grün). Erst dann committen. Schlägt etwas fehl: beheben. Nichts überspringen,
  keine Tests abschwächen oder löschen, nichts mit `#pragma`/Unterdrückung wegschieben.
- **Ein Commit pro Schritt**, Meldung auf Deutsch, Autor unverändert lassen (private noreply-Adresse).
  Keine echte E-Mail-Adresse, keine Zugangsdaten, keine Namen echter Kollegen oder Firmeninterna in Code,
  Doku oder Commit-Texten.
- **Nicht pushen**, bevor Steven ausdrücklich „push" sagt.
- **Keine Systemänderungen:** keine lokalen Benutzer oder Gruppen anlegen, keine Windows-Einstellungen
  ändern, nichts installieren ohne Rückfrage. Die vorhandene lokale Datenbank
  (`%LOCALAPPDATA%\IndustrieDashboard\`) **nicht löschen, sondern in `.bak` umbenennen**.
- Weicht der vorhandene Code in Namen oder Aufbau von der Spezifikation ab: vorhandene Namen behalten,
  die Struktur der Spezifikation umsetzen, die Abweichung im Bericht nennen. Gibt es einen Widerspruch,
  den du nicht selbst auflösen kannst: **stoppen und Steven fragen.**
- Nichts „nebenbei" außerhalb des Auftrags ändern.

## Schritte

**0. Ausgangslage.** `git pull`, Stand sauber? Build und Tests als Basis ausführen (zuletzt 22/22 grün).
Falls `docs/spec-identitaet-rollen.md` und diese Datei noch nicht eingecheckt sind: als eigenen Commit
„Doku: Spezifikation Identität und Rollen, Auftrag Stufe 1".

**1. Echte EF-Core-Migrationen** (Spec A5). Baseline-Migration anstelle von `EnsureCreated()` bzw.
`SicherstellenErstelltMitAuditSchutz`. Die Audit-Trigger kommen per `migrationBuilder.Sql(...)` in die
Migration (mit sauberem `Down`). `App.xaml.cs` ruft beim Start `Database.Migrate()` auf. Vorhandene
Datenbank vorher sichern (siehe Regeln). Test: Nach `Migrate()` existieren die Trigger, und
`ExecuteUpdate`/`ExecuteDelete`/Roh-SQL auf `AuditLog` scheitern weiterhin.

**2. Core-Typen und Matrix** (Spec A4). `BenutzerKennung`, `Benutzer`, `Rolle` (fünf Rollen),
`Berechtigung` (inklusive `MaschineEinrichten` als Platzhalter), `RollenBerechtigungen` als **einzige**
Stelle der Zuordnung, `IBenutzerKontext` erweitern. Tests: **jede Zelle der Matrix**; keine Rolle ->
keine Berechtigung; Schichtleitung und Administration dürfen nie freigeben.

**3. Schema und Audit** (Spec A5). Kennungsfelder in `AuditLogEintrag` und
`KontrolleingriffAnforderung`, Felder für den Zeugenpfad, neue Audit-Kategorien. Neue Migration.
Der bestehende Audit-Schutz muss für die neuen Felder weiter gelten (Test).

**4. Dienste erzwingen** (Spec A6). `KontrolleingriffService`: Berechtigungsprüfung, Vier-Augen **per
Kennung** (nie per Anzeigename), **Zeugenpfad** mit allen Regeln, Audit-Eintrag `ZugriffVerweigert`, der
auch bei abgelehnter Aktion gespeichert wird, Freigabe und Audit in einer Transaktion. Alle Tests aus
Spec A11, besonders der Regressionstest „gleicher Anzeigename, andere Kennung".
**-> ZWISCHENBERICHT an Steven, dann auf sein OK warten.**

**5. Prototyp-Anbieter** (Spec A8). `PrototypBenutzerKontext` mit festen Kennungen und Rollen
(Benutzer laut Spec, inklusive zweiter Instandhaltung, Bediener, Administrator). Nur in Debug-Builds
zulässig. Der Benutzerwechsel erscheint nur, wenn dieser Anbieter aktiv ist.

**6. Windows-Anbieter** (Spec A7). Neues Projekt `IndustrieDashboard.Identity.Windows`
(`net10.0-windows`), `WindowsBenutzerKontext`, Gruppenauflösung hinter `IGruppenPruefer`, damit
Tests ohne echte Windows-Gruppen laufen. Konfiguration `appsettings.json` mit sicherem Standard
`Windows`. Für die Entwicklung eine Datei `appsettings.Development.json` mit `Prototyp`, die **nur bei
Debug-Builds** ins Ausgabeverzeichnis kopiert wird (Bedingung in der `.csproj`), nie bei Release. Fehlende
oder unlesbare Konfiguration -> `Windows`. Unbekannter Wert, oder `Prototyp` im Release -> Start
verweigern mit verständlicher Meldung. Tests laut Spec A11.

**7. Oberfläche** (Spec A10, A6). Kopfzeile mit Anzeigename und Rollen. Nicht erlaubte Schaltflächen
deaktiviert mit Tooltip, der den Grund nennt. Im Modul Kontrolleingriffe der Zeugen-Ablauf: „Freigabe
mit Zeuge" mit Pflichtgrund, Zeuge sieht offene Anfragen und bestätigt, danach schließt die Instandhaltung
ab. Eigene Anforderungen zeigen „Freigabe durch andere Person nötig".

**8. Anzeigeverlauf** (Spec Teil B). `IVerlaufsDienst` und `MaschinenVerlaufsDienst` als langlebiger,
WPF-freier Singleton mit Ringpuffer, Quelle über `IAuswertungsQuelle` (Arbeitsspeicher-Umsetzung).
Intervall und Fenster aus der Konfiguration (Standard 5 Minuten / 8 Stunden; zum Testen kurzes
Intervall möglich). Lebenszyklus wie die Überwachung (Start in `App.xaml.cs`, Entsorgung vor
`Log.CloseAndFlush()`). `DashboardViewModel` liest die Momentaufnahme und marshallt selbst auf den
UI-Thread. Tests laut Spec Teil B.

**9. Abschluss.**

- Abnahme nach Spec A12 und Teil B durchführen (die Anwendung wirklich starten und prüfen), auch
  den `grep`-Nachweis, dass kein Anzeigename mehr für Sicherheitsentscheidungen dient.
- `CLAUDE.md` aktualisieren (technischer Stand, Sicherheitsleitlinien: fail closed, Prüfung im Dienst,
  Kennung statt Name, Konfiguration verleiht keine Rechte). Die Paketversionsangabe MaterialDesignThemes
  dort auf 5.3.2 korrigieren, falls noch 5.3.3 steht.
- Kurzen Abschnitt in `README.md` anpassen (Migrationen statt `EnsureCreated()`, Identität und Rollen).

## Bericht an Steven (Zwischen- und Abschlussbericht)

Auf Deutsch, einfach: Was ist erledigt (je Schritt Commit-Kürzel), Build-Ergebnis, Anzahl Tests,
Abweichungen von der Spezifikation, offene Punkte, und **was Steven selbst ausprobieren kann**. Für den
manuellen Test des Windows-Anbieters gibst du Steven die benötigten Befehle zum Anlegen lokaler Gruppen
(z. B. `net localgroup Industrie-Instandhaltung /add`) **als Text**, damit er sie selbst in einer
Administrator-PowerShell ausführt. Du führst sie nicht aus.
