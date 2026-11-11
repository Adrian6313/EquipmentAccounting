using System.Collections.ObjectModel;
using EquipmentAccounting.Core.Models;
using EquipmentAccounting.Core.Services;

namespace EquipmentAccounting.App.ViewModels;

/// <summary>View state for the equipment table and summary cards.</summary>
internal sealed class EquipmentListViewModel(EquipmentService service)
{
    public ObservableCollection<Equipment> Items { get; } = [];
    public int TotalCount { get; private set; }
    public decimal TotalCost { get; private set; }

    public void Reload(string? query = null)
    {
        Items.Clear();
        foreach (var item in service.Search(query ?? "")) Items.Add(item);
        var all = service.GetAll();
        TotalCount = all.Count;
        TotalCost = service.GetTotalCost();
    }
}
