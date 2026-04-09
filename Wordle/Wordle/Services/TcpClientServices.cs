using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Wordle.Services
{
    public class TcpClientService
    {
        private readonly string _host;
        private readonly int _port;

        public TcpClientService(string host = "127.0.0.1", int port = 5000)
        {
            _host = host;
            _port = port;
        }

        public async Task<string> SendMessageAsync(string message)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);

            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data);

            byte[] buffer = new byte[1024];
            int read = await stream.ReadAsync(buffer);
            return Encoding.UTF8.GetString(buffer, 0, read);
        }
    }
}
