using System.Xml.Serialization;
using System.IO;
using System;

namespace WpfChat.Client.Models
{
    [XmlRoot("ClientSettings")]
    public class ClientSettings
    {
        public string LastServerIp { get; set; } = "127.0.0.1";
        public int LastServerPort { get; set; } = 5000;
        public string LastUsername { get; set; } = "";

        private static readonly string SettingsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

        public static ClientSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new ClientSettings();
                var xs = new XmlSerializer(typeof(ClientSettings));
                using var fs = File.OpenRead(SettingsPath);
                return (ClientSettings?)xs.Deserialize(fs) ?? new ClientSettings();
            }
            catch { return new ClientSettings(); }
        }

        public void Save()
        {
            try
            {
                var xs = new XmlSerializer(typeof(ClientSettings));
                using var fs = File.Create(SettingsPath);
                xs.Serialize(fs, this);
            }
            catch { }
        }
    }
}
