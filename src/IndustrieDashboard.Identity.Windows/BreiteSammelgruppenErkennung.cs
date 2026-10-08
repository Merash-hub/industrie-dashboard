using System.Security.Principal;

namespace IndustrieDashboard.Identity.Windows;

/// <summary>
/// Erkennt, ob eine aufgelöste SID eine zu breite Sammelgruppe ist (Everyone,
/// Authentifizierte Benutzer, BUILTIN\Users, BUILTIN\Guests, Domänen-Benutzer,
/// Domänen-Gäste). Eine solche Gruppe fälschlich als Rollengruppe zu
/// konfigurieren würde die Rolle praktisch jedem angemeldeten Benutzer geben -
/// das widerspricht "geringste Rechte" und wird deshalb abgelehnt (Härtung).
///
/// Bewusst eine eigenständige, rein auf <see cref="SecurityIdentifier"/>
/// arbeitende Funktion (keine Abhängigkeit von einer echten Windows-Anmeldung),
/// damit sie sich mit konstruierten SIDs direkt testen lässt.
/// </summary>
public static class BreiteSammelgruppenErkennung
{
    private static readonly WellKnownSidType[] ZuBreiteSidTypen =
    {
        WellKnownSidType.WorldSid,              // Jeder / Everyone, S-1-1-0
        WellKnownSidType.AuthenticatedUserSid,  // Authentifizierte Benutzer, S-1-5-11
        WellKnownSidType.BuiltinUsersSid,       // BUILTIN\Users, S-1-5-32-545
        WellKnownSidType.BuiltinGuestsSid,      // BUILTIN\Guests, S-1-5-32-546
        WellKnownSidType.AccountDomainUsersSid, // Domänen-Benutzer (RID 513) der jeweiligen Domäne bzw. des Rechners
        WellKnownSidType.AccountDomainGuestsSid // Domänen-Gäste (RID 514)
    };

    /// <summary>
    /// Liefert <c>true</c> und einen erklärenden <paramref name="grund"/>, wenn
    /// <paramref name="sid"/> eine der oben genannten Sammelgruppen ist.
    /// </summary>
    public static bool IstZuBreit(SecurityIdentifier sid, out string? grund)
    {
        foreach (var typ in ZuBreiteSidTypen)
        {
            bool istDieserTyp;
            try
            {
                istDieserTyp = sid.IsWellKnown(typ);
            }
            catch
            {
                // IsWellKnown kann für Typen, die eine Domänen-SID als
                // Vergleichsbasis brauchen, in seltenen Konstellationen eine
                // Ausnahme werfen - dann ist es jedenfalls keine Übereinstimmung.
                istDieserTyp = false;
            }

            if (istDieserTyp)
            {
                grund = $"'{sid.Value}' ist eine zu breite Sammelgruppe ({typ}) und damit als Rollengruppe nicht zulässig.";
                return true;
            }
        }

        grund = null;
        return false;
    }
}
