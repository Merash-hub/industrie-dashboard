using IndustrieDashboard.Modules.Kontrolleingriffe.ViewModels;
using IndustrieDashboard.Modules.Kontrolleingriffe.Views;
using IndustrieDashboard.Shared.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrieDashboard.Modules.Kontrolleingriffe;

public class KontrolleingriffeModule : IAppModule
{
    public string AnzeigeName => "Kontrolleingriffe";

    public string IconKind => "ShieldCheckOutline";

    public int Reihenfolge => 3;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<KontrolleingriffeViewModel>();
        services.AddTransient<KontrolleingriffeView>();
    }

    public object ErzeugeStartView(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<KontrolleingriffeView>();
    }
}
