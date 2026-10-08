using IndustrieDashboard.Core.Enums;

namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Ein kritischer Eingriff (z. B. Maschine stoppen/Parameter ändern), der
/// nach dem Vier-Augen-Prinzip erst nach Freigabe durch eine zweite Person
/// wirksam wird.
/// </summary>
public class KontrolleingriffAnforderung
{
    public int Id { get; set; }

    public int MaschineId { get; set; }

    public string Beschreibung { get; set; } = string.Empty;

    public KontrolleingriffStatus Status { get; set; } = KontrolleingriffStatus.Angefordert;

    public string AngefordertVon { get; set; } = string.Empty;

    /// <summary>Stabile Kennung der anfordernden Person; maßgeblich für das Vier-Augen-Prinzip.</summary>
    public string AngefordertVonKennung { get; set; } = string.Empty;

    public DateTime AngefordertAm { get; set; } = DateTime.UtcNow;

    public string? FreigegebenVon { get; set; }

    /// <summary>Stabile Kennung der freigebenden Person (nullable bis zur Freigabe).</summary>
    public string? FreigegebenVonKennung { get; set; }

    public DateTime? FreigegebenAm { get; set; }

    // Zeugenpfad (Spezifikation A6): nur für Selbstfreigabe der Instandhaltung
    // bei Alleinbesetzung. Der Zeuge bezeugt nur und gibt nichts frei.

    public string? ZeugeKennung { get; set; }

    public string? ZeugeAnzeigename { get; set; }

    public DateTime? ZeugeBestaetigtAm { get; set; }

    /// <summary>Pflichtgrund für "Freigabe mit Zeuge" (z. B. "Alleinbesetzung").</summary>
    public string? AusnahmeGrund { get; set; }
}
