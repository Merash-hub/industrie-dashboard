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
///
/// Zwischenstand (Auftrag Stufe 1, Schritt 3): Die Methoden nehmen weiterhin
/// nur den Anzeigenamen entgegen, deshalb dient er hier vorübergehend auch als
/// Platzhalter für <see cref="AuditLogEintrag.BenutzerKennung"/> bzw.
/// <see cref="KontrolleingriffAnforderung.AngefordertVonKennung"/>. Schritt 4
/// ersetzt das durch die echte, stabile Kennung aus <c>IBenutzerKontext</c>
/// und macht das Vier-Augen-Prinzip kennungsbasiert statt namensbasiert.
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
            AngefordertVonKennung = angefordertVon,
            Status = KontrolleingriffStatus.Angefordert
        };

        db.KontrolleingriffAnforderungen.Add(anforderung);
        await db.SaveChangesAsync(ct);

        await _auditLogService.ProtokolliereAsync(new AuditLogEintrag
        {
            Benutzer = angefordertVon,
            BenutzerKennung = angefordertVon,
            Kategorie = AuditKategorie.Kontrolleingriff,
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
        anforderung.FreigegebenVonKennung = freigegebenVon;
        anforderung.FreigegebenAm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await _auditLogService.ProtokolliereAsync(new AuditLogEintrag
        {
            Benutzer = freigegebenVon,
            BenutzerKennung = freigegebenVon,
            Kategorie = AuditKategorie.Kontrolleingriff,
            Aktion = "Kontrolleingriff freigegeben",
            Zielobjekt = $"Maschine #{anforderung.MaschineId}",
            AlterWert = "Angefordert",
            NeuerWert = "Freigegeben",
            GegengezeichnetVon = freigegebenVon
        }, ct);

        return anforderung;
    }

    public async Task<KontrolleingriffAnforderung> AblehnenAsync(int anforderungId, string abgelehntVon, string? begruendung, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var anforderung = await db.KontrolleingriffAnforderungen.FirstOrDefaultAsync(a => a.Id == anforderungId, ct)
            ?? throw new InvalidOperationException($"Kontrolleingriff-Anforderung #{anforderungId} wurde nicht gefunden.");

        var vorherigerStatus = anforderung.Status;
        anforderung.Status = KontrolleingriffStatus.Abgelehnt;
        await db.SaveChangesAsync(ct);

        await _auditLogService.ProtokolliereAsync(new AuditLogEintrag
        {
            Benutzer = abgelehntVon,
            BenutzerKennung = abgelehntVon,
            Kategorie = AuditKategorie.Kontrolleingriff,
            Aktion = "Kontrolleingriff abgelehnt",
            Zielobjekt = $"Maschine #{anforderung.MaschineId}",
            AlterWert = vorherigerStatus.ToString(),
            NeuerWert = "Abgelehnt",
            Begruendung = begruendung
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

    public async Task<IReadOnlyList<KontrolleingriffAnforderung>> GetAlleAnforderungenAsync(int maxAnzahl = 100, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        return await db.KontrolleingriffAnforderungen
            .OrderByDescending(a => a.AngefordertAm)
            .Take(maxAnzahl)
            .ToListAsync(ct);
    }
}
