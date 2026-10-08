using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Infrastructure.Services;

/// <summary>
/// Prototyp-Umsetzung von <see cref="IAuswertungsQuelle"/>: sammelt die
/// Rohwerte aller Maschinen aus <see cref="IMaschinenDatenQuelle"/> im
/// Arbeitsspeicher und verdichtet sie bei Abruf zu einem Anzeigepunkt
/// (Mittelwert/Minimum/Maximum über alle Maschinen seit dem letzten Abruf).
/// Später ersetzt eine PostgreSQL-Umsetzung diese Klasse (Teil D, Stufe 2).
/// </summary>
public sealed class SimulierteAuswertungsQuelle : IAuswertungsQuelle, IDisposable
{
    private readonly IMaschinenDatenQuelle _maschinenDatenQuelle;
    private readonly object _sperre = new();
    private readonly List<double> _rohwerteSeitLetztemAbruf = new();

    public SimulierteAuswertungsQuelle(IMaschinenDatenQuelle maschinenDatenQuelle)
    {
        _maschinenDatenQuelle = maschinenDatenQuelle;
        _maschinenDatenQuelle.WertAktualisiert += OnWertAktualisiert;
    }

    private void OnWertAktualisiert(object? sender, MaschinenwertAktualisiertEventArgs e)
    {
        lock (_sperre)
        {
            _rohwerteSeitLetztemAbruf.Add(e.NeuerWert);
        }
    }

    public Anzeigepunkt? ErfasseAktuellenPunkt()
    {
        lock (_sperre)
        {
            if (_rohwerteSeitLetztemAbruf.Count == 0)
            {
                return null;
            }

            var punkt = new Anzeigepunkt(
                DateTime.UtcNow,
                Mittelwert: _rohwerteSeitLetztemAbruf.Average(),
                Minimum: _rohwerteSeitLetztemAbruf.Min(),
                Maximum: _rohwerteSeitLetztemAbruf.Max());

            _rohwerteSeitLetztemAbruf.Clear();
            return punkt;
        }
    }

    public void Dispose() => _maschinenDatenQuelle.WertAktualisiert -= OnWertAktualisiert;
}
