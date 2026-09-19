using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Abstraktion über die Quelle von Maschinendaten. In der Zielarchitektur
/// implementiert durch OPC-UA- und MQTT-Clients; für den Prototyp durch
/// eine Simulation ersetzt. Das Dashboard-Modul kennt nur dieses Interface.
/// </summary>
public interface IMaschinenDatenQuelle
{
    Task<IReadOnlyList<Maschine>> GetMaschinenAsync(CancellationToken ct = default);

    /// <summary>Wird ausgelöst, sobald neue Live-Werte für eine Maschine vorliegen.</summary>
    event EventHandler<MaschinenwertAktualisiertEventArgs>? WertAktualisiert;

    void StartUeberwachung();

    void StopUeberwachung();
}

public class MaschinenwertAktualisiertEventArgs : EventArgs
{
    public required int MaschineId { get; init; }
    public required double NeuerWert { get; init; }
    public required Enums.MaschinenStatus Status { get; init; }
    public DateTime Zeitstempel { get; init; } = DateTime.UtcNow;
}
