using Server;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using Server.Models;

// DB prüfen beim Start
using var db = new AppDbContext();
db.Database.EnsureCreated();

// Server starten
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

    // Test
    using var dbTest = new AppDbContext();
    var count = dbTest.Words.Count();
    Console.WriteLine($"Wörter in DB: {count}");


    // Zufälliges Wort aus DB holen (nur 7-Buchstaben-Wörter)
    using var db = new AppDbContext();
    var allWords = db.Words.ToList().Where(w => w.Text.Length == 7).ToList();
    if (allWords.Count == 0)
    {
        Console.WriteLine("FEHLER: Keine 7-Buchstaben-Wörter in der Datenbank!");
        client.Close();
        return;
    }
    var randomWord = allWords[new Random().Next(allWords.Count)].Text.ToUpper();

    // Wort als XML zum Client senden
    writer.WriteLine($"<Word>{randomWord}</Word>");
    Console.WriteLine($"Gesendetes Wort: {randomWord}");

    // Auf Guesses warten
    string line;
    while ((line = reader.ReadLine()) != null)
    {
        var guess = XDocument.Parse(line).Root.Value;
        var result = CheckGuess(guess, randomWord);
        writer.WriteLine(result);
    }
}

string CheckGuess(string guess, string word)
{
    var results = new char[word.Length];
    for (int i = 0; i < word.Length; i++)
    {
        if (guess[i] == word[i]) results[i] = 'G'; // Grün
        else if (word.Contains(guess[i])) results[i] = 'Y'; // Gelb
        else results[i] = 'X'; // Grau
    }
    return $"<Result>{string.Join(",", results)}</Result>";
}