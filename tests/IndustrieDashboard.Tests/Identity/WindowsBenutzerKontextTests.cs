using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Identity.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndustrieDashboard.Tests.Identity;

/// <summary>
/// Testet die Rollenermittlung von <see cref="WindowsBenutzerKontext"/> gegen
/// eine austauschbare <see cref="FakeGruppenPruefer"/> statt echter
/// Windows-Gruppen (Spezifikation A7/A11).
/// </summary>
public class WindowsBenutzerKontextTests
{
    private static readonly Benutzer Testbenutzer = new(new BenutzerKennung("S-1-5-21-0-0-0-1001"), "AX18PRO\\steven");

    [Fact]
    public void Rollen_MitgliedEinerGruppe_VergibtDieZugehoerigeRolle()
    {
        var gruppenPruefer = new FakeGruppenPruefer(new Dictionary<string, bool?>
        {
            ["Industrie-Instandhaltung"] = true
        });
        var rollenGruppen = new RollenGruppenOptions { Instandhaltung = "Industrie-Instandhaltung" };

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, NullLogger<WindowsBenutzerKontext>.Instance);

        Assert.Contains(Rolle.Instandhaltung, kontext.Rollen);
        Assert.True(kontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben));
    }

    [Fact]
    public void Rollen_KeineMitgliedschaft_VergibtKeineRolle()
    {
        var gruppenPruefer = new FakeGruppenPruefer(new Dictionary<string, bool?>
        {
            ["Industrie-Instandhaltung"] = false
        });
        var rollenGruppen = new RollenGruppenOptions { Instandhaltung = "Industrie-Instandhaltung" };

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, NullLogger<WindowsBenutzerKontext>.Instance);

        Assert.Empty(kontext.Rollen);
        Assert.False(kontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben));
        Assert.False(kontext.HatBerechtigung(Berechtigung.MaschinenAnsehen));
    }

    [Fact]
    public void Rollen_GruppeNichtAufloesbar_VergibtKeineRolleUndProtokolliertWarnung()
    {
        var gruppenPruefer = new FakeGruppenPruefer(new Dictionary<string, bool?>
        {
            ["Industrie-Tippfehler"] = null
        });
        var rollenGruppen = new RollenGruppenOptions { Instandhaltung = "Industrie-Tippfehler" };
        var logger = new AufzeichnenderLogger();

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, logger);

        Assert.Empty(kontext.Rollen);
        Assert.Single(logger.Warnungen);
    }

    [Fact]
    public void Rollen_MehrereGruppen_VergibtNurTatsaechlicheMitgliedschaften()
    {
        var gruppenPruefer = new FakeGruppenPruefer(new Dictionary<string, bool?>
        {
            ["Industrie-Bediener"] = true,
            ["Industrie-Schichtleitung"] = false
        });
        var rollenGruppen = new RollenGruppenOptions
        {
            Bediener = "Industrie-Bediener",
            Schichtleitung = "Industrie-Schichtleitung"
        };

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, NullLogger<WindowsBenutzerKontext>.Instance);

        Assert.Equal(new HashSet<Rolle> { Rolle.Bediener }, kontext.Rollen);
    }

    [Fact]
    public void AktuellerBenutzer_UebernimmtKennungUndAnzeigenameUnveraendert()
    {
        var kontext = new WindowsBenutzerKontext(
            Testbenutzer,
            new FakeGruppenPruefer(new Dictionary<string, bool?>()),
            new RollenGruppenOptions(),
            NullLogger<WindowsBenutzerKontext>.Instance);

        Assert.Equal(Testbenutzer.Kennung, kontext.AktuellerBenutzer.Kennung);
        Assert.Equal(Testbenutzer.Anzeigename, kontext.AktuellerBenutzer.Anzeigename);
    }

    private sealed class FakeGruppenPruefer : IGruppenPruefer
    {
        private readonly IReadOnlyDictionary<string, bool?> _mitgliedschaften;

        public FakeGruppenPruefer(IReadOnlyDictionary<string, bool?> mitgliedschaften) => _mitgliedschaften = mitgliedschaften;

        public bool IstMitglied(string gruppenname, out string? aufloesungsFehler)
        {
            aufloesungsFehler = null;

            if (!_mitgliedschaften.TryGetValue(gruppenname, out var ergebnis) || ergebnis is null)
            {
                aufloesungsFehler = $"Gruppe '{gruppenname}' nicht auflösbar (Test).";
                return false;
            }

            return ergebnis.Value;
        }
    }

    private sealed class AufzeichnenderLogger : ILogger<WindowsBenutzerKontext>
    {
        public List<string> Warnungen { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnungen.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
