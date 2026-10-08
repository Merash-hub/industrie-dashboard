using IndustrieDashboard.App.Identitaet;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IndustrieDashboard.Tests.App;

/// <summary>
/// Deckt die Konfigurationsfälle aus Spezifikation A8/A11 ab: fehlende
/// Konfiguration, unbekannter Anbieter, Prototyp im Release-Build.
/// </summary>
public class IdentitaetsanbieterErmittlerTests
{
    private static IConfiguration KonfigurationMit(string? identitaetsanbieter) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(identitaetsanbieter is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { [IdentitaetsanbieterErmittler.KonfigurationsSchluessel] = identitaetsanbieter })
            .Build();

    [Fact]
    public void Ermitteln_FehlendeKonfigurationsdatei_LiefertWindows()
    {
        var ergebnis = IdentitaetsanbieterErmittler.Ermitteln(konfiguration: null, istDebugBuild: true);

        Assert.Equal(Identitaetsanbieter.Windows, ergebnis);
    }

    [Fact]
    public void Ermitteln_FehlenderSchluessel_LiefertWindows()
    {
        var ergebnis = IdentitaetsanbieterErmittler.Ermitteln(KonfigurationMit(null), istDebugBuild: true);

        Assert.Equal(Identitaetsanbieter.Windows, ergebnis);
    }

    [Fact]
    public void Ermitteln_UnbekannterAnbieter_WirftException()
    {
        var konfiguration = KonfigurationMit("Entra");

        Assert.Throws<InvalidOperationException>(
            () => IdentitaetsanbieterErmittler.Ermitteln(konfiguration, istDebugBuild: true));
    }

    [Fact]
    public void Ermitteln_PrototypImReleaseBuild_WirftException()
    {
        var konfiguration = KonfigurationMit("Prototyp");

        Assert.Throws<InvalidOperationException>(
            () => IdentitaetsanbieterErmittler.Ermitteln(konfiguration, istDebugBuild: false));
    }

    [Fact]
    public void Ermitteln_PrototypImDebugBuild_LiefertPrototyp()
    {
        var konfiguration = KonfigurationMit("Prototyp");

        var ergebnis = IdentitaetsanbieterErmittler.Ermitteln(konfiguration, istDebugBuild: true);

        Assert.Equal(Identitaetsanbieter.Prototyp, ergebnis);
    }

    [Theory]
    [InlineData("Windows")]
    [InlineData("windows")]
    [InlineData("WINDOWS")]
    public void Ermitteln_Windows_IstGrossSchreibungsUnabhaengig(string wert)
    {
        var ergebnis = IdentitaetsanbieterErmittler.Ermitteln(KonfigurationMit(wert), istDebugBuild: false);

        Assert.Equal(Identitaetsanbieter.Windows, ergebnis);
    }
}
