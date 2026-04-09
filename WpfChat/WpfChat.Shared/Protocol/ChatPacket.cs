using Newtonsoft.Json;

namespace WpfChat.Shared.Protocol
{
    /// <summary>
    /// Ein Paket das über TCP gesendet wird.
    /// Jede Zeile im Netzwerk-Stream ist ein serialisiertes ChatPacket (JSON).
    /// </summary>
    public class ChatPacket
    {
        public MessageType Type { get; set; }

        // Der eigentliche Inhalt – ebenfalls als JSON-String verpackt
        public string? Payload { get; set; }

        // Paket erstellen
        public static ChatPacket Create<T>(MessageType type, T payload) => new()
        {
            Type = type,
            Payload = JsonConvert.SerializeObject(payload)
        };

        // Paket in JSON-String umwandeln (für den Netzwerk-Stream)
        public string ToJson() => JsonConvert.SerializeObject(this);

        // JSON-String zurück in Paket umwandeln
        public static ChatPacket? FromJson(string json) =>
            JsonConvert.DeserializeObject<ChatPacket>(json);

        // Payload auslesen und in den gewünschten Typ umwandeln
        public T? GetPayload<T>() =>
            Payload == null ? default : JsonConvert.DeserializeObject<T>(Payload);
    }
}
