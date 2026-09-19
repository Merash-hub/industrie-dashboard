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

    /// <summary>
    /// Ablehnen darf jede Person, auch die anfordernde selbst - eine Ablehnung
    /// durch die anfordernde Person ist fachlich eine Rücknahme und völlig
    /// legitim. Der Audit-Log-Eintrag hält fest, wer abgelehnt hat.
    /// </summary>
    Task<KontrolleingriffAnforderung> AblehnenAsync(int anforderungId, string abgelehntVon, string? begruendung, CancellationToken ct = default);

    Task<IReadOnlyList<KontrolleingriffAnforderung>> GetOffeneAnforderungenAsync(CancellationToken ct = default);

    /// <summary>Liefert alle Anforderungen, absteigend nach <see cref="KontrolleingriffAnforderung.AngefordertAm"/>.</summary>
    Task<IReadOnlyList<KontrolleingriffAnforderung>> GetAlleAnforderungenAsync(int maxAnzahl = 100, CancellationToken ct = default);
}
