using Microsoft.Extensions.Configuration;

namespace IndustrieDashboard.App.Identitaet;

/// <summary>
/// Entscheidet anhand der Konfiguration, welcher Identitätsanbieter
/// verwendet wird (Spezifikation A8). Fail closed: Eine fehlende oder nicht
/// lesbare Konfiguration ergibt den sicheren Standard <see cref="Identitaetsanbieter.Windows"/>
/// (dafür übergibt der Aufrufer <c>null</c>, wenn das Lesen der Datei
/// fehlgeschlagen ist); ein unbekannter Wert oder "Prototyp" in einem
/// Release-Build verweigern den Start, indem sie eine Ausnahme werfen - die
/// Composition Root (<c>App.xaml.cs</c>) fängt diese bereits für jeden
/// Startfehler ab, protokolliert sie und zeigt eine verständliche Meldung.
/// </summary>
public static class IdentitaetsanbieterErmittler
{
    public const string KonfigurationsSchluessel = "Sicherheit:Identitaetsanbieter";

    public static Identitaetsanbieter Ermitteln(IConfiguration? konfiguration, bool istDebugBuild)
    {
        var wert = konfiguration?[KonfigurationsSchluessel];

        if (string.IsNullOrWhiteSpace(wert))
        {
            return Identitaetsanbieter.Windows;
        }

        if (!Enum.TryParse<Identitaetsanbieter>(wert, ignoreCase: true, out var anbieter))
        {
            throw new InvalidOperationException(
                $"Unbekannter Identitätsanbieter '{wert}' in der Konfiguration ({KonfigurationsSchluessel}). " +
                "Gültige Werte: 'Windows' oder 'Prototyp'.");
        }

        if (anbieter == Identitaetsanbieter.Prototyp && !istDebugBuild)
        {
            throw new InvalidOperationException(
                "Der Prototyp-Anbieter (Sicherheit:Identitaetsanbieter = 'Prototyp') ist nur in Debug-Builds " +
                "zulässig. Dieser Release-Build muss 'Windows' verwenden.");
        }

        return anbieter;
    }
}
