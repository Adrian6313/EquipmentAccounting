using EquipmentAccounting.Core.Data;
using EquipmentAccounting.Core.Models;
using EquipmentAccounting.Core.Services;
using Xunit;

namespace EquipmentAccounting.Tests.PracticalWork17.IntegrationTests;

public sealed class EquipmentIntegrationTests : IDisposable
{
    private readonly EquipmentRepository _repository = new(":memory:");
    private EquipmentService Service => new(_repository);
    private static Equipment NewEquipment(string number) => new() { Name = "Интеграционный тест", InventoryNumber = number, Manufacturer = "Fixture", PurchaseDate = DateTime.Today, Cost = 35.25m, Department = "ИТ" };
    public void Dispose() => _repository.Dispose();

    [Fact]
    public void EquipmentCrudSearchInventoryAndTotal_AreConsistentAcrossModules()
    {
        var service = Service;
        var added = service.Add(NewEquipment("PW17-CRUD"));
        Assert.Contains(service.Search("PW17-CRUD"), x => x.Id == added.Id);
        added.Name = "Измененное оборудование"; added.Cost = 45.75m; service.Update(added);

        var stored = Assert.Single(service.Search("Измененное"));
        Assert.Equal("PW17-CRUD", stored.InventoryNumber);
        Assert.Contains(service.GetInventory(), row => row.Id == stored.Id && row.Cost == 45.75m);
        Assert.Equal(270745.75m, service.GetTotalCost());
        Assert.True(service.Delete(stored.Id));
        Assert.DoesNotContain(service.GetAll(), x => x.Id == stored.Id);
    }

    [Fact]
    public void MovementAndRepair_AreLinkedToPersistedEquipment()
    {
        var service = Service; var item = service.Add(NewEquipment("PW17-MOVE"));
        service.Move(item.Id, "Склад");
        var repair = service.AddRepair(item.Id, DateTime.Today, "Замена детали", 15.50m);

        var persisted = Assert.Single(service.GetAll(), x => x.Id == item.Id);
        var movement = Assert.Single(service.GetMovements(item.Id));
        var persistedRepair = Assert.Single(service.GetRepairs(item.Id));
        Assert.Equal("Склад", persisted.Department);
        Assert.Equal(("ИТ", "Склад"), (movement.FromDepartment, movement.ToDepartment));
        Assert.Equal(repair.Id, persistedRepair.Id);
        Assert.Equal(item.Id, persistedRepair.EquipmentId);
    }

    [Fact]
    public void InspectionCanBeRetrievedThroughServiceAfterDatabaseWrite()
    {
        var service = Service; var item = service.Add(NewEquipment("PW17-INSP"));
        service.AddInspection(item.Id, DateTime.Today, "Исправно", "Тестировщик");

        // This black-box integration check initially found that inspection rows had no read path.
        // Practical Work 17 records the finding; the repair is made only during Practical Work 18.
        var getter = typeof(EquipmentService).GetMethod("GetInspections", [typeof(int)]);
        Assert.NotNull(getter);
        var inspections = Assert.IsAssignableFrom<IReadOnlyList<Inspection>>(getter!.Invoke(service, [item.Id]));
        var inspection = Assert.Single(inspections);
        Assert.Equal("Исправно", inspection.Result);
        Assert.Equal("Тестировщик", inspection.Inspector);
        Assert.Equal(DateTime.Today, service.GetAll().Single(x => x.Id == item.Id).LastInspectionDate!.Value.Date);
    }

    [Fact]
    public void InvalidInputDoesNotPersistAndEmptyInventoryReturnsZero()
    {
        var service = Service; int before = service.GetAll().Count;
        var invalid = NewEquipment("PW17-BAD"); invalid.Cost = -0.01m;
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Add(invalid));
        Assert.Equal(before, service.GetAll().Count);

        foreach (var row in service.GetAll().ToArray()) Assert.True(service.Delete(row.Id));
        Assert.Empty(service.GetAll()); Assert.Empty(service.GetInventory()); Assert.Equal(0m, service.GetTotalCost());
    }

    [Fact] public void Variant5Workflow_CalculatesAndPersistsCaseOne() => AssertVariantWorkflow(5m, 16m, 77m);
    [Fact] public void Variant5Workflow_CalculatesAndPersistsCaseTwo() => AssertVariantWorkflow(1m, 0m, 1m);
    [Fact] public void Variant5Workflow_CalculatesAndPersistsCaseThree() => AssertVariantWorkflow(0m, 0m, 0m);

    private void AssertVariantWorkflow(decimal a, decimal b, decimal expectedC)
    {
        var service = Service;
        var workflow = new EquipmentVariant5Workflow(service);

        var added = workflow.AddEquipment("Результат расчета", $"PW17-F-{a}-{b}", "Fixture", "ИТ", DateTime.Today, a, b);

        Assert.Equal(expectedC, added.Cost);
        Assert.Equal(expectedC, service.GetAll().Single(x => x.Id == added.Id).Cost);
    }
}
