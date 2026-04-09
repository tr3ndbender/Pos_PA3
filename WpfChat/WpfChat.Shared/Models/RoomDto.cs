using System.Collections.Generic;

namespace WpfChat.Shared.Models
{
    /// <summary>Chat-Raum Daten.</summary>
    public class RoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<string> Members { get; set; } = new();
    }
}
