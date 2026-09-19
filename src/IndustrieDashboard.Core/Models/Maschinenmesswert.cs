namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Ein einzelner Zeitreihen-Messwert einer Maschine (Telemetrie).
/// In der Zielarchitektur über MQTT/OPC UA befüllt, im Prototyp simuliert.
/// </summary>
public class Maschinenmesswert
{
    public int Id { get; set; }

    public int MaschineId { get; set; }

    public Maschine? Maschine { get; set; }

    public string Kennwert { get; set; } = "Auslastung";

    public double Wert { get; set; }

    public DateTime Zeitstempel { get; set; } = DateTime.UtcNow;
}
