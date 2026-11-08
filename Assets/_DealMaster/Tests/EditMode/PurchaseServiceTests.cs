using NUnit.Framework;

public class PurchaseServiceTests
{
    private FakeWallet wallet;
    private InventoryService inventory;
    private PurchaseService service;
    private SupplierStock supplier;

    [SetUp]
    public void SetUp()
    {
        wallet = new FakeWallet(150000); // R$ 1.500
        inventory = new InventoryService(backpackCapacity: 10);
        service = new PurchaseService(wallet, inventory);

        supplier = new SupplierStock("eletro_centro", opensAtHour: 8, closesAtHour: 18,
            maxDiscount: 0.10f, volumeDiscountMinQuantity: 5, volumeDiscount: 0.05f);
        supplier.AddCatalogItem("fone", unitPriceCents: 8500, stock: 6, restockPerDay: 2);
        supplier.AddCatalogItem("notebook", unitPriceCents: 250000, stock: 2, requiredLevel: 6);
    }

    private PurchaseResult Buy(string product, int qty, long unitPrice,
        int level = 1, int hour = 10, StorageLocation dest = StorageLocation.Backpack) =>
        service.TryPurchase(supplier, product, qty, unitPrice, level, hour, dest, out _);

    [Test]
    public void Purchase_DebitsWallet_AddsInventory_TakesSupplierStock()
    {
        var result = service.TryPurchase(supplier, "fone", 2, 8500, 1, 10,
            StorageLocation.Backpack, out var receipt);

        Assert.AreEqual(PurchaseResult.Success, result);
        Assert.AreEqual(150000 - 17000, wallet.BalanceCents);
        Assert.AreEqual(2, inventory.GetQuantity(StorageLocation.Backpack, "fone"));
        Assert.AreEqual(4, supplier.GetStock("fone"));
        Assert.AreEqual(17000, receipt.TotalCents);
    }

    [Test]
    public void Purchase_FailsWithoutChangingAnything_WhenFundsInsufficient()
    {
        var poorWallet = new FakeWallet(10000);
        var poorService = new PurchaseService(poorWallet, inventory);

        var result = poorService.TryPurchase(supplier, "fone", 2, 8500, 1, 10,
            StorageLocation.Backpack, out _);

        Assert.AreEqual(PurchaseResult.InsufficientFunds, result);
        Assert.AreEqual(10000, poorWallet.BalanceCents);
        Assert.AreEqual(0, inventory.GetQuantity(StorageLocation.Backpack, "fone"));
        Assert.AreEqual(6, supplier.GetStock("fone"));
    }

    [Test]
    public void Purchase_Validations()
    {
        Assert.AreEqual(PurchaseResult.SupplierClosed, Buy("fone", 1, 8500, hour: 20));
        Assert.AreEqual(PurchaseResult.ProductNotSold, Buy("caneta", 1, 100));
        Assert.AreEqual(PurchaseResult.LevelTooLow, Buy("notebook", 1, 250000));
        Assert.AreEqual(PurchaseResult.OutOfStock, Buy("fone", 7, 8500));
        Assert.AreEqual(PurchaseResult.PriceBelowFloor, Buy("fone", 1, 5000));
        Assert.AreEqual(PurchaseResult.InvalidRequest, Buy("fone", 0, 8500));
        Assert.AreEqual(PurchaseResult.InvalidRequest, Buy("fone", 1, 8500, dest: StorageLocation.Order));
        Assert.AreEqual(150000, wallet.BalanceCents);
    }

    [Test]
    public void Purchase_FailsWhenNoSpace()
    {
        inventory.TryAdd(StorageLocation.Backpack, "outro", 9, 100);
        Assert.AreEqual(PurchaseResult.NoSpace, Buy("fone", 2, 8500));
        Assert.AreEqual(150000, wallet.BalanceCents);
    }

    [Test]
    public void Purchase_CannotBeRepeatedBeyondStock()
    {
        Assert.AreEqual(PurchaseResult.Success, Buy("fone", 3, 8500));
        Assert.AreEqual(PurchaseResult.Success, Buy("fone", 3, 8500));
        Assert.AreEqual(PurchaseResult.OutOfStock, Buy("fone", 1, 8500));
        Assert.AreEqual(150000 - 51000, wallet.BalanceCents);
    }

    [Test]
    public void Offer_AtOrAboveAsking_IsAcceptedAtAskingPrice()
    {
        var r = service.EvaluateOffer(supplier, "fone", 1, 9000);
        Assert.AreEqual(SupplierOfferResult.Accepted, r.Result);
        Assert.AreEqual(8500, r.UnitPriceCents);
    }

    [Test]
    public void Offer_WithinDiscount_IsAccepted()
    {
        var r = service.EvaluateOffer(supplier, "fone", 1, 7700); // mínimo = 7650
        Assert.AreEqual(SupplierOfferResult.Accepted, r.Result);
        Assert.AreEqual(7700, r.UnitPriceCents);
    }

    [Test]
    public void Offer_SlightlyBelowFloor_GetsCounter_FarBelow_IsRejected()
    {
        var counter = service.EvaluateOffer(supplier, "fone", 1, 7000);
        Assert.AreEqual(SupplierOfferResult.CounterOffer, counter.Result);
        Assert.Greater(counter.UnitPriceCents, 7650);
        Assert.Less(counter.UnitPriceCents, 8500);

        var rejected = service.EvaluateOffer(supplier, "fone", 1, 3000);
        Assert.AreEqual(SupplierOfferResult.Rejected, rejected.Result);
    }

    [Test]
    public void VolumeDiscount_LowersAskingPrice()
    {
        Assert.AreEqual(8500, supplier.GetVolumePriceCents("fone", 4));
        Assert.AreEqual(8075, supplier.GetVolumePriceCents("fone", 5));
    }

    [Test]
    public void Relationship_GrowsWithPurchases_AndImprovesFloor()
    {
        long floorBefore = supplier.GetFloorPriceCents("fone", 1);
        Buy("fone", 5, 8075);
        Assert.Greater(supplier.Relationship, 0f);
        Assert.Less(supplier.GetFloorPriceCents("fone", 1), floorBefore);
    }

    [Test]
    public void Restock_RefillsUpToMax()
    {
        Buy("fone", 5, 8075);
        supplier.Restock();
        Assert.AreEqual(3, supplier.GetStock("fone"));
        supplier.Restock();
        supplier.Restock();
        Assert.AreEqual(6, supplier.GetStock("fone"));
    }
}
