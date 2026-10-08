using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using Microsoft.Extensions.Logging;

namespace IndustrieDashboard.Infrastructure.Services;

/// <summary>
/// Langlebiger Singleton-Dienst für den Anzeigeverlauf (Spezifikation Teil B):
/// Ringpuffer fester Kapazität (Fenster ÷ Intervall), thread-sicher per Lock.
/// Holt sich im konfigurierten Intervall einen neuen Anzeigepunkt von
/// <see cref="IAuswertungsQuelle"/> und löst dann <see cref="VerlaufAktualisiert"/>
/// aus. Lebenszyklus wie die Überwachung: <see cref="StarteErfassung"/> einmal
/// beim Anwendungsstart, <see cref="IDisposable.Dispose"/> beim Beenden vor
/// <c>Log.CloseAndFlush()</c> (siehe App.xaml.cs).
/// </summary>
public sealed class MaschinenVerlaufsDienst : IVerlaufsDienst, IDisposable
{
    private readonly IAuswertungsQuelle _auswertungsQuelle;
    private readonly ILogger<MaschinenVerlaufsDienst> _logger;
    private readonly TimeSpan _intervall;
    private readonly int _kapazitaet;
    private readonly object _sperre = new();
    private readonly LinkedList<Anzeigepunkt> _ringpuffer = new();

    private Timer? _timer;
    private volatile bool _entsorgt;

    /// <summary>
    /// Direkter Konstruktor mit Intervall/Fenster als <see cref="TimeSpan"/> -
    /// vor allem für Tests, die eine kleine, schnell erreichbare Kapazität
    /// brauchen, ohne an die auf ganze Stunden beschränkte
    /// <see cref="VerlaufsOptionen.FensterStunden"/>-Konfiguration gebunden zu sein.
    /// </summary>
    public MaschinenVerlaufsDienst(IAuswertungsQuelle auswertungsQuelle, TimeSpan intervall, TimeSpan fenster, ILogger<MaschinenVerlaufsDienst> logger)
    {
        _auswertungsQuelle = auswertungsQuelle;
        _logger = logger;
        _intervall = intervall;
        _kapazitaet = Math.Max(1, (int)Math.Round(fenster / intervall));
    }

    public MaschinenVerlaufsDienst(IAuswertungsQuelle auswertungsQuelle, VerlaufsOptionen optionen, ILogger<MaschinenVerlaufsDienst> logger)
        : this(auswertungsQuelle, optionen.Intervall, optionen.Fenster, logger)
    {
    }

    public IReadOnlyList<Anzeigepunkt> Momentaufnahme
    {
        get
        {
            lock (_sperre)
            {
                return _ringpuffer.ToArray();
            }
        }
    }

    public event EventHandler? VerlaufAktualisiert;

    /// <summary>Nur für Diagnose und Tests: Anzahl aktuell registrierter Abonnenten von <see cref="VerlaufAktualisiert"/>.</summary>
    public int AnzahlAbonnenten => VerlaufAktualisiert?.GetInvocationList().Length ?? 0;

    public void StarteErfassung()
    {
        if (_timer is not null || _entsorgt)
        {
            return;
        }

        _logger.LogInformation(
            "Anzeigeverlauf gestartet (Intervall {Intervall}, Kapazität {Kapazitaet} Punkte)",
            _intervall, _kapazitaet);
        _timer = new Timer(_ => Erfassen(), null, _intervall, _intervall);
    }

    public void StoppeErfassung()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void Erfassen()
    {
        if (_entsorgt)
        {
            return;
        }

        var punkt = _auswertungsQuelle.ErfasseAktuellenPunkt();
        if (punkt is null)
        {
            return;
        }

        lock (_sperre)
        {
            if (_ringpuffer.Count >= _kapazitaet)
            {
                _ringpuffer.RemoveFirst();
            }

            _ringpuffer.AddLast(punkt);
        }

        if (_entsorgt)
        {
            return;
        }

        VerlaufAktualisiert?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _entsorgt = true;
        _timer?.Dispose();
        _timer = null;
        VerlaufAktualisiert = null;
    }
}
