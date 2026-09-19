namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Eine geplante Arbeitsschicht mit zugeordneten Mitarbeitenden.
/// </summary>
public class Schicht
{
    public int Id { get; set; }

    public string Bezeichnung { get; set; } = string.Empty;

    public DateTime Beginn { get; set; }

    public DateTime Ende { get; set; }

    public List<Mitarbeiter> Mitarbeiter { get; set; } = new();
}
