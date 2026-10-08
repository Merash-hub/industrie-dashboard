using System.Security.Principal;
using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Identity.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndustrieDashboard.Tests.Identity;

/// <summary>
/// Testet die Rollenermittlung von <see cref="WindowsBenutzerKontext"/> gegen
/// eine austauschbare <see cref="FakeGruppenPruefer"/> statt echter
/// Windows-Gruppen (Spezifikation A7/A11), dazu die Härtung: zu breite
/// Sammelgruppen, den Anmeldung-Audit-Eintrag und das Abfangen beliebiger
/// Ausnahmen bei der Gruppenauflösung.
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

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, new FakeAuditLogService(), NullLogger<WindowsBenutzerKontext>.Instance);

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

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, new FakeAuditLogService(), NullLogger<WindowsBenutzerKontext>.Instance);

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

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, new FakeAuditLogService(), logger);

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

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, new FakeAuditLogService(), NullLogger<WindowsBenutzerKontext>.Instance);

        Assert.Equal(new HashSet<Rolle> { Rolle.Bediener }, kontext.Rollen);
    }

    [Fact]
    public void AktuellerBenutzer_UebernimmtKennungUndAnzeigenameUnveraendert()
    {
        var kontext = new WindowsBenutzerKontext(
            Testbenutzer,
            new FakeGruppenPruefer(new Dictionary<string, bool?>()),
            new RollenGruppenOptions(),
            new FakeAuditLogService(),
            NullLogger<WindowsBenutzerKontext>.Instance);

        Assert.Equal(Testbenutzer.Kennung, kontext.AktuellerBenutzer.Kennung);
        Assert.Equal(Testbenutzer.Anzeigename, kontext.AktuellerBenutzer.Anzeigename);
    }

    // --- Härtung: Anmeldung-Audit-Eintrag ---

    [Fact]
    public void Konstruktor_SchreibtAnmeldungAuditEintragMitAllenDetails()
    {
        var gruppenPruefer = new FakeGruppenPruefer(new Dictionary<string, bool?>
        {
            ["Industrie-Instandhaltung"] = true,
            ["Industrie-Schichtleitung"] = false,
            ["Industrie-Tippfehler"] = null
        });
        var rollenGruppen = new RollenGruppenOptions
        {
            Instandhaltung = "Industrie-Instandhaltung",
            Schichtleitung = "Industrie-Schichtleitung",
            Administration = "Industrie-Tippfehler"
        };
        var audit = new FakeAuditLogService();

        var kontext = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, audit, NullLogger<WindowsBenutzerKontext>.Instance);

        var eintrag = Assert.Single(audit.Eintraege);
        Assert.Equal(AuditKategorie.Anmeldung, eintrag.Kategorie);
        Assert.Equal(Testbenutzer.Kennung.Wert, eintrag.BenutzerKennung);
        Assert.Equal(Testbenutzer.Anzeigename, eintrag.Benutzer);
        Assert.Contains("Instandhaltung", eintrag.NeuerWert);
        // Je Rolle der konfigurierte Gruppenname, auch fuer nicht aufgeloeste.
        Assert.Contains("Industrie-Instandhaltung", eintrag.Begruendung);
        Assert.Contains("Industrie-Schichtleitung", eintrag.Begruendung);
        Assert.Contains("Industrie-Tippfehler", eintrag.Begruendung);
        Assert.NotEmpty(kontext.Rollenermittlung);
    }

    [Fact]
    public void Konstruktor_KeineRolleVergeben_NeuerWertNenntDasExplizit()
    {
        var gruppenPruefer = new FakeGruppenPruefer(new Dictionary<string, bool?>());
        var rollenGruppen = new RollenGruppenOptions();
        var audit = new FakeAuditLogService();

        _ = new WindowsBenutzerKontext(Testbenutzer, gruppenPruefer, rollenGruppen, audit, NullLogger<WindowsBenutzerKontext>.Instance);

        var eintrag = Assert.Single(audit.Eintraege);
        Assert.Equal("Keine Rolle zugewiesen", eintrag.NeuerWert);
    }

    // --- Härtung: zu breite Sammelgruppen ---

    [Theory]
    [InlineData("S-1-1-0")]                                         // Everyone
    [InlineData("S-1-5-11")]                                        // Authentifizierte Benutzer
    [InlineData("S-1-5-32-545")]                                    // BUILTIN\Users
    [InlineData("S-1-5-32-546")]                                    // BUILTIN\Guests
    [InlineData("S-1-5-21-1111111111-2222222222-3333333333-513")]   // Domänen-Benutzer (RID 513)
    [InlineData("S-1-5-21-1111111111-2222222222-3333333333-514")]   // Domänen-Gäste (RID 514)
    public void BreiteSammelgruppenErkennung_BekannteSammelgruppen_LiefertTrue(string sidText)
    {
        var sid = new SecurityIdentifier(sidText);

        var istZuBreit = BreiteSammelgruppenErkennung.IstZuBreit(sid, out var grund);

        Assert.True(istZuBreit);
        Assert.NotNull(grund);
    }

    [Fact]
    public void BreiteSammelgruppenErkennung_SpezifischeGruppe_LiefertFalse()
    {
        var sid = new SecurityIdentifier("S-1-5-21-1111111111-2222222222-3333333333-1000");

        var istZuBreit = BreiteSammelgruppenErkennung.IstZuBreit(sid, out var grund);

        Assert.False(istZuBreit);
        Assert.Null(grund);
    }

    [Fact]
    public void WindowsGruppenPruefer_Everyone_WirdAlsZuBreiteSammelgruppeAbgelehnt()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var pruefer = new WindowsGruppenPruefer(identity);

        var istMitglied = pruefer.IstMitglied("Everyone", out var grund);

        Assert.False(istMitglied);
        Assert.NotNull(grund);
    }

    // --- Härtung: jede Ausnahme bei der Gruppenauflösung abfangen ---

    [Fact]
    public void WindowsGruppenPruefer_UngueltigerGruppenname_FaengtAusnahmeAbStattZuWerfen()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var pruefer = new WindowsGruppenPruefer(identity);

        // NTAccount(null) wirft ArgumentNullException - eine andere Ausnahme
        // als die bisher behandelte IdentityNotMappedException.
        var istMitglied = pruefer.IstMitglied(null!, out var grund);

        Assert.False(istMitglied);
        Assert.NotNull(grund);
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
