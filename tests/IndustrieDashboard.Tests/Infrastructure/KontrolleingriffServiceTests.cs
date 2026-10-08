using IndustrieDashboard.Core.Autorisierung;
using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Exceptions;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Infrastructure.Data;
using IndustrieDashboard.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndustrieDashboard.Tests.Infrastructure;

/// <summary>
/// Deckt das Vier-Augen-Prinzip und den Zeugenpfad aus der Spezifikation A6
/// ab. Nutzt SQLite im Arbeitsspeicher mit offen gehaltener Verbindung, damit
/// das Verhalten dem echten Anbieter entspricht (inkl. Audit-Unveränderlichkeit).
/// "Der aktuelle Benutzer" wird über eine austauschbare <see cref="TestBenutzerKontext"/>
/// gesteuert und zwischen den Aufrufen umgeschaltet, genau wie im Prototyp
/// (ein Arbeitsplatz, ein angemeldeter Benutzer zu einem Zeitpunkt).
/// </summary>
public sealed class KontrolleingriffServiceTests : IDisposable
{
    private readonly SqliteConnection _verbindung;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly TestBenutzerKontext _benutzerKontext;
    private readonly KontrolleingriffService _service;

    private static readonly Benutzer Bediener1 = new(new BenutzerKennung("proto:bediener1"), "Peter Bediener");
    private static readonly Benutzer Instandhaltung1 = new(new BenutzerKennung("proto:instand1"), "Thomas Krause");
    private static readonly Benutzer Instandhaltung2 = new(new BenutzerKennung("proto:instand2"), "Anna Weber");
    private static readonly Benutzer Schichtleitung1 = new(new BenutzerKennung("proto:leitung1"), "Erika Leitung");
    private static readonly Benutzer Administration1 = new(new BenutzerKennung("proto:admin1"), "Max Admin");

    public KontrolleingriffServiceTests()
    {
        _verbindung = new SqliteConnection("DataSource=:memory:");
        _verbindung.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_verbindung)
            .Options;

        using (var initDb = new AppDbContext(options))
        {
            initDb.Database.Migrate();
        }

        _dbContextFactory = new TestDbContextFactory(options);
        _benutzerKontext = new TestBenutzerKontext();
        _service = new KontrolleingriffService(_dbContextFactory, _benutzerKontext);
    }

    public void Dispose() => _verbindung.Dispose();

    private void AlsBenutzer(Benutzer benutzer, params Rolle[] rollen) =>
        _benutzerKontext.Setzen(benutzer, rollen);

    private async Task<IReadOnlyList<AuditLogEintrag>> AuditEintraegeAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.AuditLogEintraege.OrderBy(a => a.Id).ToListAsync();
    }

    [Fact]
    public async Task FreigebenAsync_GleicheKennung_WirftVierAugenVerletzung()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => _service.FreigebenAsync(anforderung.Id));
    }

    [Fact]
    public async Task FreigebenAsync_GleicherAnzeigenameAndereKennung_IstErlaubt()
    {
        var ersterBenutzer = new Benutzer(new BenutzerKennung("proto:instand-a"), "Gleicher Name");
        var zweiterBenutzer = new Benutzer(new BenutzerKennung("proto:instand-b"), "Gleicher Name");

        AlsBenutzer(ersterBenutzer, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        AlsBenutzer(zweiterBenutzer, Rolle.Instandhaltung);
        var ergebnis = await _service.FreigebenAsync(anforderung.Id);

        Assert.Equal(KontrolleingriffStatus.Freigegeben, ergebnis.Status);
    }

    [Fact]
    public async Task FreigebenAsync_GleicheKennungAndererAnzeigename_WirdAbgelehnt()
    {
        var kennung = new BenutzerKennung("proto:instand-x");
        var ersterAnzeigename = new Benutzer(kennung, "Erster Anzeigename");
        var zweiterAnzeigename = new Benutzer(kennung, "Zweiter Anzeigename");

        AlsBenutzer(ersterAnzeigename, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        // Regressionstest für den urspruenglichen Fehler: derselbe Mensch mit
        // anderem Anzeigenamen (Tippfehler, Umbenennung) darf sich nicht
        // selbst freigeben - die Kennung entscheidet, nicht der Name.
        AlsBenutzer(zweiterAnzeigename, Rolle.Instandhaltung);
        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => _service.FreigebenAsync(anforderung.Id));
    }

    [Fact]
    public async Task FreigebenAsync_DurchAndereInstandhaltungsperson_SetztStatusUndSchreibtAudit()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        AlsBenutzer(Instandhaltung2, Rolle.Instandhaltung);
        var ergebnis = await _service.FreigebenAsync(anforderung.Id);

        Assert.Equal(KontrolleingriffStatus.Freigegeben, ergebnis.Status);
        Assert.Equal(Instandhaltung2.Kennung.Wert, ergebnis.FreigegebenVonKennung);

        var audit = await AuditEintraegeAsync();
        Assert.Contains(audit, e => e.Aktion == "Kontrolleingriff freigegeben" && e.BenutzerKennung == Instandhaltung2.Kennung.Wert);
    }

    [Fact]
    public async Task FreigebenAsync_OhneBerechtigung_WirftNichtBerechtigtUndSchreibtZugriffVerweigert()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        AlsBenutzer(Bediener1, Rolle.Bediener);
        await Assert.ThrowsAsync<NichtBerechtigtException>(
            () => _service.FreigebenAsync(anforderung.Id));

        var audit = await AuditEintraegeAsync();
        Assert.Contains(audit, e => e.Kategorie == AuditKategorie.ZugriffVerweigert && e.BenutzerKennung == Bediener1.Kennung.Wert);
    }

    [Fact]
    public async Task FreigebenAsync_Schichtleitung_DarfNieSelbstFreigeben()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        AlsBenutzer(Schichtleitung1, Rolle.Schichtleitung);
        await Assert.ThrowsAsync<NichtBerechtigtException>(
            () => _service.FreigebenAsync(anforderung.Id));
    }

    [Fact]
    public async Task FreigebenAsync_DoppelteFreigabe_WirdAbgelehnt()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        AlsBenutzer(Instandhaltung2, Rolle.Instandhaltung);
        await _service.FreigebenAsync(anforderung.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.FreigebenAsync(anforderung.Id));
    }

    [Fact]
    public async Task AblehnenAsync_DurchAndereInstandhaltungsperson_SetztStatusAbgelehnt()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        AlsBenutzer(Instandhaltung2, Rolle.Instandhaltung);
        var ergebnis = await _service.AblehnenAsync(anforderung.Id, "Doch nicht nötig");

        Assert.Equal(KontrolleingriffStatus.Abgelehnt, ergebnis.Status);
    }

    [Fact]
    public async Task AblehnenAsync_DurchAnfordernde_WirftVierAugenVerletzung()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => _service.AblehnenAsync(anforderung.Id, "Rücknahme"));
    }

    [Fact]
    public async Task GetOffeneAnforderungenAsync_LiefertNurUnentschiedene()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var offen = await _service.AnfordernAsync(1, "Bleibt offen");
        var wirdFreigegeben = await _service.AnfordernAsync(2, "Wird freigegeben");

        AlsBenutzer(Instandhaltung2, Rolle.Instandhaltung);
        await _service.FreigebenAsync(wirdFreigegeben.Id);

        var offene = await _service.GetOffeneAnforderungenAsync();

        Assert.Single(offene);
        Assert.Equal(offen.Id, offene[0].Id);
    }

    // --- Zurückziehen (eigene Rücknahme, ohne Vier-Augen-Prüfung) ---

    [Fact]
    public async Task ZurueckziehenAsync_DurchAnfordernde_SetztStatusZurueckgezogenUndSchreibtAudit()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        var ergebnis = await _service.ZurueckziehenAsync(anforderung.Id);

        Assert.Equal(KontrolleingriffStatus.Zurueckgezogen, ergebnis.Status);

        var audit = await AuditEintraegeAsync();
        Assert.Contains(audit, e => e.Aktion == "Kontrolleingriff zurückgezogen" && e.BenutzerKennung == Instandhaltung1.Kennung.Wert);
    }

    [Fact]
    public async Task ZurueckziehenAsync_WaehrendZeugeAngefragt_IstErlaubt()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");
        await _service.FreigabeMitZeugeAnfordernAsync(anforderung.Id, "Alleinbesetzung");

        var ergebnis = await _service.ZurueckziehenAsync(anforderung.Id);

        Assert.Equal(KontrolleingriffStatus.Zurueckgezogen, ergebnis.Status);
    }

    [Fact]
    public async Task ZurueckziehenAsync_DurchFremdePerson_WirftNichtBerechtigt()
    {
        AlsBenutzer(Bediener1, Rolle.Bediener);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        await Assert.ThrowsAsync<NichtBerechtigtException>(
            () => _service.ZurueckziehenAsync(anforderung.Id));
    }

    [Fact]
    public async Task ZurueckziehenAsync_BereitsFreigegeben_WirftInvalidOperation()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        AlsBenutzer(Instandhaltung2, Rolle.Instandhaltung);
        await _service.FreigebenAsync(anforderung.Id);

        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ZurueckziehenAsync(anforderung.Id));
    }

    // --- Zeugenpfad (Spezifikation A6) ---

    [Fact]
    public async Task FreigebenAsync_SelbstOhneZeuge_WirftVierAugenVerletzung()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");

        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => _service.FreigebenAsync(anforderung.Id));
    }

    [Fact]
    public async Task FreigabeMitZeugeAnfordernAsync_OhnePflichtgrund_WirftVierAugenVerletzung()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");

        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => _service.FreigabeMitZeugeAnfordernAsync(anforderung.Id, ""));
    }

    [Fact]
    public async Task AlsZeugeBestaetigenAsync_DurchAnfordernde_WirftVierAugenVerletzung()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");
        await _service.FreigabeMitZeugeAnfordernAsync(anforderung.Id, "Alleinbesetzung");

        // Gleiche Kennung wie die anfordernde Person, diesmal mit der
        // Berechtigung zum Zeugen - damit diese Prüfung wirklich die Kennung
        // testet und nicht nur zufällig an der fehlenden Berechtigung scheitert.
        var dieselbePersonMitZeugenrecht = new Benutzer(Instandhaltung1.Kennung, Instandhaltung1.Anzeigename);
        AlsBenutzer(dieselbePersonMitZeugenrecht, Rolle.Schichtleitung);

        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => _service.AlsZeugeBestaetigenAsync(anforderung.Id));
    }

    [Theory]
    [InlineData(Rolle.Bediener)]
    [InlineData(Rolle.Administration)]
    public async Task AlsZeugeBestaetigenAsync_OhneBerechtigung_WirftNichtBerechtigt(Rolle rolleOhneZeugenrecht)
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");
        await _service.FreigabeMitZeugeAnfordernAsync(anforderung.Id, "Alleinbesetzung");

        var unberechtigt = rolleOhneZeugenrecht == Rolle.Bediener ? Bediener1 : Administration1;
        AlsBenutzer(unberechtigt, rolleOhneZeugenrecht);

        await Assert.ThrowsAsync<NichtBerechtigtException>(
            () => _service.AlsZeugeBestaetigenAsync(anforderung.Id));
    }

    [Fact]
    public async Task FreigebenAsync_AbgelaufeneZeugenbestaetigung_WirftVierAugenVerletzung()
    {
        var kurzeGueltigkeit = new KontrolleingriffService(_dbContextFactory, _benutzerKontext, TimeSpan.FromMinutes(15));

        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await kurzeGueltigkeit.AnfordernAsync(1, "Alleinbesetzung");
        await kurzeGueltigkeit.FreigabeMitZeugeAnfordernAsync(anforderung.Id, "Alleinbesetzung");

        AlsBenutzer(Schichtleitung1, Rolle.Schichtleitung);
        await kurzeGueltigkeit.AlsZeugeBestaetigenAsync(anforderung.Id);

        // Bestaetigungszeitpunkt manipuliert in die Vergangenheit, damit die
        // Gueltigkeit (15 Minuten) nachweislich abgelaufen ist.
        await using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            var geladen = await db.KontrolleingriffAnforderungen.SingleAsync(a => a.Id == anforderung.Id);
            geladen.ZeugeBestaetigtAm = DateTime.UtcNow.AddMinutes(-20);
            await db.SaveChangesAsync();
        }

        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => kurzeGueltigkeit.FreigebenAsync(anforderung.Id));
    }

    [Fact]
    public async Task Zeugenpfad_VollstaendigerAblauf_GibtFreiUndAuditEnthaeltAlleDreiKennungen()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");
        await _service.FreigabeMitZeugeAnfordernAsync(anforderung.Id, "Alleinbesetzung");

        AlsBenutzer(Schichtleitung1, Rolle.Schichtleitung);
        var nachBestaetigung = await _service.AlsZeugeBestaetigenAsync(anforderung.Id);
        Assert.Equal(KontrolleingriffStatus.ZeugeBestaetigt, nachBestaetigung.Status);

        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var ergebnis = await _service.FreigebenAsync(anforderung.Id);

        Assert.Equal(KontrolleingriffStatus.Freigegeben, ergebnis.Status);

        var audit = await AuditEintraegeAsync();
        var freigabeEintrag = Assert.Single(audit, e => e.Kategorie == AuditKategorie.FreigabeMitZeuge);

        Assert.Contains(Instandhaltung1.Kennung.Wert, freigabeEintrag.NeuerWert);
        Assert.Contains(Schichtleitung1.Kennung.Wert, freigabeEintrag.NeuerWert);
    }

    // --- Härtung: Gleichzeitigkeitsschutz (Status als Concurrency-Token) ---

    [Fact]
    public async Task AblehnenAsync_AnforderungZwischenzeitlichAnderswoGeaendert_WirftGleichzeitigkeitskonflikt()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Testbeschreibung");

        // Simuliert eine zweite, schon länger offene Ansicht derselben
        // Anforderung: geladen, als ihr Status noch "Angefordert" war.
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_verbindung).Options;
        var veralteterKontext = new AppDbContext(options);
        _ = await veralteterKontext.KontrolleingriffAnforderungen.SingleAsync(a => a.Id == anforderung.Id);

        // Eine andere, unabhängige Aktion schließt die Anforderung in der
        // Zwischenzeit bereits ab.
        AlsBenutzer(Instandhaltung2, Rolle.Instandhaltung);
        await _service.FreigebenAsync(anforderung.Id);

        // Dieselbe (veraltete) Ansicht versucht jetzt abzulehnen: Wegen EF
        // Cores Identity Map liest der Dienst über denselben Kontext weiterhin
        // den im Tracker gehaltenen, veralteten Status "Angefordert" - die
        // Prüfungen lassen die Aktion zu, aber das Speichern scheitert am
        // Concurrency-Token (Status in der Datenbank ist längst "Freigegeben").
        var einmaligerKontextMitVeraltetemStand = new EinmalVeralteterKontextFactory(options, veralteterKontext);
        var dienstMitVeralteterAnsicht = new KontrolleingriffService(einmaligerKontextMitVeraltetemStand, _benutzerKontext);

        var ausnahme = await Assert.ThrowsAsync<GleichzeitigkeitskonfliktException>(
            () => dienstMitVeralteterAnsicht.AblehnenAsync(anforderung.Id, "Zu spät"));

        Assert.Contains("gleichzeitig", ausnahme.Message, StringComparison.OrdinalIgnoreCase);

        var audit = await AuditEintraegeAsync();
        Assert.Contains(audit, e => e.Kategorie == AuditKategorie.ZugriffVerweigert && e.Aktion == "Kontrolleingriff abgelehnt");

        // Die zuerst abgeschlossene Aktion bleibt gültig.
        var endstand = await _service.GetAlleAnforderungenAsync();
        Assert.Equal(KontrolleingriffStatus.Freigegeben, endstand.Single(a => a.Id == anforderung.Id).Status);
    }

    // --- Härtung: Berechtigung vor dem Laden (kein Erraten vorhandener Ids) ---

    [Fact]
    public async Task FreigebenAsync_OhneBerechtigungAufNichtExistierendeId_WirftNichtBerechtigtNichtNichtGefunden()
    {
        AlsBenutzer(Bediener1, Rolle.Bediener);

        var ausnahme = await Assert.ThrowsAsync<NichtBerechtigtException>(
            () => _service.FreigebenAsync(anforderungId: 999999));

        Assert.DoesNotContain("gefunden", ausnahme.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ZurueckziehenAsync_OhneBerechtigungAufNichtExistierendeId_WirftNichtBerechtigtNichtNichtGefunden()
    {
        // Administration ist die einzige Rolle ohne KontrolleingriffAnfordern.
        AlsBenutzer(Administration1, Rolle.Administration);

        var ausnahme = await Assert.ThrowsAsync<NichtBerechtigtException>(
            () => _service.ZurueckziehenAsync(anforderungId: 999999));

        Assert.DoesNotContain("gefunden", ausnahme.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AlsZeugeBestaetigenAsync_OhneBerechtigungAufNichtExistierendeId_WirftNichtBerechtigtNichtNichtGefunden()
    {
        AlsBenutzer(Bediener1, Rolle.Bediener);

        var ausnahme = await Assert.ThrowsAsync<NichtBerechtigtException>(
            () => _service.AlsZeugeBestaetigenAsync(anforderungId: 999999));

        Assert.DoesNotContain("gefunden", ausnahme.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- Härtung: Längenbegrenzung (ablehnen statt abschneiden) ---

    [Fact]
    public async Task AnfordernAsync_ZuLangeBeschreibung_WirftArgumentExceptionUndLegtNichtsAn()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var zuLang = new string('x', 501);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.AnfordernAsync(1, zuLang));

        var alle = await _service.GetAlleAnforderungenAsync();
        Assert.Empty(alle);
    }

    [Fact]
    public async Task AnfordernAsync_BeschreibungGenau500Zeichen_IstErlaubt()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var genauRichtig = new string('x', 500);

        var anforderung = await _service.AnfordernAsync(1, genauRichtig);

        Assert.Equal(genauRichtig, anforderung.Beschreibung);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("  ab  ")]
    public async Task FreigabeMitZeugeAnfordernAsync_GrundNachTrimZuKurz_WirftVierAugenVerletzung(string zuKurzerGrund)
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");

        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => _service.FreigabeMitZeugeAnfordernAsync(anforderung.Id, zuKurzerGrund));
    }

    [Fact]
    public async Task FreigabeMitZeugeAnfordernAsync_GrundZuLang_WirftVierAugenVerletzung()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");
        var zuLang = new string('x', 501);

        await Assert.ThrowsAsync<VierAugenVerletzungException>(
            () => _service.FreigabeMitZeugeAnfordernAsync(anforderung.Id, zuLang));
    }

    [Fact]
    public async Task FreigabeMitZeugeAnfordernAsync_GrundWirdGetrimmtGespeichert()
    {
        AlsBenutzer(Instandhaltung1, Rolle.Instandhaltung);
        var anforderung = await _service.AnfordernAsync(1, "Alleinbesetzung");

        var ergebnis = await _service.FreigabeMitZeugeAnfordernAsync(anforderung.Id, "  Alleinbesetzung  ");

        Assert.Equal("Alleinbesetzung", ergebnis.AusnahmeGrund);
    }

    private sealed class EinmalVeralteterKontextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private AppDbContext? _veralteterKontext;

        public EinmalVeralteterKontextFactory(DbContextOptions<AppDbContext> options, AppDbContext veralteterKontext)
        {
            _options = options;
            _veralteterKontext = veralteterKontext;
        }

        public AppDbContext CreateDbContext()
        {
            if (_veralteterKontext is { } kontext)
            {
                _veralteterKontext = null;
                return kontext;
            }

            return new AppDbContext(_options);
        }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

        public AppDbContext CreateDbContext() => new(_options);
    }

    /// <summary>
    /// Frei steuerbarer <see cref="IBenutzerKontext"/> für Tests: erlaubt, im
    /// Unterschied zu <see cref="PrototypBenutzerKontext"/>, beliebige
    /// Kombinationen aus Kennung, Anzeigename und Rollen, auch zwei
    /// verschiedene Kennungen mit demselben Anzeigenamen (Regressionstest).
    /// </summary>
    private sealed class TestBenutzerKontext : IBenutzerKontext
    {
        public Benutzer AktuellerBenutzer { get; private set; } = new(new BenutzerKennung("proto:unset"), "Unset");

        public IReadOnlySet<Rolle> Rollen { get; private set; } = new HashSet<Rolle>();

        public event EventHandler? BenutzerGewechselt;

        public bool HatBerechtigung(Berechtigung berechtigung) => RollenBerechtigungen.Hat(Rollen, berechtigung);

        public void Setzen(Benutzer benutzer, params Rolle[] rollen)
        {
            AktuellerBenutzer = benutzer;
            Rollen = rollen.ToHashSet();
            BenutzerGewechselt?.Invoke(this, EventArgs.Empty);
        }
    }
}
