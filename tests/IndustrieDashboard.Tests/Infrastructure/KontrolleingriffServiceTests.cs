using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Infrastructure.Data;
using IndustrieDashboard.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndustrieDashboard.Tests.Infrastructure;

/// <summary>
/// Deckt das Vier-Augen-Prinzip aus der Spezifikation "Modul Kontrolleingriffe"
/// ab. Nutzt SQLite im Arbeitsspeicher mit offen gehaltener Verbindung, damit
/// kein zusätzliches Test-NuGet-Paket nötig ist und das Verhalten dem echten
/// Anbieter entspricht (inkl. der Unveränderlichkeits-Trigger auf dem
/// Audit-Trail).
/// </summary>
public sealed class KontrolleingriffServiceTests : IDisposable
{
    private readonly SqliteConnection _verbindung;
    private readonly IAuditLogService _auditLogService;
    private readonly IKontrolleingriffService _kontrolleingriffService;

    public KontrolleingriffServiceTests()
    {
        _verbindung = new SqliteConnection("DataSource=:memory:");
        _verbindung.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_verbindung)
            .Options;

        using (var initDb = new AppDbContext(options))
        {
            initDb.SicherstellenErstelltMitAuditSchutz();
        }

        var dbContextFactory = new TestDbContextFactory(options);
        _auditLogService = new AuditLogService(dbContextFactory);
        _kontrolleingriffService = new KontrolleingriffService(dbContextFactory, _auditLogService);
    }

    public void Dispose() => _verbindung.Dispose();

    [Fact]
    public async Task FreigebenAsync_DurchDieselbePerson_WirftException()
    {
        var anforderung = await _kontrolleingriffService.AnfordernAsync(1, "Testbeschreibung", "Anna Weber");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _kontrolleingriffService.FreigebenAsync(anforderung.Id, "Anna Weber"));
    }

    [Fact]
    public async Task FreigebenAsync_DurchAndereePerson_SetztStatusUndSchreibtAudit()
    {
        var anforderung = await _kontrolleingriffService.AnfordernAsync(1, "Testbeschreibung", "Anna Weber");

        var ergebnis = await _kontrolleingriffService.FreigebenAsync(anforderung.Id, "Thomas Krause");

        Assert.Equal(KontrolleingriffStatus.Freigegeben, ergebnis.Status);
        Assert.Equal("Thomas Krause", ergebnis.FreigegebenVon);
        Assert.NotNull(ergebnis.FreigegebenAm);

        var audit = await _auditLogService.GetEintraegeAsync();
        Assert.Contains(audit, e => e.Aktion == "Kontrolleingriff freigegeben" && e.Benutzer == "Thomas Krause");
    }

    [Fact]
    public async Task AblehnenAsync_SetztStatusAbgelehnt_AuchDurchAnfordernden()
    {
        var anforderung = await _kontrolleingriffService.AnfordernAsync(1, "Testbeschreibung", "Anna Weber");

        var ergebnis = await _kontrolleingriffService.AblehnenAsync(anforderung.Id, "Anna Weber", "Doch nicht nötig");

        Assert.Equal(KontrolleingriffStatus.Abgelehnt, ergebnis.Status);

        var audit = await _auditLogService.GetEintraegeAsync();
        Assert.Contains(audit, e => e.Aktion == "Kontrolleingriff abgelehnt" && e.Benutzer == "Anna Weber");
    }

    [Fact]
    public async Task GetOffeneAnforderungenAsync_LiefertNurAngeforderte()
    {
        var offen = await _kontrolleingriffService.AnfordernAsync(1, "Bleibt offen", "Anna Weber");
        var wirdFreigegeben = await _kontrolleingriffService.AnfordernAsync(2, "Wird freigegeben", "Anna Weber");
        await _kontrolleingriffService.FreigebenAsync(wirdFreigegeben.Id, "Thomas Krause");

        var offene = await _kontrolleingriffService.GetOffeneAnforderungenAsync();

        Assert.Single(offene);
        Assert.Equal(offen.Id, offene[0].Id);
        Assert.All(offene, a => Assert.Equal(KontrolleingriffStatus.Angefordert, a.Status));
    }

    [Fact]
    public void PrototypBenutzerKontext_Wechsle_AendertBenutzerUndLoestEventAus()
    {
        var kontext = new PrototypBenutzerKontext();
        var neuerBenutzer = kontext.VerfuegbareBenutzer[1];
        var ausgeloest = false;
        kontext.BenutzerGewechselt += (_, _) => ausgeloest = true;

        kontext.Wechsle(neuerBenutzer);

        Assert.Equal(neuerBenutzer, kontext.AktuellerBenutzer);
        Assert.True(ausgeloest);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

        public AppDbContext CreateDbContext() => new(_options);
    }
}
