using System.Windows.Input;

namespace IndustrieDashboard.Shared.Mvvm;

/// <summary>
/// ICommand für asynchrone Operationen (z. B. Laden von Daten, Kontrolleingriffe
/// freigeben). Verhindert Doppelausführung, während die Aktion bereits läuft.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private bool _laeuft;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_laeuft && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        _laeuft = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute(parameter);
        }
        finally
        {
            _laeuft = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
