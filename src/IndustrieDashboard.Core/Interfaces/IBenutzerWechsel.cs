namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Reine Prototyp-Funktionalität zum Umschalten des angemeldeten Benutzers,
/// bewusst getrennt von <see cref="IBenutzerKontext"/>, damit sie sich später
/// rückstandslos entfernen lässt, sobald eine echte Anmeldung (Active
/// Directory / IdentityServer) existiert.
/// </summary>
public interface IBenutzerWechsel
{
    IReadOnlyList<string> VerfuegbareBenutzer { get; }

    void Wechsle(string benutzer);
}
