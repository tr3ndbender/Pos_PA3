using System;
using System.IO;
using System.Xml.Serialization;

namespace WPF_Chat
{
    // Diese Klasse speichert die Einstellungen des Clients.
    // Sie wird beim Beenden des Programms als XML-Datei gespeichert
    // und beim nächsten Start wieder geladen.
    //
    // [XmlRoot] gibt den Namen des Root-Elements in der XML-Datei an.
    // Die gespeicherte Datei sieht z.B. so aus:
    //   <Settings>
    //     <ServerIP>192.168.1.10</ServerIP>
    //     <ServerPort>5000</ServerPort>
    //   </Settings>
    [XmlRoot("Settings")]
    public class ClientSettings
    {
        // Die zuletzt verwendete Server-IP-Adresse
        public string ServerIP { get; set; } = "127.0.0.1";

        // Der zuletzt verwendete Server-Port
        public int ServerPort { get; set; } = 5000;

        // Serialisiert (=konvertiert) die Einstellungen in eine XML-Datei und speichert sie.
        // XmlSerializer wandelt das Objekt automatisch in XML um.
        public void Save(string path)
        {
            var serializer = new XmlSerializer(typeof(ClientSettings));
            using var writer = new StreamWriter(path);
            serializer.Serialize(writer, this);
        }

        // Lädt die Einstellungen aus einer XML-Datei.
        // Gibt Standardwerte zurück wenn:
        //   - die Datei nicht existiert (erster Start)
        //   - die Datei kaputt ist (Fehler beim Lesen)
        public static ClientSettings Load(string path)
        {
            if (!File.Exists(path))
                return new ClientSettings(); // Datei nicht vorhanden → Standardwerte

            try
            {
                var serializer = new XmlSerializer(typeof(ClientSettings));
                using var reader = new StreamReader(path);
                // Deserialisieren = XML zurück in ein C#-Objekt umwandeln
                return (ClientSettings)serializer.Deserialize(reader)!;
            }
            catch
            {
                return new ClientSettings(); // Fehler beim Laden → Standardwerte
            }
        }
    }
}
