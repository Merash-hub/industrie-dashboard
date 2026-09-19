using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IndustrieDashboard.Shared.Mvvm;

/// <summary>
/// Basisklasse für alle ViewModels. Stellt INotifyPropertyChanged inkl.
/// bequemer SetProperty-Hilfsmethode bereit (Standard-MVVM, framework-agnostisch,
/// damit später problemlos gegen CommunityToolkit.Mvvm o. Ä. getauscht werden kann).
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T feld, T wert, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(feld, wert))
        {
            return false;
        }

        feld = wert;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
