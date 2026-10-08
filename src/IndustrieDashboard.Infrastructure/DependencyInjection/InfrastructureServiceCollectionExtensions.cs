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
    public static IServiceCollection AddInfrastruktur(this IServiceCollection services, string sqliteDbPfad, VerlaufsOptionen? verlaufsOptionen = null)
    {
        services.AddPooledDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={sqliteDbPfad}"));

        services.AddSingleton<IAuditLogService, AuditLogService>();
        services.AddSingleton<IKontrolleingriffService, KontrolleingriffService>();

        // IBenutzerKontext (und ggf. IBenutzerWechsel) werden bewusst NICHT hier
        // registriert: Welcher Anbieter (Windows/Prototyp) verwendet wird, ist
        // eine Entscheidung der Composition Root anhand der Konfiguration
        // (siehe App.xaml.cs, Spezifikation A7/A8) - die Infrastruktur-Schicht
        // kennt diese Entscheidung nicht.

        // Für den Prototyp: simulierte Datenquelle als Singleton, damit der
        // Timer über die Lebensdauer der App läuft. Später hier einfach durch
        // die echte OPC-UA-/MQTT-Implementierung ersetzen.
        services.AddSingleton<IMaschinenDatenQuelle, SimulierteMaschinenDatenQuelle>();

        // Anzeigeverlauf (Spezifikation Teil B): langlebiger Singleton, überdauert
        // Modulwechsel. IAuswertungsQuelle wird später durch eine PostgreSQL-
        // Umsetzung ersetzt (Teil D, Stufe 2), ohne dass sich an MaschinenVerlaufsDienst
        // oder den Modulen etwas ändert.
        services.AddSingleton(verlaufsOptionen ?? new VerlaufsOptionen());
        services.AddSingleton<IAuswertungsQuelle, SimulierteAuswertungsQuelle>();
        services.AddSingleton<IVerlaufsDienst, MaschinenVerlaufsDienst>();

        return services;
    }
}
