using IndustrieDashboard.Infrastructure.Simulation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndustrieDashboard.Tests.Infrastructure;

public class SimulierteMaschinenDatenQuelleTests
{
    [Fact]
    public async Task GetMaschinenAsync_LiefertBeispielMaschinen()
    {
        var quelle = new SimulierteMaschinenDatenQuelle(NullLogger<SimulierteMaschinenDatenQuelle>.Instance);

        var maschinen = await quelle.GetMaschinenAsync();

        Assert.NotEmpty(maschinen);
        Assert.All(maschinen, m => Assert.False(string.IsNullOrWhiteSpace(m.Name)));
    }

    [Fact]
    public async Task StartUeberwachung_LoestIrgendwannWertAktualisiertAus()
    {
        var quelle = new SimulierteMaschinenDatenQuelle(NullLogger<SimulierteMaschinenDatenQuelle>.Instance);
        var tcs = new TaskCompletionSource();

        quelle.WertAktualisiert += (_, _) => tcs.TrySetResult();
        quelle.StartUeberwachung();

        var abgeschlossen = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        quelle.StopUeberwachung();

        Assert.Same(tcs.Task, abgeschlossen);
    }
}
