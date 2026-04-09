using Server;
using Server.Models;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Text;

using var db = new AppDbContext();
db.Database.EnsureCreated();



/* TEST
foreach (Word word in alle)
{
    Console.WriteLine(word.Text);
}
*/

var listener = new TcpListener(IPAddress.Any, 12345);
listener.Start();
Console.WriteLine("Server läuft auf Port 12345...");

while (true)
{
    var client = listener.AcceptTcpClient(); // Wartet auf Verbindung
    _ = Task.Run(() => HandleClient(client));
}

void HandleClient(TcpClient client)
{
    var stream = client.GetStream();
    var reader = new StreamReader(stream);
    var writer = new StreamWriter(stream) { AutoFlush = true };

    using var db = new AppDbContext();
    var alle = db.Words.ToList();

    var randomWord = alle[new Random().Next(alle.Count)].Text.ToUpper();

    // Als XML gesendet an den CLient
    writer.WriteLine($"<Word>{randomWord}</Word>");
    Console.WriteLine($"Gesendetes Wort: {randomWord}");

    //Nachricht abfangen vom Client
    string line;
    while ((line = reader.ReadLine()) != null)
    {
        var antwort = XDocument.Parse(line).Root.Value;
        //writer.WriteLine("Antwort an Client zurück");
        Console.WriteLine(antwort);
    }

}