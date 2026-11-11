namespace EquipmentAccounting.Core.Models;

public sealed class Equipment
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string InventoryNumber { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public DateTime PurchaseDate { get; set; } = DateTime.Today;
    public decimal Cost { get; set; }
    public string Department { get; set; } = "";
    public DateTime? LastInspectionDate { get; set; }
    public string RepairSummary { get; set; } = "";
}

public sealed record Repair(int Id, int EquipmentId, DateTime RepairDate, string Description, decimal Cost);
public sealed record Inspection(int Id, int EquipmentId, DateTime InspectionDate, string Result, string Inspector);
public sealed record EquipmentMovement(int Id, int EquipmentId, string FromDepartment, string ToDepartment, DateTime MovedAt);
public sealed record InventoryRow(int Id, string Name, string InventoryNumber, string Manufacturer, DateTime PurchaseDate, decimal Cost, string Department, DateTime? LastInspectionDate);
