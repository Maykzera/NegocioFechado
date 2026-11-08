using NUnit.Framework;

public class CustomerBrainTests
{
    // Exemplo da conversa: fone de ouvido com preço de referência de R$ 150.
    private static readonly ProductPitch Fone =
        new ProductPitch("fone", ProductCategory.Electronics, 15000, 0.8f);

    private static readonly ProductPitch Martelo =
        new ProductPitch("martelo", ProductCategory.Tools, 4000, 0.8f);

    private static CustomerBrain Rafael(int seed = 1) =>
        new CustomerBrain("rafael", CustomerArchetype.Economico, ProductCategory.Electronics, 15000, seed);

    [Test]
    public void Budget_StaysWithinTenPercentOfExpectation()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var c = Rafael(seed);
            Assert.That(c.BudgetCents, Is.InRange(13500, 16500));
        }
    }

    [Test]
    public void SameSeedAndActions_GiveSameResult()
    {
        CustomerResponse Run()
        {
            var c = Rafael(42);
            c.Greet();
            c.AskAboutNeeds();
            c.PresentProduct(Fone);
            c.ExplainBenefit("bateria", isTrue: false);
            return c.ProposePrice(16000);
        }

        var a = Run();
        var b = Run();
        Assert.AreEqual(a.Type, b.Type);
        Assert.AreEqual(a.PriceCents, b.PriceCents);
        Assert.AreEqual(a.Line, b.Line);
    }

    [Test]
    public void WrongProduct_AsksForAnother()
    {
        var c = Rafael();
        var r = c.PresentProduct(Martelo);
        Assert.AreEqual(CustomerResponseType.WantsOtherProduct, r.Type);
    }

    [Test]
    public void GoodService_RaisesWillingnessToPay()
    {
        var cold = Rafael();
        cold.PresentProduct(Fone);
        long coldWillingness = cold.GetWillingnessCents();

        var warm = Rafael();
        warm.Greet();
        warm.AskAboutNeeds();
        warm.PresentProduct(Fone);
        warm.ExplainBenefit("bateria", true);
        warm.ExplainBenefit("garantia", true);
        warm.AnswerObjection();

        Assert.Greater(warm.GetWillingnessCents(), coldWillingness);
        Assert.Less(coldWillingness, 15000, "Sem atendimento, o cliente econômico não paga o preço cheio.");
    }

    [Test]
    public void Demanding_NeedsArguments_BeforeTalkingPrice()
    {
        var c = new CustomerBrain("e", CustomerArchetype.Exigente, ProductCategory.Electronics, 15000, 5);
        c.PresentProduct(Fone);
        var cold = c.ProposePrice(15000);
        Assert.AreEqual(CustomerResponseType.Doubtful, cold.Type);
        Assert.AreEqual(CustomerState.Negotiating, c.State);

        c.AskAboutNeeds();
        c.ExplainBenefit("bateria", true);
        c.ExplainBenefit("garantia", true);
        c.AnswerObjection();
        Assert.Greater(c.GetWillingnessCents(), 0);
    }

    [Test]
    public void FairPrice_AfterGoodService_IsAccepted()
    {
        var c = Rafael();
        c.Greet();
        c.AskAboutNeeds();
        c.PresentProduct(Fone);
        c.ExplainBenefit("bateria", true);
        c.ExplainBenefit("garantia", true);

        long price = c.GetWillingnessCents();
        var r = c.ProposePrice(price);

        Assert.AreEqual(CustomerResponseType.Accept, r.Type);
        Assert.AreEqual(CustomerState.Accepted, c.State);
        Assert.AreEqual(price, c.AgreedPriceCents);
    }

    [Test]
    public void HighPrice_GetsCounterOfferBelowProposal()
    {
        var c = Rafael();
        c.PresentProduct(Fone);
        var r = c.ProposePrice(20000);

        Assert.AreEqual(CustomerResponseType.CounterOffer, r.Type);
        Assert.Less(r.PriceCents, 20000);
        Assert.LessOrEqual(r.PriceCents, c.BudgetCents);
    }

    [Test]
    public void CounterOffers_RiseOverRounds()
    {
        var c = Rafael();
        c.PresentProduct(Fone);
        var first = c.ProposePrice(20000);
        var second = c.ProposePrice(20000);

        Assert.AreEqual(CustomerResponseType.CounterOffer, second.Type);
        Assert.GreaterOrEqual(second.PriceCents, first.PriceCents);
    }

    [Test]
    public void AcceptingCounter_ClosesDeal()
    {
        var c = Rafael();
        c.PresentProduct(Fone);
        var counter = c.ProposePrice(20000);
        var r = c.AcceptCounterOffer();

        Assert.AreEqual(CustomerResponseType.Accept, r.Type);
        Assert.AreEqual(counter.PriceCents, c.AgreedPriceCents);
    }

    [Test]
    public void Customer_Leaves_WhenPatienceRunsOut()
    {
        var c = new CustomerBrain("apressado", CustomerArchetype.Apressado, ProductCategory.Electronics, 15000, 3);
        c.PresentProduct(Fone);
        c.ProposePrice(40000);
        var r = c.ProposePrice(40000);

        Assert.AreEqual(CustomerState.Left, c.State);
        Assert.AreEqual(CustomerResponseType.Leave, r.Type);
        Assert.IsTrue(c.IsFinished);
    }

    [Test]
    public void Pressure_LowersTrust()
    {
        var c = Rafael();
        float before = c.Trust;
        c.Pressure();
        Assert.Less(c.Trust, before);
    }

    [Test]
    public void FalseClaims_AreAlwaysRecordedForTheMentor()
    {
        var c = new CustomerBrain("d", CustomerArchetype.Desconfiado, ProductCategory.Electronics, 15000, 7);
        c.PresentProduct(Fone);
        c.ExplainBenefit("a_prova_dagua", isTrue: false);

        Assert.IsTrue(System.Linq.Enumerable.Any(c.Memory, e => e.Type == NegotiationEventType.MadeFalseClaim));
    }

    [Test]
    public void Indecisive_AsksForTime_ThenReconsiders()
    {
        bool sawThinking = false;
        for (int seed = 0; seed < 20 && !sawThinking; seed++)
        {
            var c = new CustomerBrain("i", CustomerArchetype.Indeciso, ProductCategory.Electronics, 15000, seed);
            c.AskAboutNeeds();
            c.PresentProduct(Fone);
            c.ExplainBenefit("bateria", true);

            long nearPrice = c.GetWillingnessCents() + 800;
            var r = c.ProposePrice(nearPrice);
            if (r.Type != CustomerResponseType.AskForTime) continue;

            sawThinking = true;
            Assert.AreEqual(CustomerState.Thinking, c.State);
            var decision = c.Reconsider(nearPrice);
            Assert.AreNotEqual(CustomerState.Thinking, c.State);
            Assert.That(decision.Type,
                Is.EqualTo(CustomerResponseType.Accept).Or.EqualTo(CustomerResponseType.CounterOffer));
        }
        Assert.IsTrue(sawThinking, "O cliente indeciso deveria pedir um tempo em algum cenário.");
    }

    [Test]
    public void FinishedConversation_IgnoresFurtherActions()
    {
        var c = Rafael();
        c.EndConversation();
        var r = c.ProposePrice(100);
        Assert.AreEqual(CustomerState.Left, c.State);
        Assert.AreEqual(CustomerResponseType.Leave, r.Type);
    }

    [Test]
    public void Lines_ReplacePriceAndCategory()
    {
        var c = Rafael();
        Assert.That(c.AskAboutNeeds().Line, Does.Contain("eletrônicos"));
        c.PresentProduct(Fone);
        var counter = c.ProposePrice(20000);
        Assert.That(counter.Line, Does.Contain("R$"));
    }

    [Test]
    public void FormatReais_UsesBrazilianFormat()
    {
        Assert.AreEqual("R$ 145,00", CustomerBrain.FormatReais(14500));
        Assert.AreEqual("R$ 1.500,00", CustomerBrain.FormatReais(150000));
        Assert.AreEqual("R$ 0,05", CustomerBrain.FormatReais(5));
    }
}
