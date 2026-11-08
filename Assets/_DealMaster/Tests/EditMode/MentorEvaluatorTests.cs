using System.Linq;
using NUnit.Framework;

public class MentorEvaluatorTests
{
    private static readonly ProductPitch Fone = new ProductPitch("fone_bt", ProductCategory.Electronics, 15000, 0.75f);

    private static NegotiationRecord Record(CustomerBrain brain, bool sale, long price, bool packed = true, float packScore = 0.9f)
    {
        return new NegotiationRecord
        {
            Day = 1,
            CustomerName = brain.CustomerId,
            Archetype = brain.Archetype,
            Need = brain.Need,
            ProductId = "fone_bt",
            ProductCategory = ProductCategory.Electronics,
            ReferencePriceCents = 15000,
            ProductCostCents = 8500,
            MaterialsCostCents = packed ? 320 : 0,
            FeeCents = SalesService.FeeFor(price),
            SaleClosed = sale,
            AgreedPriceCents = sale ? price : 0,
            Packed = packed,
            PackagingScore = packScore,
            FinalTrust = brain.Trust,
            FinalState = brain.State,
            Events = brain.Memory.ToList()
        };
    }

    private static CustomerBrain GoodNegotiation()
    {
        var c = new CustomerBrain("Rafael", CustomerArchetype.Economico, ProductCategory.Electronics, 15000, 1);
        c.Greet();
        c.AskAboutNeeds();
        c.PresentProduct(Fone);
        c.ExplainBenefit("bateria", true);
        c.ExplainBenefit("garantia", true);
        c.AnswerObjection();
        c.ProposePrice(c.GetWillingnessCents());
        Assert.AreEqual(CustomerState.Accepted, c.State);
        return c;
    }

    [Test]
    public void HonestProfitableSale_IsApproved()
    {
        var c = GoodNegotiation();
        var e = MentorEvaluator.Evaluate(Record(c, true, c.AgreedPriceCents));
        Assert.GreaterOrEqual(e.Grade, 7f, string.Join(" | ", e.Criteria.Select(x => $"{x.Name}={x.Value:0.00}")));
        Assert.IsTrue(MentorEvaluator.IsApproved(e));
        Assert.IsTrue(e.Observations.Any(o => o.Contains("honesta")));
    }

    [Test]
    public void SameHistory_GivesSameGrade()
    {
        var c = GoodNegotiation();
        var r = Record(c, true, c.AgreedPriceCents);
        var a = MentorEvaluator.Evaluate(r);
        var b = MentorEvaluator.Evaluate(r);
        Assert.AreEqual(a.Grade, b.Grade);
        CollectionAssert.AreEqual(a.Observations, b.Observations);
        Assert.AreEqual(MentorEvaluator.Version, a.EvaluatorVersion);
    }

    [Test]
    public void Lies_AreAlwaysDetectedByMentor_AndLowerGrade()
    {
        var honest = GoodNegotiation();
        var liar = new CustomerBrain("Rafael", CustomerArchetype.Economico, ProductCategory.Electronics, 15000, 1);
        liar.AskAboutNeeds();
        liar.PresentProduct(Fone);
        liar.ExplainBenefit("a_prova_dagua", false);
        liar.ExplainBenefit("marca_famosa", false);
        liar.ProposePrice(liar.GetWillingnessCents() > 0 ? liar.GetWillingnessCents() : 12000);
        if (liar.State != CustomerState.Accepted && liar.LastCounterOfferCents > 0) liar.AcceptCounterOffer();

        var e = MentorEvaluator.Evaluate(Record(liar, liar.State == CustomerState.Accepted, liar.AgreedPriceCents));
        var h = MentorEvaluator.Evaluate(Record(honest, true, honest.AgreedPriceCents));
        Assert.IsTrue(e.Observations.Any(o => o.Contains("falsa")));
        Assert.Less(e.Grade, h.Grade);
        Assert.Less(e.Criteria.First(x => x.Name == "Transparência").Value, 0.5f);
    }

    [Test]
    public void ExcessiveDiscount_AndLoss_AreFlagged()
    {
        var c = new CustomerBrain("Ana", CustomerArchetype.Apressado, ProductCategory.Electronics, 15000, 2);
        c.PresentProduct(Fone);
        c.ProposePrice(8000);
        var e = MentorEvaluator.Evaluate(Record(c, c.State == CustomerState.Accepted, 8000));
        Assert.IsTrue(e.Observations.Any(o => o.Contains("Desconto excessivo")));
        Assert.IsTrue(e.Observations.Any(o => o.Contains("prejuízo")));
        Assert.AreEqual(0f, e.Criteria.First(x => x.Name == "Margem de lucro").Value);
    }

    [Test]
    public void NotAskingNeeds_AndPressure_AreFlagged_WithSuggestions()
    {
        var c = new CustomerBrain("Bia", CustomerArchetype.Economico, ProductCategory.Electronics, 15000, 3);
        c.PresentProduct(Fone);
        c.Pressure();
        c.EndConversation();
        var e = MentorEvaluator.Evaluate(Record(c, false, 0));
        Assert.IsTrue(e.Observations.Any(o => o.Contains("não perguntou")));
        Assert.IsTrue(e.Observations.Any(o => o.Contains("Pressionar")));
        Assert.IsNotEmpty(e.Suggestions);
        Assert.Less(e.Grade, 5f);
    }

    [Test]
    public void Service_ReviewsInOrder_AndTracksAverage()
    {
        var s = new MentorService();
        var good = GoodNegotiation();
        s.Submit(Record(good, true, good.AgreedPriceCents));
        var bad = new CustomerBrain("Caio", CustomerArchetype.Economico, ProductCategory.Electronics, 15000, 4);
        bad.EndConversation();
        s.Submit(Record(bad, false, 0));

        var first = s.ReviewNext();
        var second = s.ReviewNext();
        Assert.AreEqual("Rafael", first.CustomerName);
        Assert.AreEqual("Caio", second.CustomerName);
        Assert.IsNull(s.ReviewNext());
        Assert.AreEqual(2, s.History.Count);
        Assert.IsTrue(s.HasApprovedProfitableSale);
        Assert.AreEqual(System.Math.Round((first.Grade + second.Grade) / 2, 1), s.RecentAverage(), 0.051);
    }

    [Test]
    public void GreatServiceButLoss_IsNeverApproved()
    {
        var c = GoodNegotiation();
        // mesma conversa excelente, mas vendida abaixo do custo
        var e = MentorEvaluator.Evaluate(Record(c, true, 6200));
        Assert.LessOrEqual(e.Grade, 6.0f);
        Assert.IsFalse(MentorEvaluator.IsApproved(e));
        Assert.IsTrue(e.Observations.Any(o => o.Contains("prejuízo")));
    }

    [Test]
    public void AcceptingLowCounter_IsFlagged_NotAsCustomerAcceptingFirst()
    {
        var c = new CustomerBrain("Ana", CustomerArchetype.Economico, ProductCategory.Electronics, 15000, 5);
        c.AskAboutNeeds();
        c.PresentProduct(Fone);
        c.ProposePrice(20000);
        if (c.LastCounterOfferCents > 0) c.AcceptCounterOffer();
        var e = MentorEvaluator.Evaluate(Record(c, c.State == CustomerState.Accepted, c.AgreedPriceCents));
        Assert.IsFalse(e.Observations.Any(o => o.Contains("aceitou de primeira")));
    }
}
