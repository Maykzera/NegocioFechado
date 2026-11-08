#if DEALMASTER_ANTHROPIC_SDK
using NUnit.Framework;

/// <summary>Testa a leitura das respostas JSON da IA (sem chamar a API).</summary>
public class ClaudeParsingTests
{
    [Test]
    public void ParsesCustomerTurn_WithPurchase()
    {
        string json = "{\"fala\":\"Tá bom, pode cadastrar por 89,90.\",\"interesse\":82,\"confianca\":75,\"humor\":\"satisfeito\",\"objecao\":\"\",\"decisao\":\"quer_comprar\",\"servico_aceito\":\"netveloz_500\",\"preco_aceito_centavos\":8990}";
        var t = ClaudeCustomerAI.Parse(json);
        Assert.AreEqual(CustomerDecision.WantsToBuy, t.Decision);
        Assert.AreEqual("netveloz_500", t.AcceptedServiceId);
        Assert.AreEqual(8990, t.AcceptedPriceCents);
        Assert.AreEqual("claude", t.Source);
        Assert.IsNull(t.Objection);
    }

    [Test]
    public void InvalidServiceOrPrice_AreIgnored_AndHangUpIsMarked()
    {
        var t = ClaudeCustomerAI.Parse("{\"fala\":\"Não quero!\",\"interesse\":5,\"confianca\":3,\"humor\":\"irritado\",\"objecao\":\"\",\"decisao\":\"desligou\",\"servico_aceito\":\"\",\"preco_aceito_centavos\":0}");
        Assert.AreEqual(CustomerDecision.HungUp, t.Decision);
        StringAssert.Contains("desligou", t.Speech);
        var b = ClaudeCustomerAI.Parse("{\"fala\":\"Sim\",\"interesse\":90,\"confianca\":90,\"humor\":\"ok\",\"objecao\":\"\",\"decisao\":\"quer_comprar\",\"servico_aceito\":\"inexistente\",\"preco_aceito_centavos\":3}");
        Assert.IsNull(b.AcceptedServiceId);
        Assert.IsNull(b.AcceptedPriceCents);
    }

    [Test]
    public void SupervisorParse_EnforcesLieCapEvenIfAiForgets()
    {
        var report = new CallReport { CallId = "C1", LeadName = "Ana", SaleClosed = true };
        report.Lies.Add("não tem fidelidade");
        string json = "{\"nota\":9.5,\"veredito\":\"Ótimo\",\"criterios\":[{\"nome\":\"Transparência\",\"nota\":9}],\"observacoes\":[\"Boa ligação\"],\"sugestoes\":[],\"mentiras\":[]}";
        var r = ClaudeSupervisor.Parse(json, report);
        Assert.LessOrEqual(r.Grade, 4f);
        Assert.Contains("não tem fidelidade", r.LiesFound);
        Assert.IsFalse(r.Approved);
    }
}
#endif
