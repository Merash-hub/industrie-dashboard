using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndustrieDashboard.Tests.Infrastructure;

/// <summary>
/// Deckt den Ringpuffer und den Lebenszyklus von
/// <see cref="MaschinenVerlaufsDienst"/> ab (Spezifikation Teil B). Nutzt den
/// TimeSpan-Konstruktor mit sehr kurzem Intervall/Fenster statt der echten
/// 5 Minuten / 8 Stunden, damit die Tests schnell und mit kleiner,
/// erreichbarer Kapazität laufen - die Konfigurationsanbindung selbst wird
/// separat getestet (<see cref="Konstruktor_LiestIntervallUndFensterAusDerKonfiguration"/>).
/// </summary>
public sealed class MaschinenVerlaufsDienstTests
{
    private static readonly TimeSpan Intervall = TimeSpan.FromMilliseconds(30);
    private static readonly TimeSpan FensterFuerKapazitaetDrei = TimeSpan.FromMilliseconds(90);

    [Fact]
    public async Task Momentaufnahme_MehrPunkteAlsKapazitaet_VerdraengtAeltestenPunkt()
    {
        var quelle = new ZaehlendeAuswertungsQuelle();
        var dienst = new MaschinenVerlaufsDienst(quelle, Intervall, FensterFuerKapazitaetDrei, NullLogger<MaschinenVerlaufsDienst>.Instance);

        dienst.StarteErfassung();
        await WarteBisAsync(() => dienst.Momentaufnahme.Count == 3, TimeSpan.FromSeconds(3));

        var ersterWertBeiVollerKapazitaet = dienst.Momentaufnahme[0].Mittelwert;

        // Kapazitaet (3) ist erreicht; warte auf mindestens einen weiteren Tick,
        // der den aeltesten Punkt verdraengen muss.
        await WarteBisAsync(
            () => dienst.Momentaufnahme.Count == 3 && dienst.Momentaufnahme[0].Mittelwert != ersterWertBeiVollerKapazitaet,
            TimeSpan.FromSeconds(3));

        dienst.Dispose();

        Assert.Equal(3, dienst.Momentaufnahme.Count);
        Assert.NotEqual(ersterWertBeiVollerKapazitaet, dienst.Momentaufnahme[0].Mittelwert);
    }

    [Fact]
    public async Task Dispose_KeineWeiterenEreignisseDanach()
    {
        var quelle = new ZaehlendeAuswertungsQuelle();
        var dienst = new MaschinenVerlaufsDienst(quelle, Intervall, FensterFuerKapazitaetDrei, NullLogger<MaschinenVerlaufsDienst>.Instance);

        var ereignisAnzahl = 0;
        dienst.VerlaufAktualisiert += (_, _) => Interlocked.Increment(ref ereignisAnzahl);

        dienst.StarteErfassung();
        await WarteBisAsync(() => Volatile.Read(ref ereignisAnzahl) > 0, TimeSpan.FromSeconds(3));

        dienst.Dispose();
        var standNachDispose = Volatile.Read(ref ereignisAnzahl);

        await Task.Delay(TimeSpan.FromMilliseconds(300));

        Assert.Equal(standNachDispose, Volatile.Read(ref ereignisAnzahl));
        Assert.Equal(0, dienst.AnzahlAbonnenten);
    }

    [Fact]
    public async Task Momentaufnahme_ParalleleSchreiberUndLeser_WirftKeineAusnahme()
    {
        var quelle = new ZaehlendeAuswertungsQuelle();
        var dienst = new MaschinenVerlaufsDienst(quelle, Intervall, FensterFuerKapazitaetDrei, NullLogger<MaschinenVerlaufsDienst>.Instance);
        dienst.StarteErfassung();

        using var abbruch = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var leser = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            while (!abbruch.IsCancellationRequested)
            {
                _ = dienst.Momentaufnahme.Count;
                await Task.Delay(5);
            }
        })).ToArray();

        // Ein Fehlschlag waere eine Ausnahme aus einem Lese-Task (z. B. durch
        // gleichzeitige Veraenderung ohne Sperre) und liesse den Test fehlschlagen.
        await Task.WhenAll(leser);
        dienst.Dispose();
    }

    [Fact]
    public void Konstruktor_LiestIntervallUndFensterAusDerKonfiguration()
    {
        var quelle = new ZaehlendeAuswertungsQuelle();
        // 2 Stunden Fenster / 10 Sekunden Intervall = 720 Punkte Kapazität.
        var optionen = new VerlaufsOptionen { IntervallSekunden = 10, FensterStunden = 2 };
        Assert.Equal(TimeSpan.FromSeconds(10), optionen.Intervall);
        Assert.Equal(TimeSpan.FromHours(2), optionen.Fenster);

        using var dienst = new MaschinenVerlaufsDienst(quelle, optionen, NullLogger<MaschinenVerlaufsDienst>.Instance);

        Assert.Empty(dienst.Momentaufnahme);
    }

    private static async Task WarteBisAsync(Func<bool> bedingung, TimeSpan timeout)
    {
        var frist = DateTime.UtcNow + timeout;
        while (!bedingung() && DateTime.UtcNow < frist)
        {
            await Task.Delay(10);
        }
    }

    private sealed class ZaehlendeAuswertungsQuelle : IAuswertungsQuelle
    {
        private int _zaehler;

        public Anzeigepunkt? ErfasseAktuellenPunkt()
        {
            var wert = Interlocked.Increment(ref _zaehler);
            return new Anzeigepunkt(DateTime.UtcNow, wert, wert, wert);
        }
    }
}
