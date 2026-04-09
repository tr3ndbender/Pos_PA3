namespace WpfChat.Shared.Models
{
    /// <summary>Benutzer-Daten die zwischen Client und Server übertragen werden.</summary>
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Color { get; set; } = "#000000";
        public string? ProfileImageBase64 { get; set; } // Profilbild als Base64-String
    }
}
