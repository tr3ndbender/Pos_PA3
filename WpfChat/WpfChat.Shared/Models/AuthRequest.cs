using System.Collections.Generic;

namespace WpfChat.Shared.Models
{
    /// <summary>Wird beim Login und Registrieren gesendet.</summary>
    public class AuthRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    /// <summary>Antwort des Servers auf Login/Registrierung.</summary>
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public UserDto? User { get; set; } // Bei Login: die User-Daten
    }

    /// <summary>Wird gesendet wenn der User sein Profil ändert (Farbe oder Bild).</summary>
    public class UpdateProfileRequest
    {
        public string Color { get; set; } = "#000000";
        public string? ProfileImageBase64 { get; set; }
    }

    /// <summary>Wird gesendet um einem Raum beizutreten oder ihn zu erstellen.</summary>
    public class RoomRequest
    {
        public string RoomName { get; set; } = "";
    }

    /// <summary>Antwort wenn ein Raum erfolgreich betreten wurde.</summary>
    public class JoinRoomResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public RoomDto? Room { get; set; }
        public List<ChatMessageDto> RecentMessages { get; set; } = new(); // letzte 50 Nachrichten
    }
}
