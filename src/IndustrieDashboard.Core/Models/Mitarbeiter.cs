namespace IndustrieDashboard.Core.Models;

public class Mitarbeiter
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Personalnummer { get; set; } = string.Empty;

    public string Qualifikation { get; set; } = string.Empty;
}
