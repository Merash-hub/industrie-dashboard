using IndustrieDashboard.Modules.Dashboard.ViewModels;
using IndustrieDashboard.Modules.Dashboard.Views;
using IndustrieDashboard.Shared.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrieDashboard.Modules.Dashboard;

/// <summary>
/// Registrierungspunkt des Dashboard-Moduls. Wird in
/// IndustrieDashboard.App/App.xaml.cs in die Liste der Module aufgenommen.
/// </summary>
public class DashboardModule : IAppModule
{
    public string AnzeigeName => "Dashboard";

    public string IconKind => "ViewDashboard";

    public int Reihenfolge => 0;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DashboardView>();
    }

    public object ErzeugeStartView(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<DashboardView>();
    }
}
