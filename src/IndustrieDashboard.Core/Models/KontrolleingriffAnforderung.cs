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

    public DateTime AngefordertAm { get; set; } = DateTime.UtcNow;

    public string? FreigegebenVon { get; set; }

    public DateTime? FreigegebenAm { get; set; }
}
