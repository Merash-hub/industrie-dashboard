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

        // Erster Testbenutzer ist laut PrototypBenutzerKontext Maschineneinrichter.
        Assert.True(kontext.HatBerechtigung(Berechtigung.MaschinenAnsehen));
        Assert.False(kontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben));
    }

    [Fact]
    public void VerfuegbareBenutzer_EnthaeltSechsTestbenutzerLautSpezifikationA8()
    {
        var kontext = new PrototypBenutzerKontext();

        // Spec A8: Steven (Maschineneinrichter), Anna (Schichtleitung),
        // Thomas (Instandhaltung), ein zweiter Instandhaltungs-Testbenutzer,
        // ein Bediener, ein Testadmin - je eine eindeutige Kennung.
        Assert.Equal(6, kontext.VerfuegbareBenutzer.Count);
        Assert.Equal(kontext.VerfuegbareBenutzer.Count, kontext.VerfuegbareBenutzer.Distinct().Count());
    }

    [Theory]
    [InlineData(0, Rolle.Maschineneinrichter)]
    [InlineData(1, Rolle.Schichtleitung)]
    [InlineData(2, Rolle.Instandhaltung)]
    [InlineData(3, Rolle.Instandhaltung)]
    [InlineData(4, Rolle.Bediener)]
    [InlineData(5, Rolle.Administration)]
    public void Wechsle_JederTestbenutzer_HatDieErwarteteRolle(int index, Rolle erwarteteRolle)
    {
        var kontext = new PrototypBenutzerKontext();

        kontext.Wechsle(kontext.VerfuegbareBenutzer[index]);

        Assert.Equal(new HashSet<Rolle> { erwarteteRolle }, kontext.Rollen);
    }

    [Fact]
    public void Wechsle_BeideInstandhaltungsTestbenutzer_HabenUnterschiedlicheKennung()
    {
        var kontext = new PrototypBenutzerKontext();

        kontext.Wechsle(kontext.VerfuegbareBenutzer[2]);
        var ersteKennung = kontext.AktuellerBenutzer.Kennung;

        kontext.Wechsle(kontext.VerfuegbareBenutzer[3]);
        var zweiteKennung = kontext.AktuellerBenutzer.Kennung;

        // Damit bleibt die Freigabe im Normalfall testbar (zwei verschiedene
        // Personen der Instandhaltung), nicht nur der Zeugenpfad.
        Assert.NotEqual(ersteKennung, zweiteKennung);
    }
}
