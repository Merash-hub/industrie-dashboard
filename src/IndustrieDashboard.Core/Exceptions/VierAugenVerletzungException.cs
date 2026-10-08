namespace IndustrieDashboard.Core.Exceptions;

/// <summary>
/// Das Vier-Augen-Prinzip bzw. der Zeugenpfad wurde verletzt (z. B. gleiche
/// Kennung bei Anfordern und Freigeben, Zeuge gleich Anfordernder, abgelaufene
/// oder fehlende Zeugenbestätigung, fehlender Pflichtgrund für den
/// Zeugenpfad). Wird immer zusammen mit einem Audit-Eintrag der Kategorie
/// <see cref="IndustrieDashboard.Core.Enums.AuditKategorie.ZugriffVerweigert"/>
/// geworfen (Spezifikation A6).
/// </summary>
public sealed class VierAugenVerletzungException : Exception
{
    public VierAugenVerletzungException(string message) : base(message)
    {
    }
}
