using IndustrieDashboard.Core.Enums;

namespace IndustrieDashboard.Identity.Windows;

/// <summary>
/// Bindung für den Konfigurationsabschnitt <c>Sicherheit:RollenGruppen</c>
/// aus <c>appsettings.json</c> (Spezifikation A7): je Rolle der Name einer
/// Windows-Gruppe. Die Konfiguration kann nur auswählen und benennen, nie
/// Rechte verleihen - die Basisrolle kommt ausschließlich aus echter
/// Gruppenmitgliedschaft in Windows.
/// </summary>
public sealed class RollenGruppenOptions
{
    public string? Bediener { get; set; }

    public string? Maschineneinrichter { get; set; }

    public string? Schichtleitung { get; set; }

    public string? Instandhaltung { get; set; }

    public string? Administration { get; set; }

    public IEnumerable<(Rolle Rolle, string? Gruppenname)> AlleZuordnungen() => new[]
    {
        (Rolle.Bediener, Bediener),
        (Rolle.Maschineneinrichter, Maschineneinrichter),
        (Rolle.Schichtleitung, Schichtleitung),
        (Rolle.Instandhaltung, Instandhaltung),
        (Rolle.Administration, Administration),
    };
}
