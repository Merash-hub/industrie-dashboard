# Sicherheitsrichtlinie

## Eine Sicherheitslücke melden

Bitte melde Sicherheitslücken **privat über GitHub**: im Repository auf den Reiter
**„Security"** gehen und dort **„Report a vulnerability"** wählen. So sieht nur der
Betreuer des Projekts die Meldung, bis eine Lösung vorliegt.

Bitte **keine** öffentlichen Issues, Pull Requests oder Diskussionen für Sicherheitslücken
anlegen. Eine E-Mail-Adresse für Meldungen gibt es bewusst nicht.

Hilfreich in einer Meldung:

- betroffene Stelle (Datei, Klasse, Konfiguration) und Commit-Kürzel
- Schritte zum Nachvollziehen
- was dadurch möglich wird (z. B. Aktion ohne Berechtigung, Umgehen des Vier-Augen-Prinzips,
  Ändern des Audit-Logs)

## Unterstützter Stand

| Stand | Unterstützt |
|---|---|
| `main` (neuester Commit) | ✅ |
| ältere Commits, Meilensteine | ❌ |

**Wichtiger Hinweis:** Dies ist ein Prototyp. Er ist **nicht** für sicherheitsgerichtete
Steuerungsfunktionen an echten Anlagen zugelassen oder geprüft (zum Beispiel Not-Halt im Sinne
der Maschinensicherheit). Ein „Not-Stopp" in der Anwendung ist eine protokollierte Anforderung,
keine Sicherheitsfunktion.

## Umfang

**Im Umfang:**

- Quellcode in `src/` und `tests/`
- Konfiguration (`appsettings.json`, `appsettings.Development.json`, Projektdateien,
  `.github/`)

**Nicht im Umfang:**

- Test- und Simulationsdaten (z. B. die simulierten Maschinenwerte und die festen
  Prototyp-Testbenutzer)
- Schwachstellen in Fremdpaketen selbst; diese bitte beim jeweiligen Projekt melden. Dass
  dieses Projekt eine verwundbare Version einsetzt, ist dagegen im Umfang.

## Bestehende Schutzmaßnahmen

- **Rollen:** Fünf Rollen mit einer zentralen Berechtigungsmatrix. Im Betrieb kommen die Rollen
  ausschließlich aus echter Windows-Gruppenmitgliedschaft; die Konfiguration kann keine Rechte
  verleihen.
- **Vier-Augen-Prinzip:** Kontrolleingriffe müssen von einer anderen Person freigegeben werden.
  Verglichen wird die stabile Benutzerkennung (Windows: SID), nie der Anzeigename. Bei
  Alleinbesetzung der Instandhaltung gibt es einen Zeugenpfad mit Pflichtgrund.
- **Audit-Schutz:** Das Audit-Log ist nur erweiterbar. Ändern und Löschen werden im Code und
  zusätzlich in der Datenbank (Trigger) abgelehnt. Jede Verweigerung wird protokolliert.
- **Fail closed:** Im Zweifel wird verweigert. Keine Rolle bedeutet keine Berechtigung; eine nicht
  auflösbare oder zu breite Windows-Gruppe vergibt keine Rolle; der Prototyp-Anbieter startet in
  Release-Builds nicht.
- **Lieferkette:** Build, Tests und eine Prüfung auf bekannte Schwachstellen in Paketen laufen bei
  jedem Push automatisch (`.github/workflows/ci.yml`); Dependabot schlägt Aktualisierungen vor.

Details: [`docs/spec-identitaet-rollen.md`](docs/spec-identitaet-rollen.md).
