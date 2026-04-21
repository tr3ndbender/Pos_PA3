using LinqToDB;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Network;
using Network;
using NinjaNye.SearchExtensions.Soundex;
using Server;
using Server.Models;
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


        using var db = new BabynamesDB(new DataOptions() // um auto generated TT zu nutzen, muss namespace Datamodels dazu
            .UseSQLite(@"Data Source=Babynames.db"));

        var query = from b in db.Babynames
                    select b;

        var alle = query.ToList();
        var alleAlternative = alle.Select(b => b).DistinctBy(b => new { b.Name, b.Sex }).ToList();

        /* Test ob ORM Connection geklappt hat
        foreach (Babyname babyname in alle)
        {
            Console.WriteLine(babyname.Name);
        }
        */



        Transfer<MSG> transfer = new Transfer<MSG>(client);

            transfer.OnMessageReceived += (sender, msg) =>
            {
                Transfer<MSG> t = (Transfer<MSG>)sender;

                if (msg.Type == MSG.MessageType.SEARCH)
                {
                    Console.WriteLine("Request received: " + msg.Type);

                    var query = (from n in db.Babynames
                                 where n.Name.StartsWith(msg.Search) && n.Sex == msg.Sex
                                 select n.Name).Distinct();

                    List<string> NamesList = query.ToList();

                    MSG suchAntwort = new MSG()
                    {
                        Type = MessageType.SEARCHRESULT,
                        Names = NamesList

                    };

                    transfer.Send(suchAntwort);
                    Console.WriteLine("Antowrt gesendet: SearchResult!");
                }
                else if (msg.Type == MessageType.ALTERNATIVE)
                {
                    Console.WriteLine("Request received: " + msg.Type);

                    //List<string> alternativeNamesList = msg.Search.HasTheSameSoundex("Hossein", "en-US");



                    var result = alleAlternative.AsQueryable()
                     .Where(x => x.Sex == msg.Sex)
                     .SoundexOf(x => x.Name)
                     .Matching(msg.Search);
                    List<string> alternativeList = result.Select(x => x.Name).ToList();


                    MSG alternativeAntwort = new MSG()
                    {
                        Type = MessageType.ALTERNATIVERESULT,
                        Names = alternativeList

                    };

                    transfer.Send(alternativeAntwort);
                    Console.WriteLine("Antowrt gesendet: Alternative!");
                }
                else if (msg.Type == MessageType.DETAIL)
                {
                    Console.WriteLine("Request recieved: " + msg.Type);

                    List<Babyname> jahresListe = db.Babynames
                                                    .Where(b => b.Name == msg.Search && b.Sex == msg.Sex)
                                                    .OrderBy(b => b.Year)
                                                    .ToList();


                    MSG alternativeAntwort = new MSG()
                    {
                        Type = MessageType.DETAILRESULT,
                        Details = jahresListe

                    };
                    transfer.Send(alternativeAntwort);
                    Console.WriteLine("Antowrt gesendet: Details!");
                }


            };

        Console.ReadLine();
    }
}