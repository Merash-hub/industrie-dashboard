namespace IndustrieDashboard.Core.Enums;

/// <summary>
/// Betriebszustand einer Maschine, wie er z. B. über OPC UA / MQTT gemeldet wird.
/// </summary>
public enum MaschinenStatus
{
    Unbekannt = 0,
    Laeuft = 1,
    Gestoppt = 2,
    Stoerung = 3,
    Wartung = 4,
    Ruestet = 5
}
