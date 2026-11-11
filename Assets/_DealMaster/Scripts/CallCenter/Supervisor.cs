using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Serializable]
public sealed class ReviewCriterion
{
    public string Name;
    public float Score; // 0 a 1

    public ReviewCriterion() { }
    public ReviewCriterion(string name, float score) { Name = name; Score = score; }
}

/// <summary>Avaliação do supervisor sobre uma ligação.</summary>
[Serializable]
public sealed class SupervisorReview
{
    public string CallId;
    public int Day;
    public string LeadName;
    public float Grade;
    public string Verdict;
    public List<ReviewCriterion> Criteria = new List<ReviewCriterion>();
    public List<string> Observations = new List<string>();
    public List<string> Suggestions = new List<string>();
    public List<string> LiesFound = new List<string>();
    public bool SaleClosed;
    public bool ChargebackApplied;
    public string Source;

    public bool Approved => Grade >= 7f && LiesFound.Count == 0;
}

public interface ISupervisorAI
{
    Task<SupervisorReview> ReviewAsync(CallReport report);
}

/// <summary>
/// Supervisor offline: avalia a ligação pelos sinais do relatório.
/// Determinístico: o mesmo relatório gera sempre a mesma nota.
/// </summary>
public sealed class OfflineSupervisor : ISupervisorAI
{
    public const int Version = 1;

    private static readonly (string name, float weight)[] Weights =
    {
        ("Abertura e apresentação", 0.10f),
        ("Descoberta da necessidade", 0.15f),
        ("Oferta adequada", 0.12f),
        ("Argumentação", 0.12f),
        ("Tratamento de objeções", 0.10f),
        ("Negociação de preço", 0.10f),
        ("Transparência", 0.16f),
        ("Respeito ao cliente", 0.07f),
        ("Fechamento", 0.08f),
    };

    public Task<SupervisorReview> ReviewAsync(CallReport report) => Task.FromResult(Review(report));

    public static SupervisorReview Review(CallReport r)
    {
        var review = new SupervisorReview { CallId = r.CallId, Day = r.Day, LeadName = r.LeadName, SaleClosed = r.SaleClosed, Source = "offline" };
        var obs = review.Observations;

        if (r.Outcome == CallOutcome.NoAnswer)
        {
            review.Grade = 0;
            review.Verdict = "Ninguém atendeu. Nada a avaliar.";
            return review;
        }

        float opening = (r.Greeted ? 0.35f : 0f) + (r.Introduced ? 0.4f : 0f) + (r.AskedPermission ? 0.25f : 0f);
        float discovery = Math.Min(1f, r.NeedQuestionsBeforePitch * 0.5f + (r.NeedQuestions - r.NeedQuestionsBeforePitch) * 0.15f);
        float fit = r.FirstPitchServiceId == null ? 0f : r.PitchedFit ? 1f : 0.2f;
        float arguments = r.FirstPitchServiceId == null ? 0f : Math.Min(1f, r.Facts.Count / 3f);
        float objections = r.ObjectionsRaised == 0 ? (r.EmpathyCount > 0 ? 1f : 0.75f) : Math.Min(1f, 0.3f + 0.7f * r.ObjectionsAnswered / r.ObjectionsRaised);
        float price = 0.5f;
        if (r.SaleClosed && r.ListPriceCents > 0)
        {
            double ratio = (double)r.SoldPriceCents / r.ListPriceCents;
            price = ratio >= 0.97 ? 1f : ratio >= 0.9 ? 0.8f : ratio >= 0.85 ? 0.6f : 0.4f;
        }
        float transparency = Math.Max(0f, 1f - 0.5f * r.Lies.Count);
        float respect = Math.Max(0f, 1f - 0.35f * r.PressureCount - 1f * r.RudeCount);
        if (r.DoNotCall) respect = r.RespectedDoNotCall ? 1f : 0f;
        float closing = r.SaleClosed ? 1f : r.Outcome == CallOutcome.HungUp ? 0.1f : 0.35f;
        if (r.DoNotCall && r.RespectedDoNotCall) { closing = 1f; fit = 1f; arguments = 1f; discovery = 1f; price = 1f; objections = 1f; }

        float[] values = { opening, discovery, fit, arguments, objections, price, transparency, respect, closing };
        float total = 0f;
        for (int i = 0; i < Weights.Length; i++)
        {
            float v = Clamp01(values[i]);
            review.Criteria.Add(new ReviewCriterion(Weights[i].name, v));
            total += v * Weights[i].weight;
        }
        float grade = (float)Math.Round(total * 10f, 1);

        // Regras que nenhum bom atendimento compensa.
        if (r.Lies.Count > 0) grade = Math.Min(grade, 4f);
        if (r.RudeCount > 0) grade = Math.Min(grade, 2f);
        if (r.DoNotCall && !r.RespectedDoNotCall) grade = Math.Min(grade, 3f);
        review.Grade = grade;
        review.LiesFound.AddRange(r.Lies);

        // Observações
        if (r.DoNotCall)
            obs.Add(r.RespectedDoNotCall ? "O cliente pediu para não receber ligações e você respeitou. Correto." : "O cliente pediu para não ser incomodado e você insistiu. Isso gera reclamação e multa para a empresa.");
        if (!r.Introduced) obs.Add("Você não se apresentou nem disse de qual empresa estava ligando.");
        if (r.NeedQuestionsBeforePitch > 0) obs.Add("Boa descoberta: perguntou sobre a necessidade antes de oferecer.");
        else if (r.FirstPitchServiceId != null) obs.Add("Você ofereceu o serviço antes de entender o que o cliente precisava.");
        if (r.FirstPitchServiceId != null && !r.PitchedFit) obs.Add("O primeiro serviço oferecido não tinha a ver com a necessidade do cliente.");
        if (r.Facts.Count > 0) obs.Add($"Argumentos verdadeiros usados: {string.Join("; ", r.Facts)}.");
        foreach (var lie in r.Lies) obs.Add($"Informação falsa: você disse que o serviço \"{lie}\". Isso não é verdade.");
        if (r.PressureCount > 0) obs.Add("Você pressionou o cliente. Pressão afasta e gera arrependimento.");
        if (r.RudeCount > 0) obs.Add("Falta de educação com o cliente é inaceitável.");
        if (r.ObjectionsRaised > r.ObjectionsAnswered) obs.Add("O cliente levantou objeções que ficaram sem resposta.");
        if (r.SaleClosed && r.ListPriceCents > 0 && r.SoldPriceCents < r.ListPriceCents * 0.9) obs.Add("Você deu um desconto grande. Tente defender o valor antes de baixar o preço.");
        if (r.SaleClosed && r.Lies.Count == 0 && r.PressureCount == 0) obs.Add("Venda honesta. É assim que se trabalha.");

        foreach (var c in review.Criteria.OrderBy(c => c.Score).ThenBy(c => c.Name, StringComparer.Ordinal).Take(3))
            if (c.Score < 0.75f) review.Suggestions.Add(Suggestion(c.Name));

        review.Verdict = review.LiesFound.Count > 0 ? "Reprovado: houve informação falsa. Se teve venda, ela será cancelada."
            : grade >= 8.5f ? "Excelente ligação."
            : grade >= 7f ? "Aprovado. Boa ligação, com pontos a melhorar."
            : grade >= 5f ? "Regular. Revise os pontos abaixo."
            : "Reprovado. Vamos treinar.";
        return review;
    }

    public static string Suggestion(string criterion)
    {
        switch (criterion)
        {
            case "Abertura e apresentação": return "Cumprimente, diga seu nome e a empresa, e pergunte se a pessoa pode falar agora.";
            case "Descoberta da necessidade": return "Faça perguntas abertas antes de oferecer: \"Como está sua internet hoje?\"";
            case "Oferta adequada": return "Ofereça o serviço que resolve o problema que o cliente contou.";
            case "Argumentação": return "Use os argumentos verdadeiros do catálogo, ligados ao problema do cliente.";
            case "Tratamento de objeções": return "Quando o cliente hesitar, mostre que entendeu e responda a dúvida dele.";
            case "Negociação de preço": return "Defenda o valor antes de dar desconto; desconto é o último recurso.";
            case "Transparência": return "Nunca prometa o que o serviço não faz. Consulte o catálogo.";
            case "Respeito ao cliente": return "Sem pressão e sem insistência. Se a pessoa não quer, agradeça e encerre.";
            default: return "Quando o cliente aceitar, confirme o serviço e o valor e registre a venda no CRM.";
        }
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
