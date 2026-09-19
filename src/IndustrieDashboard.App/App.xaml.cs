using System.IO;
using System.Windows;
using IndustrieDashboard.App.ViewModels;
using IndustrieDashboard.App.Views;
using IndustrieDashboard.Infrastructure.Data;
using IndustrieDashboard.Infrastructure.DependencyInjection;
using IndustrieDashboard.Modules.Dashboard;
using IndustrieDashboard.Modules.Kontrolleingriffe;
using IndustrieDashboard.Modules.Maschinenueberwachung;
using IndustrieDashboard.Modules.Schichtplanung;
using IndustrieDashboard.Shared.Events;
using IndustrieDashboard.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace IndustrieDashboard.App;

/// <summary>
/// Composition Root der Anwendung: baut den DI-Container, registriert
/// Infrastruktur und alle Fachmodule und startet das Hauptfenster.
///
/// Neues Modul hinzufügen? Einfach eine IAppModule-Implementierung schreiben
/// und hier in <see cref="_alleModule"/> eintragen - mehr Änderungen an der
/// Shell sind nicht nötig (siehe IndustrieDashboard.Shared.Modules.IAppModule).
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    private readonly IReadOnlyList<IAppModule> _alleModule = new List<IAppModule>
    {
        new DashboardModule(),
        new SchichtplanungModule(),
        new MaschinenueberwachungModule(),
        new KontrolleingriffeModule()
    };

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var datenVerzeichnis = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IndustrieDashboard");
        Directory.CreateDirectory(datenVerzeichnis);

        var sqliteDbPfad = Path.Combine(datenVerzeichnis, "industriedashboard.db");
        var logDateiPfad = Path.Combine(datenVerzeichnis, "logs", "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(logDateiPfad, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            services.AddInfrastruktur(sqliteDbPfad);
            services.AddSingleton<IEventAggregator, EventAggregator>();

            foreach (var modul in _alleModule)
            {
                modul.RegisterServices(services);
            }

            services.AddSingleton(_alleModule);
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            // Prototyp: Schema direkt aus dem EF-Core-Modell erzeugen. Sobald echte
            // Migrationen existieren (dotnet ef migrations add ...), hier durch
            // db.Database.Migrate() ersetzen.
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = dbContextFactory.CreateDbContext();
                db.SicherstellenErstelltMitAuditSchutz();
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Anwendung konnte nicht gestartet werden");
            MessageBox.Show(
                $"Die Anwendung konnte nicht gestartet werden:\n\n{ex.Message}",
                "Startfehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
