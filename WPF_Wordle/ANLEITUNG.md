# WPF Wordle – Schritt-für-Schritt Anleitung

Dieses Dokument erklärt, wie das WPF-Wordle-Projekt aufgebaut wurde – Schritt für Schritt, mit dem echten Code aus dem Projekt.

---

## Projektstruktur

Die Solution besteht aus **zwei Projekten**:

```
WPF_Wordle/
├── Server/              ← Konsolen-App (TCP-Server + Datenbank)
│   ├── Models/Word.cs
│   ├── AppDbContext.cs
│   └── Program.cs
└── WPF_Wordle/          ← WPF-App (Client/GUI)
    ├── Models/Cell.cs
    ├── MainWindow.xaml
    └── MainWindow.xaml.cs
```

---

## Schritt 1 – Datenbank mittels ORM anbinden

**Projekt:** `Server`

Das ORM (Object-Relational Mapper) ist **Entity Framework Core** mit SQLite als Datenbank.

### 1.1 NuGet-Paket installieren

Im `Server.csproj` wird das Paket eingebunden:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.5" />
</ItemGroup>
```

> Via Terminal: `dotnet add package Microsoft.EntityFrameworkCore.Sqlite`

### 1.2 Das Model erstellen (`Models/Word.cs`)

Das Model repräsentiert eine Zeile in der Datenbanktabelle:

```csharp
namespace Server.Models
{
    public class Word
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }
}
```

### 1.3 Den DbContext erstellen (`AppDbContext.cs`)

Der `DbContext` ist die Brücke zwischen C# und der Datenbank:

```csharp
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server
{
    internal class AppDbContext : DbContext
    {
        public DbSet<Word> Words { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseSqlite("Data Source=wordle.db");
    }
}
```

- `DbSet<Word>` = die Tabelle `Words` in der DB
- `UseSqlite(...)` = Verbindungsstring zur SQLite-Datei

### 1.4 Datenbank beim Start erstellen

In `Program.cs` wird beim Start sichergestellt, dass die DB existiert:

```csharp
using var db = new AppDbContext();
db.Database.EnsureCreated();
```

`EnsureCreated()` legt die Datenbank und alle Tabellen an, falls sie noch nicht existieren.

---

## Schritt 2 – Client mit Server über Port 12345 verbinden (XML-Transfer)

### 2.1 Server: TCP-Listener starten (`Program.cs`)

```csharp
var listener = new TcpListener(IPAddress.Any, 12345);
listener.Start();
Console.WriteLine("Server läuft auf Port 12345...");

while (true)
{
    var client = listener.AcceptTcpClient(); // Wartet auf Verbindung
    _ = Task.Run(() => HandleClient(client)); // Jeden Client in eigenem Task
}
```

- `IPAddress.Any` = akzeptiert Verbindungen von überall
- `Task.Run(...)` = jeder Client bekommt seinen eigenen Thread

### 2.2 Server: Zufälliges Wort aus DB holen und als XML senden

```csharp
void HandleClient(TcpClient client)
{
    var stream = client.GetStream();
    var reader = new StreamReader(stream);
    var writer = new StreamWriter(stream) { AutoFlush = true };

    // Zufälliges Wort aus DB holen (nur 7-Buchstaben-Wörter)
    using var db = new AppDbContext();
    var allWords = db.Words.ToList().Where(w => w.Text.Length == 7).ToList();
    var randomWord = allWords[new Random().Next(allWords.Count)].Text.ToUpper();

    // Wort als XML zum Client senden
    writer.WriteLine($"<Word>{randomWord}</Word>");
}
```

Das XML sieht dann so aus: `<Word>HOLIDAY</Word>`

### 2.3 Client: Mit Server verbinden und Wort empfangen (`MainWindow.xaml.cs`)

```csharp
// Felder für die Verbindung
TcpClient _tcp;
StreamReader _reader;
StreamWriter _writer;
string _serverWord = "";

public MainWindow()
{
    InitializeComponent();

    // Mit Server verbinden
    _tcp = new TcpClient("127.0.0.1", 12345);
    var stream = _tcp.GetStream();
    _reader = new StreamReader(stream);
    _writer = new StreamWriter(stream) { AutoFlush = true };

    // Wort vom Server empfangen und aus XML parsen
    var xml = _reader.ReadLine();                      // "<Word>HOLIDAY</Word>"
    _serverWord = XDocument.Parse(xml).Root.Value;     // → "HOLIDAY"
}
```

---

## Schritt 3 – GUI: Worteingabe und "Raten"-Button

**Datei:** `MainWindow.xaml`

```xml
<StackPanel Grid.Column="0" Margin="10" HorizontalAlignment="Center">

    <!-- Texteingabe für das Wort (max. 7 Zeichen) -->
    <TextBox x:Name="GuessInput" MaxLength="7" FontSize="20"
             Width="200" Margin="0,10"/>

    <!-- Raten-Button -->
    <Button x:Name="RatenButton" Content="Raten" Click="RatenButton_Click"
            Width="100" FontSize="16" Margin="0,5"/>

</StackPanel>
```

- `MaxLength="7"` verhindert Eingaben länger als 7 Zeichen
- `Click="RatenButton_Click"` verknüpft den Button mit der Methode im Code-Behind

---

## Schritt 4 – Validierung (7 Zeichen) und Serverüberprüfung

**Datei:** `MainWindow.xaml.cs` – Methode `RatenButton_Click`

```csharp
private void RatenButton_Click(object sender, RoutedEventArgs e)
{
    var guess = GuessInput.Text.ToUpper().Trim();

    // Validierung: muss genau 7 Zeichen haben
    if (guess.Length != 7)
    {
        MessageBox.Show("Das Wort muss genau 7 Zeichen haben!", "Fehler");
        return;
    }

    // Guess als XML zum Server schicken
    _writer.WriteLine($"<Guess>{guess}</Guess>");

    // Ergebnis als XML empfangen
    var resultXml = _reader.ReadLine();               // "<Result>G,X,Y,G,X,X,G</Result>"
    var colors = XDocument.Parse(resultXml).Root.Value.Split(',');

    // Grid einfärben (Schritt 5)
    ColorRow(_currentRow, guess, colors);
    _currentRow++;

    // Gewonnen oder verloren prüfen (Schritt 6)
    if (colors.All(c => c == "G"))
        EndGame(true);
    else if (_currentRow >= 6)
        EndGame(false);

    GuessInput.Clear();
}
```

### Serverseite: Guess überprüfen

Der Server vergleicht Buchstabe für Buchstabe und antwortet mit XML:

```csharp
string CheckGuess(string guess, string word)
{
    var results = new char[word.Length];
    for (int i = 0; i < word.Length; i++)
    {
        if (guess[i] == word[i])        results[i] = 'G'; // Grün  = richtig
        else if (word.Contains(guess[i])) results[i] = 'Y'; // Gelb  = falsche Stelle
        else                             results[i] = 'X'; // Grau  = nicht im Wort
    }
    return $"<Result>{string.Join(",", results)}</Result>";
}
```

Beispiel: Wort = `HOLIDAY`, Guess = `HALLWAY`  
→ Antwort: `<Result>G,X,G,G,X,Y,G</Result>`

---

## Schritt 5 – UniformGrid mit ItemsControl (6 Zeilen × 7 Spalten)

### 5.1 Das Cell-Model (`Models/Cell.cs`)

Jede Zelle im Grid hat einen Buchstaben und eine Farbe. `INotifyPropertyChanged` sorgt dafür, dass die GUI automatisch aktualisiert wird:

```csharp
public class Cell : INotifyPropertyChanged
{
    private string _letter = "";
    private Brush _color = Brushes.LightGray;

    public string Letter
    {
        get => _letter;
        set { _letter = value; OnPropertyChanged(); }
    }

    public Brush Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
```

### 5.2 ItemsControl mit UniformGrid (`MainWindow.xaml`)

```xml
<ItemsControl x:Name="GuessGrid" Margin="0,15">

    <!-- UniformGrid als Layout: 6 Zeilen, 7 Spalten -->
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <UniformGrid Rows="6" Columns="7"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>

    <!-- Template für jede Zelle -->
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Background="{Binding Color}" Width="40" Height="40" Margin="2">
                <TextBlock Text="{Binding Letter}"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center"
                           FontSize="18" FontWeight="Bold"/>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>

</ItemsControl>
```

- `{Binding Color}` → bindet an `Cell.Color`
- `{Binding Letter}` → bindet an `Cell.Letter`

### 5.3 Zellen initialisieren und einfärben

Im Konstruktor werden 42 leere Zellen erstellt (6 × 7):

```csharp
ObservableCollection<Cell> _cells = new();

// Im Konstruktor:
for (int i = 0; i < 42; i++) _cells.Add(new Cell());
GuessGrid.ItemsSource = _cells;
```

Nach jedem Guess wird die entsprechende Zeile eingefärbt:

```csharp
void ColorRow(int row, string guess, string[] colors)
{
    for (int i = 0; i < 7; i++)
    {
        _cells[row * 7 + i].Letter = guess[i].ToString();
        _cells[row * 7 + i].Color = colors[i] switch
        {
            "G" => Brushes.Green,   // Richtige Position
            "Y" => Brushes.Yellow,  // Im Wort, falsche Stelle
            _   => Brushes.Gray     // Nicht im Wort
        };
    }
}
```

---

## Schritt 6 – Versuche hochzählen

Der aktuelle Versuch wird mit `_currentRow` verfolgt:

```csharp
int _currentRow = 0;
```

Nach jedem Guess wird er erhöht:

```csharp
ColorRow(_currentRow, guess, colors);
_currentRow++;
```

- Bei Sieg: `_stats[_currentRow - 1]++` (Versuch 1–6)
- Bei Niederlage (nach 6 Versuchen): `_stats[6]++`

---

## Schritt 7 – Fenster halbieren und Statistik anzeigen

### 7.1 XAML: Zweispalten-Layout mit ausgeblendeter Statistik

Das Fenster hat von Anfang an zwei Spalten. Die rechte Spalte hat zunächst `Width="0"`:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition x:Name="StatsColumn" Width="0"/>  <!-- Versteckt -->
    </Grid.ColumnDefinitions>

    <!-- Linke Seite: Spiel -->
    <StackPanel Grid.Column="0" ...>
        <!-- TextBox, Button, Grid -->
    </StackPanel>

    <!-- Rechte Seite: Statistik (anfangs unsichtbar) -->
    <StackPanel x:Name="StatsPanel" Grid.Column="1" Visibility="Collapsed" Margin="20,10">
        <Label x:Name="WordLabel" FontSize="16" FontWeight="Bold" Margin="0,0,0,10"/>
        <Label x:Name="Stat1"/>
        <Label x:Name="Stat2"/>
        <Label x:Name="Stat3"/>
        <Label x:Name="Stat4"/>
        <Label x:Name="Stat5"/>
        <Label x:Name="Stat6"/>
        <Label x:Name="StatNever"/>
    </StackPanel>
</Grid>
```

### 7.2 Code-Behind: Statistik einblenden (`EndGame`)

```csharp
int[] _stats = new int[7]; // [0-5] = Versuch 1-6 erraten, [6] = nie erraten

void EndGame(bool won)
{
    // Statistik aktualisieren
    if (won) _stats[_currentRow - 1]++;
    else     _stats[6]++;

    // Eingabe sperren
    GuessInput.IsEnabled = false;
    RatenButton.IsEnabled = false;

    // Fenster verbreitern (verdoppeln)
    this.Width *= 2;

    // Rechte Spalte aufmachen
    var grid = (Grid)this.Content;
    grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

    // Statistik einblenden und befüllen
    StatsPanel.Visibility = Visibility.Visible;
    WordLabel.Content = $"Das Wort war: {_serverWord}";
    Stat1.Content    = $"Beim 1. Versuch: {_stats[0]}";
    Stat2.Content    = $"Beim 2. Versuch: {_stats[1]}";
    Stat3.Content    = $"Beim 3. Versuch: {_stats[2]}";
    Stat4.Content    = $"Beim 4. Versuch: {_stats[3]}";
    Stat5.Content    = $"Beim 5. Versuch: {_stats[4]}";
    Stat6.Content    = $"Beim 6. Versuch: {_stats[5]}";
    StatNever.Content = $"Nie erraten:    {_stats[6]}";
}
```

---

## Kommunikationsablauf (Übersicht)

```
Client (WPF)                          Server (Console)
────────────────────────────────────────────────────────
Verbindung aufbauen         ──►       AcceptTcpClient()
                            ◄──       <Word>HOLIDAY</Word>

Guess eingeben + Raten      ──►       <Guess>HALLWAY</Guess>
                            ◄──       <Result>G,X,G,G,X,Y,G</Result>

(weitere Versuche...)

Spiel endet                           Verbindung bleibt offen
```

---

## Häufige Fehler

| Fehler | Ursache | Lösung |
|---|---|---|
| Server startet nicht | Port 12345 belegt | Anderen Prozess beenden oder Port ändern |
| Keine Wörter in DB | `wordle.db` leer | Wörter manuell einfügen oder Seed-Methode schreiben |
| `NullReferenceException` beim Parsen | XML-Format falsch | `Console.WriteLine(xml)` zur Diagnose |
| Grid bleibt grau | `ObservableCollection` nicht gebunden | `GuessGrid.ItemsSource = _cells` im Konstruktor prüfen |
