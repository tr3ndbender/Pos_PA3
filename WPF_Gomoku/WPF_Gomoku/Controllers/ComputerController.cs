using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPF_Gomoku.Models;

namespace WPF_Gomoku.Controllers;

// Spielmodus: Mensch (Schwarz) gegen einfache KI (Weiß)
public class ComputerController : IGameController
{
    private bool _gameOver = false;
    private bool _playerTurn = true; // true = Spieler dran, false = Computer dran
    private string _statusMessage = "Du bist dran (Schwarz)";

    public GameBoard Board { get; }
    public ICommand CellClickCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public ComputerController(int boardSize)
    {
        Board = new GameBoard(boardSize);
        // Command ist nur aktiv wenn der Spieler dran ist und das Spiel läuft
        CellClickCommand = new RelayCommand(CellClicked, _ => _playerTurn && !_gameOver);
    }

    private void CellClicked(object? parameter)
    {
        if (parameter is not Cell cell || cell.State != CellState.Empty) return;

        // Spielerzug (Schwarz)
        PlaceStone(cell.Row, cell.Col, CellState.Black);
        if (_gameOver) return;

        // Computerzug (Weiß)
        _playerTurn = false;
        ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged();

        var (r, c) = FindBestMove();
        PlaceStone(r, c, CellState.White);

        if (!_gameOver)
        {
            _playerTurn = true;
            ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged();
            StatusMessage = "Du bist dran (Schwarz)";
        }
    }

    private void PlaceStone(int row, int col, CellState state)
    {
        Board.GetCell(row, col).State = state;

        if (Board.CheckWin(row, col, state))
        {
            StatusMessage = state == CellState.Black ? "Du gewinnst!" : "Computer gewinnt!";
            _gameOver = true;
            ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged();
        }
    }

    // Einfache KI-Strategie (3 Schritte):
    //   1. Kann ich gewinnen? → Ja: setze dort
    //   2. Droht der Gegner zu gewinnen? → Ja: blockiere
    //   3. Setze neben einen vorhandenen Stein
    private (int row, int col) FindBestMove()
    {
        // 1. Gewinnzug für Computer?
        var win = FindWinningMove(CellState.White);
        if (win.HasValue) return win.Value;

        // 2. Blockierzug gegen Spieler?
        var block = FindWinningMove(CellState.Black);
        if (block.HasValue) return block.Value;

        // 3. Neben bestehende Steine setzen
        var adjacent = FindAdjacentEmpty();
        if (adjacent.HasValue) return adjacent.Value;

        // 4. Mitte, falls frei
        int mid = Board.Size / 2;
        if (Board.GetCell(mid, mid).State == CellState.Empty) return (mid, mid);

        // 5. Irgendeine freie Zelle
        return FindAnyEmpty();
    }

    // Prüft ob ein sofortiger Gewinnzug für 'state' möglich ist
    private (int, int)? FindWinningMove(CellState state)
    {
        foreach (var cell in Board.Cells)
        {
            if (cell.State != CellState.Empty) continue;

            // Stein temporär setzen und Gewinnbedingung prüfen
            cell.State = state;
            bool wins = Board.CheckWin(cell.Row, cell.Col, state);
            cell.State = CellState.Empty; // zurücksetzen

            if (wins) return (cell.Row, cell.Col);
        }
        return null;
    }

    // Sucht eine leere Zelle die direkt neben einem gesetzten Stein liegt
    private (int, int)? FindAdjacentEmpty()
    {
        foreach (var cell in Board.Cells)
        {
            if (cell.State != CellState.Empty) continue;

            for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = cell.Row + dr, nc = cell.Col + dc;
                if (nr >= 0 && nr < Board.Size && nc >= 0 && nc < Board.Size)
                    if (Board.GetCell(nr, nc).State != CellState.Empty)
                        return (cell.Row, cell.Col);
            }
        }
        return null;
    }

    private (int, int) FindAnyEmpty()
    {
        foreach (var cell in Board.Cells)
            if (cell.State == CellState.Empty)
                return (cell.Row, cell.Col);
        return (0, 0);
    }

    public void Cleanup() { }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
