using System.Windows.Media;
using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.Modules.Dashboard.ViewModels;

/// <summary>Präsentationslogik für eine einzelne Maschinen-Kachel im Dashboard.</summary>
public class MaschineViewModel : ViewModelBase
{
    private double _aktuellerWert;
    private MaschinenStatus _status;

    public MaschineViewModel(Maschine maschine)
    {
        Id = maschine.Id;
        Name = maschine.Name;
        Standort = maschine.Standort;
        _aktuellerWert = maschine.AktuellerWert;
        _status = maschine.Status;
    }

    public int Id { get; }

    public string Name { get; }

    public string Standort { get; }

    public double AktuellerWert
    {
        get => _aktuellerWert;
        private set => SetProperty(ref _aktuellerWert, value);
    }

    public MaschinenStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    /// <summary>Farbe der Status-Ampel, direkt als Brush bindbar (kein Konverter nötig).</summary>
    public Brush StatusBrush => Status switch
    {
        MaschinenStatus.Laeuft => Brushes.MediumSeaGreen,
        MaschinenStatus.Stoerung => Brushes.IndianRed,
        MaschinenStatus.Wartung => Brushes.Orange,
        MaschinenStatus.Ruestet => Brushes.DodgerBlue,
        MaschinenStatus.Gestoppt => Brushes.Gray,
        _ => Brushes.Gray
    };

    public string StatusText => Status switch
    {
        MaschinenStatus.Laeuft => "Läuft",
        MaschinenStatus.Stoerung => "Störung",
        MaschinenStatus.Wartung => "Wartung",
        MaschinenStatus.Ruestet => "Rüstet",
        MaschinenStatus.Gestoppt => "Gestoppt",
        _ => "Unbekannt"
    };

    public void Aktualisieren(double neuerWert, MaschinenStatus status)
    {
        AktuellerWert = Math.Round(neuerWert, 1);
        Status = status;
    }
}
