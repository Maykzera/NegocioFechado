using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class SalesServiceTests
{
    private FakeWallet wallet;
    private InventoryService inv;
    private PackagingService packaging;
    private FinanceLedger ledger;
    private SalesService sales;

    private static ProductInfo Fone => DefaultContent.Products.First(p => p.Id == "fone_bt");

    [SetUp]
    public void SetUp()
    {
        ProductDatabase.Clear();
        DefaultContent.RegisterProducts();
        wallet = new FakeWallet(10000);
        inv = new InventoryService();
        packaging = new PackagingService(wallet, inv);
        ledger = new FinanceLedger();
        sales = new SalesService(wallet, inv, packaging, ledger);
    }

    private void PackOneFone()
    {
        inv.TryAdd(StorageLocation.PackingBench, "fone_bt", 1, 8500);
        var input = new PackagingInput
        {
            Product = Fone, Inspected = true, Box = BoxSize.Small, PaddingSheets = 2, PlacementOffset = 0.05f,
            FlapsClosed = true, TapeStrips = new List<float> { 0.95f, 0.9f }, LabelApplied = true,
            LabelAccuracy = 0.9f, SecondsSpent = 30
        };
        Assert.IsTrue(packaging.TryFinalize(input, out _, out _));
    }

    [Test]
    public void PackedSale_CreditsPriceMinusFee_AndComputesProfit()
    {
        PackOneFone(); // materiais: 320
        long before = wallet.BalanceCents;

        Assert.IsTrue(sales.TryCompleteSale("Rafael", "fone_bt", 14500, true, 0.99, out var sale, out _));

        Assert.AreEqual(before + 14500 - 290, wallet.BalanceCents);
        Assert.AreEqual(0, inv.GetQuantity(StorageLocation.Order, "fone_bt"));
        Assert.AreEqual(0, packaging.Orders.Count);
        Assert.AreEqual(14500 - 290 - 8500 - 320, sale.ProfitCents);
        Assert.IsFalse(sale.Damaged);
        Assert.Greater(sales.Reputation, 50f);
    }

    [Test]
    public void Sale_FailsWithoutChanges_WhenNoOrder()
    {
        Assert.IsFalse(sales.TryCompleteSale("Rafael", "fone_bt", 14500, true, 0.5, out _, out var error));
        Assert.IsNotNull(error);
        Assert.AreEqual(10000, wallet.BalanceCents);
        Assert.AreEqual(0, ledger.Entries.Count);
    }

    [Test]
    public void UnpackedSale_UsesBackpack_AndHasHigherRisk()
    {
        inv.TryAdd(StorageLocation.Backpack, "fone_bt", 1, 8500);
        Assert.IsTrue(sales.TryCompleteSale("Ana", "fone_bt", 14000, false, 0.99, out var sale, out _));
        Assert.IsFalse(sale.Packed);
        Assert.AreEqual(0, sale.MaterialsCostCents);
        Assert.Greater(sale.DamageRisk, 0.3f);
    }

    [Test]
    public void DamagedDelivery_CreatesComplaint_AndLowersReputation()
    {
        inv.TryAdd(StorageLocation.Backpack, "fone_bt", 1, 8500);
        bool complained = false;
        sales.OnComplaint += _ => complained = true;

        Assert.IsTrue(sales.TryCompleteSale("Ana", "fone_bt", 14000, false, 0.0, out var sale, out _));
        Assert.IsTrue(sale.Damaged);
        Assert.IsTrue(complained);
        Assert.Less(sales.Reputation, 50f);
        Assert.AreEqual(1, sales.PendingComplaints().Count());
    }

    [Test]
    public void ResolvingComplaint_RefundsAndAffectsReputation()
    {
        inv.TryAdd(StorageLocation.Backpack, "fone_bt", 2, 8500);
        sales.TryCompleteSale("Ana", "fone_bt", 14000, false, 0.0, out var a, out _);
        sales.TryCompleteSale("Bia", "fone_bt", 14000, false, 0.0, out var b, out _);
        float rep = sales.Reputation;
        long balance = wallet.BalanceCents;

        Assert.IsTrue(sales.ResolveComplaint(a, ComplaintResolution.FullRefund, out _));
        Assert.AreEqual(balance - 14000, wallet.BalanceCents);
        Assert.Greater(sales.Reputation, rep);
        Assert.AreEqual(14000, a.RefundedCents);

        float rep2 = sales.Reputation;
        Assert.IsTrue(sales.ResolveComplaint(b, ComplaintResolution.Deny, out _));
        Assert.Less(sales.Reputation, rep2);
        Assert.IsFalse(sales.ResolveComplaint(b, ComplaintResolution.FullRefund, out _));
        Assert.AreEqual(0, sales.PendingComplaints().Count());
    }

    [Test]
    public void Ledger_SummarizesRevenueAndExpenses()
    {
        PackOneFone();
        ledger.Record(LedgerEntryType.PackagingMaterials, -320, "embalagem");
        sales.TryCompleteSale("Rafael", "fone_bt", 14500, true, 0.99, out _, out _);

        Assert.AreEqual(14500, ledger.Revenue);
        Assert.AreEqual(320 + 290, ledger.Expenses);
    }
}
