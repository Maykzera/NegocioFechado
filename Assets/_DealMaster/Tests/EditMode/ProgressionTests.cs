using System.Linq;
using NUnit.Framework;

public class ProgressionTests
{
    [Test]
    public void XpCurve_AndTitles()
    {
        Assert.AreEqual(0, ProgressionService.XpForLevel(1));
        Assert.AreEqual(100, ProgressionService.XpForLevel(2));
        Assert.AreEqual(300, ProgressionService.XpForLevel(3));
        Assert.AreEqual("Vendedor iniciante", ProgressionService.TitleFor(5));
        Assert.AreEqual("Revendedor profissional", ProgressionService.TitleFor(6));
        Assert.AreEqual("Magnata comercial", ProgressionService.TitleFor(31));
    }

    [Test]
    public void AddingXp_LevelsUp_PossiblyMoreThanOnce()
    {
        var p = new ProgressionService();
        int levelUps = 0;
        p.OnLevelUp += _ => levelUps++;
        p.AddXp(320, "teste");
        Assert.AreEqual(3, p.Level);
        Assert.AreEqual(2, levelUps);
        Assert.AreEqual(280, p.XpToNext);
    }

    [Test]
    public void Restore_RecomputesLevel()
    {
        var p = new ProgressionService();
        p.Restore(1500);
        Assert.AreEqual(6, p.Level);
    }

    [Test]
    public void Capacities_GrowWithLevel()
    {
        Assert.AreEqual(10, ProgressionService.CapacitiesFor(1).backpack);
        Assert.AreEqual(14, ProgressionService.CapacitiesFor(2).backpack);
        Assert.AreEqual(100, ProgressionService.CapacitiesFor(3).warehouse);
        Assert.AreEqual(20, ProgressionService.CapacitiesFor(5).backpack);
    }

    [Test]
    public void ProfitableSale_GivesMoreXp_ThanLoss()
    {
        var good = new SaleRecord { PriceCents = 15000, ProductCostCents = 8500 };
        var bad = new SaleRecord { PriceCents = 8000, ProductCostCents = 8500 };
        Assert.Greater(ProgressionService.XpForSale(good), ProgressionService.XpForSale(bad));
    }

    [Test]
    public void Missions_CompleteOnce_IncludingMainMission()
    {
        var m = new MissionService();
        var ctx = new MissionContext { Purchases = 1 };
        Assert.AreEqual(1, m.Check(ctx).Count);
        Assert.AreEqual(0, m.Check(ctx).Count);

        ctx.PackedOrders = 1; ctx.Sales = 1; ctx.ApprovedProfitableSale = true; ctx.TotalProfitCents = 5000;
        var done = m.Check(ctx);
        Assert.IsTrue(done.Any(x => x.Id == "principal"));
        Assert.IsTrue(m.MainMission.Completed);

        var restored = new MissionService();
        restored.Restore(m.CompletedIds);
        Assert.IsTrue(restored.MainMission.Completed);
    }

    [Test]
    public void Clock_AdvancesAndEndsDay()
    {
        var c = new GameClock { GameMinutesPerSecond = 1f };
        int ended = 0;
        c.OnDayEnded += d => ended = d;
        Assert.AreEqual(8, c.Hour);
        c.Advance(60f); // +1h
        Assert.AreEqual(9, c.Hour);
        c.Advance(13 * 60f); // chega às 22h
        Assert.AreEqual(1, ended);
        Assert.AreEqual(2, c.Day);
        Assert.AreEqual(8, c.Hour);
    }

    [Test]
    public void Clock_Paused_DoesNotAdvance()
    {
        var c = new GameClock { Paused = true };
        c.Advance(1000f);
        Assert.AreEqual(8, c.Hour);
    }
}
