using IndustrieDashboard.App.ViewModels;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Infrastructure.Services;
using IndustrieDashboard.Shared.Events;
using IndustrieDashboard.Shared.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndustrieDashboard.Tests.App;

/// <summary>
/// Belegt, dass <see cref="MainWindowViewModel"/> pro Modulwechsel einen
/// eigenen DI-Scope verwendet und den vorherigen beim Wechsel verwirft: Nach
/// mehrfachem Hin- und Herwechseln darf sich am EventAggregator und am
/// Benutzerkontext nicht mehr als ein Abonnent pro Ereignis ansammeln (sonst
/// würden tote ViewModel-Instanzen wie KontrolleingriffeViewModel weiter
/// mitlaufen). Nutzt die echten Singleton-Dienste (EventAggregator,
/// PrototypBenutzerKontext), aber ein leichtgewichtiges Test-Modul statt der
/// echten WPF-Views - die brauchen eine laufende Application mit
/// MaterialDesign-Ressourcen, was hier irrelevant ist: geprüft wird die
/// DI-Scope-Lebensdauer, nicht das Rendering.
/// </summary>
public sealed class MainWindowViewModelTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EventAggregator _eventAggregator;
    private readonly PrototypBenutzerKontext _benutzerKontext;
    private readonly List<IAppModule> _module;

    public MainWindowViewModelTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventAggregator, EventAggregator>();
        services.AddSingleton<PrototypBenutzerKontext>();
        services.AddSingleton<IBenutzerKontext>(sp => sp.GetRequiredService<PrototypBenutzerKontext>());
        services.AddTransient<AbonnierendesTestViewModel>();

        _module = new List<IAppModule> { new TestModul("A", 0), new TestModul("B", 1) };

        _serviceProvider = services.BuildServiceProvider();
        _eventAggregator = (EventAggregator)_serviceProvider.GetRequiredService<IEventAggregator>();
        _benutzerKontext = _serviceProvider.GetRequiredService<PrototypBenutzerKontext>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public void MehrfacherModulwechsel_HinterlaesstNurEinenAbonnentenProEreignis()
    {
        var viewModel = new MainWindowViewModel(_serviceProvider, _module);
        var eintragA = viewModel.NavigationEintraege.Single(e => e.Modul == _module[0]);
        var eintragB = viewModel.NavigationEintraege.Single(e => e.Modul == _module[1]);

        for (var i = 0; i < 5; i++)
        {
            viewModel.AusgewaehlterEintrag = eintragB;
            viewModel.AusgewaehlterEintrag = eintragA;
        }

        viewModel.AusgewaehlterEintrag = eintragB;

        Assert.Equal(1, _eventAggregator.AnzahlAbonnenten<KontrolleingriffStatusGeaendertEvent>());
        Assert.Equal(1, _benutzerKontext.AnzahlBenutzerGewechseltAbonnenten);

        viewModel.Dispose();

        Assert.Equal(0, _eventAggregator.AnzahlAbonnenten<KontrolleingriffStatusGeaendertEvent>());
        Assert.Equal(0, _benutzerKontext.AnzahlBenutzerGewechseltAbonnenten);
    }

    private sealed class TestModul : IAppModule
    {
        public TestModul(string anzeigeName, int reihenfolge)
        {
            AnzeigeName = anzeigeName;
            Reihenfolge = reihenfolge;
        }

        public string AnzeigeName { get; }

        public string IconKind => "Test";

        public int Reihenfolge { get; }

        public void RegisterServices(IServiceCollection services)
        {
        }

        public object ErzeugeStartView(IServiceProvider serviceProvider) =>
            serviceProvider.GetRequiredService<AbonnierendesTestViewModel>();
    }

    /// <summary>
    /// Steht stellvertretend für Modul-ViewModels wie KontrolleingriffeViewModel:
    /// abonniert beim Erzeugen Ereignisse an Singleton-Diensten und meldet
    /// sich in Dispose() wieder ab.
    /// </summary>
    private sealed class AbonnierendesTestViewModel : IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IBenutzerKontext _benutzerKontext;

        public AbonnierendesTestViewModel(IEventAggregator eventAggregator, IBenutzerKontext benutzerKontext)
        {
            _eventAggregator = eventAggregator;
            _benutzerKontext = benutzerKontext;

            _eventAggregator.Subscribe<KontrolleingriffStatusGeaendertEvent>(OnStatusGeaendert);
            _benutzerKontext.BenutzerGewechselt += OnBenutzerGewechselt;
        }

        private void OnStatusGeaendert(KontrolleingriffStatusGeaendertEvent evt)
        {
        }

        private void OnBenutzerGewechselt(object? sender, EventArgs e)
        {
        }

        public void Dispose()
        {
            _eventAggregator.Unsubscribe<KontrolleingriffStatusGeaendertEvent>(OnStatusGeaendert);
            _benutzerKontext.BenutzerGewechselt -= OnBenutzerGewechselt;
        }
    }
}
