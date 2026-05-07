using LinqToDB;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Network;
using Network;
using NinjaNye.SearchExtensions.Soundex;
using Server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using DataModels;
using static Server.MSG;

internal class Programm
{
    
    internal Programm()
    {
        
        
    }

    public static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 12345);
        server.Start();

        TcpClient client = server.AcceptTcpClient();
        Console.WriteLine("Client connected! " + client.Client.RemoteEndPoint);

        // wenn man es mit CLI erstellt, muss man es so verwenden, anders als mit TT
        var options = new DataOptions().UseSQLite(@"Data Source=Wordle.db");
        using var db = new WordleDB(new DataOptions<WordleDB>(options));

        // wichtig Datenbank in AusgabeOrdner kopieren, sonst funktioniert nichts

        var alle = db.Woerters.Select(w => w.Wort);

        Random rnd = new Random();
        var liste = alle.ToList();
        string initialWord = liste[rnd.Next(0, liste.Count)];

        string checkInitial = initialWord.ToUpper();
        
        bool checkFinished = false;

        Console.WriteLine(initialWord);

        List<String> controlGuess = new(); 

        /*alle.ToList().ForEach(w =>
        {
            Console.WriteLine(w);
        }); */

        Transfer<MSG> transfer = new Transfer<MSG>(client);

        transfer.OnMessageReceived += (sender, msg) =>
        {
            Transfer<MSG> t = (Transfer<MSG>)sender;

            Console.WriteLine("Request received: " + msg.Type);
            if (!checkFinished)
            {
                if (msg.Type == MessageType.GUESS)
                {
                    Console.WriteLine("Guess recieved: " + msg.Wort);

                    if (msg.Wort.Length != 7)
                    {
                        Console.WriteLine("Wort muss 7 Buchstaben lang sein");
                        return;
                    }
                    controlGuess.Clear();

                    for (int i = 0; i < 7; i++)
                    {
                        if (checkInitial.Contains(msg.Wort[i]) && checkInitial[i] == msg.Wort[i])
                        {
                            controlGuess.Add("G"); //Gruen
                        }
                        else if (checkInitial.Contains(msg.Wort[i]))
                        {
                            controlGuess.Add("Y"); //GELB
                        }
                        else
                        {
                            controlGuess.Add("X"); //GRAU
                        }
                    }

                    if (msg.Wort == checkInitial)
                    {
                        checkFinished = true;
                    }

                    MSG Response = new MSG()
                    {
                        Type = MessageType.RESPONSE,
                        Results = controlGuess,
                        Wort = msg.Wort,
                        trueOrFalse = checkFinished
                    };

                    transfer.Send(Response);
                    Console.WriteLine("Antowrt gesendet: RESPONSE!");
                }
            }
            else
            {
                Console.WriteLine("Already finished");
            }

            
        };

            Console.ReadLine();
    }
}