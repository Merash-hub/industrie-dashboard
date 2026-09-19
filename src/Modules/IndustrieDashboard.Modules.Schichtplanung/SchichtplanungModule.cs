using IndustrieDashboard.Modules.Schichtplanung.Views;
using IndustrieDashboard.Shared.Modules;
using IndustrieDashboard.Shared.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrieDashboard.Modules.Schichtplanung;

public class SchichtplanungModule : IAppModule
{
    public string AnzeigeName => "Schichtplanung";

    public string IconKind => "CalendarAccount";

    public int Reihenfolge => 1;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient(_ => new PlatzhalterViewModel(
            "Schichtplanung",
            "Planung von Schichten und Zuordnung von Mitarbeitenden. Das Datenmodell (Schicht, Mitarbeiter) " +
            "ist im Core-Projekt bereits angelegt; UI und Geschäftslogik folgen im nächsten Ausbauschritt.",
            new[]
            {
                "Kalender-/Wochenansicht der Schichten",
                "Zuordnung von Mitarbeitenden zu Schichten (Drag & Drop)",
                "Qualifikationsabgleich (z. B. Einweisung erforderlich)",
                "Export als PDF/Excel für den Aushang"
            }));
        services.AddTransient<SchichtplanungView>();
    }

    public object ErzeugeStartView(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<SchichtplanungView>();
    }
}
