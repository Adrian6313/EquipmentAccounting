using System.Windows;
using System.Windows.Controls;

namespace EquipmentAccounting.App;

/// <summary>Вспомогательное дочернее окно для просмотра журналов оборудования.</summary>
internal sealed class HistoryWindow : Window
{
    public HistoryWindow(string title, object rows)
    {
        Title = title;
        Width = 900;
        Height = 480;
        MinWidth = 620;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var layout = new DockPanel { Margin = new Thickness(16) };
        var close = new Button
        {
            Content = "Закрыть",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 10, 0, 0),
            IsCancel = true
        };
        DockPanel.SetDock(close, Dock.Bottom);
        layout.Children.Add(close);
        layout.Children.Add(new DataGrid
        {
            ItemsSource = rows as System.Collections.IEnumerable,
            AutoGenerateColumns = true,
            IsReadOnly = true,
            CanUserAddRows = false,
            AlternatingRowBackground = System.Windows.Media.Brushes.AliceBlue
        });
        Content = layout;
    }
}
