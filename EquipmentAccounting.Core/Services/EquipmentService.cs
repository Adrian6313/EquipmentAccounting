using EquipmentAccounting.Core.Data;
using EquipmentAccounting.Core.Models;
using Microsoft.Data.Sqlite;

namespace EquipmentAccounting.Core.Services;

public sealed class EquipmentService(EquipmentRepository repository)
{
    public IReadOnlyList<Equipment> GetAll() => repository.GetAll();
    public IReadOnlyList<Equipment> Search(string query) => string.IsNullOrWhiteSpace(query) ? GetAll() : repository.Search(query);
    public IReadOnlyList<Equipment> GetByDepartment(string department) => string.IsNullOrWhiteSpace(department) ? GetAll() : GetAll().Where(e => string.Equals(e.Department, department.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    public decimal GetTotalCost() => repository.GetTotalCost();
    public IReadOnlyList<InventoryRow> GetInventory() => repository.GetInventory();
    public IReadOnlyList<Repair> GetRepairs(int id) => repository.GetRepairs(id);
    public IReadOnlyList<Inspection> GetInspections(int id) => repository.GetInspections(id);
    public IReadOnlyList<EquipmentMovement> GetMovements(int id) => repository.GetMovements(id);

    public Equipment Add(Equipment equipment)
    {
        EquipmentValidator.Validate(equipment);
        EnsureInventoryNumberIsUnique(equipment.InventoryNumber, null);
        try { return repository.Add(equipment); }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new ArgumentException("Инвентарный номер уже используется.", nameof(equipment), ex); }
    }

    public void Update(Equipment equipment)
    {
        EquipmentValidator.Validate(equipment);
        EnsureInventoryNumberIsUnique(equipment.InventoryNumber, equipment.Id);
        try { repository.Update(equipment); }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new ArgumentException("Инвентарный номер уже используется.", nameof(equipment), ex); }
    }

    public bool Delete(int id) => repository.Delete(id);
    public void Move(int id, string department) => repository.Move(id, department);
    public Repair AddRepair(int id, DateTime date, string description, decimal cost) => repository.AddRepair(id, date, description, cost);
    public Inspection AddInspection(int id, DateTime date, string result, string inspector) => repository.AddInspection(id, date, result, inspector);

    private void EnsureInventoryNumberIsUnique(string inventoryNumber, int? exceptId)
    {
        if (repository.Search(inventoryNumber).Any(e => e.InventoryNumber.Equals(inventoryNumber.Trim(), StringComparison.OrdinalIgnoreCase) && e.Id != exceptId))
            throw new ArgumentException("Инвентарный номер уже используется.", nameof(inventoryNumber));
    }
}
