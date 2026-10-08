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
/// </summary>
public class PrototypBenutzerKontext : IBenutzerKontext, IBenutzerWechsel
{
    private static readonly IReadOnlyList<(Benutzer Benutzer, IReadOnlySet<Rolle> Rollen)> Testbenutzer = new List<(Benutzer, IReadOnlySet<Rolle>)>
    {
        (new Benutzer(new BenutzerKennung("proto:steven"), "Steven Katzer (Bediener)"), RollenMenge(Rolle.Bediener)),
        (new Benutzer(new BenutzerKennung("proto:anna"), "Anna Weber (Schichtleitung)"), RollenMenge(Rolle.Schichtleitung)),
        (new Benutzer(new BenutzerKennung("proto:thomas"), "Thomas Krause (Instandhaltung)"), RollenMenge(Rolle.Instandhaltung)),
    };

    private int _aktuellerIndex;

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
