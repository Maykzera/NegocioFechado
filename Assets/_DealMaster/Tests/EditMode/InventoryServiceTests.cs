using NUnit.Framework;

public class InventoryServiceTests
{
    [Test]
    public void Add_RespectsCapacity()
    {
        var inv = new InventoryService(backpackCapacity: 5);

        Assert.IsTrue(inv.TryAdd(StorageLocation.Backpack, "fone", 5, 8500));
        Assert.IsFalse(inv.TryAdd(StorageLocation.Backpack, "cabo", 1, 1000));
        Assert.AreEqual(5, inv.GetUsedUnits(StorageLocation.Backpack));
    }

    [Test]
    public void Add_RejectsInvalidInput()
    {
        var inv = new InventoryService();

        Assert.IsFalse(inv.TryAdd(StorageLocation.Backpack, "fone", 0, 100));
        Assert.IsFalse(inv.TryAdd(StorageLocation.Backpack, "fone", -1, 100));
        Assert.IsFalse(inv.TryAdd(StorageLocation.Backpack, "", 1, 100));
        Assert.IsFalse(inv.TryAdd(StorageLocation.Backpack, "fone", 1, -5));
        Assert.AreEqual(0, inv.History.Count);
    }

    [Test]
    public void AverageCost_IsWeighted()
    {
        var inv = new InventoryService();
        inv.TryAdd(StorageLocation.Warehouse, "fone", 2, 8000);
        inv.TryAdd(StorageLocation.Warehouse, "fone", 2, 9000);

        Assert.AreEqual(8500, inv.GetStack(StorageLocation.Warehouse, "fone").AverageUnitCostCents);
        Assert.AreEqual(34000, inv.GetTotalStockValueCents());
    }

    [Test]
    public void Transfer_IsAtomic_WhenDestinationFull()
    {
        var inv = new InventoryService(backpackCapacity: 10, benchCapacity: 1);
        inv.TryAdd(StorageLocation.Backpack, "fone", 3, 8500);

        Assert.IsFalse(inv.TryTransfer(StorageLocation.Backpack, StorageLocation.PackingBench, "fone", 2));
        Assert.AreEqual(3, inv.GetQuantity(StorageLocation.Backpack, "fone"));
        Assert.AreEqual(0, inv.GetQuantity(StorageLocation.PackingBench, "fone"));
    }

    [Test]
    public void Transfer_MovesQuantityAndCost()
    {
        var inv = new InventoryService();
        inv.TryAdd(StorageLocation.Backpack, "fone", 3, 8500);

        Assert.IsTrue(inv.TryTransfer(StorageLocation.Backpack, StorageLocation.Warehouse, "fone", 3));
        Assert.IsNull(inv.GetStack(StorageLocation.Backpack, "fone"));
        Assert.AreEqual(3, inv.GetQuantity(StorageLocation.Warehouse, "fone"));
        Assert.AreEqual(25500, inv.GetTotalStockValueCents());
    }

    [Test]
    public void TotalValue_IsPreserved_WhenSplittingUnevenCosts()
    {
        var inv = new InventoryService();
        inv.TryAdd(StorageLocation.Warehouse, "a", 1, 100);
        inv.TryAdd(StorageLocation.Warehouse, "a", 2, 101); // total 302, não divisível por 3

        inv.TryTransfer(StorageLocation.Warehouse, StorageLocation.Backpack, "a", 1);
        inv.TryTransfer(StorageLocation.Warehouse, StorageLocation.Backpack, "a", 2);

        Assert.AreEqual(302, inv.GetTotalStockValueCents());
    }

    [Test]
    public void Reserved_UnitsCannotBeRemovedOrTransferred()
    {
        var inv = new InventoryService();
        inv.TryAdd(StorageLocation.Warehouse, "fone", 2, 8500);

        Assert.IsTrue(inv.TryReserve(StorageLocation.Warehouse, "fone", 2));
        Assert.IsFalse(inv.TryRemove(StorageLocation.Warehouse, "fone", 1, out _));
        Assert.IsFalse(inv.TryTransfer(StorageLocation.Warehouse, StorageLocation.Backpack, "fone", 1));

        Assert.IsTrue(inv.TryRelease(StorageLocation.Warehouse, "fone", 1));
        Assert.IsTrue(inv.TryRemove(StorageLocation.Warehouse, "fone", 1, out long cost));
        Assert.AreEqual(8500, cost);
    }

    [Test]
    public void History_RecordsMovements()
    {
        var inv = new InventoryService();
        inv.TryAdd(StorageLocation.Backpack, "fone", 1, 8500, "compra");
        inv.TryTransfer(StorageLocation.Backpack, StorageLocation.Warehouse, "fone", 1);

        Assert.AreEqual(3, inv.History.Count);
        Assert.AreEqual(InventoryMovementType.Added, inv.History[0].Type);
        Assert.AreEqual(InventoryMovementType.TransferredOut, inv.History[1].Type);
        Assert.AreEqual(InventoryMovementType.TransferredIn, inv.History[2].Type);
    }
}
