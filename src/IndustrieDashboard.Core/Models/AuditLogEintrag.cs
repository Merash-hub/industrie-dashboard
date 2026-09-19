namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Unveränderlicher Audit-Trail-Eintrag für sicherheitsrelevante Aktionen.
/// Wird u. a. beim Vier-Augen-Prinzip für Kontrolleingriffe geschrieben.
/// </summary>
public class AuditLogEintrag
{
    public int Id { get; set; }

    public DateTime Zeitstempel { get; set; } = DateTime.UtcNow;

    public string Benutzer { get; set; } = string.Empty;

    public string Aktion { get; set; } = string.Empty;

    public string Zielobjekt { get; set; } = string.Empty;

    public string? AlterWert { get; set; }

    public string? NeuerWert { get; set; }

    public string? Begruendung { get; set; }

    /// <summary>Zweite Person beim Vier-Augen-Prinzip (falls zutreffend).</summary>
    public string? GegengezeichnetVon { get; set; }
}
