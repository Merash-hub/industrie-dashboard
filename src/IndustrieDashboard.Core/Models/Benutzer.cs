namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Ein angemeldeter Benutzer: stabile <see cref="Kennung"/> für
/// Sicherheitsentscheidungen, <see cref="Anzeigename"/> ausschließlich für
/// die Darstellung in der Oberfläche und im Audit-Log (als Momentaufnahme).
/// </summary>
public sealed record Benutzer(BenutzerKennung Kennung, string Anzeigename);
