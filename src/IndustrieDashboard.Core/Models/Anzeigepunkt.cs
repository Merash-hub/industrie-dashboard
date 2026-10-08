namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Ein verdichteter Punkt für den Anzeigeverlauf (Spezifikation Teil B):
/// Mittelwert, Minimum und Maximum über ein Aktualisierungsintervall, nie ein
/// Rohwert. Der Zeitpunkt markiert das Ende des Intervalls, in dem der Punkt
/// erfasst wurde.
/// </summary>
public sealed record Anzeigepunkt(DateTime Zeitpunkt, double Mittelwert, double Minimum, double Maximum);
