using NUnit.Framework;

public class CustomerMemoryTests
{
    [Test]
    public void Stranger_HasNoBonus_AndNoGreeting()
    {
        var m = new CustomerMemory();
        Assert.AreEqual(0f, m.TrustBonus("Ana"));
        Assert.IsNull(m.Greeting("Ana"));
    }

    [Test]
    public void GoodPurchases_RaiseTrust_ComplaintsAndLies_LowerIt()
    {
        var m = new CustomerMemory();
        m.RecordPurchase("Ana", 15000);
        m.RecordPurchase("Ana", 9000);
        Assert.Greater(m.TrustBonus("Ana"), 0f);
        Assert.AreEqual(24000, m.Get("Ana").TotalSpentCents);

        m.RecordComplaint("Bia");
        m.RecordLieDetected("Bia");
        Assert.Less(m.TrustBonus("Bia"), 0f);
        Assert.That(m.Greeting("Bia"), Does.Contain("problema"));
    }

    [Test]
    public void Bonus_IsClamped()
    {
        var m = new CustomerMemory();
        for (int i = 0; i < 20; i++) m.RecordPurchase("Caio", 100);
        for (int i = 0; i < 20; i++) m.RecordComplaint("Duda");
        Assert.LessOrEqual(m.TrustBonus("Caio"), 0.25f);
        Assert.GreaterOrEqual(m.TrustBonus("Duda"), -0.3f);
    }

    [Test]
    public void ReturningHappyCustomer_StartsWithMoreTrust()
    {
        var m = new CustomerMemory();
        m.RecordPurchase("Rafael", 15000);

        var fresh = new CustomerBrain("Rafael", CustomerArchetype.Economico, ProductCategory.Electronics, 15000, 1);
        var returning = new CustomerBrain("Rafael", CustomerArchetype.Economico, ProductCategory.Electronics, 15000, 1);
        returning.ApplyRelationship(m.TrustBonus("Rafael"));

        Assert.Greater(returning.Trust, fresh.Trust);
    }
}
