using IndustrieDashboard.Modules.Kontrolleingriffe.Views;
using IndustrieDashboard.Shared.Modules;
using IndustrieDashboard.Shared.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrieDashboard.Modules.Kontrolleingriffe;

public class KontrolleingriffeModule : IAppModule
{
    public string AnzeigeName => "Kontrolleingriffe";

    public string IconKind => "ShieldCheckOutline";

    public int Reihenfolge => 3;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient(_ => new PlatzhalterViewModel(
            "Kontrolleingriffe",
            "Übersicht und Freigabe kritischer Eingriffe nach dem Vier-Augen-Prinzip sowie Einsicht in " +
            "den vollständigen Audit-Trail. IKontrolleingriffService und IAuditLogService sind bereits " +
            "implementiert (siehe Infrastructure-Projekt).",
            new[]
            {
                "Liste offener Anforderungen mit Freigabe-Funktion",
                "Vollständige Audit-Log-Ansicht (filter-/exportierbar)",
                "Rollen-/Rechtekonzept (wer darf anfordern, wer freigeben)",
                "Anbindung an Active Directory / IdentityServer"
            }));
        services.AddTransient<KontrolleingriffeView>();
    }

    public object ErzeugeStartView(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<KontrolleingriffeView>();
    }
}
