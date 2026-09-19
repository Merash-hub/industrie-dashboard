namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Unveränderlicher Audit-Trail-Eintrag für sicherheitsrelevante Aktionen.
/// Wird u. a. beim Vier-Augen-Prinzip für Kontrolleingriffe geschrieben.
/// Alle Eigenschaften außer <see cref="Id"/> sind nur bei der Erzeugung
/// befüllbar (init-only); nachträgliches Ändern ist bereits durch den
/// Compiler ausgeschlossen. Zusätzlich blockiert der Datenkontext der
/// Infrastruktur-Schicht Update/Delete auf dieser Tabelle technisch.
/// </summary>
public class AuditLogEintrag
{
    public int Id { get; private set; }

    public DateTime Zeitstempel { get; init; } = DateTime.UtcNow;

    public required string Benutzer { get; init; }

    public required string Aktion { get; init; }

    public required string Zielobjekt { get; init; }

    public string? AlterWert { get; init; }

    public string? NeuerWert { get; init; }

    public string? Begruendung { get; init; }

    /// <summary>Zweite Person beim Vier-Augen-Prinzip (falls zutreffend).</summary>
    public string? GegengezeichnetVon { get; init; }
}
