using System.Collections.ObjectModel;

namespace WPF_Gomoku.Models;

// Das Spielfeld – enthält alle Zellen und die Spiellogik (Gewinnprüfung)
//
// Model im MVC-Pattern: Kennt nur die Daten, nicht die Anzeige.
public class GameBoard
{
    public int Size { get; }

    // ObservableCollection: Wie List<T>, aber WPF bekommt automatisch Bescheid
    // wenn Elemente hinzugefügt oder entfernt werden → ItemsControl aktualisiert sich
    //
    // Die Zellen sind als FLACHE LISTE gespeichert, damit der UniformGrid sie direkt nutzen kann.
    // Reihenfolge: Row 0 Col 0, Row 0 Col 1, ..., Row 1 Col 0, ...
    public ObservableCollection<Cell> Cells { get; } = new();

    public GameBoard(int size)
    {
        Size = size;

        // Alle Zellen erstellen und zur Liste hinzufügen
        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
                Cells.Add(new Cell { Row = row, Col = col });
    }

    // Einzelne Zelle per Zeile/Spalte abrufen
    public Cell GetCell(int row, int col) => Cells[row * Size + col];

    // Prüft ob der Spieler der zuletzt bei (row, col) gesetzt hat, gewonnen hat.
    // Wir prüfen alle 4 Achsen: horizontal, vertikal, diagonal ↘, diagonal ↗
    public bool CheckWin(int row, int col, CellState state)
    {
        // Für jede Achse: Steine in beide Richtungen zählen.
        // Der soeben gesetzte Stein selbst zählt als 1 → wir brauchen 4 weitere (>=4).
        return CountInDirection(row, col, state,  1,  0) + CountInDirection(row, col, state, -1,  0) >= 4  // ←→
            || CountInDirection(row, col, state,  0,  1) + CountInDirection(row, col, state,  0, -1) >= 4  // ↑↓
            || CountInDirection(row, col, state,  1,  1) + CountInDirection(row, col, state, -1, -1) >= 4  // ↘↖
            || CountInDirection(row, col, state,  1, -1) + CountInDirection(row, col, state, -1,  1) >= 4; // ↙↗
    }

    // Zählt aufeinanderfolgende gleichfarbige Steine ab (row,col) in Richtung (dRow, dCol)
    // Stoppt an Feldrand oder anderen Steinen
    private int CountInDirection(int row, int col, CellState state, int dRow, int dCol)
    {
        int count = 0;
        int r = row + dRow;
        int c = col + dCol;

        while (r >= 0 && r < Size && c >= 0 && c < Size && GetCell(r, c).State == state)
        {
            count++;
            r += dRow;
            c += dCol;
        }

        return count;
    }

    // Spielfeld zurücksetzen (alle Zellen leeren)
    public void Reset()
    {
        foreach (var cell in Cells)
            cell.State = CellState.Empty;
    }
}
