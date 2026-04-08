using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WPF_Gomoku.Models;

namespace WPF_Gomoku.Controllers;

// Netzwerk-Spielmodus: Client-Seite
// Client = Weiß (Server macht den ersten Zug)
//
// Wichtig: Verwende die statische Factory-Methode ConnectAsync() statt des Konstruktors.
// So kann das Spielfeld korrekt erstellt werden NACHDEM die Größe vom Server empfangen wurde.
public class NetworkClientController : IGameController
{
    private bool _gameOver = false;
    private bool _myTurn = false; // Client wartet auf ersten Zug des Servers
    private string _statusMessage = "Verbunden! Gegner ist dran (Schwarz)...";

    private readonly TcpClient _tcpClient;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly CancellationTokenSource _cts = new();

    public GameBoard Board { get; }
    public ICommand CellClickCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    // Privater Konstruktor: Wird nur von ConnectAsync aufgerufen
    private NetworkClientController(GameBoard board, TcpClient client, StreamReader reader, StreamWriter writer)
    {
        Board = board;
        _tcpClient = client;
        _reader = reader;
        _writer = writer;
        CellClickCommand = new RelayCommand(CellClicked, _ => _myTurn && !_gameOver);
    }

    // Factory-Methode: Verbindet zum Server, empfängt Spielfeldgröße, gibt fertigen Controller zurück
    // Rückgabe: (controller, errorMessage) – errorMessage ist null wenn alles OK
    public static async Task<(NetworkClientController? controller, string? error)> ConnectAsync(
        string host, int port = 5000)
    {
        try
        {
            var tcpClient = new TcpClient();

            // Zum Server verbinden (async = wartet ohne UI zu blockieren)
            await tcpClient.ConnectAsync(host, port);

            var stream = tcpClient.GetStream();
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream) { AutoFlush = true };

            // Erste Nachricht vom Server = Spielfeldgröße
            var sizeLine = await reader.ReadLineAsync();
            int size = int.Parse(sizeLine!);

            var board = new GameBoard(size);
            var controller = new NetworkClientController(board, tcpClient, reader, writer);

            // Hintergrund-Loop für eingehende Züge starten (absichtlich nicht awaited)
            _ = Task.Run(() => controller.ReceiveMovesAsync(controller._cts.Token));

            return (controller, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    // Wird aufgerufen wenn der Client-Spieler eine Zelle klickt
    private void CellClicked(object? parameter)
    {
        if (parameter is not Cell cell || cell.State != CellState.Empty) return;

        // Stein setzen und Zug an Server senden
        cell.State = CellState.White;
        _writer.WriteLine($"{cell.Row},{cell.Col}");
        _myTurn = false;
        ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged();

        if (Board.CheckWin(cell.Row, cell.Col, CellState.White))
        {
            _gameOver = true;
            StatusMessage = "Du gewinnst!";
            return;
        }

        StatusMessage = "Gegner ist dran (Schwarz)...";
    }

    // Hintergrund-Loop: Empfängt Züge des Servers
    private async Task ReceiveMovesAsync(CancellationToken ct)
    {
        while (!_gameOver && !ct.IsCancellationRequested)
        {
            var line = await _reader.ReadLineAsync(ct);
            if (line == null) break;

            var parts = line.Split(',');
            if (parts.Length != 2) continue;

            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Board.GetCell(row, col).State = CellState.Black;

                if (Board.CheckWin(row, col, CellState.Black))
                {
                    _gameOver = true;
                    StatusMessage = "Gegner gewinnt!";
                    return;
                }

                _myTurn = true;
                ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged(); 
                StatusMessage = "Du bist dran (Weiß)";
            });
        }
    }

    public void Cleanup()
    {
        _cts.Cancel();
        _writer.Close();
        _reader.Close();
        _tcpClient.Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}