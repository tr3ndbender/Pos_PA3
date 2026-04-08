using System.ComponentModel;

namespace WPF_Einkaufslistengenerator;

// Ein Eintrag in der Einkaufsliste.
// INotifyPropertyChanged sorgt dafür, dass die ListBox sich automatisch aktualisiert,
// wenn z.B. die Anzahl geändert wird.
public class EinkaufsArtikel : INotifyPropertyChanged
{
    private int _anzahl;

    // Name des Produkts (wird für XML-Serialisierung benötigt → public Setter)
    public string Name { get; set; } = "";

    // Anzahl mit PropertyChanged → UI aktualisiert sich automatisch
    public int Anzahl
    {
        get => _anzahl;
        set
        {
            _anzahl = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anzahl)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
