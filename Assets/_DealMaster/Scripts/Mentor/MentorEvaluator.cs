using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Registro estruturado de uma negociação, entregue ao mestre.</summary>
public sealed class NegotiationRecord
{
    public int Day;
    public string CustomerName;
    public CustomerArchetype Archetype;
    public ProductCategory Need;
    public string ProductId;
    public ProductCategory? ProductCategory;
    public long ReferencePriceCents;
    public long ProductCostCents;
    public long MaterialsCostCents;
    public long FeeCents;
    public bool SaleClosed;
    public long AgreedPriceCents;
    public bool Packed;
    public float PackagingScore;
    public float FinalTrust;
    public CustomerState FinalState;
    public List<NegotiationEvent> Events = new List<NegotiationEvent>();
}

public readonly struct CriterionScore
{
    public readonly string Name;
    public readonly float Value;

    public CriterionScore(string name, float value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>Avaliação do mestre sobre uma negociação.</summary>
public sealed class MentorEvaluation
{
    public int EvaluatorVersion;
    public int Day;
    public string CustomerName;
    public string ProductId;
    public bool SaleClosed;
    public long AgreedPriceCents;
    public float Grade;
    public List<CriterionScore> Criteria = new List<CriterionScore>();
    public List<string> Observations = new List<string>();
    public List<string> Suggestions = new List<string>();
    public string Verdict;
}

/// <summary>
/// O mestre negociador: analisa a negociação inteira e dá uma nota de 0 a 10, com justificativa.
/// Determinístico: o mesmo histórico e a mesma versão do avaliador geram sempre a mesma nota.
/// </summary>
public static class MentorEvaluator
{
    public const int Version = 1;

    private static readonly (string name, float weight)[] Weights =
    {
        ("Compreensão da necessidade", 0.14f),
        ("Conhecimento do produto", 0.12f),
        ("Clareza dos argumentos", 0.08f),
        ("Tratamento de objeções", 0.10f),
        ("Estratégia de preço", 0.12f),
        ("Margem de lucro", 0.12f),
        ("Transparência", 0.14f),
        ("Satisfação do cliente", 0.10f),
        ("Resultado da venda", 0.08f)
    };

    public static MentorEvaluation Evaluate(NegotiationRecord r)
    {
        if (r == null) throw new ArgumentNullException(nameof(r));
        var ev = r.Events ?? new List<NegotiationEvent>();
        var eval = new MentorEvaluation
        {
            EvaluatorVersion = Version,
            Day = r.Day,
            CustomerName = r.CustomerName,
            ProductId = r.ProductId,
            SaleClosed = r.SaleClosed,
            AgreedPriceCents = r.AgreedPriceCents
        };

        int Count(NegotiationEventType t) => ev.Count(e => e.Type == t);
        int IndexOf(NegotiationEventType t) => ev.FindIndex(e => e.Type == t);

        int asked = Count(NegotiationEventType.AskedNeeds);
        int presented = Count(NegotiationEventType.PresentedProduct);
        int trueArgs = Count(NegotiationEventType.ExplainedBenefit);
        int lies = Count(NegotiationEventType.MadeFalseClaim);
        int liesCaught = Count(NegotiationEventType.FalseClaimDetected);
        int answered = Count(NegotiationEventType.AnsweredObjection);
        int pressured = Count(NegotiationEventType.Pressured);
        int counters = Count(NegotiationEventType.CustomerCountered) + Count(NegotiationEventType.CustomerAskedForTime);
        var proposals = ev.Where(e => e.Type == NegotiationEventType.ProposedPrice).Select(e => e.PriceCents).ToList();
        bool rightProduct = r.ProductCategory.HasValue && r.ProductCategory.Value == r.Need;
        bool askedBeforePresenting = asked > 0 && (presented == 0 || IndexOf(NegotiationEventType.AskedNeeds) < IndexOf(NegotiationEventType.PresentedProduct));
        long reference = Math.Max(1, r.ReferencePriceCents);

        // 1. Necessidade
        float need = (askedBeforePresenting ? 0.5f : asked > 0 ? 0.3f : 0f) + (rightProduct ? 0.5f : 0f);
        if (presented > 1) need -= 0.1f * (presented - 1);

        // 2. Conhecimento
        float knowledge = presented == 0 ? 0f : Math.Min(1f, trueArgs / 2f);

        // 3. Clareza
        float clarity = trueArgs + lies == 0 ? (presented > 0 ? 0.4f : 0f) : (float)trueArgs / (trueArgs + lies);

        // 4. Objeções
        float objections = counters == 0 ? (answered > 0 ? 1f : 0.8f) : Math.Min(1f, 0.4f + 0.3f * answered);

        // 5. Estratégia de preço
        float pricing = 0f;
        if (proposals.Count > 0)
        {
            double first = (double)proposals[0] / reference;
            pricing = first >= 0.95 && first <= 1.2 ? 1f
                : first >= 0.85 && first <= 1.35 ? 0.7f
                : first > 1.6 || first < 0.7 ? 0.2f : 0.45f;
            if (proposals.Count > 4) pricing -= 0.15f;
            if (r.SaleClosed && r.AgreedPriceCents < reference * 0.8) pricing -= 0.3f;
        }

        // 6. Margem
        long profit = r.AgreedPriceCents - r.FeeCents - r.ProductCostCents - r.MaterialsCostCents;
        float margin = !r.SaleClosed || r.AgreedPriceCents <= 0 ? 0f
            : profit <= 0 ? 0f : Math.Min(1f, (float)profit / r.AgreedPriceCents / 0.35f);

        // 7. Transparência
        float transparency = Math.Max(0f, 1f - 0.4f * lies - 0.2f * liesCaught);

        // 8. Satisfação
        float satisfaction = r.FinalState == CustomerState.Accepted ? Math.Min(1f, 0.4f + r.FinalTrust * 0.8f)
            : r.FinalState == CustomerState.Rejected ? 0.4f : 0.2f;
        satisfaction -= 0.15f * pressured;
        if (r.SaleClosed && !r.Packed) satisfaction -= 0.1f;

        // 9. Resultado
        float result = r.SaleClosed ? (profit > 0 ? 1f : 0.5f) : 0.2f;

        float[] values = { need, knowledge, clarity, objections, pricing, margin, transparency, satisfaction, result };
        float total = 0f;
        for (int i = 0; i < Weights.Length; i++)
        {
            float v = Clamp01(values[i]);
            eval.Criteria.Add(new CriterionScore(Weights[i].name, v));
            total += v * Weights[i].weight;
        }
        eval.Grade = (float)Math.Round(total * 10f, 1);

        // O mestre não aprova venda com prejuízo nem desconto excessivo, por melhor que tenha sido o atendimento.
        if (r.SaleClosed && profit <= 0) eval.Grade = Math.Min(eval.Grade, 6.0f);
        else if (r.SaleClosed && r.AgreedPriceCents < reference * 0.8) eval.Grade = Math.Min(eval.Grade, 6.5f);

        // Observações
        var obs = eval.Observations;
        if (asked == 0) obs.Add("Você não perguntou o que o cliente procurava.");
        else if (askedBeforePresenting) obs.Add("Boa abertura: entendeu a necessidade antes de oferecer.");
        if (presented > 0 && !rightProduct) obs.Add("O produto apresentado não atendia à necessidade do cliente.");
        if (lies > 0) obs.Add(liesCaught > 0
            ? $"Você usou {lies} informação(ões) falsa(s) e o cliente percebeu. Isso destrói a confiança."
            : $"Você usou {lies} informação(ões) falsa(s). O cliente não percebeu, mas eu percebi.");
        if (pressured > 0) obs.Add("Pressionar o cliente costuma afastá-lo.");
        if (r.SaleClosed && r.AgreedPriceCents < reference * 0.8) obs.Add("Desconto excessivo: vendeu bem abaixo do preço de mercado.");
        if (r.SaleClosed && profit <= 0) obs.Add("A venda deu prejuízo depois de taxas e embalagem.");
        bool acceptedFirstOffer = r.SaleClosed && proposals.Count == 1 && proposals[0] == r.AgreedPriceCents;
        if (acceptedFirstOffer && r.AgreedPriceCents < reference * 0.9)
            obs.Add("Oportunidade perdida: o cliente aceitou de primeira um preço baixo. Dava para pedir mais.");
        if (Count(NegotiationEventType.AcceptedCounter) > 0 && r.AgreedPriceCents < reference * 0.8)
            obs.Add("Você aceitou uma contraproposta muito baixa. Antes de aceitar, tente argumentar ou contrapropor.");
        if (counters > 0 && answered == 0) obs.Add("O cliente demonstrou dúvidas e elas não foram respondidas.");
        if (r.SaleClosed && !r.Packed) obs.Add("O produto foi entregue sem embalagem, com mais risco de avaria.");
        if (r.SaleClosed && r.Packed && r.PackagingScore < 0.6f) obs.Add("A embalagem foi fraca e pode gerar reclamação.");
        if (r.SaleClosed && profit > 0 && lies == 0 && pressured == 0) obs.Add("Venda honesta e lucrativa.");

        // Sugestões a partir dos critérios mais fracos
        foreach (var c in eval.Criteria.OrderBy(c => c.Value).ThenBy(c => c.Name, StringComparer.Ordinal).Take(3))
        {
            if (c.Value >= 0.75f) break;
            eval.Suggestions.Add(Suggestion(c.Name));
        }

        eval.Verdict = eval.Grade >= 8.5f ? "Excelente negociação."
            : eval.Grade >= 7f ? "Aprovado. Boa negociação, com pontos a melhorar."
            : eval.Grade >= 5f ? "Regular. Revise os pontos abaixo."
            : "Reprovado. Vamos treinar o básico.";
        return eval;
    }

    public static bool IsApproved(MentorEvaluation e) => e != null && e.Grade >= 7f;

    private static string Suggestion(string criterion)
    {
        switch (criterion)
        {
            case "Compreensão da necessidade": return "Comece perguntando o que o cliente procura e apresente o produto certo.";
            case "Conhecimento do produto": return "Explique pelo menos duas vantagens reais do produto.";
            case "Clareza dos argumentos": return "Use argumentos verdadeiros e específicos, sem exageros.";
            case "Tratamento de objeções": return "Quando o cliente hesitar, responda à dúvida antes de mexer no preço.";
            case "Estratégia de preço": return "Abra perto do preço de mercado e conceda descontos aos poucos.";
            case "Margem de lucro": return "Calcule o custo com taxa e embalagem antes de aceitar uma contraproposta.";
            case "Transparência": return "Nunca invente características. A confiança vale mais que uma venda.";
            case "Satisfação do cliente": return "Evite pressão e construa confiança com atendimento.";
            default: return "Tente concluir a venda depois de construir interesse e confiança.";
        }
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
