using System.Diagnostics;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace NetworkLibrary
{
    public class Transfer<T>
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private XmlSerializer serializer = new XmlSerializer(typeof(T));
        private EventHandler OnDisconnected;
        private EventHandler<T> _onMessageReceived;

        public event EventHandler<T> OnMessageReceived
        {
            add
            {
                _onMessageReceived += value;
            }
            remove
            {
                _onMessageReceived -= value;
            }
        }

        public Transfer(TcpClient client)
        {
            _client = client;
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };

            ThreadPool.QueueUserWorkItem(o => Receive());
        }

        public void SendMessage(T message)
        {
            StringWriter stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, message);
            _writer.WriteLine(stringWriter.ToString());
        }

        private void Receive()
        {
            string text = "";
            while (true)
            {
                try
                {
                    string data = _reader.ReadLine();
                    text += data;
                    if (data.Contains("</" + typeof(T).Name + ">"))
                    {
                        StringReader stringReader = new StringReader(text);
                        T message = (T)serializer.Deserialize(stringReader);
                        text = "";
                        _onMessageReceived?.Invoke(this, message);
                    }
                }
                catch (Exception ex)
                {
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                    break;
                }
            }
        }
    }

}
