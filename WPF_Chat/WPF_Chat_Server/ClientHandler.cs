using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace WPF_Chat_Server
{
    // Diese Klasse verwaltet die Verbindung zu EINEM Client.
    // Für jeden verbundenen Client läuft eine eigene Instanz in einem eigenen Thread.
    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly Database _db;
        private readonly Action<string> _log;
        private readonly ChatServer _server;
        private StreamWriter? _writer;

        // ---- Öffentliche Properties ----
        // "public" damit ChatServer nach Benutzer suchen kann (z.B. für private Nachrichten)

        // Die ID des eingeloggten Benutzers (-1 = noch nicht eingeloggt)
        public int UserId { get; private set; } = -1;

        // Der Benutzername des eingeloggten Benutzers
        public string Username { get; private set; } = "";

        // Das aktuelle Profil dieses Clients (wird für neue Clients gespeichert)
        // Damit neue Clients alle Profile der bereits eingeloggten User empfangen können
        public string Color { get; private set; } = "#000000";
        public string ImageBase64 { get; private set; } = "";

        // Liste der Räume denen dieser Client beigetreten ist
        private readonly List<int> _joinedRooms = new();

        public ClientHandler(TcpClient client, Database db, Action<string> log, ChatServer server)
        {
            _client = client;
            _db = db;
            _log = log;
            _server = server;
        }

        // Hauptschleife des Clients – läuft bis die Verbindung getrennt wird.
        public void Handle()
        {
            try
            {
                var stream = _client.GetStream();
                var reader = new StreamReader(stream);
                // AutoFlush = true: Daten werden sofort gesendet ohne zu warten bis der
                // Puffer voll ist
                _writer = new StreamWriter(stream) { AutoFlush = true };

                string? line;
                // ReadLine() gibt null zurück wenn die Verbindung getrennt wird
                while ((line = reader.ReadLine()) != null)
                {
                    ProcessMessage(line);
                }
            }
            catch (Exception ex)
            {
                _log($"Verbindungsfehler ({Username}): {ex.Message}");
            }
            finally
            {
                // Egal ob Fehler oder normales Beenden: Client aus der Liste entfernen
                _server.RemoveClient(this);
                _log($"{Username} hat die Verbindung getrennt");
            }
        }

        // Verarbeitet eine einzelne empfangene Nachricht.
        private void ProcessMessage(string line)
        {
            // Protokoll-Format: COMMAND|param1|param2|...
            //
            // Wir teilen maximal in 5 Teile auf.
            // Das Limit "5" verhindert dass z.B. ein Nachrichtentext der '|' enthält
            // falsch aufgeteilt wird.
            // Beispiel ohne Limit: "MSG|1|Hallo|Welt" → 4 Teile → parts[3] = "Welt" FEHLT
            // Mit Limit 5:         "MSG|1|Hallo|Welt" → ["MSG","1","Hallo|Welt"]
            var parts = line.Split('|', 5);
            var command = parts[0];

            switch (command)
            {
                case "REGISTER":
                    // REGISTER|username|password
                    bool registered = _db.RegisterUser(parts[1], parts[2]);
                    Send(registered ? "REGISTER_OK" : "REGISTER_FAIL");
                    _log(registered
                        ? $"Registrierung: {parts[1]}"
                        : $"Registrierung fehlgeschlagen: {parts[1]}");
                    break;

                case "LOGIN":
                    // LOGIN|username|password
                    var id = _db.LoginUser(parts[1], parts[2]);
                    if (id != null)
                    {
                        UserId = id.Value;
                        Username = parts[1];

                        // Gespeichertes Profil aus der Datenbank laden
                        var profile = _db.GetUserProfile(UserId);
                        Color = profile.Color;
                        ImageBase64 = profile.Image;

                        // LOGIN_OK|userId|username|color|imageBase64
                        Send($"LOGIN_OK|{UserId}|{Username}|{Color}|{ImageBase64}");
                        _log($"Login: {Username}");
                    }
                    else
                    {
                        Send("LOGIN_FAIL");
                        _log($"Login fehlgeschlagen: {parts[1]}");
                    }
                    break;

                case "GET_ROOMS":
                    // GET_ROOMS → alle Räume aus der DB schicken
                    var rooms = _db.GetRooms();
                    foreach (var room in rooms)
                        Send($"ROOM|{room.Id}|{room.Name}");
                    Send("ROOMS_END");

                    // Gleichzeitig alle Profile der bereits online User schicken.
                    // So sieht der neue Client sofort die Bilder und Farben der anderen.
                    // Nur eingeloggte Clients haben UserId != -1
                    foreach (var client in _server.GetConnectedClients())
                    {
                        if (client.UserId != -1 && client != this)
                            Send($"USER_PROFILE|{client.UserId}|{client.Username}|{client.Color}|{client.ImageBase64}");
                    }
                    break;

                case "CREATE_ROOM":
                    // CREATE_ROOM|name
                    bool created = _db.CreateRoom(parts[1]);
                    if (created)
                    {
                        // Allen Clients (inkl. Ersteller) mitteilen dass ein neuer Raum da ist
                        _server.BroadcastToAll($"ROOM_CREATED|{parts[1]}");
                        _log($"Raum erstellt: {parts[1]}");
                    }
                    else
                    {
                        Send("ROOM_EXISTS");
                        _log($"Raum existiert bereits: {parts[1]}");
                    }
                    break;

                case "JOIN_ROOM":
                    // JOIN_ROOM|roomId
                    int roomId = int.Parse(parts[1]);
                    if (!_joinedRooms.Contains(roomId))
                        _joinedRooms.Add(roomId);
                    // JOINED|roomId → Client weiß dass er dem Raum beigetreten ist
                    Send($"JOINED|{roomId}");
                    _log($"{Username} ist Raum {roomId} beigetreten");
                    break;

                case "MSG":
                    // MSG|roomId|text
                    // Nachricht in DB speichern
                    _db.SaveMessage(int.Parse(parts[1]), UserId, parts[2]);
                    // An alle anderen weiterleiten (nicht an Absender – der zeigt sie selbst an)
                    // Format für Empfänger: MSG|roomId|senderId|username|text
                    _server.Broadcast($"MSG|{parts[1]}|{UserId}|{Username}|{parts[2]}", this);
                    _log($"[Raum {parts[1]}] {Username}: {parts[2]}");
                    break;

                case "PRIVATE":
                    // PRIVATE|receiverId|text
                    // WICHTIG: Nur an den Empfänger schicken, nicht an alle!
                    int receiverId = int.Parse(parts[1]);
                    _db.SavePrivateMessage(UserId, receiverId, parts[2]);
                    // Format für Empfänger: PRIVATE|senderId|senderName|text
                    _server.SendToUser(receiverId, $"PRIVATE|{UserId}|{Username}|{parts[2]}");
                    _log($"[Privat] {Username} → User {receiverId}: {parts[2]}");
                    break;

                case "UPDATE_PROFILE":
                    // UPDATE_PROFILE|color|imageBase64
                    // Profil auf diesem Handler speichern (für neue Clients die später joinen)
                    Color = parts[1];
                    ImageBase64 = parts.Length > 2 ? parts[2] : "";

                    // In Datenbank speichern damit das Profil nach Neustart noch da ist
                    _db.UpdateUserProfile(UserId, Color, ImageBase64);

                    // Allen anderen Clients das aktualisierte Profil schicken.
                    // Format: USER_PROFILE|userId|username|color|imageBase64
                    _server.Broadcast($"USER_PROFILE|{UserId}|{Username}|{Color}|{ImageBase64}", this);
                    _log($"Profil aktualisiert: {Username}");
                    break;
            }
        }

        // Sendet eine Nachricht an diesen Client.
        // try/catch damit ein Fehler bei einem Client nicht den ganzen Server abstürzen lässt.
        public void Send(string message)
        {
            try { _writer?.WriteLine(message); }
            catch { /* Verbindung bereits getrennt – ignorieren */ }
        }
    }
}
