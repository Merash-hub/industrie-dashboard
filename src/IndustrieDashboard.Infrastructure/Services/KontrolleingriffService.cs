using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndustrieDashboard.Infrastructure.Services;

/// <summary>
/// Referenzimplementierung des Vier-Augen-Prinzips: Anfordern und Freigeben
/// sind zwei unabhängige Schritte durch (im Idealfall) zwei unterschiedliche
/// Benutzer; jeder Schritt wird zusätzlich im Audit-Log festgehalten.
/// </summary>
public class KontrolleingriffService : IKontrolleingriffService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IAuditLogService _auditLogService;

    public KontrolleingriffService(IDbContextFactory<AppDbContext> dbContextFactory, IAuditLogService auditLogService)
    {
        _dbContextFactory = dbContextFactory;
        _auditLogService = auditLogService;
    }

    public async Task<KontrolleingriffAnforderung> AnfordernAsync(int maschineId, string beschreibung, string angefordertVon, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var anforderung = new KontrolleingriffAnforderung
        {
            MaschineId = maschineId,
            Beschreibung = beschreibung,
            AngefordertVon = angefordertVon,
            Status = KontrolleingriffStatus.Angefordert
        };

        db.KontrolleingriffAnforderungen.Add(anforderung);
        await db.SaveChangesAsync(ct);

        await _auditLogService.ProtokolliereAsync(new AuditLogEintrag
        {
            Benutzer = angefordertVon,
            Aktion = "Kontrolleingriff angefordert",
            Zielobjekt = $"Maschine #{maschineId}",
            NeuerWert = beschreibung
        }, ct);

        return anforderung;
    }

    public async Task<KontrolleingriffAnforderung> FreigebenAsync(int anforderungId, string freigegebenVon, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var anforderung = await db.KontrolleingriffAnforderungen.FirstOrDefaultAsync(a => a.Id == anforderungId, ct)
            ?? throw new InvalidOperationException($"Kontrolleingriff-Anforderung #{anforderungId} wurde nicht gefunden.");

        if (string.Equals(anforderung.AngefordertVon, freigegebenVon, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vier-Augen-Prinzip verletzt: Anfordernde und freigebende Person müssen unterschiedlich sein.");
        }

        anforderung.Status = KontrolleingriffStatus.Freigegeben;
        anforderung.FreigegebenVon = freigegebenVon;
        anforderung.FreigegebenAm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await _auditLogService.ProtokolliereAsync(new AuditLogEintrag
        {
            Benutzer = freigegebenVon,
            Aktion = "Kontrolleingriff freigegeben",
            Zielobjekt = $"Maschine #{anforderung.MaschineId}",
            AlterWert = "Angefordert",
            NeuerWert = "Freigegeben",
            GegengezeichnetVon = freigegebenVon
        }, ct);

        return anforderung;
    }

    public async Task<IReadOnlyList<KontrolleingriffAnforderung>> GetOffeneAnforderungenAsync(CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        return await db.KontrolleingriffAnforderungen
            .Where(a => a.Status == KontrolleingriffStatus.Angefordert)
            .OrderBy(a => a.AngefordertAm)
            .ToListAsync(ct);
    }
}
