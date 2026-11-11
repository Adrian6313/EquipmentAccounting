using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using EquipmentAccounting.Core.Models;

namespace EquipmentAccounting.App;

internal sealed class InventoryWindow : Window
{
    private readonly IReadOnlyList<InventoryRow> _rows;
    public InventoryWindow(IReadOnlyList<InventoryRow> rows)
    {
        _rows = rows; Title = "Инвентаризационная ведомость"; Width = 1050; Height = 600; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var layout = new DockPanel { Margin = new Thickness(18) };
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var title = new TextBlock { Text = $"Инвентаризационная ведомость · записей: {rows.Count}", FontSize = 20, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        DockPanel.SetDock(title, Dock.Left); header.Children.Add(title);
        var export = new Button { Content = "Экспорт CSV", Padding = new Thickness(14, 8, 14, 8), HorizontalAlignment = HorizontalAlignment.Right }; export.Click += Export_Click; header.Children.Add(export);
        DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
        var grid = new DataGrid { ItemsSource = rows, IsReadOnly = true, AutoGenerateColumns = true, CanUserAddRows = false, AlternatingRowBackground = System.Windows.Media.Brushes.AliceBlue };
        layout.Children.Add(grid); Content = layout;
    }
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"Инвентаризация-{DateTime.Today:yyyy-MM-dd}.csv" };
        if (dialog.ShowDialog(this) != true) return;
        var lines = new List<string> { "ID;Название;Инвентарный номер;Производитель;Дата приобретения;Стоимость;Подразделение;Последний осмотр" };
        lines.AddRange(_rows.Select(r => string.Join(';', new[] { r.Id.ToString(), r.Name, r.InventoryNumber, r.Manufacturer, r.PurchaseDate.ToString("dd.MM.yyyy"), r.Cost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), r.Department, r.LastInspectionDate?.ToString("dd.MM.yyyy") ?? "" }.Select(Escape))));
        File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(true));
        MessageBox.Show(this, "Ведомость сохранена.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
