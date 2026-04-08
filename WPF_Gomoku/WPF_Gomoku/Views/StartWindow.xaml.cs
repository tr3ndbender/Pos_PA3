using System.Windows;
using WPF_Gomoku.Controllers;

namespace WPF_Gomoku.Views;

public partial class StartWindow : Window
{
    public StartWindow()
    {
        InitializeComponent();
    }

    // Client-RadioButton: IP-Feld einblenden, Feldgröße deaktivieren
    private void rbClient_Checked(object sender, RoutedEventArgs e)
    {
        pnlIp.Visibility = Visibility.Visible;
        txtSize.IsEnabled = false; // Größe kommt vom Server
    }

    private void rbClient_Unchecked(object sender, RoutedEventArgs e)
    {
        pnlIp.Visibility = Visibility.Collapsed;
        txtSize.IsEnabled = true;
    }

    // Start-Button: Controller erstellen und Spielfenster öffnen
    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        // Feldgröße validieren (außer bei Client, da kommt sie vom Server)
        int size = 15;
        if (rbClient.IsChecked != true)
        {
            if (!int.TryParse(txtSize.Text, out size) || size < 5 || size > 19)
            {
                MessageBox.Show("Feldgröße muss zwischen 5 und 19 liegen.", "Ungültige Eingabe",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        IGameController controller;

        if (rbHuman.IsChecked == true)
        {
            // Mensch vs. Mensch: einfach, kein async nötig
            controller = new LocalHumanController(size);
        }
        else if (rbComputer.IsChecked == true)
        {
            // Mensch vs. Computer
            controller = new ComputerController(size);
        }
        else if (rbServer.IsChecked == true)
        {
            // Server: Fenster sofort öffnen, Status zeigt "Warte auf Verbindung..."
            controller = new NetworkServerController(size);
        }
        else
        {
            // Client: Verbindung aufbauen BEVOR das Spielfenster geöffnet wird
            // async/await: wartet ohne den UI-Thread zu blockieren
            btnStart.IsEnabled = false;
            btnStart.Content = "Verbinde...";

            var (clientController, error) = await NetworkClientController.ConnectAsync(txtIp.Text);

            if (error != null)
            {
                MessageBox.Show($"Verbindung fehlgeschlagen:\n{error}", "Fehler",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                btnStart.IsEnabled = true;
                btnStart.Content = "Spiel starten";
                return;
            }

            controller = clientController!;
        }

        // Spielfenster öffnen und Startfenster schließen
        new GameWindow(controller).Show();
        Close();
    }
}
