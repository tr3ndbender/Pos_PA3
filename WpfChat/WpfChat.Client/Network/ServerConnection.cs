using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WpfChat.Shared.Protocol;

namespace WpfChat.Client.Network
{
    public class ServerConnection
    {
        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private readonly object _writeLock = new();
        private bool _running;
        // Wenn true wurde Disconnect() absichtlich aufgerufen → kein Disconnected-Event
        private bool _intentionalDisconnect;

        public event Action<ChatPacket>? PacketReceived;
        public event Action? Disconnected;
        public bool IsConnected => _client?.Connected ?? false;

        public async Task ConnectAsync(string host, int port)
        {
            _intentionalDisconnect = false;
            _client = new TcpClient();
            _client.NoDelay = true;           // Kein Nagle-Delay
            _client.ReceiveTimeout = 0;       // Kein Timeout beim Lesen
            await _client.ConnectAsync(host, port);
            var stream = _client.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            _running = true;
            _ = Task.Run(ReceiveLoop);
        }

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
                        PacketReceived?.Invoke(packet);
                }
            }
            catch (IOException) { /* Verbindung geschlossen */ }
            catch (Exception) { /* Andere Fehler */ }
            finally
            {
                _running = false;
                // Nur Event feuern wenn es KEIN absichtliches Trennen war
                if (!_intentionalDisconnect)
                    Disconnected?.Invoke();
            }
        }

        public void Send(ChatPacket packet)
        {
            try
            {
                lock (_writeLock)
                    _writer?.WriteLine(packet.ToJson());
            }
            catch { }
        }

        public void Disconnect()
        {
            _intentionalDisconnect = true;
            _running = false;
            try { _client?.Close(); } catch { }
        }
    }
}
