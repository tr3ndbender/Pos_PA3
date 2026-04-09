using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WpfChat.Server.Database;
using WpfChat.Shared.Models;
using WpfChat.Shared.Protocol;

namespace WpfChat.Server.Network
{
    /// <summary>
    /// Verwaltet die Verbindung zu einem einzelnen Client.
    /// Liest eingehende Pakete und reagiert darauf.
    /// </summary>
    public class ClientHandler
    {
        private readonly TcpClient _tcpClient;
        private readonly ChatServer _server;
        private readonly DatabaseManager _db;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly object _sendeLock = new();

        public string? Username { get; private set; }
        public bool IstAngemeldet => Username != null;
        public HashSet<string> BetreteneRäume { get; } = new();

        public ClientHandler(TcpClient client, ChatServer server, DatabaseManager db)
        {
            _tcpClient = client;
            _tcpClient.NoDelay = true;     // Pakete sofort senden, kein Buffering
            _server = server;
            _db = db;

            var stream = client.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        }

        public void Starten() => Task.Run(Empfangsschleife);

        // ── Empfangsschleife ──────────────────────────────────

        // Läuft solange der Client verbunden ist
        private async Task Empfangsschleife()
        {
            try
            {
                string? zeile;
                while ((zeile = await _reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(zeile)) continue;

                    var paket = ChatPacket.FromJson(zeile);
                    if (paket != null)
                        PaketVerarbeiten(paket);
                }
            }
            catch (IOException) { /* Client hat Verbindung getrennt */ }
            catch (Exception ex) { _server.Log($"Fehler bei {Username ?? "?"}: {ex.Message}"); }
            finally { Trennen(); }
        }

        // ── Paket verarbeiten ─────────────────────────────────

        private void PaketVerarbeiten(ChatPacket paket)
        {
            switch (paket.Type)
            {
                case MessageType.Register:
                    Registrieren(paket); break;

                case MessageType.Login:
                    Anmelden(paket); break;

                case MessageType.Logout:
                    Trennen(); break;

                // Alle folgenden Aktionen nur wenn angemeldet
                case MessageType.GetRooms when IstAngemeldet:
                    RäumeSenden(); break;

                case MessageType.CreateRoom when IstAngemeldet:
                    RaumErstellen(paket); break;

                case MessageType.JoinRoom when IstAngemeldet:
                    RaumBetreten(paket); break;

                case MessageType.LeaveRoom when IstAngemeldet:
                    RaumVerlassen(paket); break;

                case MessageType.ChatMessage when IstAngemeldet:
                    NachrichtSenden(paket); break;

                case MessageType.PrivateMessage when IstAngemeldet:
                    PrivateNachrichtSenden(paket); break;

                case MessageType.UpdateProfile when IstAngemeldet:
                    ProfilAktualisieren(paket); break;
            }
        }

        // ── Auth ──────────────────────────────────────────────

        private void Registrieren(ChatPacket paket)
        {
            var anfrage = paket.GetPayload<AuthRequest>();
            if (anfrage == null) return;

            bool ok = _db.BenutzerRegistrieren(anfrage.Username, anfrage.Password, out var fehler);

            Senden(ChatPacket.Create(MessageType.RegisterResponse, new AuthResponse
            {
                Success = ok,
                Message = ok ? "Registrierung erfolgreich." : fehler
            }));

            _server.Log($"Registrierung: {anfrage.Username} – {(ok ? "OK" : fehler)}");
        }

        private void Anmelden(ChatPacket paket)
        {
            var anfrage = paket.GetPayload<AuthRequest>();
            if (anfrage == null) return;

            var user = _db.BenutzerAnmelden(anfrage.Username, anfrage.Password);

            if (user == null)
            {
                Senden(ChatPacket.Create(MessageType.LoginResponse, new AuthResponse
                {
                    Success = false,
                    Message = "Benutzername oder Passwort falsch."
                }));
                return;
            }

            // Anmeldung erfolgreich
            Username = user.Username;
            _server.ClientHinzufügen(this);

            Senden(ChatPacket.Create(MessageType.LoginResponse, new AuthResponse
            {
                Success = true,
                Message = "Anmeldung erfolgreich.",
                User = user
            }));

            _server.Log($"Login: {Username} von {_tcpClient.Client.RemoteEndPoint}");

            // Alle anderen informieren dass jemand online ist
            _server.AnAlleAußer(ChatPacket.Create(MessageType.UserJoined, user), this);
        }

        // ── Räume ─────────────────────────────────────────────

        private void RäumeSenden()
        {
            var räume = _db.RäumeLaden();
            Senden(ChatPacket.Create(MessageType.RoomsResponse, räume));
        }

        private void RaumErstellen(ChatPacket paket)
        {
            var anfrage = paket.GetPayload<RoomRequest>();
            if (anfrage == null) return;

            bool ok = _db.RaumErstellen(anfrage.RoomName, out var fehler);
            if (ok)
            {
                _server.Log($"Neuer Raum: '{anfrage.RoomName}' von {Username}");
                // Alle Clients informieren dass ein neuer Raum existiert
                _server.AnAlle(ChatPacket.Create(MessageType.RoomCreated,
                    new RoomDto { Name = anfrage.RoomName }));
            }
            else
            {
                Senden(ChatPacket.Create(MessageType.Error, fehler));
            }
        }

        private void RaumBetreten(ChatPacket paket)
        {
            var anfrage = paket.GetPayload<RoomRequest>();
            if (anfrage == null) return;

            if (!_db.RaumExistiert(anfrage.RoomName))
            {
                Senden(ChatPacket.Create(MessageType.Error, "Raum nicht gefunden."));
                return;
            }

            BetreteneRäume.Add(anfrage.RoomName);

            // Letzte Nachrichten laden und Profilbilder hinzufügen
            var nachrichten = _db.LetzteNachrichten(anfrage.RoomName);
            foreach (var msg in nachrichten)
            {
                var absender = _db.BenutzerLaden(msg.SenderUsername);
                msg.SenderProfileImageBase64 = absender?.ProfileImageBase64;
            }

            Senden(ChatPacket.Create(MessageType.RoomJoined, new JoinRoomResponse
            {
                Success         = true,
                Room            = new RoomDto { Name = anfrage.RoomName, Members = _server.MitgliederImRaum(anfrage.RoomName) },
                RecentMessages  = nachrichten
            }));

            _server.Log($"{Username} hat Raum '{anfrage.RoomName}' betreten");
        }

        private void RaumVerlassen(ChatPacket paket)
        {
            var anfrage = paket.GetPayload<RoomRequest>();
            if (anfrage == null) return;

            BetreteneRäume.Remove(anfrage.RoomName);
            _server.Log($"{Username} hat Raum '{anfrage.RoomName}' verlassen");
        }

        // ── Nachrichten ───────────────────────────────────────

        private void NachrichtSenden(ChatPacket paket)
        {
            var msg = paket.GetPayload<ChatMessageDto>();
            if (msg == null || string.IsNullOrWhiteSpace(msg.RoomName)) return;
            if (!BetreteneRäume.Contains(msg.RoomName)) return;

            // Absender-Infos vom Server setzen (nicht vom Client vertrauen)
            var user = _db.BenutzerLaden(Username!);
            msg.SenderUsername         = Username!;
            msg.SenderColor            = user?.Color ?? "#000000";
            msg.SenderProfileImageBase64 = user?.ProfileImageBase64;
            msg.Timestamp              = DateTime.Now;

            _db.NachrichtSpeichern(msg);
            _server.Log($"[{msg.RoomName}] {Username}: {msg.Content}");
            _server.AnRaum(msg.RoomName, ChatPacket.Create(MessageType.ChatMessage, msg));
        }

        private void PrivateNachrichtSenden(ChatPacket paket)
        {
            var msg = paket.GetPayload<ChatMessageDto>();
            if (msg == null || string.IsNullOrWhiteSpace(msg.RecipientUsername)) return;

            var user = _db.BenutzerLaden(Username!);
            msg.SenderUsername           = Username!;
            msg.SenderColor              = user?.Color ?? "#000000";
            msg.SenderProfileImageBase64 = user?.ProfileImageBase64;
            msg.Timestamp                = DateTime.Now;

            _db.NachrichtSpeichern(msg);
            _server.Log($"[PM] {Username} → {msg.RecipientUsername}: {msg.Content}");

            // An Empfänger senden
            _server.ClientFinden(msg.RecipientUsername)?.Senden(
                ChatPacket.Create(MessageType.PrivateMessage, msg));

            // Kopie an Absender (damit er seine eigene Nachricht sieht)
            Senden(ChatPacket.Create(MessageType.PrivateMessage, msg));
        }

        // ── Profil ────────────────────────────────────────────

        private void ProfilAktualisieren(ChatPacket paket)
        {
            var anfrage = paket.GetPayload<UpdateProfileRequest>();
            if (anfrage == null) return;

            _db.ProfilAktualisieren(Username!, anfrage.Color, anfrage.ProfileImageBase64);
            _server.Log($"Profil aktualisiert: {Username}");

            // Alle Clients über das neue Profil informieren
            var user = _db.BenutzerLaden(Username!);
            if (user != null)
                _server.AnAlle(ChatPacket.Create(MessageType.UpdateProfile, user));
        }

        // ── Senden / Trennen ──────────────────────────────────

        public void Senden(ChatPacket paket)
        {
            try
            {
                lock (_sendeLock)
                    _writer.WriteLine(paket.ToJson());
            }
            catch { /* Client nicht mehr erreichbar */ }
        }

        private void Trennen()
        {
            if (Username != null)
            {
                _server.ClientEntfernen(this);
                _server.Log($"Disconnect: {Username}");
                _server.AnAlleAußer(
                    ChatPacket.Create(MessageType.UserLeft, new UserDto { Username = Username }),
                    this);
            }
            try { _tcpClient.Close(); } catch { }
        }
    }
}
