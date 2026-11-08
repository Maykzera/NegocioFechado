using System.Linq;
using NUnit.Framework;

public class DefaultContentTests
{
    [SetUp]
    public void SetUp()
    {
        ProductDatabase.Clear();
        DefaultContent.RegisterProducts();
    }

    [Test]
    public void HasTenProducts_WithUniqueIds()
    {
        Assert.AreEqual(10, DefaultContent.Products.Length);
        Assert.AreEqual(10, DefaultContent.Products.Select(p => p.Id).Distinct().Count());
    }

    [Test]
    public void EveryProduct_CanBeSoldAtProfit_AndHasArguments()
    {
        foreach (var p in DefaultContent.Products)
        {
            Assert.Greater(p.ReferencePriceCents, p.BaseCostCents, p.Id);
            Assert.IsNotEmpty(p.Benefits, p.Id);
            Assert.IsNotEmpty(p.Exaggerations, p.Id);
        }
    }

    [Test]
    public void NoPens_AsRequested()
    {
        Assert.IsFalse(DefaultContent.Products.Any(p => p.Name.ToLower().Contains("caneta")));
    }

    [Test]
    public void TwoSuppliers_SellFiveProductsEach_AtBaseCost()
    {
        var a = DefaultContent.CreateElectronicsSupplier();
        var b = DefaultContent.CreateAccessoriesSupplier();
        Assert.AreEqual(5, a.ProductIds.Count());
        Assert.AreEqual(5, b.ProductIds.Count());
        Assert.AreEqual(8500, a.GetListPriceCents("fone_bt"));
        Assert.IsTrue(a.IsOpen(10));
        Assert.IsFalse(a.IsOpen(20));
    }

    [Test]
    public void FirstDayCapital_CanBuyStarterStock()
    {
        var wallet = new FakeWallet(150000);
        var inv = new InventoryService();
        var service = new PurchaseService(wallet, inv);
        var supplier = DefaultContent.CreateElectronicsSupplier();

        var result = service.TryPurchase(supplier, "fone_bt", 1, 8500, 1, 10, StorageLocation.Backpack, out _);

        Assert.AreEqual(PurchaseResult.Success, result);
        Assert.AreEqual(141500, wallet.BalanceCents);
    }
}
