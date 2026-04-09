using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using WpfChat.Shared.Models;

namespace WpfChat.Server.Database
{
    /// <summary>
    /// Kümmert sich um alle Datenbankzugriffe (SQLite).
    /// Die Datenbankdatei ist: WpfChat.Server/Database/chat.db
    /// </summary>
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string dbPath)
        {
            // WAL-Modus erlaubt gleichzeitigen Zugriff (z.B. mit DB Browser)
            _connectionString = $"Data Source={dbPath};Cache=Shared";
            TabellenErstellen();
        }

        // ── Tabellen anlegen (falls noch nicht vorhanden) ─────
        private void TabellenErstellen()
        {
            using var conn = Verbinden();

            // WAL für gleichzeitigen Zugriff, busy_timeout damit keine sofortigen Fehler kommen
            Ausführen(conn, "PRAGMA journal_mode=WAL;");
            Ausführen(conn, "PRAGMA busy_timeout=5000;");

            Ausführen(conn, @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username           TEXT    UNIQUE NOT NULL,
                    PasswordHash       TEXT    NOT NULL,
                    Color              TEXT    NOT NULL DEFAULT '#000000',
                    ProfileImageBase64 TEXT
                );");

            Ausführen(conn, @"
                CREATE TABLE IF NOT EXISTS Rooms (
                    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT UNIQUE NOT NULL
                );");

            Ausführen(conn, @"
                CREATE TABLE IF NOT EXISTS Messages (
                    Id                INTEGER  PRIMARY KEY AUTOINCREMENT,
                    SenderUsername    TEXT     NOT NULL,
                    RoomName          TEXT,
                    RecipientUsername TEXT,
                    Content           TEXT     NOT NULL,
                    Timestamp         DATETIME DEFAULT CURRENT_TIMESTAMP
                );");

            // Standard-Raum
            Ausführen(conn, "INSERT OR IGNORE INTO Rooms (Name) VALUES ('General');");
        }

        // ── Benutzer ──────────────────────────────────────────

        /// <summary>Registriert einen neuen Benutzer. Gibt false zurück wenn der Name vergeben ist.</summary>
        public bool BenutzerRegistrieren(string username, string passwort, out string fehler)
        {
            fehler = "";
            using var conn = Verbinden();
            try
            {
                Ausführen(conn,
                    "INSERT INTO Users (Username, PasswordHash) VALUES (@u, @p)",
                    ("@u", username),
                    ("@p", PasswortHashen(passwort)));
                return true;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                fehler = "Benutzername bereits vergeben.";
                return false;
            }
        }

        /// <summary>Prüft Login-Daten. Gibt UserDto zurück bei Erfolg, sonst null.</summary>
        public UserDto? BenutzerAnmelden(string username, string passwort)
        {
            using var conn = Verbinden();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, PasswordHash, Color, ProfileImageBase64 FROM Users WHERE Username = @u";
            cmd.Parameters.AddWithValue("@u", username);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null; // User nicht gefunden

            // Passwort prüfen
            if (PasswortHashen(passwort) != reader.GetString(2)) return null;

            return new UserDto
            {
                Id       = reader.GetInt32(0),
                Username = reader.GetString(1),
                Color    = reader.GetString(3),
                ProfileImageBase64 = reader.IsDBNull(4) ? null : reader.GetString(4)
            };
        }

        /// <summary>Lädt einen Benutzer anhand des Usernamens.</summary>
        public UserDto? BenutzerLaden(string username)
        {
            using var conn = Verbinden();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Color, ProfileImageBase64 FROM Users WHERE Username = @u";
            cmd.Parameters.AddWithValue("@u", username);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new UserDto
            {
                Id       = reader.GetInt32(0),
                Username = reader.GetString(1),
                Color    = reader.GetString(2),
                ProfileImageBase64 = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }

        /// <summary>Speichert die Profiländerung (Farbe + Bild) in der Datenbank.</summary>
        public void ProfilAktualisieren(string username, string farbe, string? bildBase64)
        {
            using var conn = Verbinden();
            Ausführen(conn,
                "UPDATE Users SET Color = @c, ProfileImageBase64 = @b WHERE Username = @u",
                ("@c", farbe),
                ("@b", (object?)bildBase64 ?? DBNull.Value),
                ("@u", username));
        }

        // ── Räume ─────────────────────────────────────────────

        /// <summary>Gibt alle Räume zurück.</summary>
        public List<RoomDto> RäumeLaden()
        {
            var räume = new List<RoomDto>();
            using var conn = Verbinden();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Rooms ORDER BY Name";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                räume.Add(new RoomDto { Id = reader.GetInt32(0), Name = reader.GetString(1) });

            return räume;
        }

        /// <summary>Erstellt einen neuen Raum. Gibt false zurück wenn der Name schon existiert.</summary>
        public bool RaumErstellen(string name, out string fehler)
        {
            fehler = "";
            using var conn = Verbinden();
            try
            {
                Ausführen(conn, "INSERT INTO Rooms (Name) VALUES (@n)", ("@n", name));
                return true;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                fehler = "Ein Raum mit diesem Namen existiert bereits.";
                return false;
            }
        }

        /// <summary>Prüft ob ein Raum existiert.</summary>
        public bool RaumExistiert(string name)
        {
            using var conn = Verbinden();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Rooms WHERE Name = @n";
            cmd.Parameters.AddWithValue("@n", name);
            return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
        }

        // ── Nachrichten ───────────────────────────────────────

        /// <summary>Speichert eine Nachricht in der Datenbank.</summary>
        public void NachrichtSpeichern(ChatMessageDto msg)
        {
            using var conn = Verbinden();
            Ausführen(conn,
                "INSERT INTO Messages (SenderUsername, RoomName, RecipientUsername, Content, Timestamp) VALUES (@s, @r, @rec, @c, @t)",
                ("@s",   msg.SenderUsername),
                ("@r",   (object?)msg.RoomName          ?? DBNull.Value),
                ("@rec", (object?)msg.RecipientUsername  ?? DBNull.Value),
                ("@c",   msg.Content),
                ("@t",   msg.Timestamp.ToString("o")));
        }

        /// <summary>Lädt die letzten N Nachrichten eines Raumes (älteste zuerst).</summary>
        public List<ChatMessageDto> LetzteNachrichten(string raumName, int anzahl = 50)
        {
            var liste = new List<ChatMessageDto>();
            using var conn = Verbinden();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT m.SenderUsername, u.Color, m.Content, m.Timestamp
                FROM Messages m
                LEFT JOIN Users u ON u.Username = m.SenderUsername
                WHERE m.RoomName = @r
                ORDER BY m.Timestamp DESC
                LIMIT @a";
            cmd.Parameters.AddWithValue("@r", raumName);
            cmd.Parameters.AddWithValue("@a", anzahl);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // Vorne einfügen damit älteste Nachricht oben steht
                liste.Insert(0, new ChatMessageDto
                {
                    SenderUsername = reader.GetString(0),
                    SenderColor    = reader.IsDBNull(1) ? "#000000" : reader.GetString(1),
                    Content        = reader.GetString(2),
                    Timestamp      = DateTime.Parse(reader.GetString(3)),
                    RoomName       = raumName
                });
            }
            return liste;
        }

        // ── Hilfsmethoden ─────────────────────────────────────

        private SqliteConnection Verbinden()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // SQL ohne Rückgabewert ausführen
        private static void Ausführen(SqliteConnection conn, string sql,
            params (string name, object? wert)[] parameter)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, wert) in parameter)
                cmd.Parameters.AddWithValue(name, wert ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // Passwort mit SHA-256 + Salt hashen
        private static string PasswortHashen(string passwort)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes("WpfChatSalt_" + passwort);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}
