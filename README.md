# 🪟 WPF Cheat Sheet

> Kompakter Spickzettel für WPF / .NET 8 / C#. Alle Beispiele sind lauffähig und direkt einsetzbar.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat&logo=csharp)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D6?style=flat&logo=windows)

---

## 📚 Inhaltsverzeichnis

1. [SQLite Datenbank anbinden](#1--sqlite-datenbank-anbinden)
2. [Server / Client über TCP](#2--server--client-über-tcp)
3. [Bindings in der GUI](#3--bindings-in-der-gui)
4. [Wichtigste GUI-Elemente](#4--wichtigste-gui-elemente)
5. [Commands (Button-Klicks)](#5--commands-button-klicks-ohne-code-behind)
6. [LINQ + CSV verarbeiten](#6--linq--csv-einlesen-und-verarbeiten)
7. [Projektstruktur & Datei-Zuständigkeiten](#7--projektstruktur--datei-zuständigkeiten)
8. [XAML ↔ C# Verbindungen](#8--xaml--c-verbindungen)
9. [ObservableCollection](#9--observablecollection)
10. [Quick Reference](#-quick-reference)

---

## 1. 🗄️ SQLite Datenbank anbinden

**NuGet Packages:**

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

**Model:**

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

**DbContext:**

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=app.db");
}
```

**Verwenden (CRUD):**

```csharp
using var db = new AppDbContext();
db.Database.EnsureCreated();           // erstellt DB falls nicht existiert

// CREATE
db.Products.Add(new Product { Name = "Apfel", Price = 1.20m });
db.SaveChanges();

// READ
var alle = db.Products.ToList();
var einer = db.Products.FirstOrDefault(p => p.Id == 1);

// UPDATE
einer.Price = 2.50m;
db.SaveChanges();

// DELETE
db.Products.Remove(einer);
db.SaveChanges();
```

**Alternative ohne EF (rohes ADO.NET mit `Microsoft.Data.Sqlite`):**

```csharp
using var conn = new SqliteConnection("Data Source=app.db");
conn.Open();
var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT Id, Name FROM Products WHERE Price > $p";
cmd.Parameters.AddWithValue("$p", 1.0);
using var reader = cmd.ExecuteReader();
while (reader.Read())
    Console.WriteLine($"{reader.GetInt32(0)}: {reader.GetString(1)}");
```

---

## 2. 🌐 Server / Client über TCP

### Server

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();
Console.WriteLine("Server läuft auf Port 5000...");

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(async () =>
    {
        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int read = await stream.ReadAsync(buffer);
        string msg = Encoding.UTF8.GetString(buffer, 0, read);
        Console.WriteLine($"Empfangen: {msg}");

        byte[] answer = Encoding.UTF8.GetBytes("Hallo vom Server");
        await stream.WriteAsync(answer);
        client.Close();
    });
}
```

### Client (als Service-Klasse in WPF)

**Services/TcpClientService.cs**

```csharp
using System.Net.Sockets;
using System.Text;

namespace MeineApp.Services;

public class TcpClientService
{
    private readonly string _host;
    private readonly int _port;

    public TcpClientService(string host = "127.0.0.1", int port = 5000)
    {
        _host = host;
        _port = port;
    }

    public async Task<string> SendMessageAsync(string message)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_host, _port);

        NetworkStream stream = client.GetStream();
        byte[] data = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(data);

        byte[] buffer = new byte[1024];
        int read = await stream.ReadAsync(buffer);
        return Encoding.UTF8.GetString(buffer, 0, read);
    }
}
```

**MainWindow.xaml.cs**

```csharp
// Feld außerhalb aller Methoden, aber INNERHALB der Klasse
public partial class MainWindow : Window
{
    private readonly TcpClientService _tcp = new(); // ← Feld der Klasse

    public MainWindow()
    {
        InitializeComponent(); // ← Konstruktor bleibt sauber
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        string eingabe = InputTextBox.Text;
        string antwort = await _tcp.SendMessageAsync(eingabe);
        ResultTextBlock.Text = antwort;
    }
}
```

> [!TIP]
> **Merksatz:** Server = `TcpListener` + `AcceptTcpClientAsync`. Client = `TcpClient` + `ConnectAsync`. Beide reden über `NetworkStream`.

---

## 3. 🔗 Bindings in der GUI

### ViewModel mit `INotifyPropertyChanged`

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class MainViewModel : INotifyPropertyChanged
{
    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
```

### DataContext setzen (Code-Behind)

```csharp
public MainWindow()
{
    InitializeComponent();
    DataContext = new MainViewModel();
}
```

### XAML Bindings

```xml
<!-- Einfaches Binding -->
<TextBox Text="{Binding Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<TextBlock Text="{Binding Name}" />

<!-- Binding an Element-Eigenschaft -->
<Slider x:Name="mySlider" Minimum="0" Maximum="100" />
<TextBlock Text="{Binding ElementName=mySlider, Path=Value}" />

<!-- Liste binden -->
<ListBox ItemsSource="{Binding Products}"
         DisplayMemberPath="Name"
         SelectedItem="{Binding SelectedProduct}" />
```

### Binding-Modi

| Mode | Bedeutung |
|---|---|
| `OneWay` | Source → UI |
| `TwoWay` | Source ↔ UI (für TextBox typisch) |
| `OneTime` | Einmal beim Laden |
| `OneWayToSource` | UI → Source |

---

## 4. 🎨 Wichtigste GUI-Elemente

```xml
<Window x:Class="App.MainWindow" Title="Demo" Height="400" Width="600">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Label + TextBox -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <Label Content="Name:" />
            <TextBox Width="200" Text="{Binding Name, Mode=TwoWay}" />
        </StackPanel>

        <!-- ListBox / ComboBox -->
        <ListBox Grid.Row="1" ItemsSource="{Binding Products}"
                 SelectedItem="{Binding SelectedProduct}" Margin="10"/>

        <!-- Buttons + CheckBox + RadioButton -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="10">
            <Button Content="Speichern" Command="{Binding SaveCommand}" Width="100"/>
            <CheckBox Content="Aktiv" IsChecked="{Binding IsActive}" Margin="10,0"/>
            <RadioButton Content="A" GroupName="Auswahl"/>
            <RadioButton Content="B" GroupName="Auswahl"/>
        </StackPanel>
    </Grid>
</Window>
```

### Layout-Container

| Container | Verwendung |
|---|---|
| `Grid` | Zeilen + Spalten (mächtigster Container) |
| `StackPanel` | Elemente in Reihe (horizontal/vertikal) |
| `DockPanel` | Andocken an Rändern |
| `WrapPanel` | Umbrechen wie Text |
| `Canvas` | Absolute Positionen |

---

## 5. ⚡ Commands (Button-Klicks ohne Code-Behind)

### RelayCommand (Standard-Implementation)

```csharp
using System.Windows.Input;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
```

### Im ViewModel

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    public ICommand SaveCommand { get; }

    public MainViewModel()
    {
        SaveCommand = new RelayCommand(Save, () => !string.IsNullOrEmpty(Name));
    }

    private void Save()
    {
        MessageBox.Show($"Gespeichert: {Name}");
    }

    // ... Name + INotifyPropertyChanged wie oben
}
```

### XAML

```xml
<Button Content="Speichern" Command="{Binding SaveCommand}" />

<!-- mit Parameter -->
<Button Content="Löschen"
        Command="{Binding DeleteCommand}"
        CommandParameter="{Binding SelectedProduct}" />
```

> [!NOTE]
> Command statt Click-Event = saubere Trennung zwischen GUI und Logik. `CanExecute` deaktiviert automatisch den Button.

---

## 6. 📊 LINQ + CSV einlesen und verarbeiten

### CSV einlesen

```csharp
using System.Globalization;

var lines = File.ReadAllLines("products.csv");

var products = lines
    .Skip(1)                              // Header überspringen
    .Select(line => line.Split(';'))
    .Select(parts => new Product
    {
        Id = int.Parse(parts[0]),
        Name = parts[1],
        Price = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
        Category = parts[3]
    })
    .ToList();
```

### LINQ-Operationen (das Wichtigste)

```csharp
// FILTER
var teuer = products.Where(p => p.Price > 1.50m).ToList();

// SORT
var sortiert = products.OrderBy(p => p.Price).ToList();
var absteigend = products.OrderByDescending(p => p.Name).ToList();

// PROJECT (auswählen)
var namen = products.Select(p => p.Name).ToList();

// SINGLE / FIRST
var ersterApfel = products.FirstOrDefault(p => p.Name == "Apfel");

// AGGREGATE
decimal summe = products.Sum(p => p.Price);
decimal schnitt = products.Average(p => p.Price);
int anzahl = products.Count(p => p.Category == "Obst");
decimal max = products.Max(p => p.Price);

// GROUP BY
var nachKategorie = products
    .GroupBy(p => p.Category)
    .Select(g => new
    {
        Kategorie = g.Key,
        Anzahl = g.Count(),
        Gesamtpreis = g.Sum(p => p.Price)
    })
    .ToList();

// ANY / ALL
bool gibtTeure = products.Any(p => p.Price > 5);
bool alleBilig = products.All(p => p.Price < 10);

// DISTINCT
var kategorien = products.Select(p => p.Category).Distinct().ToList();
```

---

## 7. 📁 Projektstruktur & Datei-Zuständigkeiten

```
MeineApp/
├── Models/
│   └── Product.cs              ← nur Datenhaltung, keine Logik
├── Services/
│   └── TcpClientService.cs     ← Netzwerk, Datenbank, externe APIs
├── ViewModels/
│   └── MainViewModel.cs        ← Logik, Commands, INotifyPropertyChanged
├── Views/  (optional)
│   └── MainWindow.xaml         ← nur UI-Layout, kein C#-Code
│   └── MainWindow.xaml.cs      ← nur Event-Handler, DataContext setzen
└── App.xaml
```

### Was gehört wo rein?

| Datei | Gehört rein | Gehört NICHT rein |
|---|---|---|
| `Model/*.cs` | Properties, keine Logik | SQL, HTTP, Berechnung |
| `Services/*.cs` | TCP, DB, HTTP, Datei-IO | UI-Elemente, Window |
| `ViewModel/*.cs` | Commands, Properties, Logik | `new Window()`, direkte UI-Refs |
| `MainWindow.xaml` | UI-Elemente, Bindings, Layout | C#-Code |
| `MainWindow.xaml.cs` | `DataContext`, Event-Handler | Geschäftslogik, SQL |

### Felder in der Klasse: wo genau?

```csharp
namespace MeineApp
{
    public partial class MainWindow : Window   // ← Klasse beginnt
    {
        // ✅ Felder hier – außerhalb von Methoden, INNERHALB der Klasse
        private readonly TcpClientService _tcp = new();
        private readonly MainViewModel _vm = new();

        public MainWindow()                    // ← Konstruktor
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void Button_Click(...)         // ← Methoden danach
        {
            // kein Feld hier definieren!
        }
    }                                          // ← Klasse endet
}
```

> [!WARNING]
> Felder **außerhalb** der Klasse (direkt im Namespace) → Compilerfehler CS0116!

---

## 8. 🔌 XAML ↔ C# Verbindungen

### Variante A: Name-Zugriff (einfach, direkt)

Das Element bekommt in XAML einen `Name` → dann ist es direkt in CS verfügbar.

**XAML:**
```xml
<TextBox Name="InputTextBox" />
<TextBlock Name="ResultTextBlock" />
<Button Name="SendButton" Content="Senden" Click="SendButton_Click" />
```

**CS (MainWindow.xaml.cs):**
```csharp
// Lesen
string text = InputTextBox.Text;

// Schreiben
ResultTextBlock.Text = "Antwort vom Server";

// Event-Handler muss exakt so heißen wie Click="..."
private async void SendButton_Click(object sender, RoutedEventArgs e)
{
    string eingabe = InputTextBox.Text;
    string antwort = await _tcp.SendMessageAsync(eingabe);
    ResultTextBlock.Text = antwort;
}
```

> [!IMPORTANT]
> Der Name in XAML (`Name="ResultTextBlock"`) muss **exakt** mit dem Namen in CS (`ResultTextBlock.Text`) übereinstimmen – Groß-/Kleinschreibung zählt!

### Variante B: Binding (sauber, empfohlen)

Kein direkter Name-Zugriff nötig – das ViewModel verbindet sich automatisch.

**XAML:**
```xml
<TextBox Text="{Binding InputText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<TextBlock Text="{Binding ResultText}" />
<Button Content="Senden" Command="{Binding SendCommand}" />
```

**ViewModel:**
```csharp
private string _inputText = "";
public string InputText
{
    get => _inputText;
    set { _inputText = value; OnPropertyChanged(); }
}

private string _resultText = "";
public string ResultText
{
    get => _resultText;
    set { _resultText = value; OnPropertyChanged(); }
}
```

**MainWindow.xaml.cs:**
```csharp
public MainWindow()
{
    InitializeComponent();
    DataContext = new MainViewModel(); // ← einzige Verbindung nötig
}
```

### Welche Input-Elemente gibt es?

| XAML-Element | Zweck | Property für Wert |
|---|---|---|
| `TextBox` | Texteingabe vom User | `.Text` |
| `TextBlock` | Text anzeigen (read-only) | `.Text` |
| `CheckBox` | Boolean an/aus | `.IsChecked` |
| `ComboBox` | Dropdown-Auswahl | `.SelectedItem` / `.Text` |
| `ListBox` | Liste mit Auswahl | `.SelectedItem` |
| `Slider` | Zahl per Schieberegler | `.Value` |
| `PasswordBox` | Passwort (kein Binding!) | `.Password` |
| `Label` | Beschriftung (kein Input) | `.Content` |

### Event-Handler verbinden

```xml
<!-- In XAML den Event angeben -->
<Button Click="MeinButton_Click" />
<TextBox TextChanged="MeinTextBox_TextChanged" />
<ListBox SelectionChanged="MeineListe_SelectionChanged" />
```

```csharp
// In CS exakt gleiche Signatur verwenden
private void MeinButton_Click(object sender, RoutedEventArgs e) { }
private void MeinTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
private void MeineListe_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
```

---

## 9. 📋 ObservableCollection

### Warum nicht `List<T>`?

| | `List<T>` | `ObservableCollection<T>` |
|---|---|---|
| GUI aktualisiert sich bei Add/Remove | ❌ Nein | ✅ Ja, automatisch |
| Für statische Daten | ✅ OK | ✅ OK |
| Für dynamische Listen in GUI | ❌ Nicht verwenden | ✅ Pflicht |

### Grundverwendung

```csharp
using System.Collections.ObjectModel;

public class MainViewModel : INotifyPropertyChanged
{
    // ✅ ObservableCollection als Property
    public ObservableCollection<Product> Products { get; } = new();

    public MainViewModel()
    {
        // Daten laden beim Start
        Products.Add(new Product { Name = "Apfel", Price = 1.20m });
        Products.Add(new Product { Name = "Brot",  Price = 2.50m });
    }

    // Element hinzufügen → GUI aktualisiert sich sofort
    public void AddProduct(Product p) => Products.Add(p);

    // Element entfernen → GUI aktualisiert sich sofort
    public void RemoveProduct(Product p) => Products.Remove(p);
}
```

### XAML binden

```xml
<ListBox ItemsSource="{Binding Products}"
         DisplayMemberPath="Name"
         SelectedItem="{Binding SelectedProduct}" />
```

### Aus Datenbank laden in ObservableCollection

```csharp
public MainViewModel()
{
    using var db = new AppDbContext();
    db.Database.EnsureCreated();

    var alleProdukte = db.Products.ToList();
    foreach (var p in alleProdukte)
        Products.Add(p);
}
```

### Aus CSV laden in ObservableCollection

```csharp
public MainViewModel()
{
    var lines = File.ReadAllLines("products.csv").Skip(1);
    foreach (var line in lines)
    {
        var parts = line.Split(';');
        Products.Add(new Product
        {
            Id    = int.Parse(parts[0]),
            Name  = parts[1],
            Price = decimal.Parse(parts[2], CultureInfo.InvariantCulture)
        });
    }
}
```

### Ausgewähltes Element verwenden

```csharp
private Product? _selectedProduct;
public Product? SelectedProduct
{
    get => _selectedProduct;
    set { _selectedProduct = value; OnPropertyChanged(); }
}
```

```xml
<ListBox ItemsSource="{Binding Products}"
         DisplayMemberPath="Name"
         SelectedItem="{Binding SelectedProduct}" />

<!-- Details des ausgewählten Elements anzeigen -->
<TextBlock Text="{Binding SelectedProduct.Name}" />
<TextBlock Text="{Binding SelectedProduct.Price}" />
```

---

## 🎯 Quick Reference

| Aufgabe | Schlüssel-API |
|---|---|
| SQLite + EF | `DbContext` + `UseSqlite` + `EnsureCreated` |
| TCP Server | `TcpListener.Start()` + `AcceptTcpClientAsync` |
| TCP Client | `TcpClient.ConnectAsync` + `NetworkStream` |
| Binding aktivieren | `INotifyPropertyChanged` + `DataContext` |
| Liste binden | `ObservableCollection<T>` + `ItemsSource` |
| Button-Logik | `ICommand` + `RelayCommand` |
| CSV lesen | `File.ReadAllLines` + `Split` + `Select` |
| LINQ-Kette | `Where → OrderBy → Select → ToList` |
| UI-Element lesen | `MeinTextBox.Text` (per Name) |
| UI-Element schreiben | `MeinTextBlock.Text = "..."` (per Name) |
| Async Event-Handler | `private async void Button_Click(...)` |

---

## ⚠️ Goldene Regeln

> [!WARNING]
> Diese Fehler kosten im Test am meisten Punkte:

1. **`ObservableCollection<T>` statt `List<T>`** für GUI-Listen → automatisches Update.
2. **`UpdateSourceTrigger=PropertyChanged`** bei TextBox, sonst erst bei Fokus-Verlust.
3. Bei CSV immer **`CultureInfo.InvariantCulture`** für Dezimalzahlen → sonst Komma/Punkt-Chaos.
4. TCP: immer **`using` + `async`**, sonst hängt der Port.
5. EF: **`EnsureCreated()`** für Tests, **`Migrations`** für Produktion.
6. Felder gehören **innerhalb der Klasse**, nicht in den Namespace → CS0116!
7. `Name="..."` in XAML und der CS-Name müssen **exakt übereinstimmen**.
8. `TextBox` = Eingabe, `TextBlock` = Anzeige – nie verwechseln.
9. `DataContext = new MainViewModel()` im **Konstruktor** von MainWindow setzen.
10. UI-Thread nie blockieren → immer **`async/await`** bei langsamen Operationen.

---

## 📖 Lizenz

Frei nutzbar für Lernzwecke.
