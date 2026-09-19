using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Verwaltet Kontrolleingriffe inkl. Vier-Augen-Freigabe. Jede Zustandsänderung
/// wird zusätzlich über <see cref="IAuditLogService"/> protokolliert.
/// </summary>
public interface IKontrolleingriffService
{
    Task<KontrolleingriffAnforderung> AnfordernAsync(int maschineId, string beschreibung, string angefordertVon, CancellationToken ct = default);

    Task<KontrolleingriffAnforderung> FreigebenAsync(int anforderungId, string freigegebenVon, CancellationToken ct = default);

    Task<IReadOnlyList<KontrolleingriffAnforderung>> GetOffeneAnforderungenAsync(CancellationToken ct = default);
}
