#if DEALMASTER_ANTHROPIC_SDK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;

/// <summary>
/// Cliente interpretado pelo Claude. Cada fala do vendedor vai para a API com o histórico da ligação,
/// e a resposta volta em JSON estruturado (fala + interesse, confiança e decisão).
/// Qualquer erro (sem internet, chave inválida, limite, recusa) gera exceção e a ligação usa o cliente offline.
/// </summary>
public sealed class ClaudeCustomerAI : ICustomerAI
{
    private readonly AnthropicClient client;
    private readonly string model;

    public string Name => "claude";

    public ClaudeCustomerAI(string apiKey, string model)
    {
        client = new AnthropicClient { ApiKey = apiKey };
        this.model = string.IsNullOrWhiteSpace(model) ? AISettings.DefaultModel : model;
    }

    public Task<CustomerTurn> AnswerAsync(CallContext context) => Ask(context, null);

    public Task<CustomerTurn> RespondAsync(CallContext context, string playerMessage) => Ask(context, playerMessage);

    private async Task<CustomerTurn> Ask(CallContext ctx, string playerMessage)
    {
        var messages = BuildMessages(ctx, playerMessage);
        Message response;
        try
        {
            response = await client.Messages.Create(new MessageCreateParams
            {
                Model = model,
                MaxTokens = 2048,
                System = ClaudePrompts.CustomerSystem(ctx),
                Messages = messages,
                OutputConfig = new OutputConfig { Effort = Effort.Low, Format = new JsonOutputFormat { Schema = CustomerSchema() } },
            });
        }
        catch (AnthropicUnauthorizedException) { throw new InvalidOperationException("Chave da API inválida."); }
        catch (AnthropicRateLimitException) { throw new InvalidOperationException("Limite de uso da API atingido. Tente de novo em instantes."); }
        catch (AnthropicApiException e) { throw new InvalidOperationException("Erro da API: " + e.Message); }

        if (response.StopReason == "refusal") throw new InvalidOperationException("A IA recusou esta fala.");
        string json = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        return Parse(json);
    }

    /// <summary>Histórico alternado: toque do telefone, depois cada fala do vendedor (user) e do cliente (assistant).</summary>
    private static List<MessageParam> BuildMessages(CallContext ctx, string playerMessage)
    {
        var list = new List<MessageParam> { new UserMessageParam { Content = ClaudePrompts.RingMessage } };
        int raw = 0;
        foreach (var line in ctx.History)
        {
            if (line.FromPlayer) list.Add(new UserMessageParam { Content = line.Text });
            else list.Add(new AssistantMessageParam { Content = raw < ctx.PreviousRawReplies.Count ? ctx.PreviousRawReplies[raw++] : line.Text });
        }
        // A fala atual já foi adicionada ao histórico pela ligação; se não, acrescenta.
        if (playerMessage != null && (ctx.History.Count == 0 || !ctx.History[ctx.History.Count - 1].FromPlayer))
            list.Add(new UserMessageParam { Content = playerMessage });
        return list;
    }

    public static CustomerTurn Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        string decision = Str(r, "decisao");
        string service = Str(r, "servico_aceito");
        long price = r.TryGetProperty("preco_aceito_centavos", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt64() : 0;
        var turn = new CustomerTurn
        {
            Speech = Str(r, "fala"),
            Interest = Clamp(Int(r, "interesse")),
            Trust = Clamp(Int(r, "confianca")),
            Mood = Str(r, "humor"),
            Objection = string.IsNullOrWhiteSpace(Str(r, "objecao")) ? null : Str(r, "objecao"),
            Decision = decision == "quer_comprar" ? CustomerDecision.WantsToBuy
                : decision == "recusou" ? CustomerDecision.Refused
                : decision == "desligou" ? CustomerDecision.HungUp
                : CustomerDecision.Talking,
            Source = "claude",
            Raw = json
        };
        if (turn.Decision == CustomerDecision.WantsToBuy)
        {
            turn.AcceptedServiceId = ServiceCatalog.Get(service) != null ? service : null;
            turn.AcceptedPriceCents = price >= 500 && price <= 500000 ? price : (long?)null;
        }
        if (turn.Decision == CustomerDecision.HungUp && !turn.Speech.Contains("desligou")) turn.Speech += " (desligou)";
        if (string.IsNullOrWhiteSpace(turn.Speech)) throw new FormatException("Resposta vazia da IA.");
        return turn;
    }

    private static Dictionary<string, JsonElement> CustomerSchema()
    {
        var ids = new List<string> { "" };
        ids.AddRange(ServiceCatalog.All.Select(s => s.Id));
        return new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "fala", "interesse", "confianca", "humor", "objecao", "decisao", "servico_aceito", "preco_aceito_centavos" }),
            ["properties"] = JsonSerializer.SerializeToElement(new Dictionary<string, object>
            {
                ["fala"] = new { type = "string" },
                ["interesse"] = new { type = "integer" },
                ["confianca"] = new { type = "integer" },
                ["humor"] = new { type = "string" },
                ["objecao"] = new { type = "string" },
                ["decisao"] = new { type = "string", @enum = new[] { "conversando", "quer_comprar", "recusou", "desligou" } },
                ["servico_aceito"] = new { type = "string", @enum = ids },
                ["preco_aceito_centavos"] = new { type = "integer" },
            }),
        };
    }

    private static string Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";
    private static int Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
    private static int Clamp(int v) => v < 0 ? 0 : v > 100 ? 100 : v;
}

/// <summary>Supervisor interpretado pelo Claude: lê a transcrição e as informações oficiais e devolve a avaliação.</summary>
public sealed class ClaudeSupervisor : ISupervisorAI
{
    private readonly AnthropicClient client;
    private readonly string model;

    public ClaudeSupervisor(string apiKey, string model)
    {
        client = new AnthropicClient { ApiKey = apiKey };
        this.model = string.IsNullOrWhiteSpace(model) ? AISettings.DefaultModel : model;
    }

    public async Task<SupervisorReview> ReviewAsync(CallReport report)
    {
        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = model,
            MaxTokens = 4096,
            System = ClaudePrompts.SupervisorSystem(),
            Messages = new List<MessageParam> { new UserMessageParam { Content = ClaudePrompts.SupervisorInput(report) } },
            OutputConfig = new OutputConfig { Effort = Effort.Medium, Format = new JsonOutputFormat { Schema = Schema() } },
        });
        if (response.StopReason == "refusal") throw new InvalidOperationException("A IA recusou a avaliação.");
        string json = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        return Parse(json, report);
    }

    public static SupervisorReview Parse(string json, CallReport report)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        var review = new SupervisorReview { CallId = report.CallId, Day = report.Day, LeadName = report.LeadName, SaleClosed = report.SaleClosed, Source = "claude" };
        review.Verdict = r.GetProperty("veredito").GetString();
        foreach (var c in r.GetProperty("criterios").EnumerateArray())
            review.Criteria.Add(new ReviewCriterion(c.GetProperty("nome").GetString(), (float)Math.Max(0, Math.Min(10, c.GetProperty("nota").GetDouble())) / 10f));
        foreach (var o in r.GetProperty("observacoes").EnumerateArray()) review.Observations.Add(o.GetString());
        foreach (var s in r.GetProperty("sugestoes").EnumerateArray()) review.Suggestions.Add(s.GetString());
        foreach (var m in r.GetProperty("mentiras").EnumerateArray()) review.LiesFound.Add(m.GetString());
        float grade = (float)Math.Round(Math.Max(0, Math.Min(10, r.GetProperty("nota").GetDouble())), 1);

        // As regras do jogo valem mesmo que a IA esqueça: mentira, grosseria e insistência limitam a nota.
        foreach (var lie in report.Lies) if (!review.LiesFound.Contains(lie)) review.LiesFound.Add(lie);
        if (review.LiesFound.Count > 0) grade = Math.Min(grade, 4f);
        if (report.RudeCount > 0) grade = Math.Min(grade, 2f);
        if (report.DoNotCall && !report.RespectedDoNotCall) grade = Math.Min(grade, 3f);
        review.Grade = grade;
        return review;
    }

    private static Dictionary<string, JsonElement> Schema() => new Dictionary<string, JsonElement>
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "nota", "veredito", "criterios", "observacoes", "sugestoes", "mentiras" }),
        ["properties"] = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["nota"] = new { type = "number" },
            ["veredito"] = new { type = "string" },
            ["criterios"] = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    additionalProperties = false,
                    required = new[] { "nome", "nota" },
                    properties = new Dictionary<string, object> { ["nome"] = new { type = "string" }, ["nota"] = new { type = "number" } },
                },
            },
            ["observacoes"] = new { type = "array", items = new { type = "string" } },
            ["sugestoes"] = new { type = "array", items = new { type = "string" } },
            ["mentiras"] = new { type = "array", items = new { type = "string" } },
        }),
    };
}
#endif
