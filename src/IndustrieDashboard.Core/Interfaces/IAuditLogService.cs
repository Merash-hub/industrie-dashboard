using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Schreibt und liest den Audit-Trail. Jede sicherheitsrelevante Aktion
/// (insb. Kontrolleingriffe) muss hierüber protokolliert werden.
/// </summary>
public interface IAuditLogService
{
    Task ProtokolliereAsync(AuditLogEintrag eintrag, CancellationToken ct = default);

    Task<IReadOnlyList<AuditLogEintrag>> GetEintraegeAsync(int maxAnzahl = 200, CancellationToken ct = default);
}
