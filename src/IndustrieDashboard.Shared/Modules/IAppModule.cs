using Microsoft.Extensions.DependencyInjection;

namespace IndustrieDashboard.Shared.Modules;

/// <summary>
/// Erweiterungspunkt für Fachmodule (Dashboard, Schichtplanung, ...). Jedes Modul
/// registriert seine eigenen Services/ViewModels und liefert Metadaten für die
/// Navigation in der Shell. Neue Module werden nur hier angemeldet
/// (siehe App.xaml.cs), der Rest der Anwendung muss nicht angepasst werden.
/// </summary>
public interface IAppModule
{
    /// <summary>Anzeigename in der Navigation, z. B. "Dashboard".</summary>
    string AnzeigeName { get; }

    /// <summary>Material-Design-Icon-Kind (siehe MaterialDesignIcons), z. B. "ViewDashboard".</summary>
    string IconKind { get; }

    /// <summary>Reihenfolge in der Navigationsleiste (aufsteigend).</summary>
    int Reihenfolge { get; }

    void RegisterServices(IServiceCollection services);

    /// <summary>Erzeugt (via DI) die Wurzel-View des Moduls für die Navigation.</summary>
    object ErzeugeStartView(IServiceProvider serviceProvider);
}
