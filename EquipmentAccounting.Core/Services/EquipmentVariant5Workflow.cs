using EquipmentAccounting.Core.Models;

namespace EquipmentAccounting.Core.Services;

/// <summary>Связывает расчёт по формуле варианта 5 с созданием и сохранением оборудования.</summary>
public sealed class EquipmentVariant5Workflow(EquipmentService equipmentService)
{
    private readonly EquipmentService _equipmentService = equipmentService ?? throw new ArgumentNullException(nameof(equipmentService));

    /// <summary>Вариант 5: c = a³ − 3b.</summary>
    public decimal CalculateC(decimal a, decimal b) => a * a * a - 3m * b;

    /// <summary>Передаёт параметры в CalculateC и записывает результат в стоимость оборудования.</summary>
    public Equipment CreateEquipment(string name, string inventoryNumber, string manufacturer, string department, DateTime purchaseDate, decimal a, decimal b)
    {
        decimal c = CalculateC(a, b);
        return new Equipment
        {
            Name = name,
            InventoryNumber = inventoryNumber,
            Manufacturer = manufacturer,
            Department = department,
            PurchaseDate = purchaseDate,
            Cost = c
        };
    }

    /// <summary>Создаёт оборудование через CreateEquipment и сохраняет его через EquipmentService.</summary>
    public Equipment AddEquipment(string name, string inventoryNumber, string manufacturer, string department, DateTime purchaseDate, decimal a, decimal b)
    {
        Equipment equipment = CreateEquipment(name, inventoryNumber, manufacturer, department, purchaseDate, a, b);
        return _equipmentService.Add(equipment);
    }
}
