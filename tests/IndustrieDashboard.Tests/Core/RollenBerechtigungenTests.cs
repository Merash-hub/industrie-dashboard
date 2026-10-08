using IndustrieDashboard.Core.Autorisierung;
using IndustrieDashboard.Core.Enums;
using Xunit;

namespace IndustrieDashboard.Tests.Core;

/// <summary>
/// Deckt jede Zelle der Berechtigungsmatrix aus der Spezifikation (Abschnitt
/// A4) ab. <see cref="RollenBerechtigungen"/> ist die einzige Stelle im Code,
/// die festlegt, welche Rolle was darf - diese Tests sind deshalb die
/// Referenz für die gesamte Autorisierung.
/// </summary>
public class RollenBerechtigungenTests
{
    public static IEnumerable<object[]> MatrixZellen()
    {
        (Rolle Rolle, Berechtigung Berechtigung, bool Erwartet)[] zellen =
        {
            (Rolle.Bediener, Berechtigung.MaschinenAnsehen, true),
            (Rolle.Bediener, Berechtigung.MaschineEinrichten, false),
            (Rolle.Bediener, Berechtigung.KontrolleingriffAnfordern, true),
            (Rolle.Bediener, Berechtigung.KontrolleingriffFreigeben, false),
            (Rolle.Bediener, Berechtigung.KontrolleingriffAblehnen, false),
            (Rolle.Bediener, Berechtigung.KontrolleingriffAlsZeugeBestaetigen, false),
            (Rolle.Bediener, Berechtigung.AuswertungAnsehen, false),
            (Rolle.Bediener, Berechtigung.AuditLogLesen, false),
            (Rolle.Bediener, Berechtigung.SchichtplanAnsehen, true),
            (Rolle.Bediener, Berechtigung.SchichtplanBearbeiten, false),
            (Rolle.Bediener, Berechtigung.VertretungErnennen, false),

            (Rolle.Maschineneinrichter, Berechtigung.MaschinenAnsehen, true),
            (Rolle.Maschineneinrichter, Berechtigung.MaschineEinrichten, true),
            (Rolle.Maschineneinrichter, Berechtigung.KontrolleingriffAnfordern, true),
            (Rolle.Maschineneinrichter, Berechtigung.KontrolleingriffFreigeben, false),
            (Rolle.Maschineneinrichter, Berechtigung.KontrolleingriffAblehnen, false),
            (Rolle.Maschineneinrichter, Berechtigung.KontrolleingriffAlsZeugeBestaetigen, false),
            (Rolle.Maschineneinrichter, Berechtigung.AuswertungAnsehen, false),
            (Rolle.Maschineneinrichter, Berechtigung.AuditLogLesen, false),
            (Rolle.Maschineneinrichter, Berechtigung.SchichtplanAnsehen, true),
            (Rolle.Maschineneinrichter, Berechtigung.SchichtplanBearbeiten, false),
            (Rolle.Maschineneinrichter, Berechtigung.VertretungErnennen, false),

            (Rolle.Schichtleitung, Berechtigung.MaschinenAnsehen, true),
            (Rolle.Schichtleitung, Berechtigung.MaschineEinrichten, true),
            (Rolle.Schichtleitung, Berechtigung.KontrolleingriffAnfordern, true),
            (Rolle.Schichtleitung, Berechtigung.KontrolleingriffFreigeben, false),
            (Rolle.Schichtleitung, Berechtigung.KontrolleingriffAblehnen, false),
            (Rolle.Schichtleitung, Berechtigung.KontrolleingriffAlsZeugeBestaetigen, true),
            (Rolle.Schichtleitung, Berechtigung.AuswertungAnsehen, true),
            (Rolle.Schichtleitung, Berechtigung.AuditLogLesen, true),
            (Rolle.Schichtleitung, Berechtigung.SchichtplanAnsehen, true),
            (Rolle.Schichtleitung, Berechtigung.SchichtplanBearbeiten, true),
            (Rolle.Schichtleitung, Berechtigung.VertretungErnennen, true),

            (Rolle.Instandhaltung, Berechtigung.MaschinenAnsehen, true),
            (Rolle.Instandhaltung, Berechtigung.MaschineEinrichten, true),
            (Rolle.Instandhaltung, Berechtigung.KontrolleingriffAnfordern, true),
            (Rolle.Instandhaltung, Berechtigung.KontrolleingriffFreigeben, true),
            (Rolle.Instandhaltung, Berechtigung.KontrolleingriffAblehnen, true),
            (Rolle.Instandhaltung, Berechtigung.KontrolleingriffAlsZeugeBestaetigen, false),
            (Rolle.Instandhaltung, Berechtigung.AuswertungAnsehen, true),
            (Rolle.Instandhaltung, Berechtigung.AuditLogLesen, false),
            (Rolle.Instandhaltung, Berechtigung.SchichtplanAnsehen, true),
            (Rolle.Instandhaltung, Berechtigung.SchichtplanBearbeiten, false),
            (Rolle.Instandhaltung, Berechtigung.VertretungErnennen, false),

            (Rolle.Administration, Berechtigung.MaschinenAnsehen, true),
            (Rolle.Administration, Berechtigung.MaschineEinrichten, false),
            (Rolle.Administration, Berechtigung.KontrolleingriffAnfordern, false),
            (Rolle.Administration, Berechtigung.KontrolleingriffFreigeben, false),
            (Rolle.Administration, Berechtigung.KontrolleingriffAblehnen, false),
            (Rolle.Administration, Berechtigung.KontrolleingriffAlsZeugeBestaetigen, false),
            (Rolle.Administration, Berechtigung.AuswertungAnsehen, false),
            (Rolle.Administration, Berechtigung.AuditLogLesen, true),
            (Rolle.Administration, Berechtigung.SchichtplanAnsehen, true),
            (Rolle.Administration, Berechtigung.SchichtplanBearbeiten, false),
            (Rolle.Administration, Berechtigung.VertretungErnennen, false),
        };

        foreach (var (rolle, berechtigung, erwartet) in zellen)
        {
            yield return new object[] { rolle, berechtigung, erwartet };
        }
    }

    [Theory]
    [MemberData(nameof(MatrixZellen))]
    public void Hat_JedeZelleDerMatrix_EntsprichtDerSpezifikation(Rolle rolle, Berechtigung berechtigung, bool erwartet)
    {
        var rollen = new HashSet<Rolle> { rolle };

        Assert.Equal(erwartet, RollenBerechtigungen.Hat(rollen, berechtigung));
    }

    [Theory]
    [InlineData(Berechtigung.MaschinenAnsehen)]
    [InlineData(Berechtigung.KontrolleingriffAnfordern)]
    [InlineData(Berechtigung.KontrolleingriffFreigeben)]
    [InlineData(Berechtigung.AuditLogLesen)]
    public void Hat_KeineRolle_LiefertImmerFalse(Berechtigung berechtigung)
    {
        var keineRollen = new HashSet<Rolle>();

        Assert.False(RollenBerechtigungen.Hat(keineRollen, berechtigung));
    }

    [Theory]
    [InlineData(Rolle.Schichtleitung)]
    [InlineData(Rolle.Administration)]
    public void Hat_SchichtleitungUndAdministration_DuerfenNieFreigebenOderAblehnen(Rolle rolle)
    {
        var rollen = new HashSet<Rolle> { rolle };

        Assert.False(RollenBerechtigungen.Hat(rollen, Berechtigung.KontrolleingriffFreigeben));
        Assert.False(RollenBerechtigungen.Hat(rollen, Berechtigung.KontrolleingriffAblehnen));
    }

    [Fact]
    public void Hat_MehrereRollen_BildetVereinigung()
    {
        var rollen = new HashSet<Rolle> { Rolle.Bediener, Rolle.Administration };

        // Nur Administration hat AuditLogLesen, nur Bediener hat KontrolleingriffAnfordern:
        // beides muss über die Vereinigung verfügbar sein.
        Assert.True(RollenBerechtigungen.Hat(rollen, Berechtigung.AuditLogLesen));
        Assert.True(RollenBerechtigungen.Hat(rollen, Berechtigung.KontrolleingriffAnfordern));
        Assert.False(RollenBerechtigungen.Hat(rollen, Berechtigung.KontrolleingriffFreigeben));
    }
}
