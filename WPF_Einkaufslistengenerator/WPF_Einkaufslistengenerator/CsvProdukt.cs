using LINQtoCSV;

namespace WPF_Einkaufslistengenerator;

// Repräsentiert eine Zeile in der CSV-Datei (Format: Gruppe;Produkt, keine Headerzeile).
// FieldIndex gibt die Spaltennummer an (1-basiert).
public class CsvProdukt
{
    [CsvColumn(FieldIndex = 1)]
    public string Gruppe { get; set; } = "";

    [CsvColumn(FieldIndex = 2)]
    public string Produkt { get; set; } = "";
}
