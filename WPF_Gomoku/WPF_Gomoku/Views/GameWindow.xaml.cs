using System.Windows;
using WPF_Gomoku.Controllers;

namespace WPF_Gomoku.Views;

public partial class GameWindow : Window
{
    private readonly IGameController _controller;

    public GameWindow(IGameController controller)
    {
        InitializeComponent();

        _controller = controller;

        // DataContext setzen: alle Bindings in GameWindow.xaml beziehen sich hierauf
        // z.B. {Binding StatusMessage} → controller.StatusMessage
        //      {Binding Board.Cells}   → controller.Board.Cells
        DataContext = controller;
    }

    // Wenn das Fenster geschlossen wird: Controller aufräumen
    // z.B. Netzwerkverbindung schließen
    protected override void OnClosed(EventArgs e)
    {
        _controller.Cleanup();
        base.OnClosed(e);
    }
}
