using System.IO;
using System.Windows;
using IndustrieDashboard.App.Identitaet;
using IndustrieDashboard.App.ViewModels;
using IndustrieDashboard.App.Views;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Identity.Windows;
using IndustrieDashboard.Infrastructure.Data;
using IndustrieDashboard.Infrastructure.DependencyInjection;
using IndustrieDashboard.Infrastructure.Services;
using IndustrieDashboard.Modules.Dashboard;
using IndustrieDashboard.Modules.Kontrolleingriffe;
using IndustrieDashboard.Modules.Maschinenueberwachung;
using IndustrieDashboard.Modules.Schichtplanung;
using IndustrieDashboard.Shared.Events;
using IndustrieDashboard.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            IConfiguration? konfiguration;
            try
            {
                konfiguration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .Build();
            }
            catch (Exception ex)
            {
                // Fail closed, aber nicht Fail-Startup: eine unlesbare
                // Konfigurationsdatei fuehrt zum sicheren Standard "Windows"
                // (Spezifikation A8), nicht zum Startabbruch.
                Log.Warning(ex, "Konfigurationsdatei konnte nicht gelesen werden; Standard 'Windows' wird verwendet.");
                konfiguration = null;
            }

#if DEBUG
            const bool istDebugBuild = true;
#else
            const bool istDebugBuild = false;
#endif

            var identitaetsanbieter = IdentitaetsanbieterErmittler.Ermitteln(konfiguration, istDebugBuild);
            Log.Information("Identitätsanbieter: {Identitaetsanbieter}", identitaetsanbieter);

            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            services.AddInfrastruktur(sqliteDbPfad);
            services.AddSingleton<IEventAggregator, EventAggregator>();

            switch (identitaetsanbieter)
            {
                case Identitaetsanbieter.Prototyp:
                    // Erst die konkrete Instanz registrieren, dann beide Schnittstellen
                    // darauf abbilden, damit IBenutzerKontext und IBenutzerWechsel
                    // garantiert dieselbe Instanz liefern.
                    services.AddSingleton<PrototypBenutzerKontext>();
                    services.AddSingleton<IBenutzerKontext>(sp => sp.GetRequiredService<PrototypBenutzerKontext>());
                    services.AddSingleton<IBenutzerWechsel>(sp => sp.GetRequiredService<PrototypBenutzerKontext>());
                    break;

                case Identitaetsanbieter.Windows:
                    var rollenGruppen = konfiguration?.GetSection("Sicherheit:RollenGruppen").Get<RollenGruppenOptions>()
                        ?? new RollenGruppenOptions();
                    // Keine IBenutzerWechsel-Registrierung: Der Benutzerwechsel ist
                    // reine Prototyp-Funktionalität (Spezifikation A8).
                    services.AddSingleton<WindowsBenutzerKontext>(sp =>
                        WindowsBenutzerKontext.AusAktuellerAnmeldung(
                            rollenGruppen,
                            sp.GetRequiredService<IAuditLogService>(),
                            sp.GetRequiredService<ILogger<WindowsBenutzerKontext>>()));
                    services.AddSingleton<IBenutzerKontext>(sp => sp.GetRequiredService<WindowsBenutzerKontext>());
                    break;
            }

            foreach (var modul in _alleModule)
            {
                modul.RegisterServices(services);
            }

            services.AddSingleton(_alleModule);
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = dbContextFactory.CreateDbContext();
                db.Database.Migrate();
            }

            // Die Maschinenüberwachung ist ein geteilter Singleton-Dienst und gehört
            // damit auf Anwendungsebene: läuft vom Programmstart bis zum Beenden
            // (dort per _serviceProvider.Dispose(), das IMaschinenDatenQuelle als
            // IDisposable automatisch mit stoppt), unabhängig davon, welches Modul
            // gerade angezeigt wird. Ein kurzlebiges Modul-ViewModel darf sie nicht
            // abschalten, nur wenn es selbst gerade nicht mehr angezeigt wird.
            _serviceProvider.GetRequiredService<IMaschinenDatenQuelle>().StartUeberwachung();

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
        // Reihenfolge wichtig: Erst den Container disposen (das stoppt u. a.
        // die Maschinenüberwachung über IDisposable und protokolliert das
        // noch), danach den Logger schließen - nicht umgekehrt, sonst gehen
        // Log-Meldungen aus der Container-Entsorgung ins Leere.
        _serviceProvider?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
