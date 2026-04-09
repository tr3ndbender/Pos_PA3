# WPF Chat – vollständige Projekt-Anleitung

Diese Anleitung erklärt **jeden Teil des Projekts** mit dem echten Code – was er macht, warum er so gebaut wurde, und was man daraus für andere Projekte mitnehmen kann.

---

## Inhaltsverzeichnis

1. [Projektstruktur – drei Projekte in einer Solution](#1-projektstruktur)
2. [Das Protokoll – ChatPacket und MessageType](#2-das-protokoll)
3. [Shared Models – DTOs](#3-shared-models--dtos)
4. [Datenbank – SQLite ohne ORM](#4-datenbank--sqlite-ohne-orm)
5. [Server – TcpListener und Client-Verwaltung](#5-server--tcplistener-und-client-verwaltung)
6. [ClientHandler – pro Client ein eigener Handler](#6-clienthandler--pro-client-ein-eigener-handler)
7. [Server-GUI](#7-server-gui)
8. [Client-Netzwerk – ServerConnection](#8-client-netzwerk--serverconnection)
9. [Login & Register – TaskCompletionSource](#9-login--register--taskcompletionsource)
10. [Client-GUI – Hauptfenster mit Tabs](#10-client-gui--hauptfenster-mit-tabs)
11. [ViewModels & INotifyPropertyChanged](#11-viewmodels--inotifypropertychanged)
12. [Bilder als Base64 übertragen](#12-bilder-als-base64-übertragen)
13. [Einstellungen speichern – XML-Serialisierung](#13-einstellungen-speichern--xml-serialisierung)
14. [Value Converter in WPF](#14-value-converter-in-wpf)
15. [Thread-Safety in WPF – Dispatcher](#15-thread-safety-in-wpf--dispatcher)
16. [Gesamtablauf – von Login bis Nachricht](#16-gesamtablauf--von-login-bis-nachricht)

---

## 1. Projektstruktur

Die Solution besteht aus **drei Projekten**:

```
WpfChat/
├── WpfChat.Shared/      ← Class Library – von Client UND Server genutzt
│   ├── Protocol/
│   │   ├── MessageType.cs   (alle Paket-Typen als Enum)
│   │   └── ChatPacket.cs    (JSON-Paket mit Type + Payload)
│   └── Models/
│       ├── UserDto.cs
│       ├── RoomDto.cs
│       ├── ChatMessageDto.cs
│       └── AuthRequest.cs
│
├── WpfChat.Server/      ← WPF-App (der Server hat auch eine GUI!)
│   ├── Database/
│   │   └── DatabaseManager.cs   (alle DB-Operationen)
│   ├── Network/
│   │   ├── ChatServer.cs        (TcpListener, Client-Liste)
│   │   └── ClientHandler.cs     (Verbindung zu einem Client)
│   └── MainWindow.xaml/.cs      (Server-GUI: Port, Start/Stop, Log)
│
└── WpfChat.Client/      ← WPF-App (der eigentliche Chat-Client)
    ├── Network/
    │   └── ServerConnection.cs  (Verbindung zum Server)
    ├── Views/
    │   ├── LoginWindow.xaml/.cs
    │   └── MainWindow.xaml/.cs
    ├── Models/
    │   ├── ChatTabItem.cs
    │   ├── ClientSettings.cs
    │   └── MessageViewModel.cs
    ├── Helpers/
    │   └── ImageHelper.cs
    └── Converters/
        └── Converters.cs
```

**Warum drei Projekte?**  
Das `Shared`-Projekt enthält alle Klassen, die Client und Server kennen müssen (Protokoll, DTOs). So gibt es keinen doppelten Code – beide referenzieren dieselbe Library.

```xml
<!-- In WpfChat.Client.csproj und WpfChat.Server.csproj: -->
<ProjectReference Include="..\WpfChat.Shared\WpfChat.Shared.csproj" />
```

> **Wiederverwendbar:** Dieses Muster – Shared Library für gemeinsamen Code – ist Standard in Client-Server-Projekten jeder Größe.

---

## 2. Das Protokoll

**Datei:** `WpfChat.Shared/Protocol/`

### Das Problem: Wie schickt man verschiedene Nachrichten-Typen über denselben TCP-Stream?

Lösung: **Jede Nachricht ist ein `ChatPacket`** mit zwei Feldern:
- `Type` – was ist das für eine Nachricht?
- `Payload` – die eigentlichen Daten (als JSON-String)

### MessageType – alle Paket-Typen als Enum

```csharp
public enum MessageType
{
    // Anmeldung
    Register, RegisterResponse,
    Login, LoginResponse,
    Logout,

    // Räume
    GetRooms, RoomsResponse,
    CreateRoom, RoomCreated,
    JoinRoom, RoomJoined,
    LeaveRoom, RoomLeft,

    // Nachrichten
    ChatMessage,
    PrivateMessage,

    // Profil
    UpdateProfile,

    // Server-Benachrichtigungen
    UserJoined, UserLeft,
    Error
}
```

Ein Enum für alle Typen ist übersichtlicher als Magic Strings wie `"LOGIN"` oder `"CHAT"`.

### ChatPacket – das universelle Paket

```csharp
public class ChatPacket
{
    public MessageType Type { get; set; }
    public string? Payload { get; set; }  // Inhalt als JSON-String

    // Paket erstellen: T wird automatisch zu JSON serialisiert
    public static ChatPacket Create<T>(MessageType type, T payload) => new()
    {
        Type = type,
        Payload = JsonConvert.SerializeObject(payload)
    };

    // Für den Stream: ganzes Paket → JSON-String (eine Zeile)
    public string ToJson() => JsonConvert.SerializeObject(this);

    // Aus dem Stream: JSON-String → ChatPacket
    public static ChatPacket? FromJson(string json) =>
        JsonConvert.DeserializeObject<ChatPacket>(json);

    // Payload auslesen und in gewünschten Typ umwandeln
    public T? GetPayload<T>() =>
        Payload == null ? default : JsonConvert.DeserializeObject<T>(Payload);
}
```

**Wie sieht ein Paket im Stream aus?**

```json
{"Type":2,"Payload":"{\"Username\":\"Max\",\"Password\":\"geheim\"}"}
```

Das ist eine einzige Zeile (`\n` am Ende). So kann man mit `ReadLine()` genau ein Paket auf einmal lesen.

**Verwendung:**

```csharp
// Paket senden:
var paket = ChatPacket.Create(MessageType.Login, new AuthRequest
{
    Username = "Max",
    Password = "geheim"
});
writer.WriteLine(paket.ToJson());

// Paket empfangen und Payload auslesen:
var paket = ChatPacket.FromJson(zeile);
var anfrage = paket.GetPayload<AuthRequest>();
```

> **Wiederverwendbar:** Dieses "typisiertes Paket mit JSON-Payload"-Muster kann 1:1 in jedem anderen TCP-Projekt verwendet werden.

---

## 3. Shared Models – DTOs

**DTO = Data Transfer Object** – eine Klasse, die nur Daten enthält, die über das Netzwerk übertragen werden.

### UserDto

```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Color { get; set; } = "#000000";
    public string? ProfileImageBase64 { get; set; }  // Bild als Base64-Text
}
```

### ChatMessageDto

```csharp
public class ChatMessageDto
{
    public string SenderUsername { get; set; } = "";
    public string SenderColor { get; set; } = "#000000";
    public string? SenderProfileImageBase64 { get; set; }

    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }

    public string? RoomName { get; set; }            // gesetzt bei Raum-Nachrichten
    public string? RecipientUsername { get; set; }   // gesetzt bei privaten Nachrichten

    public bool IsPrivate => RecipientUsername != null;  // Hilfseigenschaft
}
```

`IsPrivate` ist eine berechnete Eigenschaft – sie wird nie übertragen, sondern immer live aus `RecipientUsername` berechnet.

### AuthRequest und AuthResponse

```csharp
public class AuthRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public UserDto? User { get; set; }  // nur bei erfolgreichem Login
}
```

---

## 4. Datenbank – SQLite ohne ORM

**Datei:** `WpfChat.Server/Database/DatabaseManager.cs`

Hier wird **kein Entity Framework** verwendet, sondern direkt `Microsoft.Data.Sqlite`. Das gibt mehr Kontrolle und ist für kleinere Projekte oft einfacher.

### Verbindungsaufbau mit WAL-Modus

```csharp
public DatabaseManager(string dbPath)
{
    _connectionString = $"Data Source={dbPath};Cache=Shared";
    TabellenErstellen();
}
```

Im Konstruktor werden sofort die Tabellen angelegt:

```csharp
private void TabellenErstellen()
{
    using var conn = Verbinden();

    // WAL = Write-Ahead Logging: erlaubt gleichzeitigen Lese- und Schreibzugriff
    Ausführen(conn, "PRAGMA journal_mode=WAL;");
    // busy_timeout: wenn DB gesperrt, 5 Sekunden warten statt sofort Fehler
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

    // Standard-Raum automatisch erstellen
    Ausführen(conn, "INSERT OR IGNORE INTO Rooms (Name) VALUES ('General');");
}
```

### Passwörter hashen mit SHA-256

Passwörter werden **niemals im Klartext** gespeichert:

```csharp
private static string PasswortHashen(string passwort)
{
    using var sha = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes("WpfChatSalt_" + passwort);
    return Convert.ToBase64String(sha.ComputeHash(bytes));
}
```

- Der **Salt** (`WpfChatSalt_`) wird vor das Passwort gehängt – so sind gleiche Passwörter nicht identisch gehasht.
- `SHA256.Create()` → `ComputeHash()` → `Convert.ToBase64String()` ist das Standard-Muster für SHA-256 in C#.

### Benutzer registrieren

```csharp
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
        // Fehlercode 19 = UNIQUE constraint violated (Name schon vergeben)
        fehler = "Benutzername bereits vergeben.";
        return false;
    }
}
```

`when (ex.SqliteErrorCode == 19)` ist ein **Exception-Filter** – nur dieser spezifische SQLite-Fehler wird abgefangen, alle anderen laufen weiter hoch.

### Login prüfen

```csharp
public UserDto? BenutzerAnmelden(string username, string passwort)
{
    using var conn = Verbinden();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Id, Username, PasswordHash, Color, ProfileImageBase64 " +
                      "FROM Users WHERE Username = @u";
    cmd.Parameters.AddWithValue("@u", username);   // IMMER Parameter, nie String-Concatenation!

    using var reader = cmd.ExecuteReader();
    if (!reader.Read()) return null;  // User existiert nicht

    if (PasswortHashen(passwort) != reader.GetString(2)) return null;  // Passwort falsch

    return new UserDto
    {
        Id       = reader.GetInt32(0),
        Username = reader.GetString(1),
        Color    = reader.GetString(3),
        ProfileImageBase64 = reader.IsDBNull(4) ? null : reader.GetString(4)
    };
}
```

`reader.IsDBNull(4)` – vor dem Auslesen eines nullable-Feldes immer prüfen, sonst `InvalidCastException`.

### Letzte Nachrichten laden (mit JOIN)

```csharp
public List<ChatMessageDto> LetzteNachrichten(string raumName, int anzahl = 50)
{
    // SQL mit JOIN auf Users für die Benutzerfarbe:
    cmd.CommandText = @"
        SELECT m.SenderUsername, u.Color, m.Content, m.Timestamp
        FROM Messages m
        LEFT JOIN Users u ON u.Username = m.SenderUsername
        WHERE m.RoomName = @r
        ORDER BY m.Timestamp DESC
        LIMIT @a";

    while (reader.Read())
    {
        // liste.Insert(0, ...) statt Add → älteste Nachricht steht am Ende der DESC-Liste
        // Nach dem Umdrehen durch Insert(0) steht sie vorne = oben im Chat
        liste.Insert(0, new ChatMessageDto { ... });
    }
    return liste;
}
```

### Hilfsmethode Ausführen

```csharp
private static void Ausführen(SqliteConnection conn, string sql,
    params (string name, object? wert)[] parameter)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    foreach (var (name, wert) in parameter)
        cmd.Parameters.AddWithValue(name, wert ?? DBNull.Value);
    cmd.ExecuteNonQuery();
}
```

`params (string, object?)[]` erlaubt beliebig viele benannte Parameter inline:

```csharp
Ausführen(conn, "UPDATE Users SET Color = @c WHERE Username = @u",
    ("@c", "#FF0000"),
    ("@u", "Max"));
```

> **Wiederverwendbar:** Diese Hilfsmethode + das WAL-PRAGMA-Muster kann 1:1 in jedem SQLite-Projekt übernommen werden.

---

## 5. Server – TcpListener und Client-Verwaltung

**Datei:** `WpfChat.Server/Network/ChatServer.cs`

### Server starten

```csharp
public void Starten(int port)
{
    _listener = new TcpListener(IPAddress.Any, port);
    _listener.Start();
    _läuft = true;
    Task.Run(VerbindungenAnnehmen);  // Läuft im Hintergrund
}
```

`IPAddress.Any` = Server akzeptiert Verbindungen von allen Netzwerkkarten (localhost UND LAN).

### Auf Verbindungen warten (async)

```csharp
private async Task VerbindungenAnnehmen()
{
    while (_läuft)
    {
        try
        {
            var tcpClient = await _listener!.AcceptTcpClientAsync();
            Log($"Verbindung von {tcpClient.Client.RemoteEndPoint}");

            // Neuen Handler starten – blockiert den Loop NICHT
            var handler = new ClientHandler(tcpClient, this, _db);
            handler.Starten();
        }
        catch
        {
            if (!_läuft) break;  // Normales Stoppen → kein Fehler loggen
        }
    }
}
```

`await AcceptTcpClientAsync()` – der Thread schläft, bis ein Client kommt, ohne die CPU zu blockieren.

### Thread-sichere Client-Liste

```csharp
private readonly List<ClientHandler> _clients = new();
private readonly object _lock = new();

public void ClientHinzufügen(ClientHandler client)
{
    lock (_lock) _clients.Add(client);
}

public void ClientEntfernen(ClientHandler client)
{
    lock (_lock) _clients.Remove(client);
}
```

`lock (_lock)` ist notwendig, weil mehrere Clients gleichzeitig auf die Liste zugreifen können. Ohne Lock → `InvalidOperationException` beim Modifizieren während Iteration.

### Nachrichten verteilen

```csharp
// An alle angemeldeten Clients:
public void AnAlle(ChatPacket paket)
{
    lock (_lock)
        foreach (var c in _clients.Where(c => c.IstAngemeldet))
            c.Senden(paket);
}

// An alle außer einem (z.B. Absender):
public void AnAlleAußer(ChatPacket paket, ClientHandler ausnahme)
{
    lock (_lock)
        foreach (var c in _clients.Where(c => c.IstAngemeldet && c != ausnahme))
            c.Senden(paket);
}

// Nur an Clients in einem bestimmten Raum:
public void AnRaum(string raumName, ChatPacket paket, ClientHandler? ausnahme = null)
{
    lock (_lock)
        foreach (var c in _clients.Where(c =>
            c.IstAngemeldet && c.BetreteneRäume.Contains(raumName) && c != ausnahme))
            c.Senden(paket);
}
```

### Logging via Event

```csharp
public event Action<string>? OnLog;

public void Log(string nachricht)
{
    var zeile = $"[{DateTime.Now:HH:mm:ss}] {nachricht}";
    OnLog?.Invoke(zeile);   // null-safe: kein Fehler wenn niemand subscribt
}
```

Die GUI subscribt auf `OnLog` und zeigt die Zeilen im ListBox an. Der Server selbst weiß nichts von der GUI.

---

## 6. ClientHandler – pro Client ein eigener Handler

**Datei:** `WpfChat.Server/Network/ClientHandler.cs`

Jeder verbundene Client bekommt eine eigene `ClientHandler`-Instanz.

### Felder und Status

```csharp
public string? Username { get; private set; }
public bool IstAngemeldet => Username != null;          // True sobald Login erfolgreich
public HashSet<string> BetreteneRäume { get; } = new(); // In welchen Räumen ist der Client?
```

`HashSet<string>` statt `List<string>` für Räume, weil `.Contains()` bei HashSet O(1) ist.

### Empfangsschleife

```csharp
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
    catch (IOException) { /* Client hat Verbindung getrennt – normal */ }
    catch (Exception ex) { _server.Log($"Fehler bei {Username ?? "?"}: {ex.Message}"); }
    finally { Trennen(); }  // Immer aufräumen, egal warum die Schleife endet
}
```

### Paket verarbeiten – switch mit Guards

```csharp
private void PaketVerarbeiten(ChatPacket paket)
{
    switch (paket.Type)
    {
        case MessageType.Register:
            Registrieren(paket); break;
        case MessageType.Login:
            Anmelden(paket); break;

        // "when IstAngemeldet" = Guard: nur wenn schon eingeloggt
        case MessageType.GetRooms when IstAngemeldet:
            RäumeSenden(); break;
        case MessageType.ChatMessage when IstAngemeldet:
            NachrichtSenden(paket); break;
        // ...
    }
}
```

Das `when IstAngemeldet` ist ein **switch-Guard** – ein nicht eingeloggter Client kann keine Nachrichten schicken.

### Absender-Infos vom Server setzen

```csharp
private void NachrichtSenden(ChatPacket paket)
{
    var msg = paket.GetPayload<ChatMessageDto>();

    // Absender-Infos IMMER vom Server laden, nicht dem Client vertrauen!
    var user = _db.BenutzerLaden(Username!);
    msg.SenderUsername           = Username!;
    msg.SenderColor              = user?.Color ?? "#000000";
    msg.SenderProfileImageBase64 = user?.ProfileImageBase64;
    msg.Timestamp                = DateTime.Now;

    _db.NachrichtSpeichern(msg);
    _server.AnRaum(msg.RoomName, ChatPacket.Create(MessageType.ChatMessage, msg));
}
```

Der Server überschreibt `SenderUsername` und `Timestamp` – der Client kann diese Felder nicht fälschen.

### Thread-sicheres Senden

```csharp
private readonly object _sendeLock = new();

public void Senden(ChatPacket paket)
{
    try
    {
        lock (_sendeLock)        // Verhindert gleichzeitiges Schreiben aus mehreren Threads
            _writer.WriteLine(paket.ToJson());
    }
    catch { /* Client nicht mehr erreichbar */ }
}
```

Ohne `lock` könnten zwei Threads gleichzeitig in `_writer.WriteLine` schreiben → kaputte JSON-Zeilen.

---

## 7. Server-GUI

**Datei:** `WpfChat.Server/MainWindow.xaml`

```xml
<StackPanel DockPanel.Dock="Top" Orientation="Horizontal">
    <Label Content="Port:"/>
    <TextBox x:Name="TxtPort" Text="5000" Width="70"/>
    <Button x:Name="BtnStart" Content="▶ Starten" Click="BtnStart_Click"/>
    <Button x:Name="BtnStop" Content="⏹ Stoppen" IsEnabled="False"/>
    <Label x:Name="LblStatus" Content="Gestoppt" Foreground="Red"/>
</StackPanel>

<ListBox x:Name="LstLog" FontFamily="Consolas" FontSize="12"/>
```

Im Code-Behind:

```csharp
private void BtnStart_Click(object sender, RoutedEventArgs e)
{
    // DB-Pfad relativ zum Projektordner berechnen (nicht zum bin-Ordner)
    var exeDir = AppDomain.CurrentDomain.BaseDirectory;
    var projectDir = Path.GetFullPath(Path.Combine(exeDir, "..", "..", ".."));
    var dbPath = Path.Combine(projectDir, "Database", "chat.db");

    _db = new DatabaseManager(dbPath);
    _server = new ChatServer(_db);

    // Event für Log-Einträge: Server → GUI
    _server.OnLog += AppendLog;
    _server.Starten(port);
}

// Wird aus dem Server-Thread aufgerufen → muss auf UI-Thread dispatcht werden
private void AppendLog(string message)
{
    Dispatcher.Invoke(() =>
    {
        LstLog.Items.Add(message);
        LstLog.ScrollIntoView(LstLog.Items[^1]);  // [^1] = letztes Element (C# 8+)
    });
}
```

`[^1]` ist der **Index-from-end-Operator** (C# 8) – entspricht `Items[Items.Count - 1]`.

---

## 8. Client-Netzwerk – ServerConnection

**Datei:** `WpfChat.Client/Network/ServerConnection.cs`

```csharp
public class ServerConnection
{
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly object _writeLock = new();
    private bool _running;
    private bool _intentionalDisconnect;

    // Events – andere Klassen können sich registrieren
    public event Action<ChatPacket>? PacketReceived;
    public event Action? Disconnected;
}
```

### Verbinden (async)

```csharp
public async Task ConnectAsync(string host, int port)
{
    _intentionalDisconnect = false;
    _client = new TcpClient();
    _client.NoDelay = true;       // Kein Nagle-Algorithmus – Pakete sofort senden
    _client.ReceiveTimeout = 0;   // Kein Timeout beim Lesen
    await _client.ConnectAsync(host, port);

    var stream = _client.GetStream();
    _reader = new StreamReader(stream, Encoding.UTF8);
    _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    _running = true;

    _ = Task.Run(ReceiveLoop);    // Empfangsschleife im Hintergrund starten
}
```

`_client.NoDelay = true` deaktiviert den **Nagle-Algorithmus** – ohne das werden kleine Pakete gesammelt und verzögert gesendet, was in einem Chat spürbar wäre.

### Empfangsschleife mit Event

```csharp
private async Task ReceiveLoop()
{
    try
    {
        string? line;
        while (_running && (line = await _reader!.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var packet = ChatPacket.FromJson(line);
            if (packet != null)
                PacketReceived?.Invoke(packet);  // Event feuern
        }
    }
    catch (IOException) { }
    finally
    {
        _running = false;
        // Nur feuern wenn NICHT absichtlich getrennt
        if (!_intentionalDisconnect)
            Disconnected?.Invoke();
    }
}
```

`_intentionalDisconnect` verhindert, dass `Disconnected` gefeuert wird wenn `Disconnect()` selbst aufgerufen wurde – das würde sonst zu doppeltem Login-Fenster führen.

---

## 9. Login & Register – TaskCompletionSource

**Datei:** `WpfChat.Client/Views/LoginWindow.xaml.cs`

Das ist eines der interessantesten Muster im Projekt: Die Antwort des Servers kommt asynchron (via Event), aber der Login-Code muss **auf diese Antwort warten**.

Lösung: `TaskCompletionSource<T>` – man kann eine `Task` manuell abschließen.

```csharp
private TaskCompletionSource<AuthResponse>? _authTcs;

private async Task Authenticate(bool isRegister)
{
    var conn = new ServerConnection();
    await conn.ConnectAsync(ip, port);

    // TCS erstellen – gibt eine Task, die wir manuell abschließen können
    _authTcs = new TaskCompletionSource<AuthResponse>();

    conn.PacketReceived += OnPacketReceived;
    conn.Disconnected += OnDisconnected;
    _connection = conn;

    // Paket senden
    conn.Send(ChatPacket.Create(isRegister ? MessageType.Register : MessageType.Login,
        new AuthRequest { Username = username, Password = password }));

    // Warten: entweder TCS wird gesetzt (Antwort kam) oder 8 Sekunden Timeout
    var completed = await Task.WhenAny(_authTcs.Task, Task.Delay(8000));
    AuthResponse? response = completed == _authTcs.Task ? _authTcs.Task.Result : null;

    if (response == null) { ShowError("Timeout – Server antwortet nicht."); return; }
    if (!response.Success) { ShowError(response.Message); return; }

    // Hauptfenster ZUERST erstellen, dann Loginfenster schließen
    var mainWin = new MainWindow(_connection, response.User!);
    mainWin.Show();
    Close();
}

// Wird im Netzwerk-Thread aufgerufen – setzt die TCS ab
private void OnPacketReceived(ChatPacket packet)
{
    if (packet.Type == MessageType.LoginResponse || packet.Type == MessageType.RegisterResponse)
    {
        var resp = packet.GetPayload<AuthResponse>();
        _authTcs?.TrySetResult(resp ?? new AuthResponse { Success = false });
    }
}

// Falls Verbindung abbricht bevor Antwort kommt
private void OnDisconnected()
{
    _authTcs?.TrySetResult(new AuthResponse
        { Success = false, Message = "Verbindung unerwartet getrennt." });
}
```

**Ablauf:**
1. `_authTcs = new TaskCompletionSource<AuthResponse>()` – erzeugt eine "wartende" Task
2. `await Task.WhenAny(...)` – wartet bis TCS gesetzt wird ODER 8 Sek. vorbei sind
3. Im Event-Handler: `_authTcs.TrySetResult(...)` – "weckt" die wartende Task auf
4. `TrySetResult` statt `SetResult` – sicher falls es zweimal aufgerufen wird

> **Wiederverwendbar:** `TaskCompletionSource<T>` ist das Standard-Muster um Event-basierte APIs in async/await Code zu wrappen.

---

## 10. Client-GUI – Hauptfenster mit Tabs

**Datei:** `WpfChat.Client/Views/MainWindow.xaml`

### Layout mit DockPanel

```xml
<DockPanel>
    <Menu DockPanel.Dock="Top">...</Menu>
    <StatusBar DockPanel.Dock="Bottom">...</StatusBar>
    <!-- Rest füllt die Mitte -->
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="200"/>  <!-- Raum-Liste -->
            <ColumnDefinition Width="5"/>    <!-- Splitter -->
            <ColumnDefinition Width="*"/>    <!-- Chat-Bereich -->
            <ColumnDefinition Width="5"/>    <!-- Splitter -->
            <ColumnDefinition Width="160"/>  <!-- Mein Profil -->
        </Grid.ColumnDefinitions>
        ...
    </Grid>
</DockPanel>
```

`GridSplitter` erlaubt dem User, die Spaltenbreiten per Drag & Drop zu verändern:

```xml
<GridSplitter Grid.Column="1" Width="5" HorizontalAlignment="Stretch"/>
```

### Dynamische Tabs mit ItemsControl

Statt fixer Tabs werden die Tabs dynamisch aus einer `ObservableCollection` generiert:

```csharp
private readonly ObservableCollection<ChatTabItem> _tabs = new();

// Im Konstruktor:
TabChats.ItemsSource = _tabs;
```

```xml
<TabControl x:Name="TabChats" SelectionChanged="TabChats_SelectionChanged">

    <!-- Wie der Tab-Header aussieht -->
    <TabControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <!-- HeaderText zeigt "● Raumname" wenn ungelesene Nachrichten da sind -->
                <TextBlock Text="{Binding HeaderText}"
                           Foreground="{Binding HasUnread,
                               Converter={StaticResource UnreadColorConverter}}"/>
                <!-- X-Button zum Schließen -->
                <Button Content="✕" Tag="{Binding}" Click="BtnCloseTab_Click"
                        Background="Transparent" BorderBrush="Transparent"/>
            </StackPanel>
        </DataTemplate>
    </TabControl.ItemTemplate>

    <!-- Inhalt des Tabs: Liste der Nachrichten -->
    <TabControl.ContentTemplate>
        <DataTemplate>
            <ListView ItemsSource="{Binding Messages}">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <!-- Nachrichtenblase -->
                        <Border Margin="4,2" Padding="8" CornerRadius="6">
                            <Border.Style>
                                <Style TargetType="Border">
                                    <Setter Property="Background" Value="#F0F0F0"/>
                                    <!-- Private Nachrichten bekommen anderen Hintergrund -->
                                    <Style.Triggers>
                                        <DataTrigger Binding="{Binding IsPrivate}" Value="True">
                                            <Setter Property="Background" Value="#FFF3E0"/>
                                        </DataTrigger>
                                    </Style.Triggers>
                                </Style>
                            </Border.Style>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="50"/>
                                    <ColumnDefinition Width="*"/>
                                </Grid.ColumnDefinitions>
                                <!-- Rundes Profilbild -->
                                <Border Grid.Column="0" Width="46" Height="46" CornerRadius="23">
                                    <Border.Clip>
                                        <EllipseGeometry Center="23,23" RadiusX="23" RadiusY="23"/>
                                    </Border.Clip>
                                    <Image Source="{Binding SenderAvatarImage}" Stretch="UniformToFill"/>
                                </Border>
                                <!-- Benutzername + Text -->
                                <StackPanel Grid.Column="1" Margin="8,0,0,0">
                                    <TextBlock Text="{Binding SenderUsername}"
                                               Foreground="{Binding SenderColorBrush}"
                                               FontWeight="Bold"/>
                                    <TextBlock Text="{Binding Content}" TextWrapping="Wrap"/>
                                </StackPanel>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </DataTemplate>
    </TabControl.ContentTemplate>
</TabControl>
```

**Rundes Profilbild** – Trick mit `Border.Clip`:

```xml
<Border Width="46" Height="46" CornerRadius="23">
    <Border.Clip>
        <EllipseGeometry Center="23,23" RadiusX="23" RadiusY="23"/>
    </Border.Clip>
    <Image Source="{Binding SenderAvatarImage}" Stretch="UniformToFill"/>
</Border>
```

`EllipseGeometry` als Clip schneidet das Bild rund aus – `CornerRadius` allein reicht nicht, weil das Bild sonst noch in den Ecken herausragt.

### Enter = Senden, Shift+Enter = Zeilenumbruch

```csharp
private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
    {
        SendCurrentMessage();
        e.Handled = true;  // Verhindert dass \n in TextBox eingefügt wird
    }
}
```

### Paket-Handling auf UI-Thread

```csharp
// Wird vom Netzwerk-Thread aufgerufen
private void OnPacketReceived(ChatPacket packet)
{
    Dispatcher.Invoke(() => HandlePacket(packet));  // Auf UI-Thread wechseln
}

private void HandlePacket(ChatPacket packet)
{
    switch (packet.Type)
    {
        case MessageType.RoomsResponse:
        {
            var rooms = packet.GetPayload<List<RoomDto>>();
            _rooms.Clear();
            foreach (var r in rooms) _rooms.Add(r);
            break;
        }
        case MessageType.ChatMessage:
        {
            var chatMsg = packet.GetPayload<ChatMessageDto>();
            var tab = _tabs.FirstOrDefault(t => t.Name == chatMsg.RoomName);
            if (tab != null)
            {
                tab.Messages.Add(new MessageViewModel(chatMsg));
                if (TabChats.SelectedItem != tab)
                    tab.HasUnread = true;  // Ungelesener Indikator setzen
            }
            break;
        }
        // ... alle anderen MessageTypes
    }
}
```

### Tab schließen

```csharp
private void BtnCloseTab_Click(object sender, RoutedEventArgs e)
{
    // Tag="{Binding}" im XAML setzt das ChatTabItem als Tag des Buttons
    if (sender is Button btn && btn.Tag is ChatTabItem tab)
    {
        if (!tab.IsPrivate)
            _connection.Send(ChatPacket.Create(MessageType.LeaveRoom,
                new RoomRequest { RoomName = tab.Name }));
        _tabs.Remove(tab);
    }
}
```

---

## 11. ViewModels & INotifyPropertyChanged

**Datei:** `WpfChat.Client/Models/MessageViewModel.cs`

`INotifyPropertyChanged` signalisiert WPF, wenn sich eine Eigenschaft ändert, damit die GUI automatisch aktualisiert wird.

### Das Standard-Muster

```csharp
public class MessageViewModel : INotifyPropertyChanged
{
    private BitmapImage? _avatarImage;

    public BitmapImage? SenderAvatarImage
    {
        get => _avatarImage;
        private set { _avatarImage = value; OnPropertyChanged(); }  // Notify!
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // [CallerMemberName] füllt den Namen automatisch – kein Magic String nötig
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

`[CallerMemberName]` füllt `name` automatisch mit dem Namen der aufrufenden Property – man muss `"SenderAvatarImage"` nicht als String schreiben.

### SenderColorBrush – live berechnet

```csharp
public SolidColorBrush SenderColorBrush
{
    get
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(_dto.SenderColor);
            return new SolidColorBrush(color);
        }
        catch { return Brushes.Black; }
    }
}
```

Kein Backing-Field – der Brush wird jedes Mal neu aus dem Hex-String berechnet.

### ChatTabItem – Unread-Indikator

```csharp
public class ChatTabItem : INotifyPropertyChanged
{
    private bool _hasUnread;

    public bool HasUnread
    {
        get => _hasUnread;
        set
        {
            _hasUnread = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HeaderText));  // HeaderText hängt von HasUnread ab
        }
    }

    // HeaderText ändert sich je nach HasUnread
    public string HeaderText => HasUnread ? $"● {Name}" : Name;

    public ObservableCollection<MessageViewModel> Messages { get; } = new();
}
```

`OnPropertyChanged(nameof(HeaderText))` – wenn `HasUnread` sich ändert, muss WPF auch `HeaderText` neu binden.

---

## 12. Bilder als Base64 übertragen

**Datei:** `WpfChat.Client/Helpers/ImageHelper.cs`

Bilder können nicht direkt über JSON übertragen werden – sie werden als Base64-String kodiert.

### Bild laden und verkleinern

```csharp
public static BitmapImage? LoadAndResize(string filePath, int pixelSize = 50)
{
    var bi = new BitmapImage();
    bi.BeginInit();
    bi.CacheOption = BitmapCacheOption.OnLoad;  // Datei nach dem Laden freigeben
    bi.DecodePixelWidth = pixelSize;             // Beim Laden schon verkleinern (spart RAM)
    bi.DecodePixelHeight = pixelSize;
    bi.UriSource = new Uri(filePath, UriKind.Absolute);
    bi.EndInit();
    bi.Freeze();  // Thread-sicher machen (für UI-Binding nötig)
    return bi;
}
```

`bi.Freeze()` macht das `BitmapImage` **unveränderlich** – nur dann kann es von mehreren Threads gleichzeitig gelesen werden (wichtig fürs Binding).

### Base64 → BitmapImage

```csharp
public static BitmapImage? FromBase64(string base64, int pixelSize = 50)
{
    var bytes = Convert.FromBase64String(base64);
    var bi = new BitmapImage();
    bi.BeginInit();
    bi.CacheOption = BitmapCacheOption.OnLoad;
    bi.DecodePixelWidth = pixelSize;
    bi.StreamSource = new MemoryStream(bytes);
    bi.EndInit();
    bi.Freeze();
    return bi;
}
```

### BitmapImage → Base64

```csharp
public static string? ToBase64(BitmapImage? image)
{
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(image));
    using var ms = new MemoryStream();
    encoder.Save(ms);
    return Convert.ToBase64String(ms.ToArray());
}
```

> **Wiederverwendbar:** Diese drei Methoden sind in jedem WPF-Projekt mit Bildübertragung direkt nutzbar.

---

## 13. Einstellungen speichern – XML-Serialisierung

**Datei:** `WpfChat.Client/Models/ClientSettings.cs`

```csharp
[XmlRoot("ClientSettings")]
public class ClientSettings
{
    public string LastServerIp { get; set; } = "127.0.0.1";
    public int LastServerPort { get; set; } = 5000;
    public string LastUsername { get; set; } = "";

    private static readonly string SettingsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

    public static ClientSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new ClientSettings();
            var xs = new XmlSerializer(typeof(ClientSettings));
            using var fs = File.OpenRead(SettingsPath);
            return (ClientSettings?)xs.Deserialize(fs) ?? new ClientSettings();
        }
        catch { return new ClientSettings(); }  // Bei Fehler: Standardwerte
    }

    public void Save()
    {
        var xs = new XmlSerializer(typeof(ClientSettings));
        using var fs = File.Create(SettingsPath);
        xs.Serialize(fs, this);
    }
}
```

Die `settings.xml` sieht so aus:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ClientSettings>
  <LastServerIp>192.168.1.10</LastServerIp>
  <LastServerPort>5000</LastServerPort>
  <LastUsername>Max</LastUsername>
</ClientSettings>
```

> **Wiederverwendbar:** `XmlSerializer` + `Load()`/`Save()` Muster funktioniert für beliebige Einstellungsklassen ohne zusätzliche NuGet-Pakete.

---

## 14. Value Converter in WPF

**Datei:** `WpfChat.Client/Converters/Converters.cs`

Ein `IValueConverter` wandelt einen Wert in der Binding-Pipeline um – ohne Code-Behind.

```csharp
public class UnreadColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? Brushes.OrangeRed : Brushes.Black;

    public object ConvertBack(...)  // Nur bei TwoWay-Binding nötig
        => throw new NotImplementedException();
}
```

**Im XAML registrieren und verwenden:**

```xml
<Window.Resources>
    <conv:UnreadColorConverter x:Key="UnreadColorConverter"/>
</Window.Resources>

<!-- Binding: HasUnread (bool) → Farbe über Converter -->
<TextBlock Text="{Binding HeaderText}"
           Foreground="{Binding HasUnread,
               Converter={StaticResource UnreadColorConverter}}"/>
```

`HasUnread = true` → `OrangeRed`, `HasUnread = false` → `Black`.

> **Wiederverwendbar:** Converter sind überall dort nützlich, wo ein bool/enum direkt in eine Farbe, Sichtbarkeit oder anderen Wert umgewandelt werden soll.

---

## 15. Thread-Safety in WPF – Dispatcher

In WPF darf die GUI **nur vom UI-Thread** verändert werden. Netzwerk-Daten kommen aber von anderen Threads.

### Dispatcher.Invoke – synchron warten

```csharp
// Wird aus dem Netzwerk-Thread aufgerufen:
private void AppendLog(string message)
{
    Dispatcher.Invoke(() =>        // Führt den Code im UI-Thread aus, wartet bis fertig
    {
        LstLog.Items.Add(message);
        LstLog.ScrollIntoView(LstLog.Items[^1]);
    });
}
```

### Dispatcher.BeginInvoke – feuern und vergessen

```csharp
// Scroll zum Ende nach dem Rendern:
Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
{
    var lv = FindChild<ListView>(TabChats);
    lv?.ScrollIntoView(lv.Items[^1]);
});
```

`BeginInvoke` kehrt sofort zurück. `DispatcherPriority.Background` bedeutet: nach dem aktuellen Render-Zyklus ausführen, also nachdem das neue Element sichtbar ist.

### Rekursive VisualTree-Suche

```csharp
private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
{
    int count = VisualTreeHelper.GetChildrenCount(parent);
    for (int i = 0; i < count; i++)
    {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is T t) return t;
        var result = FindChild<T>(child);  // Rekursiv in die Tiefe
        if (result != null) return result;
    }
    return null;
}
```

Der **VisualTree** ist die tatsächliche Struktur aller WPF-Elemente zur Laufzeit – `FindChild<ListView>(tabControl)` findet die innere ListView eines TabControls, auch wenn sie tief verschachtelt ist.

---

## 16. Gesamtablauf – von Login bis Nachricht

```
CLIENT                                    SERVER
──────────────────────────────────────────────────────────────────
App.OnStartup()
  → LoginWindow.Show()

[User gibt IP, Port, User, PW ein]
[Klick auf "Anmelden"]

await ConnectAsync("127.0.0.1", 5000)  → AcceptTcpClientAsync()
                                          → new ClientHandler(...)
                                          → Empfangsschleife startet

Send(Login-Paket)                ──────► PaketVerarbeiten(Login)
                                            → BenutzerAnmelden()
                                            → Username = "Max"
                                            → ClientHinzufügen(this)
                                 ◄──────  Send(LoginResponse { Success=true })
                                 ◄──────  AnAlleAußer(UserJoined "Max")

TaskCompletionSource gesetzt
→ MainWindow.Show()
→ LoginWindow.Close()

Send(GetRooms)                   ──────► RäumeSenden()
                                 ◄──────  Send(RoomsResponse [...])

[User doppelklickt auf "General"]
Send(JoinRoom "General")         ──────► RaumBetreten()
                                            → BetreteneRäume.Add("General")
                                            → LetzteNachrichten laden
                                 ◄──────  Send(RoomJoined { Room, RecentMessages })

→ Neuer Tab "General" mit Nachrichten-History

[User tippt Nachricht, drückt Enter]
Send(ChatMessage)                ──────► NachrichtSenden()
                                            → DB.NachrichtSpeichern()
                                            → AnRaum("General", ChatMessage)
                                 ◄──────  Send(ChatMessage) [an alle im Raum]

→ Nachricht erscheint im Tab
```

---

## Wichtigste Muster – Übersicht

| Muster | Wo im Projekt | Wiederverwendbar für |
|---|---|---|
| `ChatPacket` (Type + JSON-Payload) | `Shared/Protocol` | Jedes TCP-Protokoll |
| `TaskCompletionSource<T>` | `LoginWindow` | Events in async/await wrappen |
| `INotifyPropertyChanged` + `[CallerMemberName]` | ViewModels | Alle WPF Data-Bindings |
| `ObservableCollection<T>` | `MainWindow` | Dynamische Listen in WPF |
| `lock (_lock)` für thread-sichere Listen | `ChatServer` | Überall mit Multi-Threading |
| `Dispatcher.Invoke` | Server-GUI, Client-GUI | Netzwerk-Thread → UI-Thread |
| `XmlSerializer` Load/Save | `ClientSettings` | Einfache Einstellungsdateien |
| `BitmapImage.Freeze()` | `ImageHelper` | Bilder thread-sicher machen |
| `IValueConverter` | `Converters` | bool/enum → Farbe/Sichtbarkeit |
| `EllipseGeometry` als Clip | `MainWindow.xaml` | Runde Profilbilder |
| SHA-256 + Salt hashen | `DatabaseManager` | Passwörter sicher speichern |
| WAL-Modus + busy_timeout | `DatabaseManager` | SQLite mit parallelen Zugriffen |
| `GridSplitter` | `MainWindow.xaml` | Verschiebbare Spalten |
| Exception-Filter `when (code == 19)` | `DatabaseManager` | Spezifische SQLite-Fehler |
| `params (string, object?)[]` | `DatabaseManager` | Flexible SQL-Hilfsmethoden |
| switch-Guards `when IstAngemeldet` | `ClientHandler` | Zustandsabhängige Logik |
