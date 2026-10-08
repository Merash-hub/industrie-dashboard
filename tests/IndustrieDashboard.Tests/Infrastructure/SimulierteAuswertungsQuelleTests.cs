using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Infrastructure.Services;
using Xunit;

namespace IndustrieDashboard.Tests.Infrastructure;

public sealed class SimulierteAuswertungsQuelleTests : IDisposable
{
    private readonly FakeMaschinenDatenQuelle _maschinenDatenQuelle = new();
    private readonly SimulierteAuswertungsQuelle _quelle;

    public SimulierteAuswertungsQuelleTests()
    {
        _quelle = new SimulierteAuswertungsQuelle(_maschinenDatenQuelle);
    }

    public void Dispose() => _quelle.Dispose();

    [Fact]
    public void ErfasseAktuellenPunkt_OhneRohwerte_LiefertNull()
    {
        Assert.Null(_quelle.ErfasseAktuellenPunkt());
    }

    [Fact]
    public void ErfasseAktuellenPunkt_VerdichtetRohwerteZuMittelwertMinimumMaximum()
    {
        _maschinenDatenQuelle.Melden(10);
        _maschinenDatenQuelle.Melden(20);
        _maschinenDatenQuelle.Melden(30);

        var punkt = _quelle.ErfasseAktuellenPunkt();

        Assert.NotNull(punkt);
        Assert.Equal(20, punkt!.Mittelwert);
        Assert.Equal(10, punkt.Minimum);
        Assert.Equal(30, punkt.Maximum);
    }

    [Fact]
    public void ErfasseAktuellenPunkt_SetztRohwerteNachAbrufZurueck()
    {
        _maschinenDatenQuelle.Melden(50);
        _quelle.ErfasseAktuellenPunkt();

        var zweiterAbruf = _quelle.ErfasseAktuellenPunkt();

        Assert.Null(zweiterAbruf);
    }

    [Fact]
    public void Dispose_MeldetVomEreignisAb()
    {
        _quelle.Dispose();

        _maschinenDatenQuelle.Melden(99);

        Assert.Null(_quelle.ErfasseAktuellenPunkt());
    }

    private sealed class FakeMaschinenDatenQuelle : IMaschinenDatenQuelle
    {
        public event EventHandler<MaschinenwertAktualisiertEventArgs>? WertAktualisiert;

        public void Melden(double wert) => WertAktualisiert?.Invoke(this, new MaschinenwertAktualisiertEventArgs
        {
            MaschineId = 1,
            NeuerWert = wert,
            Status = MaschinenStatus.Laeuft
        });

        public Task<IReadOnlyList<Maschine>> GetMaschinenAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Maschine>>(Array.Empty<Maschine>());

        public void StartUeberwachung()
        {
        }

        public void StopUeberwachung()
        {
        }
    }
}
