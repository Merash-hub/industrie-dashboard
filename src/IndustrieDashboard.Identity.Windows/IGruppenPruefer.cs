namespace IndustrieDashboard.Identity.Windows;

/// <summary>
/// Prüft Mitgliedschaft in einer (lokalen oder Domänen-)Windows-Gruppe für
/// genau eine Identität, hinter einer austauschbaren Schnittstelle, damit
/// <see cref="WindowsBenutzerKontext"/> ohne echte Windows-Gruppen getestet
/// werden kann (Spezifikation A7/A11).
/// </summary>
public interface IGruppenPruefer
{
    /// <summary>
    /// Liefert <c>true</c>, wenn die Identität Mitglied der Gruppe
    /// <paramref name="gruppenname"/> ist. Lässt sich die Gruppe nicht zu
    /// einer SID auflösen (z. B. Schreibfehler in der Konfiguration, Gruppe
    /// existiert nicht), liefert die Methode <c>false</c> und füllt
    /// <paramref name="aufloesungsFehler"/> mit einer Begründung - niemals
    /// "im Zweifel erlauben" (Spezifikation A7).
    /// </summary>
    bool IstMitglied(string gruppenname, out string? aufloesungsFehler);
}
