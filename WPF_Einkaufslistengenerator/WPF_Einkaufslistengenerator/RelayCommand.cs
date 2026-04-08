using System.Windows.Input;

namespace WPF_Einkaufslistengenerator;

// Universeller ICommand-Wrapper: jeder Button/Menüpunkt bekommt eine Instanz davon.
// execute  = was passiert beim Klick
// canExecute = wann ist der Befehl aktiv (optional)
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    // WPF fragt das automatisch ab – CommandManager.RequerySuggested löst das aus
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
