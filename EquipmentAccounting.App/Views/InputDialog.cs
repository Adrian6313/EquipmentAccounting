using System.Windows;
using System.Windows.Controls;

namespace EquipmentAccounting.App;

internal static class InputDialog
{
    public static string? Show(Window owner, string title, string prompt, string initialValue = "")
    {
        var window = new Window { Title = title, Owner = owner, Width = 400, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) });
        var box = new TextBox { Text = initialValue, Padding = new Thickness(8), MinHeight = 34 }; panel.Children.Add(box);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var ok = new Button { Content = "Сохранить", IsDefault = true, Padding = new Thickness(12, 7, 12, 7), MinWidth = 90 }; ok.Click += (_, _) => window.DialogResult = true;
        var cancel = new Button { Content = "Отмена", IsCancel = true, Padding = new Thickness(12, 7, 12, 7), MinWidth = 80, Margin = new Thickness(8, 0, 0, 0) };
        buttons.Children.Add(ok); buttons.Children.Add(cancel); panel.Children.Add(buttons); window.Content = panel; window.Loaded += (_, _) => { box.Focus(); box.SelectAll(); };
        return window.ShowDialog() == true ? box.Text.Trim() : null;
    }
}
