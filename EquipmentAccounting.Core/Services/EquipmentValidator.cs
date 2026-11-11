using EquipmentAccounting.Core.Models;

namespace EquipmentAccounting.Core.Services;

public static class EquipmentValidator
{
    public static void Validate(Equipment equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        if (string.IsNullOrWhiteSpace(equipment.Name)) throw new ArgumentException("Укажите название оборудования.", nameof(equipment));
        if (string.IsNullOrWhiteSpace(equipment.InventoryNumber)) throw new ArgumentException("Укажите инвентарный номер.", nameof(equipment));
        if (string.IsNullOrWhiteSpace(equipment.Manufacturer)) throw new ArgumentException("Укажите производителя.", nameof(equipment));
        if (string.IsNullOrWhiteSpace(equipment.Department)) throw new ArgumentException("Укажите подразделение.", nameof(equipment));
        if (equipment.Cost < 0) throw new ArgumentOutOfRangeException(nameof(equipment), "Стоимость не может быть отрицательной.");
        if (equipment.PurchaseDate.Date > DateTime.Today) throw new ArgumentException("Дата приобретения не может быть в будущем.", nameof(equipment));
        if (equipment.LastInspectionDate is { } inspection && inspection.Date > DateTime.Today) throw new ArgumentException("Дата осмотра не может быть в будущем.", nameof(equipment));
    }
}
