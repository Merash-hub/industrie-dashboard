# Auftrag an Claude Code: Stufe 1b (CI, Lieferkette, README)

Du arbeitest im Repo `C:\Projekte\IndustrieDashboard`. Steven ist kein Berufsentwickler: Erkläre auf Deutsch, einfach, ohne Jargon.
Leitlinie wie immer: so sauber und sicher wie möglich. Es gelten die Arbeitsregeln aus `docs/auftrag-stufe1.md` (Build und Tests nach jedem Schritt, ein Commit pro Schritt, Autor unverändert, keine echte E-Mail, keine Zugangsdaten, keine Systemänderungen, nichts nebenbei ändern, nicht pushen bevor Steven "push" sagt).

## Ziel
Bei jedem Push und jedem Pull Request baut und testet GitHub das Projekt automatisch auf einem sauberen Windows-Rechner. Die Lieferkette wird überwacht. Die README beschreibt den tatsächlichen Stand.

## Schritt 1: CI-Workflow `.github/workflows/ci.yml`
- Trigger: `push` auf `main` und `pull_request` gegen `main`. **Niemals** `pull_request_target`.
- `permissions: contents: read` auf oberster Ebene, keine weiteren Rechte, keine Secrets.
- `concurrency`: laufende Läufe derselben Referenz abbrechen. `timeout-minutes: 20`.
- Ein Job auf `windows-latest`:
  - `actions/checkout` mit `persist-credentials: false`
  - `actions/setup-dotnet` mit .NET 10 (`10.0.x`)
  - `dotnet restore`
  - `dotnet build` in **Debug** und danach in **Release** (Release prüft, dass alles kompiliert; die Tests laufen in Debug, weil der Prototyp-Anbieter in Release absichtlich nicht startet)
  - `dotnet test` in Debug, Ergebnis als Testbericht hochladen ist optional (nur wenn mit GitHub-eigenen Aktionen ohne Zusatzrechte möglich)
  - `dotnet list package --vulnerable --include-transitive`: der Lauf schlägt fehl, wenn bekannte Schwachstellen gemeldet werden (Ausgabe auf den Hinweistext prüfen, nicht auf den Exitcode, der ist immer 0)
- **Nur GitHub-eigene Aktionen** (`actions/...`). Jede Aktion auf einen **vollständigen Commit-Hash** festnageln, mit Kommentar zur Version. Hash selbst ermitteln: neueste stabile Hauptversion, die älter als zwei Wochen ist, per `git ls-remote https://github.com/actions/<name>` (bei Tags mit `^{}` den eigentlichen Commit nehmen). Nichts erfinden, nur Hashes verwenden, die du wirklich abgefragt hast.
- Kein CodeQL-Workflow anlegen. Steven richtet CodeQL über die GitHub-Einstellungen ein, sonst gibt es Konflikte.
- YAML vor dem Commit auf Syntaxfehler prüfen (z. B. per Python-YAML-Parser oder `actionlint`, falls schon vorhanden; nichts installieren).

## Schritt 2: `.github/dependabot.yml`
- Ökosysteme `nuget` und `github-actions`, wöchentlich, `open-pull-requests-limit` 5, Microsoft-Pakete gruppieren.
- Kommentar in der Datei: Vorschläge von Dependabot werden nicht ungeprüft übernommen, sondern erst nach grünem CI-Lauf.

## Schritt 3: `SECURITY.md`
- Zweck: Wie man eine Sicherheitslücke meldet. **Über GitHub "Report a vulnerability" (privates Melden)**, nicht per E-Mail, keine Adresse nennen.
- Unterstützter Stand: nur `main`. Hinweis: Prototyp, nicht für sicherheitsgerichtete Steuerungsfunktionen an echten Anlagen zugelassen oder geprüft.
- Was im Umfang ist (Quellcode, Konfiguration) und was nicht (Test- und Simulationsdaten).
- Kurzer Absatz zu den bestehenden Schutzmaßnahmen (Rollen, Vier-Augen, Audit-Schutz, Fail-closed) mit Verweis auf `docs/spec-identitaet-rollen.md`.

## Schritt 4: README aufräumen (Wahrheitsgehalt vor Schönheit)
Gegen den echten Stand prüfen und korrigieren:
- Tabelle "Was ist enthalten": Kontrolleingriffe sind **funktionsfähig** (mit Zeugenpfad und Zurückziehen), Identität und Rollen (Windows-Anbieter, fünf Rollen), Audit-Schutz, Migrationen, Anzeigeverlauf. Weiterhin offen: Schichtplanung, Maschinenüberwachung als Fachmodul, OPC UA/MQTT/SignalR, TLS, Serverdienst, externe Datenbank.
- Strukturbaum: neues Projekt `IndustrieDashboard.Identity.Windows` aufnehmen, Kontrolleingriffe nicht mehr als Platzhalter.
- Den Abschnitt "Hinweis zur Entstehung" (Linux-Sandbox, nicht gebaut) **ersetzen** durch "Qualitätsnachweis": Build und Tests laufen bei jedem Push automatisch auf Windows (Statusabzeichen für den Workflow oben in der README), zuletzt manuell auf Windows 10 und 11 geprüft.
- Neuer kurzer Abschnitt "Sicherheit": Was umgesetzt ist, was bewusst offen ist, mit Verweis auf `SECURITY.md`, Spezifikation und `CHANGELOG.md`. Ehrlicher Hinweis: Prototyp, nicht für sicherheitsgerichtete Funktionen.
- Keine persönlichen Angaben, kein Bezug zu Bewerbungen, keine Firmeninterna.

## Schritt 5: Doku
- `CHANGELOG.md`: Eintrag zu diesem Auftrag.
- `CLAUDE.md`: kurz erwähnen, dass CI existiert, wie lokal dasselbe geprüft wird, und dass Aktionen nur auf Commit-Hash festgenagelt und nur GitHub-eigen sein dürfen.

## Abschluss
- Lokal alles prüfen: Build, Tests, keine Warnungen außer den bekannten.
- **Nicht pushen**, sondern Bericht an Steven: je Schritt Commit-Kürzel, die verwendeten Aktionen mit Hash und Version, Abweichungen, offene Punkte.
- Nach Stevens "push": pushen und Steven sagen, dass er unter dem Reiter "Actions" des Repos prüft, ob der erste Lauf grün ist. Schlägt er fehl, hilfst du beim Beheben anhand des Textes, den Steven dir einfügt.
