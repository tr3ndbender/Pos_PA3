using System.Windows.Input;

namespace WPF_Gomoku.Controllers;

// RelayCommand: Standard-Implementierung von ICommand für MVVM/MVC
//
// ICommand ist das WPF-Interface für Aktionen die an Buttons/Klicks gebunden werden.
// Statt Click-Events direkt im Code-Behind zu behandeln, binden wir Commands aus dem Controller.
//
// Verwendung in XAML:
//   Command="{Binding MeinCommand}"
//   CommandParameter="{Binding}"   ← gibt das aktuelle DataContext-Objekt als Parameter weiter
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;       // Was soll ausgeführt werden?
    private readonly Func<object?, bool>? _canExecute; // Wann ist der Command aktiv?

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    // Wird ausgeführt wenn der Button geklickt wird
    public void Execute(object? parameter) => _execute(parameter);

    // Bestimmt ob der Button aktiv (klickbar) ist
    // Wenn null → immer aktiv
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    // Event das WPF aufruft um CanExecute neu zu prüfen
    public event EventHandler? CanExecuteChanged;

    // Manuell auslösen wenn sich der aktive Zustand geändert hat
    // z.B. nach Spielende: Command deaktivieren
    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
