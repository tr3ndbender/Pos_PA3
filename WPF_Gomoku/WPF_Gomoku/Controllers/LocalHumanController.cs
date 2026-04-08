using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPF_Gomoku.Models;

namespace WPF_Gomoku.Controllers;

// Spielmodus: Zwei Spieler am gleichen PC, abwechselnd klicken
public class LocalHumanController : IGameController
{
    private CellState _currentPlayer = CellState.Black; // Schwarz beginnt immer
    private bool _gameOver = false;
    private string _statusMessage = "Schwarz ist dran";

    public GameBoard Board { get; }
    public ICommand CellClickCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public LocalHumanController(int boardSize)
    {
        Board = new GameBoard(boardSize);

        // RelayCommand: führt CellClicked aus, aber nur wenn das Spiel noch läuft
        CellClickCommand = new RelayCommand(CellClicked, _ => !_gameOver);
    }

    // Wird aufgerufen wenn der Spieler eine Zelle klickt
    // parameter = die geklickte Cell (aus CommandParameter="{Binding}" im XAML)
    private void CellClicked(object? parameter)
    {
        if (parameter is not Cell cell) return;
        if (cell.State != CellState.Empty) return; // Zelle bereits belegt → ignorieren

        // Stein des aktuellen Spielers setzen
        cell.State = _currentPlayer;

        // Gewinnbedingung prüfen
        if (Board.CheckWin(cell.Row, cell.Col, _currentPlayer))
        {
            string winner = _currentPlayer == CellState.Black ? "Schwarz" : "Weiß";
            StatusMessage = $"{winner} gewinnt!";
            _gameOver = true;
            // CanExecute neu auswerten → Klicks werden deaktiviert
            ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged();
            return;
        }

        // Spieler wechseln
        _currentPlayer = _currentPlayer == CellState.Black ? CellState.White : CellState.Black;
        StatusMessage = $"{(_currentPlayer == CellState.Black ? "Schwarz" : "Weiß")} ist dran";
    }

    public void Cleanup() { } // Kein Aufräumen nötig

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
