using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPF_Chat
{
    // Diese Klasse repräsentiert eine einzelne Chat-Nachricht.
    // Sie wird als Datenbindungsquelle (Binding) für das DataTemplate in der ListBox verwendet.
    // Das bedeutet: WPF liest die Properties dieser Klasse und zeigt sie im DataTemplate an.
    public class ChatMessage
    {
        // Der Benutzername des Absenders (z.B. "Max")
        public string Username { get; set; } = "";

        // Der eigentliche Text der Nachricht
        public string Text { get; set; } = "";

        // Die Farbe des Benutzers als WPF-Pinsel – wird für den Benutzernamen verwendet.
        // SolidColorBrush ist der WPF-Typ für einfarbige Pinsel (z.B. Rot, Blau, ...)
        public SolidColorBrush Color { get; set; } = Brushes.Black;

        // Das Profilbild des Absenders als BitmapImage.
        // Kann null sein wenn der Benutzer kein Bild gesetzt hat.
        public BitmapImage? Image { get; set; }
    }
}
