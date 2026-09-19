using System.Windows.Input;

namespace IndustrieDashboard.Shared.Mvvm;

/// <summary>Einfaches synchrones ICommand für MVVM-Bindings.</summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // Bewusst kein CommandManager.RequerySuggested (WPF-spezifisch, würde die
    // Shared-Bibliothek an WPF binden und in WPF selbst unnötig viele
    // CanExecute-Aufrufe auslösen). ViewModels rufen RaiseCanExecuteChanged()
    // gezielt auf, wenn sich der Zustand ändert.
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
