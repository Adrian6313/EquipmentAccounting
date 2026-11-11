using EquipmentAccounting.Core.Data;
using EquipmentAccounting.Core.Models;
using EquipmentAccounting.Core.Services;

namespace EquipmentAccounting.Tests.PracticalWork16.FunctionalTests;

public sealed class EquipmentFunctionalTests : IDisposable
{
    private readonly EquipmentRepository _repository = new(":memory:");
    private EquipmentService Service => new(_repository);
    private static Equipment NewEquipment(string number = "TEST-100") => new() { Name = "Тестовый монитор", InventoryNumber = number, Manufacturer = "Acme", PurchaseDate = DateTime.Today.AddDays(-5), Cost = 1200.50m, Department = "ИТ" };
    public void Dispose() => _repository.Dispose();

    [Fact] public void AddEquipment_AcceptsZeroCostAndTodayAsBoundaryValues()
    {
        var service = Service;
        var equipment = NewEquipment("TEST-BOUNDARY"); equipment.Cost = 0; equipment.PurchaseDate = DateTime.Today;
        var added = service.Add(equipment);
        Assert.Equal(0m, added.Cost); Assert.Equal(DateTime.Today, added.PurchaseDate.Date);
    }

    [Fact] public void UpdateEquipment_RejectsInventoryNumberUsedByAnotherRecord()
    {
        var service = Service; var items = service.GetAll(); var edited = items[0]; string originalNumber = edited.InventoryNumber;
        edited.InventoryNumber = items[1].InventoryNumber;
        Assert.Throws<ArgumentException>(() => service.Update(edited));
        Assert.Equal(originalNumber, service.GetAll().Single(e => e.Id == edited.Id).InventoryNumber);
    }

    [Fact] public void TotalCost_PreservesDecimalCentPrecision()
    {
        var service = Service;
        var first = NewEquipment("TEST-DEC-1"); first.Cost = 0.10m;
        var second = NewEquipment("TEST-DEC-2"); second.Cost = 0.20m;
        service.Add(first); service.Add(second);
        Assert.Equal(270700.30m, service.GetTotalCost());
    }

    [Fact] public void MoveEquipment_RejectsEmptyOrUnchangedDepartment()
    {
        var service = Service; var item = service.Add(NewEquipment());
        Assert.Throws<ArgumentException>(() => service.Move(item.Id, " "));
        Assert.Throws<ArgumentException>(() => service.Move(item.Id, "ИТ"));
        Assert.Empty(service.GetMovements(item.Id));
        Assert.Equal("ИТ", service.GetAll().Single(e => e.Id == item.Id).Department);
    }

    [Fact] public void AddRepair_RejectsInvalidFields()
    {
        var service = Service; var item = service.Add(NewEquipment());
        Assert.Throws<ArgumentException>(() => service.AddRepair(item.Id, DateTime.Today, " ", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddRepair(item.Id, DateTime.Today, "Ремонт", -0.01m));
        Assert.Empty(service.GetRepairs(item.Id));
    }

    [Fact] public void AddInspection_RejectsMissingResultOrInspector()
    {
        var service = Service; var item = service.Add(NewEquipment());
        Assert.Throws<ArgumentException>(() => service.AddInspection(item.Id, DateTime.Today, " ", "Проверяющий"));
        Assert.Throws<ArgumentException>(() => service.AddInspection(item.Id, DateTime.Today, "Исправно", " "));
        Assert.Null(service.GetAll().Single(e => e.Id == item.Id).LastInspectionDate);
    }

    [Fact] public void RequiredEquipmentFields_AreRejectedWhenBlank()
    {
        var service = Service;
        var noName = NewEquipment("TEST-NAME"); noName.Name = " "; Assert.Throws<ArgumentException>(() => service.Add(noName));
        var noManufacturer = NewEquipment("TEST-MAKER"); noManufacturer.Manufacturer = " "; Assert.Throws<ArgumentException>(() => service.Add(noManufacturer));
        var noDepartment = NewEquipment("TEST-DEPT"); noDepartment.Department = " "; Assert.Throws<ArgumentException>(() => service.Add(noDepartment));
    }

    [Fact] public void EmptyEquipmentList_HasNoRowsAndZeroTotal()
    {
        var service = Service;
        foreach (var item in service.GetAll().ToArray()) Assert.True(service.Delete(item.Id));
        Assert.Empty(service.GetAll()); Assert.Empty(service.Search("anything")); Assert.Empty(service.GetInventory()); Assert.Equal(0m, service.GetTotalCost());
    }
}
