using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Infrastructure.Services;
using Xunit;

namespace IndustrieDashboard.Tests.Infrastructure;

public class PrototypBenutzerKontextTests
{
    [Fact]
    public void Wechsle_AendertBenutzerUndLoestEventAus()
    {
        var kontext = new PrototypBenutzerKontext();
        var neuerBenutzer = kontext.VerfuegbareBenutzer[1];
        var ausgeloest = false;
        kontext.BenutzerGewechselt += (_, _) => ausgeloest = true;

        kontext.Wechsle(neuerBenutzer);

        Assert.Equal(neuerBenutzer, kontext.AktuellerBenutzer.Anzeigename);
        Assert.True(ausgeloest);
    }

    [Fact]
    public void HatBerechtigung_RichtetSichNachRolleDesAktuellenBenutzers()
    {
        var kontext = new PrototypBenutzerKontext();

        // Erster Testbenutzer ist laut PrototypBenutzerKontext Bediener.
        Assert.True(kontext.HatBerechtigung(Berechtigung.MaschinenAnsehen));
        Assert.False(kontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben));
    }
}
