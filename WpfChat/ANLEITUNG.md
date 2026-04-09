# WPF Chat – Projektanleitung

## Projektstruktur

```
WpfChat/
├── WpfChat.sln                  ← Visual Studio Solution
├── WpfChat.Shared/              ← Gemeinsame Bibliothek (Client + Server)
│   ├── Protocol/
│   │   ├── MessageType.cs       ← Protokoll-Enum (alle Nachrichtentypen)
│   │   └── ChatPacket.cs        ← JSON-Serialisierung der Pakete
│   └── Models/
│       ├── UserDto.cs           ← Benutzer-Datenmodell
│       ├── RoomDto.cs           ← Raum-Datenmodell
│       ├── ChatMessageDto.cs    ← Nachricht-Datenmodell
│       └── AuthRequest.cs       ← Alle Request/Response-Klassen
│
├── WpfChat.Server/              ← WPF Server-Anwendung
│   ├── Database/
│   │   └── DatabaseManager.cs  ← SQLite-Datenbankzugriff (LINQ-style)
│   ├── Network/
│   │   ├── ChatServer.cs        ← TCP-Listener, Client-Verwaltung
│   │   └── ClientHandler.cs     ← Protokoll-Handler pro Client
│   ├── MainWindow.xaml          ← Server-GUI mit Protokoll-ListBox
│   └── MainWindow.xaml.cs
│
└── WpfChat.Client/              ← WPF Client-Anwendung
    ├── Models/
    │   ├── ClientSettings.cs    ← XML-Persistierung (IP, Port, Username)
    │   ├── ChatTabItem.cs       ← TabControl-Datenmodell
    │   └── MessageViewModel.cs  ← ViewModel für Nachrichten + Avatar
    ├── Network/
    │   └── ServerConnection.cs  ← TCP-Verbindung zum Server
    ├── Helpers/
    │   └── ImageHelper.cs       ← Bild laden/skalieren/Base64
    ├── Converters/
    │   └── Converters.cs        ← UnreadColorConverter (WPF)
    └── Views/
        ├── LoginWindow.xaml     ← Anmeldung / Registrierung
        └── MainWindow.xaml      ← Hauptfenster (Tabs, Räume, Profil)
```

## Voraussetzungen

- **Visual Studio 2022** (oder neuer)
- **.NET 8 SDK** (mit Windows-Desktop-Workload)
- **NuGet-Pakete** werden automatisch beim ersten Build heruntergeladen:
  - `Extended.Wpf.Toolkit` 4.6.0 (ColorPicker)
  - `Microsoft.Data.Sqlite` 8.0.0 (SQLite)
  - `Newtonsoft.Json` 13.0.3

## Einrichten & Starten

1. `WpfChat.sln` in Visual Studio öffnen
2. NuGet-Pakete wiederherstellen (automatisch oder: *Rechtsklick auf Solution → NuGet-Pakete wiederherstellen*)
3. **Server zuerst starten**: `WpfChat.Server` als Startprojekt setzen → F5
4. Im Server-GUI: Port eingeben (Standard: 5000) → **▶ Starten** klicken
5. **Client starten**: `WpfChat.Client` als Startprojekt setzen → F5
6. Login-Dialog: Server-IP (`127.0.0.1` für lokal), Port, Benutzername, Passwort

## Funktionen

### Anmeldung / Registrierung
- Beim ersten Start „Registrieren" klicken → Konto anlegen
- Danach mit demselben Benutzernamen anmelden
- Server-IP und Port werden in `settings.xml` gespeichert

### Chat-Räume
- Linkes Panel → **Verfügbare Räume** zeigt alle Räume
- Doppelklick oder **→** Button → Raum betreten (als neuer Tab)
- **+** Button → Neuen Raum erstellen
- Tabs können mit **✕** geschlossen werden (= Raum verlassen)
- Ungelesene Nachrichten → Tab-Titel wird orange markiert

### Private Nachrichten
- Doppelklick auf Benutzer in der Online-Liste (rechts)
- Oder: Menü *Chat → Private Nachricht...*
- Erscheinen als eigener Tab mit orangem Hintergrund

### Profilbild & Farbe
- Menü *Profil → Profilbild auswählen...*: Bild wird auf 50×50 px skaliert
- Menü *Profil → Farbe ändern...*: Extended WPF Toolkit ColorPicker
- Profilbild wird nur einmal übertragen und lokal gecacht

### Server-Protokoll
- Zeigt alle Aktivitäten seit Start: Verbindungen, Logins, Nachrichten
- SQLite-Datenbank (`chat.db`) wird im Programmverzeichnis gespeichert
- **DB Browser for SQLite** kann die Datei direkt öffnen

## Kommunikationsprotokoll (TCP + JSON)

Jedes Paket ist eine JSON-Zeile:
```json
{"Type": 4, "Payload": "{\"Username\":\"Alice\",\"Password\":\"secret\"}"}
```

Alle `MessageType`-Werte sind in `WpfChat.Shared/Protocol/MessageType.cs` definiert.

## Bekannte Einschränkungen / mögliche Erweiterungen

- Passwörter werden mit SHA-256 + Salt gehasht (für Produktion: BCrypt empfohlen)
- Emojis: RichTextBox + NuGet-Paket für Emoji-Unterstützung nachrüstbar
- Datei-Übertragung: als optionale Erweiterung möglich
- Mehrere Server-Instanzen benötigen unterschiedliche Ports
