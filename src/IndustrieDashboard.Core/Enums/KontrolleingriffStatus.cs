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
    Ausgefuehrt = 3
}
