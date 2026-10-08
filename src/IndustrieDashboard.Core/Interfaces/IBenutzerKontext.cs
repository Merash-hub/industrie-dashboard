using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Liefert den aktuell angemeldeten Benutzer samt Rollen. Bewusst minimal
/// gehalten, weil diese Schnittstelle später von Active Directory bzw. Entra ID
/// bedient wird - dort gibt es kein Umschalten des Benutzers.
/// </summary>
public interface IBenutzerKontext
{
    Benutzer AktuellerBenutzer { get; }

    IReadOnlySet<Rolle> Rollen { get; }

    /// <summary>
    /// Ob der aktuelle Benutzer die Berechtigung hat (Vereinigung über alle
    /// seine Rollen). Fail closed: ohne Rolle keine Berechtigung.
    /// </summary>
    bool HatBerechtigung(Berechtigung berechtigung);

    event EventHandler? BenutzerGewechselt;
}
