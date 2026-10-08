namespace IndustrieDashboard.Core.Enums;

/// <summary>
/// Kategorie eines Audit-Log-Eintrags, zusätzlich zum freien Text in
/// <see cref="IndustrieDashboard.Core.Models.AuditLogEintrag.Aktion"/>. Macht
/// sicherheitsrelevante Ereignisse (insbesondere Verweigerungen und den
/// Zeugenpfad) in der Audit-Ansicht gezielt filterbar (Spezifikation A5/A6).
/// </summary>
public enum AuditKategorie
{
    /// <summary>Alltägliche Kontrolleingriff-Ereignisse (Anfordern, Freigeben, Ablehnen).</summary>
    Kontrolleingriff,

    Anmeldung,

    /// <summary>Ein Zugriff wurde verweigert - wird auch bei abgelehnter Aktion gespeichert.</summary>
    ZugriffVerweigert,

    /// <summary>Selbstfreigabe der Instandhaltung bei Alleinbesetzung mit Zeugenbestätigung.</summary>
    FreigabeMitZeuge,

    ZeugeBestaetigt
}
