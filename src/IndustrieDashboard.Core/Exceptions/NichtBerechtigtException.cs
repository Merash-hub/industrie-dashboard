namespace IndustrieDashboard.Core.Exceptions;

/// <summary>
/// Der aktuelle Benutzer hat nicht die erforderliche Berechtigung für die
/// versuchte Aktion. Wird immer zusammen mit einem Audit-Eintrag der
/// Kategorie <see cref="IndustrieDashboard.Core.Enums.AuditKategorie.ZugriffVerweigert"/>
/// geworfen (Spezifikation A6).
/// </summary>
public sealed class NichtBerechtigtException : Exception
{
    public NichtBerechtigtException(string message) : base(message)
    {
    }
}
