using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using EquipmentAccounting.Core.Models;

namespace EquipmentAccounting.App;

internal sealed class EquipmentDialog : Window
{
    private readonly TextBox _name = Field();
    private readonly TextBox _number = Field();
    private readonly TextBox _manufacturer = Field();
    private readonly DatePicker _purchaseDate = new() { SelectedDate = DateTime.Today };
    private readonly TextBox _cost = Field();
    private readonly ComboBox _department = new() { IsEditable = true, ItemsSource = new[] { "ИТ", "Бухгалтерия", "Производство", "Склад" } };
    private readonly DatePicker _inspectionDate = new();
    private readonly TextBox _repairSummary = Field();
    private readonly int _id;
    public Equipment? Result { get; private set; }

    public EquipmentDialog(Equipment? item = null)
    {
        _id = item?.Id ?? 0;
        Title = item is null ? "Добавить оборудование" : "Редактировать оборудование";
        Width = 500; SizeToContent = SizeToContent.Height; WindowStartupLocation = WindowStartupLocation.CenterOwner; ResizeMode = ResizeMode.NoResize;
        if (item is not null)
        {
            _name.Text = item.Name; _number.Text = item.InventoryNumber; _manufacturer.Text = item.Manufacturer;
            _purchaseDate.SelectedDate = item.PurchaseDate; _cost.Text = item.Cost.ToString(CultureInfo.CurrentCulture);
            _department.Text = item.Department; _inspectionDate.SelectedDate = item.LastInspectionDate; _repairSummary.Text = item.RepairSummary;
        }
        else _department.Text = "ИТ";
        var grid = new Grid { Margin = new Thickness(24) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) }); grid.ColumnDefinitions.Add(new ColumnDefinition());
        string[] labels = ["Название *", "Инвентарный номер *", "Производитель *", "Дата приобретения *", "Стоимость *", "Подразделение *", "Последний осмотр", "Сводка ремонтов"];
        Control[] controls = [_name, _number, _manufacturer, _purchaseDate, _cost, _department, _inspectionDate, _repairSummary];
        for (int i = 0; i < labels.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var label = new TextBlock { Text = labels[i], VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 12, 6), Foreground = System.Windows.Media.Brushes.SlateGray };
            Grid.SetRow(label, i); grid.Children.Add(label);
            controls[i].Margin = new Thickness(0, 4, 0, 4); controls[i].MinHeight = 32; Grid.SetColumn(controls[i], 1); Grid.SetRow(controls[i], i); grid.Children.Add(controls[i]);
        }
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var save = new Button { Content = "Сохранить", MinWidth = 110, Padding = new Thickness(12, 8, 12, 8), IsDefault = true };
        save.Click += (_, _) => Save();
        var cancel = new Button { Content = "Отмена", MinWidth = 90, Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(12, 8, 12, 8), IsCancel = true };
        buttons.Children.Add(save); buttons.Children.Add(cancel); Grid.SetColumnSpan(buttons, 2); Grid.SetRow(buttons, labels.Length); grid.Children.Add(buttons); Content = grid;
    }

    private void Save()
    {
        if (!decimal.TryParse(_cost.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var cost)) { Warn("Стоимость должна быть числом."); return; }
        if (_purchaseDate.SelectedDate is not { } purchase) { Warn("Укажите корректную дату приобретения."); return; }
        Result = new Equipment { Id = _id, Name = _name.Text, InventoryNumber = _number.Text, Manufacturer = _manufacturer.Text, PurchaseDate = purchase, Cost = cost, Department = _department.Text, LastInspectionDate = _inspectionDate.SelectedDate, RepairSummary = _repairSummary.Text };
        try { EquipmentAccounting.Core.Services.EquipmentValidator.Validate(Result); DialogResult = true; }
        catch (ArgumentException ex) { Warn(ex.Message); }
    }
    private void Warn(string message) => MessageBox.Show(this, message, "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
    private static TextBox Field() => new() { VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(7, 2, 7, 2) };
}
