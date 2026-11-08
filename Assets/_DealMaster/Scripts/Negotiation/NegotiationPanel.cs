using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tela de conversa e negociação com um cliente.
/// O jogador escolhe o que fazer; não existe botão "Vender".
/// </summary>
public class NegotiationPanel : IModalPanel
{
    private readonly CustomerAgent agent;
    private readonly CustomerBrain brain;
    private readonly List<string> log = new List<string>();
    private readonly HashSet<string> usedArguments = new HashSet<string>();

    private string productId;
    private bool productPacked;
    private List<(string text, bool isTrue)> arguments = new List<(string, bool)>();
    private long priceCents = 10000;
    private long lastProposal;
    private bool saleHandled;
    private bool endedRaised;
    private int liesDetectedBefore;
    private Vector2 logScroll, productScroll, argScroll;

    public string Title => $"Conversa com {agent.CustomerName}";

    public NegotiationPanel(CustomerAgent agent)
    {
        this.agent = agent;
        brain = agent.Brain;
        string memory = CustomerSpawner.Memory.Greeting(agent.CustomerName);
        if (memory != null) Add(agent.CustomerName, memory);
        liesDetectedBefore = CountEvents(NegotiationEventType.FalseClaimDetected);
    }

    public void OnClosed()
    {
        if (!brain.IsFinished && productId != null && brain.Memory.Count > 0)
        {
            // Saiu no meio da conversa: o cliente encerra.
            brain.EndConversation();
        }
        RaiseEndedOnce();
        agent.EndTalk();
    }

    public void DrawPanel(Rect area)
    {
        float leftW = area.width * 0.5f;
        var left = new Rect(area.x, area.y, leftW - 10, area.height);
        var right = new Rect(area.x + leftW + 10, area.y, area.width - leftW - 10, area.height);

        DrawIndicators(new Rect(left.x, left.y, left.width, 96));
        DrawLog(new Rect(left.x, left.y + 104, left.width, left.height - 104));

        if (brain.IsFinished) DrawFinished(right);
        else DrawActions(right);
    }

    private void DrawIndicators(Rect r)
    {
        Bar(r.x, r.y, r.width, "Confiança", brain.Trust, new Color(0.35f, 0.6f, 0.9f));
        Bar(r.x, r.y + 30, r.width, "Interesse", brain.Interest, new Color(0.9f, 0.7f, 0.3f));
        Bar(r.x, r.y + 60, r.width, "Paciência", (float)brain.PatienceLeft / brain.MaxPatience, new Color(0.4f, 0.8f, 0.5f));
    }

    private static void Bar(float x, float y, float w, string label, float v, Color c)
    {
        GUI.Label(new Rect(x, y, 110, 26), label, UIStyles.Small);
        UIStyles.Bar(new Rect(x + 110, y + 4, w - 110, 18), v, c);
    }

    private void DrawLog(Rect r)
    {
        GUI.Box(r, GUIContent.none);
        float lineH = 50;
        logScroll = GUI.BeginScrollView(new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 12),
            new Vector2(0, float.MaxValue), new Rect(0, 0, r.width - 36, log.Count * lineH));
        for (int i = 0; i < log.Count; i++)
            GUI.Label(new Rect(0, i * lineH, r.width - 40, lineH), log[i], UIStyles.Text);
        GUI.EndScrollView();
    }

    private void DrawActions(Rect r)
    {
        float y = r.y;
        float half = (r.width - 10) / 2;

        if (GUI.Button(new Rect(r.x, y, half, 38), "Cumprimentar", UIStyles.Button)) Act("Olá! Tudo bem? Posso ajudar?", brain.Greet());
        if (GUI.Button(new Rect(r.x + half + 10, y, half, 38), "Perguntar o que procura", UIStyles.Button)) Act("O que você está procurando hoje?", brain.AskAboutNeeds());
        y += 46;

        if (brain.State == CustomerState.Thinking)
        {
            GUI.Label(new Rect(r.x, y, r.width, 30), $"{agent.CustomerName} está pensando...", UIStyles.Text);
            if (GUI.Button(new Rect(r.x, y + 34, r.width, 44), "Aguardar a resposta", UIStyles.Button))
                Act("(aguarda com calma)", brain.Reconsider(lastProposal));
            return;
        }

        // Produtos
        GUI.Label(new Rect(r.x, y, r.width, 26), "Apresentar produto", UIStyles.Header);
        y += 28;
        var options = AvailableProducts();
        float listH = Mathf.Min(140, Mathf.Max(40, options.Count * 40));
        productScroll = GUI.BeginScrollView(new Rect(r.x, y, r.width, listH), productScroll, new Rect(0, 0, r.width - 20, options.Count * 40));
        for (int i = 0; i < options.Count; i++)
        {
            var (id, packed, count) = options[i];
            string label = $"{ProductDatabase.NameOf(id)} ({count}){(packed ? "" : " · sem embalagem")}";
            if (GUI.Button(new Rect(0, i * 40, r.width - 24, 36), label, id == productId ? UIStyles.Selected : UIStyles.ListItem))
                Present(id, packed);
        }
        if (options.Count == 0) GUI.Label(new Rect(0, 0, r.width, 36), "Você não tem produtos com você.", UIStyles.Small);
        GUI.EndScrollView();
        y += listH + 8;

        if (productId == null)
        {
            if (GUI.Button(new Rect(r.x, r.yMax - 40, r.width, 40), "Encerrar conversa", UIStyles.Button)) Act("Obrigado, até mais!", brain.EndConversation());
            return;
        }

        // Argumentos
        GUI.Label(new Rect(r.x, y, r.width, 26), "Argumentos", UIStyles.Header);
        y += 28;
        float argH = 120;
        argScroll = GUI.BeginScrollView(new Rect(r.x, y, r.width, argH), argScroll, new Rect(0, 0, r.width - 20, arguments.Count * 38));
        for (int i = 0; i < arguments.Count; i++)
        {
            var (text, isTrue) = arguments[i];
            GUI.enabled = !usedArguments.Contains(text);
            if (GUI.Button(new Rect(0, i * 38, r.width - 24, 34), "\"" + text + "\"", UIStyles.ListItem))
            {
                usedArguments.Add(text);
                Act(text + ".", brain.ExplainBenefit(text, isTrue));
            }
            GUI.enabled = true;
        }
        GUI.EndScrollView();
        y += argH + 6;

        if (GUI.Button(new Rect(r.x, y, half, 36), "Responder dúvida", UIStyles.Button)) Act("Entendo sua dúvida. Tem garantia e você pode testar aqui.", brain.AnswerObjection());
        if (GUI.Button(new Rect(r.x + half + 10, y, half, 36), "Pressionar", UIStyles.Button)) Act("É agora ou nunca, essa oferta acaba hoje!", brain.Pressure());
        y += 44;

        // Preço
        priceCents = UIStyles.PriceField(new Rect(r.x, y, r.width, 36), priceCents);
        y += 42;
        if (GUI.Button(new Rect(r.x, y, half, 40), "Propor preço", UIStyles.Button))
        {
            lastProposal = priceCents;
            Act($"Faço por {CustomerBrain.FormatReais(priceCents)}.", brain.ProposePrice(priceCents));
        }
        GUI.enabled = brain.LastCounterOfferCents > 0;
        if (GUI.Button(new Rect(r.x + half + 10, y, half, 40),
                brain.LastCounterOfferCents > 0 ? $"Aceitar {CustomerBrain.FormatReais(brain.LastCounterOfferCents)}" : "Aceitar contraproposta", UIStyles.Button))
            Act("Combinado!", brain.AcceptCounterOffer());
        GUI.enabled = true;
        y += 48;

        if (GUI.Button(new Rect(r.x, y, r.width, 36), "Encerrar conversa", UIStyles.Button)) Act("Tudo bem, obrigado pelo seu tempo.", brain.EndConversation());
    }

    private void DrawFinished(Rect r)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 30), StateText(), UIStyles.Header);
        if (brain.State == CustomerState.Accepted && !saleHandled) CloseSale();
        GUI.Label(new Rect(r.x, r.y + 40, r.width, 90), "A conversa foi registrada. Visite o Mestre Negociador para receber a avaliação.", UIStyles.Small);
        if (GUI.Button(new Rect(r.x, r.y + 140, 220, 44), "Sair", UIStyles.Button)) GameUI.Close();
    }

    private string StateText()
    {
        switch (brain.State)
        {
            case CustomerState.Accepted: return $"Negócio fechado: {CustomerBrain.FormatReais(brain.AgreedPriceCents)}";
            case CustomerState.Rejected: return "O cliente recusou.";
            default: return "O cliente foi embora.";
        }
    }

    // ------------------------------------------------------------------ ações

    private void Present(string id, bool packed)
    {
        var info = ProductDatabase.Get(id);
        if (info == null) return;
        productId = id;
        productPacked = packed;
        priceCents = info.ReferencePriceCents;
        usedArguments.Clear();

        // Mistura verdades e exageros numa ordem fixa por produto (o jogador precisa escolher bem).
        var rng = new System.Random(id.GetHashCode());
        arguments = info.Benefits.Select(b => (b, true))
            .Concat(info.Exaggerations.Select(e => (e, false)))
            .OrderBy(_ => rng.Next()).ToList();

        Act($"Dá uma olhada nesse {info.Name.ToLower()}.", brain.PresentProduct(info.ToPitch()));
    }

    private void Act(string playerLine, CustomerResponse response)
    {
        Add("Você", playerLine);
        Add(agent.CustomerName, response.Line);

        int lies = CountEvents(NegotiationEventType.FalseClaimDetected);
        if (lies > liesDetectedBefore)
        {
            CustomerSpawner.Memory.RecordLieDetected(agent.CustomerName);
            liesDetectedBefore = lies;
        }

        if (brain.State == CustomerState.Accepted) CloseSale();
        else if (brain.IsFinished)
        {
            CustomerSpawner.Memory.RecordUnhappyExit(agent.CustomerName);
            RaiseEndedOnce();
        }
    }

    private void CloseSale()
    {
        if (saleHandled) return;
        saleHandled = true;

        var outcome = BuildOutcome(true);
        string error = NegotiationHooks.CompleteSale?.Invoke(outcome);
        if (error != null)
        {
            outcome.SaleClosed = false;
            Add("Sistema", error);
        }
        else
        {
            CustomerSpawner.Memory.RecordPurchase(agent.CustomerName, brain.AgreedPriceCents);
        }
        endedRaised = true;
        NegotiationHooks.RaiseEnded(outcome);
    }

    private void RaiseEndedOnce()
    {
        if (endedRaised || brain.Memory.Count == 0) return;
        endedRaised = true;
        NegotiationHooks.RaiseEnded(BuildOutcome(false));
    }

    private NegotiationOutcome BuildOutcome(bool sale) => new NegotiationOutcome
    {
        CustomerName = agent.CustomerName,
        Archetype = brain.Archetype,
        Need = brain.Need,
        ProductId = productId,
        Packed = productPacked,
        AgreedPriceCents = sale ? brain.AgreedPriceCents : 0,
        SaleClosed = sale,
        Brain = brain
    };

    private List<(string id, bool packed, int count)> AvailableProducts()
    {
        var inv = GameServices.Inventory;
        var list = new List<(string, bool, int)>();
        foreach (var s in inv.GetStacks(StorageLocation.Order)) list.Add((s.ProductId, true, s.Available));
        foreach (var s in inv.GetStacks(StorageLocation.Backpack))
            if (list.All(l => l.Item1 != s.ProductId)) list.Add((s.ProductId, false, s.Available));
        return list;
    }

    private int CountEvents(NegotiationEventType type) => brain.Memory.Count(e => e.Type == type);

    private void Add(string who, string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        log.Add($"<b>{who}:</b> {line}");
    }
}
