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
            return _principal.IsInRole(sid);
        }
        catch (IdentityNotMappedException ex)
        {
            aufloesungsFehler = ex.Message;
            return false;
        }
    }
}
