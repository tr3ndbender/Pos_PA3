using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;

namespace WPF_WordPad
{
    public partial class MainWindow
    {
        private string? _currentFile = null;

        // Verhindert, dass SelectionChanged die Steuerelemente in einer Endlosschleife aktualisiert
        private bool _updatingControls = false;

        public MainWindow()
        {
            InitializeComponent();

            // Schriftarten laden und alphabetisch sortieren
            fontFamilyCombo.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
            fontFamilyCombo.SelectedItem = Fonts.SystemFontFamilies.FirstOrDefault(f => f.Source == "Segoe UI");
        }

        // =====================================================================
        //  DATEI-BEFEHLE
        // =====================================================================

        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            richTextBox.Document = new FlowDocument();
            _currentFile = null;
            Title = "WPF WordPad - Unbenannt";
            statusText.Text = "Neues Dokument erstellt.";
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "XAML Dateien (*.xaml)|*.xaml|Alle Dateien (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                _currentFile = dlg.FileName;
                using var stream = File.OpenRead(_currentFile);
                var range = new TextRange(
                    richTextBox.Document.ContentStart,
                    richTextBox.Document.ContentEnd);
                range.Load(stream, DataFormats.Xaml);

                Title = $"WPF WordPad - {Path.GetFileName(_currentFile)}";
                statusText.Text = $"Geladen: {_currentFile}";
            }
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Wenn bereits eine Datei geöffnet ist, direkt speichern
            if (_currentFile != null)
            {
                SaveToFile(_currentFile);
                statusText.Text = $"Gespeichert: {_currentFile}";
            }
            else
            {
                PerformSaveAs();
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e) => PerformSaveAs();

        private void PerformSaveAs()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "XAML Dateien (*.xaml)|*.xaml|Alle Dateien (*.*)|*.*",
                DefaultExt = ".xaml"
            };

            if (dlg.ShowDialog() == true)
            {
                _currentFile = dlg.FileName;
                SaveToFile(_currentFile);
                Title = $"WPF WordPad - {Path.GetFileName(_currentFile)}";
                statusText.Text = $"Gespeichert: {_currentFile}";
            }
        }

        // Speichert den RichTextBox-Inhalt als XAML-Datei
        private void SaveToFile(string path)
        {
            using var stream = File.Open(path, FileMode.Create);
            var range = new TextRange(
                richTextBox.Document.ContentStart,
                richTextBox.Document.ContentEnd);
            range.Save(stream, DataFormats.Xaml);
        }

        private void Exit_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();

        // =====================================================================
        //  SCHRIFT-STEUERELEMENTE (Extended WPF Toolkit + ComboBox)
        // =====================================================================

        private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingControls) return;
            if (fontFamilyCombo.SelectedItem is FontFamily family)
            {
                richTextBox.Selection.ApplyPropertyValue(Inline.FontFamilyProperty, family);
                richTextBox.Focus();
            }
        }

        // IntegerUpDown aus dem Extended WPF Toolkit
        private void FontSizeUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_updatingControls || fontSizeUpDown?.Value == null || richTextBox == null) return;
            double size = (double)(int)fontSizeUpDown.Value;
            richTextBox.Selection.ApplyPropertyValue(Inline.FontSizeProperty, size);
        }

        // ColorPicker aus dem Extended WPF Toolkit
        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (_updatingControls || !e.NewValue.HasValue || richTextBox == null) return;
            richTextBox.Selection.ApplyPropertyValue(
                Inline.ForegroundProperty,
                new SolidColorBrush(e.NewValue.Value));
        }

        // =====================================================================
        //  SELECTION CHANGED – Steuerelemente synchronisieren
        // =====================================================================

        // Wenn der Cursor bewegt wird, werden FontFamily/FontSize im Ribbon aktualisiert
        private void RichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            _updatingControls = true;
            try
            {
                // Schriftart synchronisieren
                if (richTextBox.Selection.GetPropertyValue(Inline.FontFamilyProperty) is FontFamily fontFamily)
                    fontFamilyCombo.SelectedItem = fontFamily;

                // Schriftgröße synchronisieren
                if (richTextBox.Selection.GetPropertyValue(Inline.FontSizeProperty) is double fontSize)
                    fontSizeUpDown.Value = (int)fontSize;
            }
            finally
            {
                _updatingControls = false;
            }
        }
    }
}
