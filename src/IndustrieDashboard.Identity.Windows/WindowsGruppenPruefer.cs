using System.Security.Principal;

namespace IndustrieDashboard.Identity.Windows;

/// <summary>
/// Echte Umsetzung von <see cref="IGruppenPruefer"/> über
/// <see cref="WindowsPrincipal.IsInRole(SecurityIdentifier)"/>. Löst den
/// Gruppennamen einmalig zu einer SID auf (<see cref="NTAccount.Translate"/>)
/// - funktioniert auch ohne Domäne mit lokalen Gruppen, z. B.
/// <c>AX18PRO\Industrie-Instandhaltung</c> (Spezifikation A7).
/// </summary>
public sealed class WindowsGruppenPruefer : IGruppenPruefer
{
    private readonly WindowsPrincipal _principal;

    public WindowsGruppenPruefer(WindowsIdentity identity)
    {
        _principal = new WindowsPrincipal(identity);
    }

    public bool IstMitglied(string gruppenname, out string? aufloesungsFehler)
    {
        aufloesungsFehler = null;

        try
        {
            var konto = new NTAccount(gruppenname);
            var sid = (SecurityIdentifier)konto.Translate(typeof(SecurityIdentifier));

            // Haertung: Eine Rollengruppe, die auf eine breite Sammelgruppe
            // (Everyone, Authentifizierte Benutzer, BUILTIN\Users/Guests,
            // Domaenen-Benutzer/-Gaeste) aufloest, vergibt die Rolle praktisch
            // jedem angemeldeten Menschen - das wird abgelehnt, nicht nur
            // protokolliert.
            if (BreiteSammelgruppenErkennung.IstZuBreit(sid, out var sammelgruppenGrund))
            {
                aufloesungsFehler = sammelgruppenGrund;
                return false;
            }

            return _principal.IsInRole(sid);
        }
        catch (Exception ex)
        {
            // Haertung: nicht nur IdentityNotMappedException (Gruppe nicht
            // gefunden), sondern jede Ausnahme bei der Gruppenaufloesung -
            // die Rolle wird dann nicht vergeben, der Grund steht im Log
            // (siehe WindowsBenutzerKontext), die Anwendung laeuft weiter.
            aufloesungsFehler = ex.Message;
            return false;
        }
    }
}
