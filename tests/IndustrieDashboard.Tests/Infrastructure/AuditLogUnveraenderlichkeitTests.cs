using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndustrieDashboard.Tests.Infrastructure;

/// <summary>
/// Belegt, dass die SQLite-Trigger aus <see cref="AppDbContext"/> UPDATE/DELETE
/// auf der Audit-Log-Tabelle auch dann verhindern, wenn der EF-Core-
/// ChangeTracker umgangen wird (ExecuteUpdate/ExecuteDelete, rohes SQL).
/// Jeder Test öffnet eine eigene In-Memory-SQLite-Datenbank, damit Tests sich
/// nicht gegenseitig beeinflussen; die Verbindung muss für die Lebensdauer
/// der In-Memory-Datenbank offen gehalten werden.
/// </summary>
public sealed class AuditLogUnveraenderlichkeitTests : IDisposable
{
    private readonly SqliteConnection _verbindung;
    private readonly AppDbContext _db;

    public AuditLogUnveraenderlichkeitTests()
    {
        _verbindung = new SqliteConnection("DataSource=:memory:");
        _verbindung.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_verbindung)
            .Options;

        _db = new AppDbContext(options);
        _db.SicherstellenErstelltMitAuditSchutz();
    }

    public void Dispose()
    {
        _db.Dispose();
        _verbindung.Dispose();
    }

    private async Task<int> LegeEintragAnAsync()
    {
        var eintrag = new AuditLogEintrag
        {
            Benutzer = "Testbenutzer",
            Aktion = "Testaktion",
            Zielobjekt = "Testobjekt"
        };

        _db.AuditLogEintraege.Add(eintrag);
        await _db.SaveChangesAsync();

        return eintrag.Id;
    }

    [Fact]
    public async Task ExecuteDelete_AufAuditTabelle_ScheitertAmTrigger()
    {
        await LegeEintragAnAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => _db.AuditLogEintraege.ExecuteDeleteAsync());

        Assert.Equal(1, await _db.AuditLogEintraege.CountAsync());
    }

    [Fact]
    public async Task RohesDeleteSql_AufAuditTabelle_ScheitertAmTrigger()
    {
        var id = await LegeEintragAnAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM AuditLogEintraege WHERE Id = {0}", id));

        Assert.Equal(1, await _db.AuditLogEintraege.CountAsync());
    }

    [Fact]
    public async Task ExecuteUpdate_AufAuditTabelle_ScheitertAmTrigger()
    {
        await LegeEintragAnAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => _db.AuditLogEintraege.ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Aktion, "Manipuliert")));
    }

    [Fact]
    public async Task RohesUpdateSql_AufAuditTabelle_ScheitertAmTrigger()
    {
        var id = await LegeEintragAnAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => _db.Database.ExecuteSqlRawAsync(
                "UPDATE AuditLogEintraege SET Aktion = 'Manipuliert' WHERE Id = {0}", id));

        var geladen = await _db.AuditLogEintraege.SingleAsync();
        Assert.Equal("Testaktion", geladen.Aktion);
    }

    [Fact]
    public async Task Anlegen_NeuerEintraege_BleibtWeiterhinMoeglich()
    {
        var id = await LegeEintragAnAsync();

        Assert.True(id > 0);
        Assert.Equal(1, await _db.AuditLogEintraege.CountAsync());
    }
}
