using System.ComponentModel;
using System.Windows.Input;
using WPF_Gomoku.Models;

namespace WPF_Gomoku.Controllers;

// Interface für alle Spielmodi – das ist der "Controller" im MVC-Pattern
//
// MVC-Pattern:
//   Model      = GameBoard / Cell  (Daten)
//   View       = GameWindow.xaml   (Anzeige)
//   Controller = IGameController   (Logik, Spielregeln, Netzwerk, ...)
//
// Durch das Interface kann GameWindow mit JEDEM Spielmodus arbeiten,
// ohne zu wissen ob es Mensch vs Mensch, KI oder Netzwerk ist.
// → "austauschbare Controller-Klasse"
public interface IGameController : INotifyPropertyChanged
{
    // Das Spielfeld-Model (gebunden an ItemsControl in der View)
    GameBoard Board { get; }

    // Statusmeldung für die UI (z.B. "Schwarz ist dran", "Weiß gewinnt!")
    string StatusMessage { get; }

    // Command der ausgeführt wird wenn eine Zelle geklickt wird
    // Der CommandParameter ist die geklickte Cell
    ICommand CellClickCommand { get; }

    // Aufräumen wenn das Fenster geschlossen wird
    // (z.B. Netzwerkverbindung schließen)
    void Cleanup();
}
