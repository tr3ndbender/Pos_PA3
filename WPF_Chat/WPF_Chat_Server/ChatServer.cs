using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WPF_Chat_Server
{
    public class ChatServer
    {
        // TcpListener wartet auf eingehende Verbindungen
        private TcpListener _listener;

        // Zugriff auf die Datenbank
        private readonly Database _db;

        // Liste aller aktuell verbundenen Clients
        // "lock (_clients)" stellt sicher dass nie zwei Threads gleichzeitig darauf zugreifen
        private readonly List<ClientHandler> _clients = new();

        // Callback-Funktion zum Schreiben ins Server-Protokoll (die ListBox in der GUI)
        private readonly Action<string> _log;

        public ChatServer(Database db, Action<string> log)
        {
            _db = db;
            _log = log;
        }

        // Startet den Server auf dem angegebenen Port.
        public void Start(int port)
        {
            // IPAddress.Any = auf allen Netzwerkkarten hören
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _log($"Server gestartet auf Port {port}");

            // AcceptClients in einem Hintergrund-Thread starten damit der UI-Thread
            // nicht blockiert wird (AcceptTcpClient wartet auf Verbindungen)
            Thread thread = new Thread(AcceptClients);
            thread.IsBackground = true; // Beendet sich automatisch wenn das Programm endet
            thread.Start();
        }

        // Läuft im Hintergrund und nimmt neue Client-Verbindungen an.
        private void AcceptClients()
        {
            while (true)
            {
                // AcceptTcpClient() blockiert bis ein Client sich verbindet
                TcpClient client = _listener.AcceptTcpClient();
                _log("Neuer Client verbunden");

                // Für jeden Client einen eigenen ClientHandler erstellen
                ClientHandler handler = new ClientHandler(client, _db, _log, this);

                // Thread-sicher zur Liste hinzufügen
                lock (_clients) { _clients.Add(handler); }

                // Client in eigenem Thread verarbeiten damit andere Clients nicht warten müssen
                Thread t = new Thread(handler.Handle);
                t.IsBackground = true;
                t.Start();
            }
        }

        // Sendet eine Nachricht an alle Clients AUSSER dem Absender.
        // Wird für Raum-Nachrichten und Profil-Updates verwendet.
        public void Broadcast(string message, ClientHandler sender)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    if (client != sender) // Absender überspringen
                        client.Send(message);
                }
            }
        }

        // Sendet eine Nachricht an ALLE verbundenen Clients (inkl. Absender).
        // Wird z.B. verwendet wenn ein neuer Raum erstellt wird.
        public void BroadcastToAll(string message)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                    client.Send(message);
            }
        }

        // Sendet eine Nachricht nur an einen bestimmten Benutzer.
        // Wird für private Nachrichten verwendet.
        public void SendToUser(int userId, string message)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    // UserId vergleichen um den richtigen Client zu finden
                    if (client.UserId == userId)
                    {
                        client.Send(message);
                        return; // Gefunden → fertig
                    }
                }
            }
        }

        // Gibt eine Kopie der Liste aller verbundenen Clients zurück.
        // Wird genutzt damit neue Clients die Profile aller anderen erhalten.
        public List<ClientHandler> GetConnectedClients()
        {
            lock (_clients) { return new List<ClientHandler>(_clients); }
        }

        // Entfernt einen Client aus der Liste (wird vom ClientHandler aufgerufen wenn
        // die Verbindung getrennt wird).
        public void RemoveClient(ClientHandler handler)
        {
            lock (_clients) { _clients.Remove(handler); }

            // Allen anderen Clients mitteilen dass dieser User offline ist
            // damit sie ihn aus der Online-Liste entfernen können
            if (handler.UserId != -1)
                Broadcast($"USER_OFFLINE|{handler.UserId}|{handler.Username}", handler);
        }
    }
}
