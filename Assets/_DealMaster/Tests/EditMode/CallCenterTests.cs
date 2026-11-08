using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

public class MessageAnalyzerTests
{
    [Test]
    public void DetectsOpeningIntents()
    {
        var a = MessageAnalyzer.Analyze("Boa tarde! Meu nome é Lucas, falo da Conecta Vendas. Tem um minutinho?");
        Assert.IsTrue(a.Has(PlayerIntent.Greeting));
        Assert.IsTrue(a.Has(PlayerIntent.Introduction));
        Assert.IsTrue(a.Has(PlayerIntent.AskPermission));
    }

    [Test]
    public void DetectsNeedQuestion_ServiceFactsAndPrice()
    {
        Assert.IsTrue(MessageAnalyzer.Analyze("Como está sua internet hoje?").Has(PlayerIntent.AskNeed));

        var a = MessageAnalyzer.Analyze("A NetVeloz tem 500 mega e instalação grátis, fica R$ 99,90 por mês.");
        CollectionAssert.Contains(a.ServicesMentioned, "netveloz_500");
        Assert.AreEqual(2, a.FactsUsed.Count);
        Assert.AreEqual(9990, a.PriceCents);
        Assert.IsFalse(a.Has(PlayerIntent.Lie));
    }

    [Test]
    public void DetectsLies_AgainstServiceInFocus()
    {
        var net = ServiceCatalog.Get("netveloz_500");
        var a = MessageAnalyzer.Analyze("E o melhor: não tem fidelidade, cancela quando quiser!", new[] { net });
        Assert.IsTrue(a.Has(PlayerIntent.Lie));
        CollectionAssert.Contains(a.LiesUsed, "não tem fidelidade");
    }

    [Test]
    public void SameWords_CanBeTrueForOneServiceAndFalseForAnother()
    {
        var seguro = ServiceCatalog.Get("protege_celular"); // realmente sem fidelidade
        var a = MessageAnalyzer.Analyze("É sem fidelidade, você cancela quando quiser.", new[] { seguro });
        Assert.IsFalse(a.Has(PlayerIntent.Lie));
        Assert.IsTrue(a.Has(PlayerIntent.Benefit));
    }

    [Test]
    public void DetectsPressureAndRudeness()
    {
        Assert.IsTrue(MessageAnalyzer.Analyze("É só hoje, última chance!").Has(PlayerIntent.Pressure));
        Assert.IsTrue(MessageAnalyzer.Analyze("Deixa de ser burro").Has(PlayerIntent.Rude));
    }

    [Test]
    public void PriceExtraction_IgnoresPlainNumbers()
    {
        Assert.AreEqual(8990, MessageAnalyzer.ExtractPrice(TextNormalizer.Normalize("faço por 89,90")));
        Assert.AreEqual(7000, MessageAnalyzer.ExtractPrice(TextNormalizer.Normalize("fica 70 reais")));
        Assert.IsNull(MessageAnalyzer.ExtractPrice(TextNormalizer.Normalize("são 500 mega")));
    }
}

public class OfflineCustomerTests
{
    private static Lead InternetLead(CustomerArchetype a = CustomerArchetype.Economico, int seed = 3)
    {
        var lead = LeadGenerator.Generate(seed, 1);
        lead.Archetype = a;
        lead.Need = ServiceCategory.Internet;
        lead.NeedServiceId = "netveloz_500";
        lead.Pain = "a internet de casa vive caindo";
        lead.BudgetCents = 10500;
        lead.DoNotCall = false;
        lead.PickupChance = 1f;
        return lead;
    }

    [Test]
    public void GoodHonestCall_LeadsToPurchase()
    {
        var c = new OfflineCustomerAI(InternetLead());
        c.Answer();
        c.Respond("Boa tarde! Meu nome é Lucas, falo da Conecta Vendas. Tem um minutinho?");
        var need = c.Respond("Como está a sua internet hoje?");
        StringAssert.Contains("internet", need.Speech);
        c.Respond("Entendo. Temos a NetVeloz Fibra 500 Mega, com 500 mega e instalação grátis.");
        c.Respond("Ela vem com roteador wi-fi 6 incluso e suporte 24 horas.");
        c.Respond("Entendo sua preocupação. A fidelidade é de 12 meses, sou sincero com você.");
        var deal = c.Respond("Fica R$ 99,90 por mês. Posso cadastrar?");
        if (deal.Decision != CustomerDecision.WantsToBuy) deal = c.Respond("Consigo fazer por R$ 89,90. Fechamos?");
        Assert.AreEqual(CustomerDecision.WantsToBuy, deal.Decision, deal.Speech);
        Assert.AreEqual("netveloz_500", deal.AcceptedServiceId);
        Assert.IsTrue(deal.AcceptedPriceCents >= 8490);
    }

    [Test]
    public void ColdPitchWithPriceRightAway_IsNotEnough()
    {
        var c = new OfflineCustomerAI(InternetLead());
        c.Answer();
        var r = c.Respond("Quer a NetVeloz por R$ 99,90?");
        Assert.AreNotEqual(CustomerDecision.WantsToBuy, r.Decision);
    }

    [Test]
    public void Rudeness_HangsUp()
    {
        var c = new OfflineCustomerAI(InternetLead());
        c.Answer();
        Assert.AreEqual(CustomerDecision.HungUp, c.Respond("Escuta aqui, seu idiota").Decision);
    }

    [Test]
    public void SuspiciousCustomer_UsuallyCatchesLies()
    {
        int caught = 0;
        for (int seed = 0; seed < 40; seed++)
        {
            var c = new OfflineCustomerAI(InternetLead(CustomerArchetype.Desconfiado, seed));
            c.Answer();
            c.Respond("Oi, falo da Conecta Vendas. Temos a internet NetVeloz.");
            float before = c.TrustValue;
            c.Respond("E não tem fidelidade, cancela quando quiser.");
            if (c.TrustValue < before) caught++;
        }
        Assert.Greater(caught, 20);
    }

    [Test]
    public void DoNotCall_RespectingEndsPolitely()
    {
        var lead = InternetLead();
        lead.DoNotCall = true;
        var c = new OfflineCustomerAI(lead);
        StringAssert.Contains("tirar meu número", c.Answer().Speech);
        var r = c.Respond("Claro, peço desculpas. Vou retirar seu número da nossa lista agora mesmo.");
        Assert.AreEqual(CustomerDecision.Refused, r.Decision);
    }

    [Test]
    public void WrongService_IsRejected()
    {
        var c = new OfflineCustomerAI(InternetLead());
        c.Answer();
        var r = c.Respond("Que tal um plano odontológico SorrisoBom?");
        Assert.AreNotEqual(CustomerDecision.WantsToBuy, r.Decision);
        Assert.Less(r.Interest, 30);
    }
}

public class SalesDeskAndSupervisorTests
{
    private static Lead Lead()
    {
        var l = LeadGenerator.Generate(5, 1);
        l.Archetype = CustomerArchetype.Apressado; l.Need = ServiceCategory.Internet; l.NeedServiceId = "netveloz_500";
        l.BudgetCents = 11000; l.DoNotCall = false; l.PickupChance = 1f; l.Pain = "a internet cai";
        return l;
    }

    private static async Task<CallSession> Call(bool lie)
    {
        var lead = Lead();
        var call = new CallSession("C1", lead, new CallContext(), new OfflineCustomerAI(lead), null, 1);
        await call.DialAsync(0.0);
        await call.SendAsync("Bom dia! Meu nome é Lucas, falo da Conecta Vendas. Tem um minuto?");
        await call.SendAsync("Como está a sua internet hoje?");
        await call.SendAsync("Temos a NetVeloz com 500 mega, instalação grátis e suporte 24 horas.");
        if (lie) await call.SendAsync("E não tem fidelidade nenhuma.");
        else await call.SendAsync("A fidelidade é de 12 meses, para ser transparente.");
        for (int i = 0; i < 3 && call.Agreement == null && call.State == CallState.InCall; i++)
            await call.SendAsync("Fica R$ 99,90 por mês, posso cadastrar?");
        return call;
    }

    [Test]
    public async Task RegisterSale_RequiresAgreement_PaysCommission()
    {
        var wallet = new FakeWallet(0);
        var desk = new SalesDesk(wallet, new FinanceLedger());
        var call = await Call(false);
        Assert.IsNotNull(call.Agreement, string.Join(" | ", call.Transcript.Select(t => t.Text)));

        // Valor diferente do combinado na ligação é recusado.
        Assert.IsFalse(desk.TryRegister(call, "netveloz_500", 7000, 1, out _, out var wrong));
        StringAssert.Contains("combinado", wrong);
        Assert.IsFalse(desk.TryRegister(call, "sorriso_bom", 3990, 1, out _, out _));

        Assert.IsTrue(desk.TryRegister(call, "netveloz_500", call.Agreement.AcceptedPriceCents.Value, 1, out var sale, out var err), err);
        Assert.AreEqual(sale.CommissionCents, wallet.BalanceCents);
        Assert.IsFalse(desk.TryRegister(call, "netveloz_500", call.Agreement.AcceptedPriceCents.Value, 1, out _, out _));
    }

    [Test]
    public async Task NoAgreement_CannotRegister()
    {
        var lead = Lead();
        var call = new CallSession("C2", lead, new CallContext(), new OfflineCustomerAI(lead), null, 1);
        await call.DialAsync(0.0);
        var desk = new SalesDesk(new FakeWallet(0), new FinanceLedger());
        Assert.IsFalse(desk.TryRegister(call, "netveloz_500", 9990, 1, out _, out var e));
        StringAssert.Contains("ainda não aceitou", e);
    }

    [Test]
    public async Task Supervisor_ApprovesHonestCall_ReprovesLies()
    {
        var honest = await Call(false);
        honest.HangUp();
        var good = OfflineSupervisor.Review(CallReport.From(honest, null));
        var liar = await Call(true);
        liar.HangUp();
        var bad = OfflineSupervisor.Review(CallReport.From(liar, null));

        Assert.Greater(good.Grade, bad.Grade);
        Assert.LessOrEqual(bad.Grade, 4f);
        Assert.IsFalse(bad.Approved);
        Assert.IsTrue(bad.Observations.Any(o => o.Contains("Informação falsa")));
        Assert.AreEqual(OfflineSupervisor.Review(CallReport.From(honest, null)).Grade, good.Grade);
    }
}

public class CallCenterGameTests
{
    [Test]
    public async Task FullDay_CallSaleReviewAndBonus()
    {
        var wallet = new FakeWallet(5000);
        var game = new CallCenterGame(wallet, 42);
        Assert.AreEqual(CallCenterGame.LeadsPerDay, game.Queue.Count);

        // Força um lead conhecido para o teste ser estável.
        var lead = game.Queue[0];
        lead.Archetype = CustomerArchetype.Apressado; lead.Need = ServiceCategory.Internet; lead.NeedServiceId = "netveloz_500";
        lead.BudgetCents = 11000; lead.DoNotCall = false; lead.PickupChance = 1f; lead.Pain = "a internet cai";

        var call = await game.StartCallAsync(lead);
        Assert.AreEqual(CallState.InCall, call.State);
        Assert.AreEqual(90, game.Energy);
        await call.SendAsync("Bom dia! Meu nome é Lucas, falo da Conecta Vendas. Tem um minuto?");
        await call.SendAsync("Como está a sua internet hoje?");
        await call.SendAsync("Temos a NetVeloz com 500 mega, instalação grátis e suporte 24 horas.");
        await call.SendAsync("A fidelidade é de 12 meses, para ser transparente.");
        for (int i = 0; i < 3 && call.Agreement == null; i++) await call.SendAsync("Fica R$ 99,90 por mês, posso cadastrar?");

        Assert.IsTrue(game.TryRegisterSale("netveloz_500", call.Agreement.AcceptedPriceCents.Value, out var err), err);
        game.FinishCall();
        Assert.AreEqual(1, game.PendingReviews.Count);
        Assert.IsTrue(game.Missions.IsDone("primeira_venda"));

        var review = await game.ReviewNextAsync();
        Assert.IsTrue(review.Approved, $"{review.Grade}: {string.Join(" | ", review.Observations)}");
        Assert.IsTrue(game.Missions.IsDone("principal"));

        Assert.IsTrue(game.BuyCoffee(out _));
        string summary = game.EndDay();
        StringAssert.Contains("Fim do dia 1", summary);
        Assert.AreEqual(2, game.Day);
        Assert.AreEqual(CallCenterGame.MaxEnergy, game.Energy);
    }

    [Test]
    public async Task LieDiscoveredBySupervisor_CancelsSale()
    {
        var wallet = new FakeWallet(0);
        var game = new CallCenterGame(wallet, 7);
        var lead = game.Queue[0];
        lead.Archetype = CustomerArchetype.Apressado; lead.Need = ServiceCategory.Internet; lead.NeedServiceId = "netveloz_500";
        lead.BudgetCents = 11000; lead.DoNotCall = false; lead.PickupChance = 1f; lead.Pain = "a internet cai";
        lead.Seed = 1; // cliente que não percebe a mentira nesta semente

        var call = await game.StartCallAsync(lead);
        await call.SendAsync("Bom dia! Meu nome é Lucas, falo da Conecta Vendas. Tem um minuto?");
        await call.SendAsync("Como está a sua internet hoje?");
        await call.SendAsync("Temos a NetVeloz com 500 mega, instalação grátis e suporte 24 horas.");
        await call.SendAsync("E não tem fidelidade, cancela quando quiser.");
        for (int i = 0; i < 4 && call.Agreement == null && call.State == CallState.InCall; i++) await call.SendAsync("Fica R$ 99,90 por mês, posso cadastrar?");
        if (call.Agreement == null) Assert.Inconclusive("O cliente percebeu a mentira e não comprou.");

        game.TryRegisterSale("netveloz_500", call.Agreement.AcceptedPriceCents.Value, out _);
        long afterSale = wallet.BalanceCents;
        game.FinishCall();
        var review = await game.ReviewNextAsync();

        Assert.IsTrue(review.ChargebackApplied);
        Assert.Less(wallet.BalanceCents, afterSale);
        Assert.IsTrue(game.Sales.Sales[0].ChargedBack);
    }

    [Test]
    public void Upgrades_AndLevelGates()
    {
        var wallet = new FakeWallet(100000);
        var game = new CallCenterGame(wallet, 1);
        Assert.IsTrue(game.BuyUpgrade(UpgradeCatalog.Headset, out _));
        Assert.IsFalse(game.BuyUpgrade(UpgradeCatalog.Headset, out _));
        Assert.IsFalse(game.BuyUpgrade(UpgradeCatalog.QualifiedLeads, out var e));
        StringAssert.Contains("nível", e);
    }

    [Test]
    public void LeadGenerator_IsDeterministic_AndRespectsLevel()
    {
        var a = LeadGenerator.Generate(99, 1, true);
        var b = LeadGenerator.Generate(99, 1, true);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.NeedServiceId, b.NeedServiceId);
        Assert.LessOrEqual(ServiceCatalog.Get(a.NeedServiceId).RequiredLevel, 1);
    }

    [Test]
    public void Catalog_IsConsistent()
    {
        foreach (var s in ServiceCatalog.All)
        {
            Assert.Less(s.MinPriceCents, s.MonthlyPriceCents, s.Id);
            Assert.IsNotEmpty(s.Facts, s.Id);
            Assert.IsNotEmpty(s.Lies, s.Id);
        }
        Assert.AreEqual(ServiceCatalog.All.Length, ServiceCatalog.All.Select(s => s.Id).Distinct().Count());
    }

    [Test]
    public void Money_FormatsBrazilian()
    {
        Assert.AreEqual("R$ 99,90", Money.Format(9990));
        Assert.AreEqual("R$ 1.500,00", Money.Format(150000));
    }
}
