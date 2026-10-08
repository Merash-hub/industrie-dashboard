using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Modules.Dashboard.ViewModels;
using IndustrieDashboard.Shared.Events;
using LiveChartsCore.SkiaSharpView;
using Xunit;

namespace IndustrieDashboard.Tests.Modules;

/// <summary>
/// Deckt die Anbindung von <see cref="DashboardViewModel"/> an den geteilten
/// <see cref="IVerlaufsDienst"/> ab (Spezifikation Teil B): Ein zweites, nach
/// einem Modulwechsel neu erzeugtes ViewModel sieht sofort den bisherigen
/// Verlauf, und es bleiben keine Abonnenten des ersten ViewModels zurück.
/// </summary>
public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task InitialisierenAsync_ZweitesViewModel_SiehtVerlaufDesErstenOhneZurueckbleibendeAbonnenten()
    {
        var maschinenDatenQuelle = new FakeMaschinenDatenQuelle();
        var benutzerKontext = new FakeBenutzerKontext();
        var kontrolleingriffService = new FakeKontrolleingriffService();
        var eventAggregator = new EventAggregator();
        var verlaufsDienst = new FakeVerlaufsDienst
        {
            Momentaufnahme = new List<Anzeigepunkt> { new(DateTime.UtcNow, 42, 40, 44) }
        };

        var erstesViewModel = new DashboardViewModel(maschinenDatenQuelle, kontrolleingriffService, benutzerKontext, verlaufsDienst, eventAggregator);
        await erstesViewModel.InitialisierenAsync();

        Assert.Equal(1, verlaufsDienst.AnzahlAbonnenten);
        Assert.Contains(42.0, WerteDerErstenSerie(erstesViewModel));

        erstesViewModel.Dispose();
        Assert.Equal(0, verlaufsDienst.AnzahlAbonnenten);

        var zweitesViewModel = new DashboardViewModel(maschinenDatenQuelle, kontrolleingriffService, benutzerKontext, verlaufsDienst, eventAggregator);
        await zweitesViewModel.InitialisierenAsync();

        Assert.Equal(1, verlaufsDienst.AnzahlAbonnenten);
        Assert.Contains(42.0, WerteDerErstenSerie(zweitesViewModel));

        zweitesViewModel.Dispose();
        Assert.Equal(0, verlaufsDienst.AnzahlAbonnenten);
    }

    private static IEnumerable<double> WerteDerErstenSerie(DashboardViewModel viewModel)
    {
        var serie = Assert.IsType<LineSeries<double>>(viewModel.AuslastungsSerien[0]);
        return Assert.IsAssignableFrom<IEnumerable<double>>(serie.Values);
    }

    private sealed class FakeVerlaufsDienst : IVerlaufsDienst
    {
        public IReadOnlyList<Anzeigepunkt> Momentaufnahme { get; set; } = Array.Empty<Anzeigepunkt>();

        public event EventHandler? VerlaufAktualisiert;

        public int AnzahlAbonnenten => VerlaufAktualisiert?.GetInvocationList().Length ?? 0;

        public void StarteErfassung()
        {
        }

        public void StoppeErfassung()
        {
        }
    }

    private sealed class FakeMaschinenDatenQuelle : IMaschinenDatenQuelle
    {
        public event EventHandler<MaschinenwertAktualisiertEventArgs>? WertAktualisiert;

        public Task<IReadOnlyList<Maschine>> GetMaschinenAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Maschine>>(Array.Empty<Maschine>());

        public void StartUeberwachung()
        {
        }

        public void StopUeberwachung()
        {
        }

        // Nur zur vollständigen Schnittstellenerfüllung; in diesem Test ungenutzt.
        public void Ausloesen(MaschinenwertAktualisiertEventArgs e) => WertAktualisiert?.Invoke(this, e);
    }

    private sealed class FakeBenutzerKontext : IBenutzerKontext
    {
        public Benutzer AktuellerBenutzer { get; } = new(new BenutzerKennung("proto:test"), "Test Benutzer");

        public IReadOnlySet<Rolle> Rollen { get; } = new HashSet<Rolle> { Rolle.Instandhaltung };

        public event EventHandler? BenutzerGewechselt;

        public bool HatBerechtigung(Berechtigung berechtigung) => true;
    }

    private sealed class FakeKontrolleingriffService : IKontrolleingriffService
    {
        public Task<KontrolleingriffAnforderung> AnfordernAsync(int maschineId, string beschreibung, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<KontrolleingriffAnforderung> FreigebenAsync(int anforderungId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<KontrolleingriffAnforderung> AblehnenAsync(int anforderungId, string? begruendung, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<KontrolleingriffAnforderung> ZurueckziehenAsync(int anforderungId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<KontrolleingriffAnforderung> FreigabeMitZeugeAnfordernAsync(int anforderungId, string ausnahmeGrund, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<KontrolleingriffAnforderung> AlsZeugeBestaetigenAsync(int anforderungId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<KontrolleingriffAnforderung>> GetOffeneAnforderungenAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<KontrolleingriffAnforderung>>(Array.Empty<KontrolleingriffAnforderung>());

        public Task<IReadOnlyList<KontrolleingriffAnforderung>> GetAlleAnforderungenAsync(int maxAnzahl = 100, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<KontrolleingriffAnforderung>>(Array.Empty<KontrolleingriffAnforderung>());
    }
}
