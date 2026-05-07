using DataModels;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.VisualBasic;
using Network;
using NinjaNye.SearchExtensions.Soundex;
using Server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Server.MSG;


internal class Programm
{
    
    internal Programm()
    {
        
        
    }

    public static void Main(string[] args)
    {
        Programm programm = new();
        Console.WriteLine("hallo");

        TcpListener server = new TcpListener(IPAddress.Any, 12345);
        server.Start();

        TcpClient client = server.AcceptTcpClient();
        Console.WriteLine("Client connected! " + client.Client.RemoteEndPoint);


        using var db = new BabynamesDB(new DataOptions() // um auto generated TT zu nutzen, muss namespace Datamodels dazu
            .UseSQLite(@"Data Source=Babynames.db"));

        var query = from b in db.Babynames
                    select b;

        var alle = query.ToList();
        /*
        foreach ( var b in alle)
        {
            Console.WriteLine(b.Name);
        }

        */

        Transfer<MSG> transfer = new Transfer<MSG>(client);

        transfer.OnMessageReceived += (sender, msg) =>
        {
            Transfer<MSG> t = (Transfer<MSG>)sender;

            if (msg.Type == MSG.MessageType.SEARCH)
            {
                var query = from b in db.Babynames
                            where b.Name.ToLower().StartsWith(msg.Search.ToLower()) && b.Sex == msg.Sex
                            select b.Name;
                var respondList = query.Distinct().ToList();

                MSG searchResult = new MSG()
                {
                    Type = MessageType.SEARCHRESULT,
                    Names = respondList

                };

                transfer.Send(searchResult);
                Console.WriteLine("Antowrt gesendet: SearchResult!");
            }
            else if (msg.Type == MSG.MessageType.DETAIL)
            {
                List<Babyname> jahresliste = db.Babynames
                                            .Where(b => b.Name == msg.Search && b.Sex == msg.Sex)
                                            .OrderBy(b => b.Year)
                                            .ToList();


                MSG detailResult = new MSG()
                {
                    Type = MessageType.DETAILRESULT,
                    Details = jahresliste

                };
                transfer.Send(detailResult);
                Console.WriteLine("Antowrt gesendet: DetailResult!");
            }
            else if (msg.Type == MSG.MessageType.ALTERNATIVE)
            {
                List<String> similarNames = db.Babynames
                                            .Where(b => b.Sex == msg.Sex)
                                            .ToList()                              // erst in den RAM laden
                                            .SoundexOf(b => b.Name).Matching(msg.Search) // dann Soundex im RAM
                                            .Select(b => b.Name)
                                            .Distinct()
                                            .ToList();

                MSG detailResult = new MSG()
                {
                    Type = MessageType.ALTERNATIVERESULT,
                    Names = similarNames 

                };
                transfer.Send(detailResult);
                Console.WriteLine("Antowrt gesendet: AlternativeResult!");
            }
        };
        Console.Read();
        }

}