namespace IndustrieDashboard.Core.Exceptions;

/// <summary>
/// Eine Anforderung wurde zwischen dem Laden und dem Speichern bereits durch
/// eine andere, gleichzeitig laufende Aktion verändert (z. B. zwei Personen
/// der Instandhaltung versuchen dieselbe Anforderung im selben Moment zu
/// bearbeiten). Die zuerst abgeschlossene Aktion gewinnt, die zweite wird
/// abgewiesen und schreibt - wie jede Verweigerung - einen Audit-Eintrag
/// <see cref="IndustrieDashboard.Core.Enums.AuditKategorie.ZugriffVerweigert"/>.
/// </summary>
public sealed class GleichzeitigkeitskonfliktException : Exception
{
    public GleichzeitigkeitskonfliktException(string message) : base(message)
    {
    }
}
