using IndustrieDashboard.Core.Enums;

namespace IndustrieDashboard.Core.Autorisierung;

/// <summary>
/// Die einzige Stelle im Code, die festlegt, welche Rolle welche Berechtigung
/// hat (Berechtigungsmatrix aus der Spezifikation, Abschnitt A4). Hat ein
/// Benutzer mehrere Rollen, gilt die Vereinigung der Berechtigungen. Hat er
/// keine Rolle, gilt: keine Berechtigung - auch nicht <see cref="Berechtigung.MaschinenAnsehen"/>.
/// Kein Rollenwissen darf außerhalb dieser Klasse verstreut werden.
/// </summary>
public static class RollenBerechtigungen
{
    private static readonly IReadOnlyDictionary<Rolle, IReadOnlySet<Berechtigung>> Matrix = new Dictionary<Rolle, IReadOnlySet<Berechtigung>>
    {
        [Rolle.Bediener] = Berechtigungen(
            Berechtigung.MaschinenAnsehen,
            Berechtigung.KontrolleingriffAnfordern,
            Berechtigung.SchichtplanAnsehen),

        // Vorläufig: alle Rechte des Bedieners plus der Platzhalter MaschineEinrichten
        // (Steven, 08.10.: Einzelrechte des Einrichters werden später verfeinert).
        [Rolle.Maschineneinrichter] = Berechtigungen(
            Berechtigung.MaschinenAnsehen,
            Berechtigung.MaschineEinrichten,
            Berechtigung.KontrolleingriffAnfordern,
            Berechtigung.SchichtplanAnsehen),

        [Rolle.Schichtleitung] = Berechtigungen(
            Berechtigung.MaschinenAnsehen,
            Berechtigung.MaschineEinrichten,
            Berechtigung.KontrolleingriffAnfordern,
            Berechtigung.KontrolleingriffAlsZeugeBestaetigen,
            Berechtigung.AuswertungAnsehen,
            Berechtigung.AuditLogLesen,
            Berechtigung.SchichtplanAnsehen,
            Berechtigung.SchichtplanBearbeiten,
            Berechtigung.VertretungErnennen),

        // Freigabe liegt allein bei der Instandhaltung (Steven, 08.10.).
        [Rolle.Instandhaltung] = Berechtigungen(
            Berechtigung.MaschinenAnsehen,
            Berechtigung.MaschineEinrichten,
            Berechtigung.KontrolleingriffAnfordern,
            Berechtigung.KontrolleingriffFreigeben,
            Berechtigung.KontrolleingriffAblehnen,
            Berechtigung.AuswertungAnsehen,
            Berechtigung.SchichtplanAnsehen),

        // Funktionstrennung: Administration gibt nichts frei und bestätigt nichts
        // (Steven, 08.10.: "Richtlinien einhalten").
        [Rolle.Administration] = Berechtigungen(
            Berechtigung.MaschinenAnsehen,
            Berechtigung.AuditLogLesen,
            Berechtigung.SchichtplanAnsehen),
    };

    private static IReadOnlySet<Berechtigung> Berechtigungen(params Berechtigung[] berechtigungen) =>
        berechtigungen.ToHashSet();

    /// <summary>Alle Berechtigungen einer einzelnen Rolle.</summary>
    public static IReadOnlySet<Berechtigung> Von(Rolle rolle) =>
        Matrix.TryGetValue(rolle, out var berechtigungen) ? berechtigungen : new HashSet<Berechtigung>();

    /// <summary>
    /// Ob die Vereinigung der übergebenen Rollen die angefragte Berechtigung
    /// enthält. Eine leere Rollenmenge liefert immer <c>false</c>.
    /// </summary>
    public static bool Hat(IReadOnlySet<Rolle> rollen, Berechtigung berechtigung) =>
        rollen.Any(rolle => Von(rolle).Contains(berechtigung));
}
