using IndustrieDashboard.Modules.Maschinenueberwachung.Views;
using IndustrieDashboard.Shared.Modules;
using IndustrieDashboard.Shared.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrieDashboard.Modules.Maschinenueberwachung;

public class MaschinenueberwachungModule : IAppModule
{
    public string AnzeigeName => "Maschinenüberwachung";

    public string IconKind => "Gauge";

    public int Reihenfolge => 2;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient(_ => new PlatzhalterViewModel(
            "Maschinenüberwachung",
            "Detailansicht je Maschine mit Verlaufsdaten, Alarmen und Grenzwerten – ergänzend zur " +
            "Gesamtübersicht im Dashboard. Nutzt dieselbe IMaschinenDatenQuelle-Abstraktion.",
            new[]
            {
                "Detailansicht je Maschine mit historischem Verlauf",
                "Grenzwert-/Alarmkonfiguration",
                "Anbindung an echte OPC-UA-/MQTT-Quellen (ersetzt die Simulation)",
                "Störungshistorie mit Ursachenanalyse"
            }));
        services.AddTransient<MaschinenueberwachungView>();
    }

    public object ErzeugeStartView(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<MaschinenueberwachungView>();
    }
}
