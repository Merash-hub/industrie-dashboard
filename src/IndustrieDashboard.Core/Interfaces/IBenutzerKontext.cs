namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Liefert den aktuell angemeldeten Benutzer. Bewusst minimal gehalten, weil
/// diese Schnittstelle später von Active Directory bzw. IdentityServer
/// bedient wird - dort gibt es kein Umschalten des Benutzers.
/// </summary>
public interface IBenutzerKontext
{
    string AktuellerBenutzer { get; }

    event EventHandler? BenutzerGewechselt;
}
