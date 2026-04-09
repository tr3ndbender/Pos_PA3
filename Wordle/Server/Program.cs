using Server;
using Server.Model;
using System.Net;
using System.Net.Sockets;
using System.Text;

using var db = new AppDbContext();
db.Database.EnsureCreated();


var alle = db.Words.ToList();

foreach (Word wort in alle)
{
    Console.WriteLine(wort.Text);
}

var listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();
Console.WriteLine("Server läuft auf Port 5000...");

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(async () =>
    {

        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int read = await stream.ReadAsync(buffer);
        string msg = Encoding.UTF8.GetString(buffer, 0, read);
        Console.WriteLine($"Empfangen: {msg}");

        // hier schickt der Server die Daten zurück
        byte[] answer = Encoding.UTF8.GetBytes("Hallo vom Server");
        await stream.WriteAsync(answer);
        client.Close();
    });
}


