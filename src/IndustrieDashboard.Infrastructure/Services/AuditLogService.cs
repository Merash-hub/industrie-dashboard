using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndustrieDashboard.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AuditLogService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task ProtokolliereAsync(AuditLogEintrag eintrag, CancellationToken ct = default)
    {
        if (eintrag.Id != 0)
        {
            throw new InvalidOperationException(
                "Audit-Log-Einträge sind unveränderlich: ProtokolliereAsync legt ausschließlich neue Einträge an, kein Wiederverwenden eines bestehenden Eintrags.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        db.AuditLogEintraege.Add(eintrag);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLogEintrag>> GetEintraegeAsync(int maxAnzahl = 200, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        return await db.AuditLogEintraege
            .AsNoTracking()
            .OrderByDescending(a => a.Zeitstempel)
            .Take(maxAnzahl)
            .ToListAsync(ct);
    }
}
