[README.md](https://github.com/user-attachments/files/26574538/README.md)
# 🪟 WPF Cheat Sheet

> Kompakter Spickzettel für WPF / .NET 8 / C#. Alle Beispiele sind lauffähig und direkt einsetzbar.



---

## 📚 Inhaltsverzeichnis

1. [SQLite Datenbank anbinden](#1--sqlite-datenbank-anbinden)
2. [Server / Client über TCP](#2--server--client-über-tcp)
3. [Bindings in der GUI](#3--bindings-in-der-gui)
4. [Wichtigste GUI-Elemente](#4--wichtigste-gui-elemente)
5. [Commands (Button-Klicks)](#5--commands-button-klicks-ohne-code-behind)
6. [LINQ + CSV verarbeiten](#6--linq--csv-einlesen-und-verarbeiten)
7. [Quick Reference](#-quick-reference)

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

### Client

```csharp
using System.Net.Sockets;
using System.Text;

using var client = new TcpClient();
await client.ConnectAsync("127.0.0.1", 5000);

NetworkStream stream = client.GetStream();
byte[] data = Encoding.UTF8.GetBytes("Hallo Server");
await stream.WriteAsync(data);

byte[] buffer = new byte[1024];
int read = await stream.ReadAsync(buffer);
Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, read));
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

// CSV-Datei: products.csv
// Id;Name;Price;Category
// 1;Apfel;1.20;Obst
// 2;Brot;2.50;Backware
// 3;Milch;1.10;Getränk

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

### Komplettes Beispiel: CSV → filtern → ListBox binden

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Product> Products { get; } = new();

    public MainViewModel()
    {
        var lines = File.ReadAllLines("products.csv");
        var teure = lines.Skip(1)
            .Select(l => l.Split(';'))
            .Where(parts => decimal.Parse(parts[2], CultureInfo.InvariantCulture) > 1m)
            .Select(parts => new Product
            {
                Id = int.Parse(parts[0]),
                Name = parts[1],
                Price = decimal.Parse(parts[2], CultureInfo.InvariantCulture)
            });

        foreach (var p in teure) Products.Add(p);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

```xml
<ListBox ItemsSource="{Binding Products}" DisplayMemberPath="Name"/>
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

---

## ⚠️ Goldene Regeln

> [!WARNING]
> Diese Fehler kosten im Test am meisten Punkte:

1. **`ObservableCollection<T>` statt `List<T>`** für GUI-Listen → automatisches Update.
2. **`UpdateSourceTrigger=PropertyChanged`** bei TextBox, sonst erst bei Fokus-Verlust.
3. Bei CSV immer **`CultureInfo.InvariantCulture`** für Dezimalzahlen → sonst Komma/Punkt-Chaos.
4. TCP: immer **`using` + `async`**, sonst hängt der Port.
5. EF: **`EnsureCreated()`** für Tests, **`Migrations`** für Produktion.

---

## 📖 Lizenz

Frei nutzbar für Lernzwecke.
