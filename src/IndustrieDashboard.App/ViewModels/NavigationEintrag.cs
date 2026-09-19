using IndustrieDashboard.Shared.Modules;

namespace IndustrieDashboard.App.ViewModels;

/// <summary>Präsentations-Wrapper eines Moduls für die Navigationsleiste.</summary>
public class NavigationEintrag
{
    public NavigationEintrag(IAppModule modul)
    {
        Modul = modul;
    }

    public IAppModule Modul { get; }

    public string AnzeigeName => Modul.AnzeigeName;

    public string IconKind => Modul.IconKind;
}
