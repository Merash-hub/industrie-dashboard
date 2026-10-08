namespace IndustrieDashboard.Infrastructure.Services;

/// <summary>
/// Bindung für den Konfigurationsabschnitt <c>Verlauf</c> aus
/// <c>appsettings.json</c> (Spezifikation Teil B). Standard: 5 Minuten
/// Aktualisierungsintervall, 8 Stunden Fenster (eine Schicht) - für
/// Vorführungen darf <see cref="IntervallSekunden"/> auf wenige Sekunden
/// gestellt werden.
/// </summary>
public sealed class VerlaufsOptionen
{
    public int IntervallSekunden { get; set; } = 300;

    public int FensterStunden { get; set; } = 8;

    public TimeSpan Intervall => TimeSpan.FromSeconds(Math.Max(1, IntervallSekunden));

    public TimeSpan Fenster => TimeSpan.FromHours(Math.Max(1, FensterStunden));
}
