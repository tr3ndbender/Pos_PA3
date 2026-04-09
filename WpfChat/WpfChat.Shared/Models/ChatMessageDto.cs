using System;

namespace WpfChat.Shared.Models
{
    /// <summary>
    /// Eine Chat-Nachricht. Wird sowohl für Raum-Nachrichten als auch
    /// für private Nachrichten verwendet.
    /// </summary>
    public class ChatMessageDto
    {
        public string SenderUsername { get; set; } = "";
        public string SenderColor { get; set; } = "#000000";
        public string? SenderProfileImageBase64 { get; set; }

        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }

        // Bei Raum-Nachrichten gesetzt, bei privaten Nachrichten null
        public string? RoomName { get; set; }

        // Bei privaten Nachrichten gesetzt, bei Raum-Nachrichten null
        public string? RecipientUsername { get; set; }

        // Hilfseigenschaft: true wenn private Nachricht
        public bool IsPrivate => RecipientUsername != null;
    }
}
