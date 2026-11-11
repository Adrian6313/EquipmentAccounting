using EquipmentAccounting.Core.Models;

namespace EquipmentAccounting.Core.Services;

/// <summary>Совместимый фасад старого типа; использует выбранную формулу варианта 5.</summary>
[Obsolete("Используйте EquipmentVariant5Workflow.")]
public sealed class EquipmentVariant27Workflow(EquipmentService equipmentService)
{
    private readonly EquipmentVariant5Workflow _workflow = new(equipmentService);

    public decimal CalculateC(decimal a, decimal b) => _workflow.CalculateC(a, b);

    public Equipment CreateEquipment(string name, string inventoryNumber, string manufacturer, string department, DateTime purchaseDate, decimal a, decimal b)
        => _workflow.CreateEquipment(name, inventoryNumber, manufacturer, department, purchaseDate, a, b);

    public Equipment AddEquipment(string name, string inventoryNumber, string manufacturer, string department, DateTime purchaseDate, decimal a, decimal b)
        => _workflow.AddEquipment(name, inventoryNumber, manufacturer, department, purchaseDate, a, b);
}
