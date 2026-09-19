using IndustrieDashboard.Core.Enums;

namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Stammdaten einer überwachten Maschine/Anlage.
/// </summary>
public class Maschine
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Standort { get; set; } = string.Empty;

    /// <summary>
    /// Technische Kennung/Adresse in der Feldebene, z. B. OPC-UA-NodeId oder MQTT-Topic.
    /// Für den Prototyp nur informativ, ohne echte Anbindung.
    /// </summary>
    public string FeldbusKennung { get; set; } = string.Empty;

    public MaschinenStatus Status { get; set; } = MaschinenStatus.Unbekannt;

    /// <summary>Zuletzt gemeldeter Hauptmesswert (z. B. Auslastung in %).</summary>
    public double AktuellerWert { get; set; }

    public DateTime LetzteAktualisierung { get; set; } = DateTime.UtcNow;

    public List<Maschinenmesswert> Messwerte { get; set; } = new();
}
