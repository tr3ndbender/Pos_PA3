using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WPF_Gomoku.Models;

namespace WPF_Gomoku.Controllers;

// Netzwerk-Spielmodus: Server-Seite
// Server = Schwarz (fängt an)
// Protokoll: Züge werden als "row,col\n" über TCP gesendet
public class NetworkServerController : IGameController
{
    private bool _gameOver = false;
    private bool _myTurn = false;  // Server darf erst nach Verbindungsaufbau ziehen
    private string _statusMessage;

    // TCP-Objekte für die Verbindung
    private TcpListener? _listener;
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    // CancellationTokenSource: ermöglicht das Abbrechen der async Operationen
    private readonly CancellationTokenSource _cts = new();

    public GameBoard Board { get; }
    public ICommand CellClickCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public NetworkServerController(int boardSize, int port = 5000)
    {
        Board = new GameBoard(boardSize);
        _statusMessage = $"Warte auf Verbindung (Port {port})...";
        CellClickCommand = new RelayCommand(CellClicked, _ => _myTurn && !_gameOver);

        // Server im Hintergrund starten (blockiert nicht den UI-Thread)
        Task.Run(() => StartServerAsync(boardSize, port, _cts.Token));
    }

    private async Task StartServerAsync(int boardSize, int port, CancellationToken ct)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            // Warten bis sich ein Client verbindet
            _client = await _listener.AcceptTcpClientAsync(ct);

            var stream = _client.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };

            // Spielfeldgröße an Client senden (damit Client das gleiche Board erstellt)
            await _writer.WriteLineAsync(boardSize.ToString());

            // Jetzt darf der Server (Schwarz) den ersten Zug machen
            _myTurn = true;
            RunOnUi(() =>
            {
                StatusMessage = "Verbunden! Du bist dran (Schwarz)";
                ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged();
            });

            // In einer Schleife auf Züge des Clients warten
            await ReceiveMovesAsync(ct);
        }
        catch (OperationCanceledException) { /* Absicht: Verbindung wurde geschlossen */ }
        catch (Exception ex) { RunOnUi(() => StatusMessage = $"Fehler: {ex.Message}"); }
    }

    // Wird aufgerufen wenn der Server-Spieler eine Zelle klickt
    private void CellClicked(object? parameter)
    {
        if (parameter is not Cell cell || cell.State != CellState.Empty) return;

        // Stein setzen und Zug an Client senden
        cell.State = CellState.Black;
        _writer?.WriteLine($"{cell.Row},{cell.Col}");
        _myTurn = false;
        ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged();

        if (Board.CheckWin(cell.Row, cell.Col, CellState.Black))
        {
            _gameOver = true;
            StatusMessage = "Du gewinnst!";
            return;
        }

        StatusMessage = "Gegner ist dran (Weiß)...";
    }

    // Hintergrund-Loop: Empfängt Züge des Clients
    private async Task ReceiveMovesAsync(CancellationToken ct)
    {
        while (!_gameOver && !ct.IsCancellationRequested)
        {
            var line = await _reader!.ReadLineAsync(ct);
            if (line == null) break; // Verbindung getrennt

            var parts = line.Split(',');
            if (parts.Length != 2) continue;

            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            // UI-Update muss auf dem UI-Thread laufen (Dispatcher)
            RunOnUi(() =>
            {
                Board.GetCell(row, col).State = CellState.White;

                if (Board.CheckWin(row, col, CellState.White))
                {
                    _gameOver = true;
                    StatusMessage = "Gegner gewinnt!";
                    return;
                }

                _myTurn = true;
                ((RelayCommand)CellClickCommand).RaiseCanExecuteChanged();
                StatusMessage = "Du bist dran (Schwarz)";
            });
        }
    }

    // Hilfsmethode: Code auf dem UI-Thread ausführen
    // Nötig weil Netzwerk-Code auf einem Hintergrund-Thread läuft
    private static void RunOnUi(Action action)
        => Application.Current.Dispatcher.Invoke(action);

    public void Cleanup()
    {
        _cts.Cancel();       // Alle async Operationen abbrechen
        _writer?.Close();
        _reader?.Close();
        _client?.Close();
        _listener?.Stop();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
