using EquipmentAccounting.Core.Data;
using EquipmentAccounting.Core.Models;
using EquipmentAccounting.Core.Services;

namespace EquipmentAccounting.Tests.PracticalWork15.UnitTests;

public sealed class EquipmentServiceTests : IDisposable
{
    private readonly EquipmentRepository _repository = new(":memory:");
    private EquipmentService Service => new(_repository);
    private static Equipment NewEquipment(string number = "TEST-100") => new() { Name = "Тестовый монитор", InventoryNumber = number, Manufacturer = "Acme", PurchaseDate = DateTime.Today.AddDays(-5), Cost = 1200.50m, Department = "ИТ" };
    public void Dispose() => _repository.Dispose();

    [Fact] public void AddEquipment_IncreasesCountAndPersistsFields()
    {
        var service = Service; int before = service.GetAll().Count; var created = service.Add(NewEquipment());
        Assert.True(created.Id > 0); Assert.Equal(before + 1, service.GetAll().Count); Assert.Equal("TEST-100", service.GetAll().Single(e => e.Id == created.Id).InventoryNumber);
    }

    [Fact] public void EditEquipment_UpdatesExistingRecord()
    {
        var service = Service; var item = service.GetAll()[0]; item.Name = "Обновленное оборудование"; item.Cost = 999m; service.Update(item);
        var updated = service.GetAll().Single(e => e.Id == item.Id); Assert.Equal("Обновленное оборудование", updated.Name); Assert.Equal(999m, updated.Cost);
    }

    [Fact] public void DeleteEquipment_RemovesRecord()
    {
        var service = Service; var item = service.Add(NewEquipment()); Assert.True(service.Delete(item.Id)); Assert.DoesNotContain(service.GetAll(), e => e.Id == item.Id);
    }

    [Fact] public void SearchEquipment_FindsByNameOrInventoryNumber()
    {
        var service = Service; service.Add(NewEquipment()); Assert.Contains(service.Search("монитор"), e => e.InventoryNumber == "TEST-100"); Assert.Contains(service.Search("TEST-100"), e => e.Name == "Тестовый монитор");
    }

    [Fact] public void AddEquipment_RejectsDuplicateInventoryNumber()
    {
        var service = Service; string existingNumber = service.GetAll()[0].InventoryNumber; Assert.Throws<ArgumentException>(() => service.Add(NewEquipment(existingNumber)));
    }

    [Fact] public void TotalCost_EqualsSumOfEquipmentCosts()
    {
        var service = Service; decimal before = service.GetTotalCost(); service.Add(NewEquipment()); Assert.Equal(before + 1200.50m, service.GetTotalCost());
    }

    [Fact] public void MoveEquipment_UpdatesDepartmentAndCreatesHistory()
    {
        var service = Service; var item = service.Add(NewEquipment()); service.Move(item.Id, "Склад"); Assert.Equal("Склад", service.GetAll().Single(e => e.Id == item.Id).Department); Assert.Contains(service.GetMovements(item.Id), m => m.FromDepartment == "ИТ" && m.ToDepartment == "Склад");
    }

    [Fact] public void AddRepair_PersistsRepairRecord()
    {
        var service = Service; var item = service.Add(NewEquipment()); var repair = service.AddRepair(item.Id, DateTime.Today, "Замена блока питания", 250m); Assert.Contains(service.GetRepairs(item.Id), r => r.Id == repair.Id && r.Cost == 250m);
    }

    [Fact] public void AddInspection_StoresResultAndUpdatesLastDate()
    {
        var service = Service; var item = service.Add(NewEquipment()); var inspection = service.AddInspection(item.Id, DateTime.Today, "Исправно", "Иванов И.И."); Assert.Equal(inspection.InspectionDate, service.GetAll().Single(e => e.Id == item.Id).LastInspectionDate); Assert.Equal("Исправно", inspection.Result);
    }

    [Fact] public void InventoryReport_ContainsCurrentEquipment()
    {
        var service = Service; var item = service.Add(NewEquipment()); Assert.Contains(service.GetInventory(), row => row.Id == item.Id && row.InventoryNumber == "TEST-100");
    }

    [Fact] public void GetByDepartment_FiltersEquipmentByDepartment()
    {
        var service = Service;
        var item = service.Add(NewEquipment("TEST-DEPT-FILTER"));
        var itItems = service.GetByDepartment("ИТ");
        Assert.Contains(itItems, e => e.Id == item.Id);
        Assert.All(itItems, e => Assert.Equal("ИТ", e.Department));
    }

    [Fact] public void InvalidEquipment_IsRejected()
    {
        var service = Service; Assert.Throws<ArgumentException>(() => service.Add(NewEquipment(" "))); var negative = NewEquipment("TEST-NEG"); negative.Cost = -1; Assert.Throws<ArgumentOutOfRangeException>(() => service.Add(negative)); var future = NewEquipment("TEST-FUTURE"); future.PurchaseDate = DateTime.Today.AddDays(1); Assert.Throws<ArgumentException>(() => service.Add(future));
    }
}
