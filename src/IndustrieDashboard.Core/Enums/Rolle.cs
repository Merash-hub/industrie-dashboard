namespace IndustrieDashboard.Core.Enums;

/// <summary>
/// Die fünf Rollen, aus denen sich Berechtigungen ableiten (siehe
/// <see cref="IndustrieDashboard.Core.Autorisierung.RollenBerechtigungen"/>).
/// Bei Windows aus Gruppenmitgliedschaft ermittelt, im Prototyp fest zugeordnet.
/// </summary>
public enum Rolle
{
    Bediener,
    Maschineneinrichter,
    Schichtleitung,
    Instandhaltung,
    Administration
}
