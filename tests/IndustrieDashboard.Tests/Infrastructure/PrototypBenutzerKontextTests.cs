using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Infrastructure.Services;
using Xunit;

namespace IndustrieDashboard.Tests.Infrastructure;

public class PrototypBenutzerKontextTests
{
    private static (PrototypBenutzerKontext Kontext, FakeAuditLogService Audit) NeuerKontext()
    {
        var audit = new FakeAuditLogService();
        return (new PrototypBenutzerKontext(audit), audit);
    }

    [Fact]
    public void Wechsle_AendertBenutzerUndLoestEventAus()
    {
        var (kontext, _) = NeuerKontext();
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
        var (kontext, _) = NeuerKontext();

        // Erster Testbenutzer ist laut PrototypBenutzerKontext Maschineneinrichter.
        Assert.True(kontext.HatBerechtigung(Berechtigung.MaschinenAnsehen));
        Assert.False(kontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben));
    }

    [Fact]
    public void VerfuegbareBenutzer_EnthaeltSechsTestbenutzerLautSpezifikationA8()
    {
        var (kontext, _) = NeuerKontext();

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
        var (kontext, _) = NeuerKontext();

        kontext.Wechsle(kontext.VerfuegbareBenutzer[index]);

        Assert.Equal(new HashSet<Rolle> { erwarteteRolle }, kontext.Rollen);
    }

    [Fact]
    public void Wechsle_BeideInstandhaltungsTestbenutzer_HabenUnterschiedlicheKennung()
    {
        var (kontext, _) = NeuerKontext();

        kontext.Wechsle(kontext.VerfuegbareBenutzer[2]);
        var ersteKennung = kontext.AktuellerBenutzer.Kennung;

        kontext.Wechsle(kontext.VerfuegbareBenutzer[3]);
        var zweiteKennung = kontext.AktuellerBenutzer.Kennung;

        // Damit bleibt die Freigabe im Normalfall testbar (zwei verschiedene
        // Personen der Instandhaltung), nicht nur der Zeugenpfad.
        Assert.NotEqual(ersteKennung, zweiteKennung);
    }

    [Fact]
    public void Konstruktor_SchreibtAnmeldungFuerErstenTestbenutzer()
    {
        var (kontext, audit) = NeuerKontext();

        var eintrag = Assert.Single(audit.Eintraege);
        Assert.Equal(AuditKategorie.Anmeldung, eintrag.Kategorie);
        Assert.Equal(kontext.AktuellerBenutzer.Kennung.Wert, eintrag.BenutzerKennung);
        Assert.Contains("Maschineneinrichter", eintrag.NeuerWert);
    }

    [Fact]
    public void Wechsle_SchreibtWeiterenAnmeldungEintragFuerNeuenBenutzer()
    {
        var (kontext, audit) = NeuerKontext();

        kontext.Wechsle(kontext.VerfuegbareBenutzer[2]);

        Assert.Equal(2, audit.Eintraege.Count);
        var letzter = audit.Eintraege[^1];
        Assert.Equal(AuditKategorie.Anmeldung, letzter.Kategorie);
        Assert.Equal("proto:thomas", letzter.BenutzerKennung);
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<AuditLogEintrag> Eintraege { get; } = new();

        public Task ProtokolliereAsync(AuditLogEintrag eintrag, CancellationToken ct = default)
        {
            Eintraege.Add(eintrag);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLogEintrag>> GetEintraegeAsync(int maxAnzahl = 200, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AuditLogEintrag>>(Eintraege.AsReadOnly());
    }
}
