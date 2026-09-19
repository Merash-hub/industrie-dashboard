using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Infrastructure.Data;
using IndustrieDashboard.Infrastructure.Services;
using IndustrieDashboard.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrieDashboard.Infrastructure.DependencyInjection;

/// <summary>
/// Zentrale Registrierung aller Infrastruktur-Services. Wird einmal aus der
/// App-Composition-Root aufgerufen (siehe App.xaml.cs).
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastruktur(this IServiceCollection services, string sqliteDbPfad)
    {
        services.AddPooledDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={sqliteDbPfad}"));

        services.AddSingleton<IAuditLogService, AuditLogService>();
        services.AddSingleton<IKontrolleingriffService, KontrolleingriffService>();

        // Für den Prototyp: simulierte Datenquelle als Singleton, damit der
        // Timer über die Lebensdauer der App läuft. Später hier einfach durch
        // die echte OPC-UA-/MQTT-Implementierung ersetzen.
        services.AddSingleton<IMaschinenDatenQuelle, SimulierteMaschinenDatenQuelle>();

        return services;
    }
}
