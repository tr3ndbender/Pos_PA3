namespace WpfChat.Shared.Protocol
{
    /// <summary>
    /// Alle Nachrichtentypen die zwischen Client und Server ausgetauscht werden.
    /// Jedes ChatPacket hat einen dieser Typen.
    /// </summary>
    public enum MessageType
    {
        // === Anmeldung ===
        Register,           // Client → Server: Registrierung
        RegisterResponse,   // Server → Client: Ergebnis der Registrierung
        Login,              // Client → Server: Anmeldung
        LoginResponse,      // Server → Client: Ergebnis der Anmeldung
        Logout,             // Client → Server: Abmeldung

        // === Räume ===
        GetRooms,           // Client → Server: Liste aller Räume anfragen
        RoomsResponse,      // Server → Client: Liste aller Räume
        CreateRoom,         // Client → Server: Neuen Raum erstellen
        RoomCreated,        // Server → alle: Neuer Raum wurde erstellt
        JoinRoom,           // Client → Server: Raum betreten
        RoomJoined,         // Server → Client: Raum erfolgreich betreten (mit Nachrichten-Historie)
        LeaveRoom,          // Client → Server: Raum verlassen
        RoomLeft,           // Server → Client: Raum erfolgreich verlassen

        // === Nachrichten ===
        ChatMessage,        // Client ↔ Server: Nachricht in einem Raum
        PrivateMessage,     // Client ↔ Server: Private Nachricht an einen User

        // === Profil ===
        UpdateProfile,      // Client → Server: Farbe/Bild geändert
                            // Server → alle: Profil eines Users hat sich geändert

        // === Server-Benachrichtigungen ===
        UserJoined,         // Server → alle: Ein User hat sich angemeldet
        UserLeft,           // Server → alle: Ein User hat sich abgemeldet
        Error               // Server → Client: Fehlermeldung
    }
}
