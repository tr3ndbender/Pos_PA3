using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using WpfChat.Server.Database;
using WpfChat.Shared.Protocol;

namespace WpfChat.Server.Network
{
    /// <summary>
    /// Der TCP-Server. Wartet auf neue Verbindungen und verwaltet alle verbundenen Clients.
    /// </summary>
    public class ChatServer
    {
        private TcpListener? _listener;
        private bool _läuft;

        private readonly DatabaseManager _db;
        private readonly List<ClientHandler> _clients = new();
        private readonly object _lock = new(); // für thread-sicheren Zugriff auf _clients

        /// <summary>Wird aufgerufen wenn etwas geloggt werden soll (für die Server-GUI).</summary>
        public event Action<string>? OnLog;

        public ChatServer(DatabaseManager db)
        {
            _db = db;
        }

        // ── Start / Stop ──────────────────────────────────────

        public void Starten(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _läuft = true;
            Log($"Server gestartet auf Port {port}");
            Task.Run(VerbindungenAnnehmen);
        }

        public void Stoppen()
        {
            _läuft = false;
            _listener?.Stop();
            Log("Server gestoppt.");
        }

        // Wartet in einer Schleife auf neue Client-Verbindungen
        private async Task VerbindungenAnnehmen()
        {
            while (_läuft)
            {
                try
                {
                    var tcpClient = await _listener!.AcceptTcpClientAsync();
                    Log($"Verbindung von {tcpClient.Client.RemoteEndPoint}");

                    // Für jeden Client einen eigenen Handler starten
                    var handler = new ClientHandler(tcpClient, this, _db);
                    handler.Starten();
                }
                catch
                {
                    if (!_läuft) break; // Server wurde gestoppt → kein Fehler
                }
            }
        }

        // ── Client-Verwaltung ─────────────────────────────────

        public void ClientHinzufügen(ClientHandler client)
        {
            lock (_lock) _clients.Add(client);
        }

        public void ClientEntfernen(ClientHandler client)
        {
            lock (_lock) _clients.Remove(client);
        }

        /// <summary>Gibt den Client mit dem angegebenen Benutzernamen zurück (oder null).</summary>
        public ClientHandler? ClientFinden(string username)
        {
            lock (_lock)
                return _clients.FirstOrDefault(c => c.Username == username);
        }

        /// <summary>Gibt alle Benutzernamen zurück die aktuell in einem Raum sind.</summary>
        public List<string> MitgliederImRaum(string raumName)
        {
            lock (_lock)
                return _clients
                    .Where(c => c.IstAngemeldet && c.BetreteneRäume.Contains(raumName))
                    .Select(c => c.Username!)
                    .ToList();
        }

        // ── Nachrichten verteilen ─────────────────────────────

        /// <summary>Sendet ein Paket an alle angemeldeten Clients.</summary>
        public void AnAlle(ChatPacket paket)
        {
            lock (_lock)
                foreach (var c in _clients.Where(c => c.IstAngemeldet))
                    c.Senden(paket);
        }

        /// <summary>Sendet ein Paket an alle außer einem bestimmten Client.</summary>
        public void AnAlleAußer(ChatPacket paket, ClientHandler ausnahme)
        {
            lock (_lock)
                foreach (var c in _clients.Where(c => c.IstAngemeldet && c != ausnahme))
                    c.Senden(paket);
        }

        /// <summary>Sendet ein Paket an alle Clients in einem bestimmten Raum.</summary>
        public void AnRaum(string raumName, ChatPacket paket, ClientHandler? ausnahme = null)
        {
            lock (_lock)
                foreach (var c in _clients.Where(c =>
                    c.IstAngemeldet && c.BetreteneRäume.Contains(raumName) && c != ausnahme))
                    c.Senden(paket);
        }

        // ── Logging ───────────────────────────────────────────

        public void Log(string nachricht)
        {
            var zeile = $"[{DateTime.Now:HH:mm:ss}] {nachricht}";
            OnLog?.Invoke(zeile);
        }
    }
}
