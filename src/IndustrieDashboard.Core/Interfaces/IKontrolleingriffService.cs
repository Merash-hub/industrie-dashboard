using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Verwaltet Kontrolleingriffe inkl. Vier-Augen-Freigabe und Zeugenpfad bei
/// Alleinbesetzung der Instandhaltung (Spezifikation A6). Wer handelt, ergibt
/// sich ausschließlich aus dem injizierten <see cref="IBenutzerKontext"/> -
/// nicht aus einem von außen übergebenen Namen -, damit die Prüfung im Dienst
/// nicht durch die Oberfläche umgangen werden kann. Jede Zustandsänderung wie
/// jede Verweigerung wird im Audit-Log festgehalten.
/// </summary>
public interface IKontrolleingriffService
{
    Task<KontrolleingriffAnforderung> AnfordernAsync(int maschineId, string beschreibung, CancellationToken ct = default);

    /// <summary>
    /// Normalfall: Freigabe durch eine andere Person als die anfordernde
    /// (Instandhaltung). Im Zeugenpfad schließt dieselbe Methode auch die
    /// Selbstfreigabe der anfordernden Person ab, sofern zuvor
    /// <see cref="AlsZeugeBestaetigenAsync"/> erfolgreich war und die
    /// Bestätigung noch nicht abgelaufen ist.
    /// </summary>
    Task<KontrolleingriffAnforderung> FreigebenAsync(int anforderungId, CancellationToken ct = default);

    /// <summary>
    /// Ablehnen unterliegt denselben Vier-Augen-Regeln wie Freigeben (nur
    /// Instandhaltung, nicht die anfordernde Person selbst) - Spezifikation
    /// A6, Normalfall-Bedingungen 1 und 2 gelten für beide Aktionen gleich.
    /// </summary>
    Task<KontrolleingriffAnforderung> AblehnenAsync(int anforderungId, string? begruendung, CancellationToken ct = default);

    /// <summary>
    /// Nur die anfordernde Person selbst darf ihre eigene, noch offene
    /// Anforderung (Status <see cref="Enums.KontrolleingriffStatus.Angefordert"/>
    /// oder <see cref="Enums.KontrolleingriffStatus.ZeugeAngefragt"/>) zurückziehen.
    /// Bewusst ohne Vier-Augen-Prüfung, da es sich um eine reine Rücknahme der
    /// eigenen, noch nicht entschiedenen Anforderung handelt.
    /// </summary>
    Task<KontrolleingriffAnforderung> ZurueckziehenAsync(int anforderungId, CancellationToken ct = default);

    /// <summary>
    /// Zeugenpfad, erster Schritt: Nur für die Instandhaltung, nur für die
    /// eigene, noch offene Anforderung, nur mit Pflichtgrund (z. B.
    /// "Alleinbesetzung"). Setzt den Status auf <see cref="Enums.KontrolleingriffStatus.ZeugeAngefragt"/>.
    /// </summary>
    Task<KontrolleingriffAnforderung> FreigabeMitZeugeAnfordernAsync(int anforderungId, string ausnahmeGrund, CancellationToken ct = default);

    /// <summary>
    /// Zeugenpfad, zweiter Schritt: Bestätigung durch eine dritte, von der
    /// anfordernden Person verschiedene Person mit der Berechtigung
    /// <see cref="Enums.Berechtigung.KontrolleingriffAlsZeugeBestaetigen"/> (Schichtleitung).
    /// </summary>
    Task<KontrolleingriffAnforderung> AlsZeugeBestaetigenAsync(int anforderungId, CancellationToken ct = default);

    Task<IReadOnlyList<KontrolleingriffAnforderung>> GetOffeneAnforderungenAsync(CancellationToken ct = default);

    /// <summary>Liefert alle Anforderungen, absteigend nach <see cref="KontrolleingriffAnforderung.AngefordertAm"/>.</summary>
    Task<IReadOnlyList<KontrolleingriffAnforderung>> GetAlleAnforderungenAsync(int maxAnzahl = 100, CancellationToken ct = default);
}
