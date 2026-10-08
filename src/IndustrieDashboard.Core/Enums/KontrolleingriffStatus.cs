namespace IndustrieDashboard.Core.Enums;

/// <summary>
/// Status eines Kontrolleingriffs im Vier-Augen-Prinzip:
/// ein kritischer Eingriff muss von einer zweiten Person freigegeben werden,
/// bevor er wirksam wird.
/// </summary>
public enum KontrolleingriffStatus
{
    Angefordert = 0,
    Freigegeben = 1,
    Abgelehnt = 2,
    Ausgefuehrt = 3,

    /// <summary>Zeugenpfad (Spezifikation A6): Instandhaltung hat "Freigabe mit Zeuge" gewählt, wartet auf Bestätigung.</summary>
    ZeugeAngefragt = 4,

    /// <summary>Zeuge hat bestätigt; die anfordernde Person kann die Freigabe jetzt abschließen.</summary>
    ZeugeBestaetigt = 5,

    /// <summary>Die anfordernde Person hat ihre eigene, noch offene Anforderung zurückgezogen.</summary>
    Zurueckgezogen = 6
}
