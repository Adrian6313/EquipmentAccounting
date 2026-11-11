using EquipmentAccounting.Core.Data;
using EquipmentAccounting.Core.Models;
using EquipmentAccounting.Core.Services;
using Xunit;

namespace EquipmentAccounting.Tests.PracticalWork18.IntegrationTests;

public sealed class EquipmentIntegrationRegressionTests : IDisposable
{
    private readonly EquipmentRepository _repository = new(":memory:");
    private EquipmentService Service => new(_repository);
    private static Equipment NewEquipment(string number) => new() { Name = "Интеграционное оборудование", InventoryNumber = number, Manufacturer = "Fixture", PurchaseDate = DateTime.Today, Cost = 99.90m, Department = "ИТ" };
    public void Dispose() => _repository.Dispose();

    [Fact]
    public void AddEditSearchInventoryTotalAndDelete_StayConsistentAcrossRepositoryAndService()
    {
        var service = Service; var item = service.Add(NewEquipment("PW18-CRUD"));
        item.Name = "Сканер"; item.Cost = 125.25m; service.Update(item);

        var found = Assert.Single(service.Search("PW18-CRUD"));
        Assert.Equal("Сканер", found.Name);
        Assert.Contains(service.GetInventory(), row => row.Id == item.Id && row.Cost == 125.25m);
        Assert.Equal(270825.25m, service.GetTotalCost());
        Assert.True(service.Delete(item.Id));
        Assert.DoesNotContain(service.GetAll(), e => e.Id == item.Id);
    }

    [Fact]
    public void MovementRepairAndInspection_ArePersistedAndCascadeWithEquipment()
    {
        var service = Service; var item = service.Add(NewEquipment("PW18-LINK"));
        service.Move(item.Id, "Склад");
        var repair = service.AddRepair(item.Id, DateTime.Today, "Профилактика", 10m);
        var inspection = service.AddInspection(item.Id, DateTime.Today, "Допущено", "Проверяющий");

        Assert.Equal("Склад", service.GetAll().Single(e => e.Id == item.Id).Department);
        Assert.Contains(service.GetMovements(item.Id), m => m.ToDepartment == "Склад");
        Assert.Contains(service.GetRepairs(item.Id), r => r.Id == repair.Id && r.EquipmentId == item.Id);
        Assert.Contains(ReadInspections(service, item.Id), i => i.Id == inspection.Id && i.Result == "Допущено");
        Assert.True(service.Delete(item.Id));
        Assert.Empty(service.GetMovements(item.Id)); Assert.Empty(service.GetRepairs(item.Id)); Assert.Empty(ReadInspections(service, item.Id));
    }

    [Fact]
    public void InvalidRecordsDoNotPersistAndEmptyDatabaseReturnsEmptyInventory()
    {
        var service = Service;
        foreach (var seed in service.GetAll().ToArray()) Assert.True(service.Delete(seed.Id));

        var noName = NewEquipment("PW18-NAME"); noName.Name = " ";
        var negativeCost = NewEquipment("PW18-COST"); negativeCost.Cost = -0.01m;
        var futureDate = NewEquipment("PW18-DATE"); futureDate.PurchaseDate = DateTime.Today.AddDays(1);
        Assert.Throws<ArgumentException>(() => service.Add(noName));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Add(negativeCost));
        Assert.Throws<ArgumentException>(() => service.Add(futureDate));

        Assert.Empty(service.GetAll()); Assert.Empty(service.Search("PW18")); Assert.Empty(service.GetInventory());
        Assert.Equal(0m, service.GetTotalCost());
    }

    [Fact] public void Variant5Workflow_CalculatesAndPersistsCaseOne() => AssertVariantWorkflow(5m, 16m, 77m);
    [Fact] public void Variant5Workflow_CalculatesAndPersistsCaseTwo() => AssertVariantWorkflow(0m, 0m, 0m);
    [Fact] public void Variant5Workflow_CalculatesAndPersistsCaseThree() => AssertVariantWorkflow(2m, -1m, 11m);

    private void AssertVariantWorkflow(decimal a, decimal b, decimal expectedC)
    {
        var service = Service; var workflow = new EquipmentVariant5Workflow(service);
        var added = workflow.AddEquipment("Расчет", $"PW18-F-{a}-{b}", "Fixture", "ИТ", DateTime.Today, a, b);

        Assert.Equal(expectedC, added.Cost);
        Assert.Equal(expectedC, service.GetAll().Single(e => e.Id == added.Id).Cost);
    }

    private static IReadOnlyList<Inspection> ReadInspections(EquipmentService service, int equipmentId)
    {
        var getter = typeof(EquipmentService).GetMethod("GetInspections", [typeof(int)]);
        Assert.NotNull(getter);
        return Assert.IsAssignableFrom<IReadOnlyList<Inspection>>(getter!.Invoke(service, [equipmentId]));
    }
}
