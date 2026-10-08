using IndustrieDashboard.Core.Autorisierung;
using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Infrastructure.Services;

/// <summary>
/// Prototyp-Implementierung von <see cref="IBenutzerKontext"/> und
/// <see cref="IBenutzerWechsel"/>: eine feste Liste von Testbenutzern mit
/// fester Kennung und Rolle, zwischen denen sich ohne echte Anmeldung
/// umschalten lässt. Wird später durch eine Active-Directory-/Entra-ID-
/// Anbindung ersetzt, die nur noch IBenutzerKontext bedient.
///
/// Nur in Debug-Builds zulässig (Spezifikation A8): der Konstruktor
/// verweigert sich in Release-Builds technisch, unabhängig davon, was die
/// Konfiguration sagt. Die verständliche Meldung an den Benutzer und der
/// Log-Eintrag beim Start gehören zur Anbieterwahl in <c>App.xaml.cs</c>.
/// </summary>
public class PrototypBenutzerKontext : IBenutzerKontext, IBenutzerWechsel
{
    // Der zweite Instandhaltungs-Testbenutzer wird gebraucht, damit die
    // Freigabe im Normalfall testbar bleibt; ohne ihn wird nur der
    // Zeugenpfad getestet (Steven, Spezifikation A8).
    private static readonly IReadOnlyList<(Benutzer Benutzer, IReadOnlySet<Rolle> Rollen)> Testbenutzer = new List<(Benutzer, IReadOnlySet<Rolle>)>
    {
        (new Benutzer(new BenutzerKennung("proto:steven"), "Steven Katzer (Maschineneinrichter)"), RollenMenge(Rolle.Maschineneinrichter)),
        (new Benutzer(new BenutzerKennung("proto:anna"), "Anna Weber (Schichtleitung)"), RollenMenge(Rolle.Schichtleitung)),
        (new Benutzer(new BenutzerKennung("proto:thomas"), "Thomas Krause (Instandhaltung)"), RollenMenge(Rolle.Instandhaltung)),
        (new Benutzer(new BenutzerKennung("proto:lena"), "Lena Vogt (Instandhaltung)"), RollenMenge(Rolle.Instandhaltung)),
        (new Benutzer(new BenutzerKennung("proto:paul"), "Paul Werner (Bediener)"), RollenMenge(Rolle.Bediener)),
        (new Benutzer(new BenutzerKennung("proto:nina"), "Nina Fischer (Administration)"), RollenMenge(Rolle.Administration)),
    };

    private int _aktuellerIndex;

    public PrototypBenutzerKontext()
    {
#if !DEBUG
        throw new InvalidOperationException(
            "PrototypBenutzerKontext ist nur in Debug-Builds zulässig (Spezifikation A8).");
#endif
    }

    public Benutzer AktuellerBenutzer => Testbenutzer[_aktuellerIndex].Benutzer;

    public IReadOnlySet<Rolle> Rollen => Testbenutzer[_aktuellerIndex].Rollen;

    public IReadOnlyList<string> VerfuegbareBenutzer => Testbenutzer.Select(t => t.Benutzer.Anzeigename).ToList();

    public event EventHandler? BenutzerGewechselt;

    /// <summary>
    /// Nur für Diagnose und Tests: Anzahl aktuell registrierter Abonnenten von
    /// <see cref="BenutzerGewechselt"/>.
    /// </summary>
    public int AnzahlBenutzerGewechseltAbonnenten => BenutzerGewechselt?.GetInvocationList().Length ?? 0;

    public bool HatBerechtigung(Berechtigung berechtigung) => RollenBerechtigungen.Hat(Rollen, berechtigung);

    public void Wechsle(string benutzer)
    {
        var index = Testbenutzer.ToList().FindIndex(t => t.Benutzer.Anzeigename == benutzer);
        if (index < 0)
        {
            throw new ArgumentException($"Unbekannter Benutzer: {benutzer}", nameof(benutzer));
        }

        if (_aktuellerIndex == index)
        {
            return;
        }

        _aktuellerIndex = index;
        BenutzerGewechselt?.Invoke(this, EventArgs.Empty);
    }

    private static IReadOnlySet<Rolle> RollenMenge(params Rolle[] rollen) => rollen.ToHashSet();
}
