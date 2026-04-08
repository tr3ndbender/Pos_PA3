using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace WPF_Chat
{
    public class ServerConnection
    {
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;

        public event Action<string> MessageReceived;
        public event Action Disconnected;

        public bool Connect(string host, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(host, port);

                var stream = _client.GetStream();
                _writer = new StreamWriter(stream) { AutoFlush = true };
                _reader = new StreamReader(stream);

                Thread t = new Thread(Listen);
                t.IsBackground = true;
                t.Start();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Listen()
        {
            try
            {
                string line;
                while ((line = _reader.ReadLine()) != null)
                {
                    MessageReceived?.Invoke(line);
                }
            }
            catch { }
            finally
            {
                Disconnected?.Invoke();
            }
        }

        public void Send(string message)
        {
            try { _writer?.WriteLine(message); }
            catch { }
        }

        public void Disconnect()
        {
            _client?.Close();
        }
    }
}
