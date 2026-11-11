using System.Globalization;
using System.IO;
using System.Windows;
using EquipmentAccounting.App.ViewModels;
using EquipmentAccounting.Core.Models;
using EquipmentAccounting.Core.Services;
using EquipmentAccounting.Core.Data;

namespace EquipmentAccounting.App;

public partial class MainWindow : Window
{
    private readonly EquipmentRepository _repository;
    private readonly EquipmentService _service;
    private readonly EquipmentListViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EquipmentAccounting");
        Directory.CreateDirectory(folder);
        _repository = new EquipmentRepository(Path.Combine(folder, "equipment.db"));
        _service = new EquipmentService(_repository);
        _viewModel = new EquipmentListViewModel(_service);
        RefreshData();
    }

    private Equipment? Selected => EquipmentGrid.SelectedItem as Equipment;

    private void RefreshData()
    {
        _viewModel.Reload(SearchBox.Text);
        EquipmentGrid.ItemsSource = _viewModel.Items;
        CountText.Text = $"Записей: {_viewModel.TotalCount}";
        TotalText.Text = $"Общая стоимость: {_viewModel.TotalCost:N2} ₽";
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_viewModel is not null) RefreshData();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EquipmentDialog { Owner = this };
        if (dialog.ShowDialog() == true) Run(() => _service.Add(dialog.Result!));
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSelection(out var item)) return;
        var dialog = new EquipmentDialog(item) { Owner = this };
        if (dialog.ShowDialog() == true) Run(() => _service.Update(dialog.Result!));
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSelection(out var item)) return;
        if (MessageBox.Show(this, $"Удалить «{item.Name}»?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            Run(() => _service.Delete(item.Id));
    }

    private void Move_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSelection(out var item)) return;
        var department = InputDialog.Show(this, "Перемещение оборудования", "Новое подразделение", "ИТ, Бухгалтерия, Производство или Склад");
        if (department is not null) Run(() => _service.Move(item.Id, department));
    }

    private void Repairs_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSelection(out var item)) return;
        var repairs = _service.GetRepairs(item.Id);
        new HistoryWindow($"Ремонты · {item.InventoryNumber}", repairs) { Owner = this }.ShowDialog();
        if (MessageBox.Show(this, "Добавить запись о ремонте?", "Ремонты", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var description = InputDialog.Show(this, "Новый ремонт", "Описание ремонта");
        var costText = description is null ? null : InputDialog.Show(this, "Новый ремонт", "Стоимость ремонта", "0");
        if (description is null || costText is null) return;
        if (!decimal.TryParse(costText, NumberStyles.Number, CultureInfo.CurrentCulture, out var cost))
        {
            MessageBox.Show(this, "Введите корректную стоимость.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Run(() => _service.AddRepair(item.Id, DateTime.Today, description, cost));
    }

    private void Inspection_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSelection(out var item)) return;
        var inspections = _service.GetInspections(item.Id);
        new HistoryWindow($"Технические осмотры · {item.InventoryNumber}", inspections) { Owner = this }.ShowDialog();
        if (MessageBox.Show(this, "Добавить запись о техническом осмотре?", "Осмотры", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var result = InputDialog.Show(this, "Технический осмотр", "Результат осмотра", "Исправно");
        var inspector = result is null ? null : InputDialog.Show(this, "Технический осмотр", "Проверяющий", "Фамилия И.О.");
        if (result is not null && inspector is not null)
            Run(() => _service.AddInspection(item.Id, DateTime.Today, result, inspector));
    }

    private void Inventory_Click(object sender, RoutedEventArgs e)
    {
        new InventoryWindow(_service.GetInventory()) { Owner = this }.ShowDialog();
        StatusText.Text = "Открыта инвентаризационная ведомость";
    }

    private void Movements_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSelection(out var item)) return;
        var movements = _service.GetMovements(item.Id);
        new HistoryWindow($"Перемещения · {item.InventoryNumber}", movements) { Owner = this }.ShowDialog();
        StatusText.Text = $"История перемещений: {item.InventoryNumber} · записей: {movements.Count}";
    }

    private bool RequireSelection(out Equipment item)
    {
        item = Selected!;
        if (item is not null) return true;
        MessageBox.Show(this, "Выберите оборудование в таблице.", "Нет выбранной записи", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private void Run(Action action)
    {
        try { action(); RefreshData(); StatusText.Text = "Операция выполнена успешно"; }
        catch (Exception ex) { StatusText.Text = "Операция не выполнена"; MessageBox.Show(this, ex.Message, "Не удалось выполнить операцию", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) { RefreshData(); StatusText.Text = "Список обновлён"; }
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
    private void About_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this, "Учёт оборудования предприятия\nПрактические работы №19–20", "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
    private void EquipmentGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Selected is { } item) StatusText.Text = $"Выбрано: {item.Name} · {item.InventoryNumber} · {item.Department}";
    }

    protected override void OnClosed(EventArgs e) { _repository.Dispose(); base.OnClosed(e); }
}
