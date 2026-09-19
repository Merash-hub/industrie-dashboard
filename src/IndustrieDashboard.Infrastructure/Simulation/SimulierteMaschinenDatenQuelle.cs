using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using Microsoft.Extensions.Logging;

namespace IndustrieDashboard.Infrastructure.Simulation;

/// <summary>
/// Platzhalter-Implementierung von <see cref="IMaschinenDatenQuelle"/> für den
/// Prototyp: erzeugt plausible, sich verändernde Messwerte für eine feste
/// Anzahl Beispielmaschinen, per Timer statt echter OPC-UA/MQTT-Anbindung.
///
/// WICHTIG für die spätere Umsetzung: Diese Klasse ist bewusst die EINZIGE
/// Stelle, die durch echte OPC-UA-/MQTT-Clients ersetzt werden muss. Da das
/// Dashboard-Modul ausschließlich gegen <see cref="IMaschinenDatenQuelle"/>
/// programmiert, ändert sich am restlichen Code nichts.
/// </summary>
public class SimulierteMaschinenDatenQuelle : IMaschinenDatenQuelle, IDisposable
{
    private readonly ILogger<SimulierteMaschinenDatenQuelle> _logger;
    private readonly Random _zufall = new();
    private readonly List<Maschine> _maschinen;
    private Timer? _timer;

    public event EventHandler<MaschinenwertAktualisiertEventArgs>? WertAktualisiert;

    public SimulierteMaschinenDatenQuelle(ILogger<SimulierteMaschinenDatenQuelle> logger)
    {
        _logger = logger;
        _maschinen = ErzeugeBeispielMaschinen();
    }

    public Task<IReadOnlyList<Maschine>> GetMaschinenAsync(CancellationToken ct = default)
    {
        return Task.FromResult((IReadOnlyList<Maschine>)_maschinen);
    }

    public void StartUeberwachung()
    {
        if (_timer is not null)
        {
            return;
        }

        _logger.LogInformation("Simulierte Maschinenüberwachung gestartet (Platzhalter für OPC UA / MQTT)");
        _timer = new Timer(_ => TickErzeugen(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
    }

    public void StopUeberwachung()
    {
        _timer?.Dispose();
        _timer = null;
        _logger.LogInformation("Simulierte Maschinenüberwachung gestoppt");
    }

    private void TickErzeugen()
    {
        foreach (var maschine in _maschinen)
        {
            // Zufälliger Random Walk um den aktuellen Wert, mit gelegentlicher Störung.
            var stoerungsChance = _zufall.NextDouble();
            if (stoerungsChance < 0.03)
            {
                maschine.Status = MaschinenStatus.Stoerung;
                maschine.AktuellerWert = 0;
            }
            else
            {
                maschine.Status = MaschinenStatus.Laeuft;
                var delta = (_zufall.NextDouble() - 0.5) * 8;
                maschine.AktuellerWert = Math.Clamp(maschine.AktuellerWert + delta, 0, 100);
            }

            maschine.LetzteAktualisierung = DateTime.UtcNow;

            WertAktualisiert?.Invoke(this, new MaschinenwertAktualisiertEventArgs
            {
                MaschineId = maschine.Id,
                NeuerWert = maschine.AktuellerWert,
                Status = maschine.Status
            });
        }
    }

    private List<Maschine> ErzeugeBeispielMaschinen()
    {
        return new List<Maschine>
        {
            new() { Id = 1, Name = "Rundlaufmessmaschine 1", Standort = "Halle 3", FeldbusKennung = "opc.tcp://sps01/RLM1", Status = MaschinenStatus.Laeuft, AktuellerWert = 72 },
            new() { Id = 2, Name = "Rundlaufmessmaschine 2", Standort = "Halle 3", FeldbusKennung = "opc.tcp://sps01/RLM2", Status = MaschinenStatus.Laeuft, AktuellerWert = 65 },
            new() { Id = 3, Name = "Extruder 1", Standort = "Halle 1", FeldbusKennung = "mqtt://plant/extruder1", Status = MaschinenStatus.Laeuft, AktuellerWert = 88 },
            new() { Id = 4, Name = "Vulkanisationspresse A", Standort = "Halle 2", FeldbusKennung = "opc.tcp://sps02/VP-A", Status = MaschinenStatus.Wartung, AktuellerWert = 0 },
        };
    }

    public void Dispose() => StopUeberwachung();
}
