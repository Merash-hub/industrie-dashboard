namespace IndustrieDashboard.Core.Enums;

/// <summary>
/// Einzelne, im jeweiligen Dienst zu erzwingende Berechtigung. Welche Rolle
/// welche Berechtigung hat, steht ausschließlich in
/// <see cref="IndustrieDashboard.Core.Autorisierung.RollenBerechtigungen"/>.
/// </summary>
public enum Berechtigung
{
    MaschinenAnsehen,

    /// <summary>Platzhalter; Einzelrechte des Maschineneinrichters werden später verfeinert.</summary>
    MaschineEinrichten,

    KontrolleingriffAnfordern,
    KontrolleingriffFreigeben,
    KontrolleingriffAblehnen,
    KontrolleingriffAlsZeugeBestaetigen,

    /// <summary>Durchschnittsauswertung, siehe Spezifikation Teil D5.</summary>
    AuswertungAnsehen,

    AuditLogLesen,

    // Modul Schichtplanung (Spezifikation Teil C):
    SchichtplanAnsehen,
    SchichtplanBearbeiten,
    VertretungErnennen
}
