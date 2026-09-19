using IndustrieDashboard.Core.Interfaces;

namespace IndustrieDashboard.Infrastructure.Services;

/// <summary>
/// Prototyp-Implementierung von <see cref="IBenutzerKontext"/> und
/// <see cref="IBenutzerWechsel"/>: eine feste Liste von Testbenutzern, zwischen
/// denen sich ohne echte Anmeldung umschalten lässt. Wird später durch eine
/// Active-Directory-/IdentityServer-Anbindung ersetzt, die nur noch
/// IBenutzerKontext bedient.
/// </summary>
public class PrototypBenutzerKontext : IBenutzerKontext, IBenutzerWechsel
{
    private static readonly IReadOnlyList<string> Testbenutzer = new[]
    {
        "Steven Katzer (Bediener)",
        "Anna Weber (Schichtleitung)",
        "Thomas Krause (Instandhaltung)"
    };

    private string _aktuellerBenutzer = Testbenutzer[0];

    public string AktuellerBenutzer => _aktuellerBenutzer;

    public IReadOnlyList<string> VerfuegbareBenutzer => Testbenutzer;

    public event EventHandler? BenutzerGewechselt;

    /// <summary>
    /// Nur für Diagnose und Tests: Anzahl aktuell registrierter Abonnenten von
    /// <see cref="BenutzerGewechselt"/>.
    /// </summary>
    public int AnzahlBenutzerGewechseltAbonnenten => BenutzerGewechselt?.GetInvocationList().Length ?? 0;

    public void Wechsle(string benutzer)
    {
        if (!Testbenutzer.Contains(benutzer))
        {
            throw new ArgumentException($"Unbekannter Benutzer: {benutzer}", nameof(benutzer));
        }

        if (_aktuellerBenutzer == benutzer)
        {
            return;
        }

        _aktuellerBenutzer = benutzer;
        BenutzerGewechselt?.Invoke(this, EventArgs.Empty);
    }
}
