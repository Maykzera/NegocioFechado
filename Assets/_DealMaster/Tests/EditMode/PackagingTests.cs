using System.Collections.Generic;
using NUnit.Framework;

public class PackagingTests
{
    private static ProductInfo Fone => System.Array.Find(DefaultContent.Products, p => p.Id == "fone_bt");
    private static ProductInfo Luminaria => System.Array.Find(DefaultContent.Products, p => p.Id == "luminaria");

    private static PackagingInput Good(ProductInfo p) => new PackagingInput
    {
        Product = p,
        Inspected = true,
        Box = p.Size,
        PaddingSheets = PackagingEvaluator.RecommendedPadding(p),
        PlacementOffset = 0.05f,
        FlapsClosed = true,
        TapeStrips = new List<float> { 0.95f, 0.9f },
        LabelApplied = true,
        LabelAccuracy = 0.9f,
        SecondsSpent = 35f
    };

    [Test]
    public void GoodPackaging_ScoresHigh_WithLowRisk()
    {
        var s = PackagingEvaluator.Evaluate(Good(Fone));
        Assert.IsTrue(s.Valid);
        Assert.Greater(s.Overall, 0.85f);
        Assert.Less(s.DamageRisk, 0.12f);
    }

    [Test]
    public void BoxTooSmall_IsInvalid()
    {
        var input = Good(Luminaria);
        input.Box = BoxSize.Small;
        var s = PackagingEvaluator.Evaluate(input);
        Assert.IsFalse(s.Valid);
    }

    [Test]
    public void MissingSteps_AreInvalid()
    {
        var noTape = Good(Fone); noTape.TapeStrips.Clear();
        var noLabel = Good(Fone); noLabel.LabelApplied = false;
        var open = Good(Fone); open.FlapsClosed = false;
        Assert.IsFalse(PackagingEvaluator.Evaluate(noTape).Valid);
        Assert.IsFalse(PackagingEvaluator.Evaluate(noLabel).Valid);
        Assert.IsFalse(PackagingEvaluator.Evaluate(open).Valid);
    }

    [Test]
    public void NoPadding_OnFragileProduct_RaisesDamageRisk()
    {
        var careless = Good(Luminaria);
        careless.PaddingSheets = 0;
        var good = PackagingEvaluator.Evaluate(Good(Luminaria));
        var bad = PackagingEvaluator.Evaluate(careless);
        Assert.Greater(bad.DamageRisk, good.DamageRisk + 0.2f);
        Assert.Less(bad.Protection, good.Protection);
    }

    [Test]
    public void OversizedBox_WithoutExtraPadding_IsWorse()
    {
        var oversized = Good(Fone);
        oversized.Box = BoxSize.Large;
        var s = PackagingEvaluator.Evaluate(oversized);
        Assert.Less(s.SizeFit, 0.5f);
        Assert.Less(s.Overall, PackagingEvaluator.Evaluate(Good(Fone)).Overall);
    }

    [Test]
    public void MaterialsCost_IsSumOfParts()
    {
        var input = Good(Fone); // caixa pequena 150 + 2 bolhas 100 + 2 fitas 40 + etiqueta 30
        Assert.AreEqual(320, PackagingEvaluator.MaterialsCost(input));
    }

    [Test]
    public void SlowPackaging_LowersTimeScore()
    {
        var slow = Good(Fone);
        slow.SecondsSpent = 200f;
        Assert.Less(PackagingEvaluator.Evaluate(slow).Time, 0.5f);
    }

    [Test]
    public void Service_Finalize_ChargesMaterials_AndMovesToOrders()
    {
        var wallet = new FakeWallet(10000);
        var inv = new InventoryService();
        inv.TryAdd(StorageLocation.PackingBench, "fone_bt", 1, 8500);
        var service = new PackagingService(wallet, inv);

        Assert.IsTrue(service.TryFinalize(Good(Fone), out var order, out _));
        Assert.AreEqual(10000 - 320, wallet.BalanceCents);
        Assert.AreEqual(1, inv.GetQuantity(StorageLocation.Order, "fone_bt"));
        Assert.AreEqual(0, inv.GetQuantity(StorageLocation.PackingBench, "fone_bt"));
        Assert.AreEqual(8500 + 320, order.TotalCostCents);
        Assert.AreEqual(1, service.Orders.Count);
    }

    [Test]
    public void Service_Finalize_FailsWithoutChanges_WhenProductNotOnBench()
    {
        var wallet = new FakeWallet(10000);
        var inv = new InventoryService();
        var service = new PackagingService(wallet, inv);

        Assert.IsFalse(service.TryFinalize(Good(Fone), out _, out var error));
        Assert.IsNotNull(error);
        Assert.AreEqual(10000, wallet.BalanceCents);
    }
}
