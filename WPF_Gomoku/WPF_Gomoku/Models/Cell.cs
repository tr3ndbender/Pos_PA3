using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPF_Gomoku.Models;

// Eine einzelne Zelle auf dem Spielfeld
//
// INotifyPropertyChanged ist das WPF-Binding-Interface:
// Wenn sich eine Property ändert, rufen wir OnPropertyChanged() auf →
// WPF bekommt die Benachrichtigung und aktualisiert die UI automatisch.
public class Cell : INotifyPropertyChanged
{
    private CellState _state = CellState.Empty;

    // Position der Zelle im Spielfeld
    public int Row { get; init; }
    public int Col { get; init; }

    // Zustand der Zelle (welcher Stein liegt hier, oder leer?)
    // Das Setter ruft OnPropertyChanged() auf → UI-Update via Binding
    public CellState State
    {
        get => _state;
        set
        {
            _state = value;
            OnPropertyChanged(); // Binding benachrichtigen
        }
    }

    // INotifyPropertyChanged: Event das WPF abonniert
    public event PropertyChangedEventHandler? PropertyChanged;

    // Hilfsmethode: Sendet das Event für die angegebene Property
    // [CallerMemberName] füllt 'name' automatisch mit dem Namen der aufrufenden Property
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
