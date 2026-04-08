using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Serialization;
using LINQtoCSV;
using Microsoft.Win32;

namespace WPF_Einkaufslistengenerator;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    // ===== ROHDATEN aus der CSV =====
    // Einfache Liste aller CsvProdukt-Objekte – LINQ arbeitet direkt darauf
    private List<CsvProdukt> _alleProdukte = new();

    // ===== EINKAUFSLISTE =====
    public ObservableCollection<EinkaufsArtikel> Einkaufsliste { get; set; } = new();

    // ===== PROPERTIES FÜR BINDING =====

    // LINQ Select + Distinct + OrderBy → alle eindeutigen Gruppen alphabetisch
    public List<string> Produktgruppen =>
        _alleProdukte
            .Select(p => p.Gruppe)   // nur die Gruppe-Spalte
            .Distinct()              // Duplikate entfernen
            .OrderBy(g => g)         // alphabetisch sortieren
            .ToList();

    // LINQ Where + Select → nur Produkte der gewählten Gruppe
    public List<string> AktuelleProdukte =>
        _alleProdukte
            .Where(p => p.Gruppe == _ausgewaehlteGruppe)  // nach Gruppe filtern
            .Select(p => p.Produkt)                        // nur Produktnamen
            .Distinct()                                    // Duplikate entfernen
            .OrderBy(p => p)                               // alphabetisch
            .ToList();

    // Ausgewählte Gruppe → löst Neuberechnung von AktuelleProdukte aus
    private string _ausgewaehlteGruppe = "";
    public string AusgewaehlteGruppe
    {
        get => _ausgewaehlteGruppe;
        set
        {
            _ausgewaehlteGruppe = value;
            OnPropertyChanged(nameof(AusgewaehlteGruppe));
            // AktuelleProdukte ist ein berechnetes Property (kein Feld),
            // daher reicht es, WPF per OnPropertyChanged zu informieren
            OnPropertyChanged(nameof(AktuelleProdukte));
        }
    }

    // Ausgewähltes Produkt in Combobox 2
    private string _ausgewaehltesProdukT = "";
    public string AusgewaehltesProdukT
    {
        get => _ausgewaehltesProdukT;
        set { _ausgewaehltesProdukT = value; OnPropertyChanged(nameof(AusgewaehltesProdukT)); }
    }

    // Freies Textfeld für eigene Produkte
    private string _eigenesProdukt = "";
    public string EigenesProdukt
    {
        get => _eigenesProdukt;
        set { _eigenesProdukt = value; OnPropertyChanged(nameof(EigenesProdukt)); }
    }

    // Anzahl – Slider und Textbox teilen denselben Wert
    private int _anzahl = 1;
    public int Anzahl
    {
        get => _anzahl;
        set { _anzahl = value < 1 ? 1 : value; OnPropertyChanged(nameof(Anzahl)); }
    }

    // ===== COMMANDS =====
    public RelayCommand HinzufuegenCommand { get; }
    public RelayCommand LoeschenCommand { get; }
    public RelayCommand NeuCommand { get; }
    public RelayCommand DruckenCommand { get; }
    public RelayCommand SpeichernCommand { get; }
    public RelayCommand LadenCommand { get; }

    // ===== KONSTRUKTOR =====
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        HinzufuegenCommand = new RelayCommand(ArtikelHinzufuegen);
        LoeschenCommand    = new RelayCommand(AuswahlLoeschen);
        NeuCommand         = new RelayCommand(NeueListe);
        DruckenCommand     = new RelayCommand(ListeDrucken);
        SpeichernCommand   = new RelayCommand(ListeSpeichern);
        LadenCommand       = new RelayCommand(ListeLaden);

        CsvEinlesen();
    }

    // ===== CSV EINLESEN (LINQtoCSVCore) =====
    private void CsvEinlesen()
    {
        try
        {
            var context = new CsvContext();
            var options = new CsvFileDescription
            {
                SeparatorChar = ';',
                FirstLineHasColumnNames = false,
                EnforceCsvColumnAttribute = true
            };

            // LINQ: Rohliste einlesen, Leerzeichen mit Select+Trim bereinigen
            _alleProdukte = context.Read<CsvProdukt>("Produkte.csv", options)
                .Select(p => new CsvProdukt
                {
                    Gruppe  = p.Gruppe.Trim(),
                    Produkt = p.Produkt.Trim()
                })
                .ToList();

            // Comboboxen aktualisieren
            OnPropertyChanged(nameof(Produktgruppen));
            OnPropertyChanged(nameof(AktuelleProdukte));

            // Erste Gruppe vorauswählen
            AusgewaehlteGruppe = Produktgruppen.FirstOrDefault() ?? "";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der CSV-Datei:\n{ex.Message}", "Fehler",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== HINZUFÜGEN =====
    private void ArtikelHinzufuegen()
    {
        // Eigenes Textfeld hat Vorrang vor Combobox
        string name = !string.IsNullOrWhiteSpace(EigenesProdukt)
            ? EigenesProdukt.Trim()
            : AusgewaehltesProdukT;

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte ein Produkt auswählen oder eingeben.", "Hinweis",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // LINQ FirstOrDefault: Produkt schon in der Liste?
        var vorhandener = Einkaufsliste.FirstOrDefault(e => e.Name == name);

        if (vorhandener != null)
            vorhandener.Anzahl += Anzahl;   // vorhanden → Anzahl erhöhen
        else
            Einkaufsliste.Add(new EinkaufsArtikel { Name = name, Anzahl = Anzahl });

        EigenesProdukt = "";
        Anzahl = 1;
    }

    // ===== LÖSCHEN (ausgewählte Einträge) =====
    private void AuswahlLoeschen()
    {
        // LINQ Cast → typsichere Liste der Auswahl
        var auswahl = ListeBox.SelectedItems.Cast<EinkaufsArtikel>().ToList();

        // ToList() verhindert Fehler beim Ändern der Collection während der Schleife
        foreach (var artikel in auswahl)
            Einkaufsliste.Remove(artikel);
    }

    // ===== NEU =====
    private void NeueListe()
    {
        var result = MessageBox.Show("Neue Liste beginnen? Alle Einträge werden gelöscht.",
                                     "Neu", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
            Einkaufsliste.Clear();
    }

    // ===== DRUCKEN =====
    private void ListeDrucken()
    {
        if (!Einkaufsliste.Any())   // LINQ Any() statt .Count == 0
        {
            MessageBox.Show("Die Einkaufsliste ist leer.", "Drucken",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        var doc = new FlowDocument
        {
            FontSize = 13,
            FontFamily = new System.Windows.Media.FontFamily("Arial"),
            PagePadding = new Thickness(60)
        };

        doc.Blocks.Add(new Paragraph(new Run("Einkaufsliste"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });

        // LINQ Select → Paragraphen aus Artikeln erzeugen, dann per foreach einfügen
        var paragraphen = Einkaufsliste
            .Select(a => new Paragraph(new Run($"• {a.Anzahl} x  {a.Name}")));

        foreach (var p in paragraphen)
            doc.Blocks.Add(p);

        dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Einkaufsliste");
    }

    // ===== SPEICHERN (XML) =====
    private void ListeSpeichern()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "XML-Datei (*.xml)|*.xml",
            DefaultExt = "xml",
            FileName = "Einkaufsliste"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var serializer = new XmlSerializer(typeof(List<EinkaufsArtikel>));
            using var stream = File.Create(dialog.FileName);
            serializer.Serialize(stream, Einkaufsliste.ToList());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", "Fehler",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== LADEN (XML) =====
    private void ListeLaden()
    {
        var dialog = new OpenFileDialog { Filter = "XML-Datei (*.xml)|*.xml" };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var serializer = new XmlSerializer(typeof(List<EinkaufsArtikel>));
            using var stream = File.OpenRead(dialog.FileName);
            var geladen = (List<EinkaufsArtikel>?)serializer.Deserialize(stream);

            if (geladen == null) return;

            Einkaufsliste.Clear();

            // LINQ ForEach-Äquivalent: jeden geladenen Artikel einfügen
            geladen.ForEach(a => Einkaufsliste.Add(a));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden:\n{ex.Message}", "Fehler",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
