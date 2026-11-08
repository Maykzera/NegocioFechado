using System;
using System.Collections.Generic;

/// <summary>Os cinco perfis de cliente da primeira versão jogável.</summary>
public enum CustomerArchetype
{
    Economico,
    Exigente,
    Indeciso,
    Apressado,
    Desconfiado
}

public enum CustomerState
{
    Browsing,
    Negotiating,
    Thinking,
    Accepted,
    Rejected,
    Left
}

public enum CustomerResponseType
{
    Greeting,
    RevealNeed,
    Interested,
    WantsOtherProduct,
    Convinced,
    Doubtful,
    CounterOffer,
    AskForTime,
    Accept,
    Reject,
    Leave
}

/// <summary>Produto apresentado ao cliente pelo jogador.</summary>
public readonly struct ProductPitch
{
    public readonly string ProductId;
    public readonly ProductCategory Category;
    public readonly long ReferencePriceCents;

    /// <summary>Qualidade/estado do produto (0 a 1).</summary>
    public readonly float Quality;

    public ProductPitch(string productId, ProductCategory category, long referencePriceCents, float quality)
    {
        ProductId = productId;
        Category = category;
        ReferencePriceCents = referencePriceCents;
        Quality = quality < 0f ? 0f : quality > 1f ? 1f : quality;
    }
}

public readonly struct CustomerResponse
{
    public readonly CustomerResponseType Type;
    public readonly long PriceCents;
    public readonly string Line;

    public CustomerResponse(CustomerResponseType type, long priceCents, string line)
    {
        Type = type;
        PriceCents = priceCents;
        Line = line;
    }
}

public enum NegotiationEventType
{
    Greeted,
    AskedNeeds,
    PresentedProduct,
    ExplainedBenefit,
    MadeFalseClaim,
    FalseClaimDetected,
    AnsweredObjection,
    Pressured,
    ProposedPrice,
    CustomerCountered,
    AcceptedCounter,
    CustomerAskedForTime,
    SaleAgreed,
    CustomerRejected,
    CustomerLeft
}

/// <summary>
/// Registro estruturado de cada passo da negociação.
/// É o que o mestre avaliador vai analisar depois.
/// </summary>
public readonly struct NegotiationEvent
{
    public readonly int Round;
    public readonly NegotiationEventType Type;
    public readonly long PriceCents;
    public readonly float TrustAfter;
    public readonly float InterestAfter;

    public NegotiationEvent(int round, NegotiationEventType type, long priceCents, float trustAfter, float interestAfter)
    {
        Round = round;
        Type = type;
        PriceCents = priceCents;
        TrustAfter = trustAfter;
        InterestAfter = interestAfter;
    }
}

/// <summary>
/// Cérebro de um cliente: necessidades, orçamento, confiança, paciência e decisões de compra.
/// Determinístico para a mesma semente e a mesma sequência de ações (testes reproduzíveis).
/// Não mexe em dinheiro nem em estoque: só decide. Quem conclui a venda é o SalesService.
/// </summary>
public sealed class CustomerBrain
{
    private sealed class ArchetypeTuning
    {
        public float BudgetMultiplier;
        public float PriceSensitivity;
        public float StartTrust;
        public float StartInterest;
        public int Patience;
        public float AcceptThreshold;
        public float Anchor;
        public float MinQuality;
        public float TrustGainFactor;
    }

    private static readonly Dictionary<CustomerArchetype, ArchetypeTuning> Tunings =
        new Dictionary<CustomerArchetype, ArchetypeTuning>
        {
            [CustomerArchetype.Economico] = new ArchetypeTuning
            {
                BudgetMultiplier = 1.00f, PriceSensitivity = 0.90f, StartTrust = 0.40f, StartInterest = 0.30f,
                Patience = 5, AcceptThreshold = 0.72f, Anchor = 0.80f, MinQuality = 0.30f, TrustGainFactor = 1.0f
            },
            [CustomerArchetype.Exigente] = new ArchetypeTuning
            {
                BudgetMultiplier = 1.30f, PriceSensitivity = 0.40f, StartTrust = 0.40f, StartInterest = 0.25f,
                Patience = 4, AcceptThreshold = 0.60f, Anchor = 0.90f, MinQuality = 0.70f, TrustGainFactor = 0.8f
            },
            [CustomerArchetype.Indeciso] = new ArchetypeTuning
            {
                BudgetMultiplier = 1.10f, PriceSensitivity = 0.60f, StartTrust = 0.45f, StartInterest = 0.30f,
                Patience = 6, AcceptThreshold = 0.62f, Anchor = 0.88f, MinQuality = 0.40f, TrustGainFactor = 1.0f
            },
            [CustomerArchetype.Apressado] = new ArchetypeTuning
            {
                BudgetMultiplier = 1.20f, PriceSensitivity = 0.40f, StartTrust = 0.50f, StartInterest = 0.45f,
                Patience = 2, AcceptThreshold = 0.50f, Anchor = 0.95f, MinQuality = 0.30f, TrustGainFactor = 1.0f
            },
            [CustomerArchetype.Desconfiado] = new ArchetypeTuning
            {
                BudgetMultiplier = 1.10f, PriceSensitivity = 0.60f, StartTrust = 0.20f, StartInterest = 0.30f,
                Patience = 4, AcceptThreshold = 0.60f, Anchor = 0.85f, MinQuality = 0.50f, TrustGainFactor = 1.5f
            }
        };

    private readonly ArchetypeTuning tuning;
    private readonly Random rng;
    private readonly CustomerProfile profile;
    private readonly List<NegotiationEvent> memory = new List<NegotiationEvent>();
    private readonly Dictionary<CustomerResponseType, int> lastLineIndex = new Dictionary<CustomerResponseType, int>();
    private readonly HashSet<string> explainedBenefits = new HashSet<string>();

    private ProductPitch? currentProduct;
    private bool needRevealed;
    private bool askedForTime;
    private int round;

    public string CustomerId => profile.customerId;
    public CustomerArchetype Archetype { get; }
    public ProductCategory Need { get; }
    public CustomerState State { get; private set; } = CustomerState.Browsing;

    public long BudgetCents => profile.budgetCents;
    public float Trust => profile.trust;
    public float Interest => profile.productInterest;
    public int PatienceLeft { get; private set; }
    public int MaxPatience => tuning.Patience;

    /// <summary>Última contraproposta do cliente (0 se não houver).</summary>
    public long LastCounterOfferCents { get; private set; }

    /// <summary>Preço fechado quando State == Accepted.</summary>
    public long AgreedPriceCents { get; private set; }

    public ProductPitch? CurrentProduct => currentProduct;

    public IReadOnlyList<NegotiationEvent> Memory => memory;

    public bool IsFinished =>
        State == CustomerState.Accepted || State == CustomerState.Rejected || State == CustomerState.Left;

    /// <param name="expectedPriceCents">Quanto o cliente imagina pagar por um produto da categoria desejada.</param>
    /// <param name="seed">Semente para variações controladas (orçamento e falas).</param>
    public CustomerBrain(
        string customerId,
        CustomerArchetype archetype,
        ProductCategory need,
        long expectedPriceCents,
        int seed)
    {
        if (expectedPriceCents <= 0) throw new ArgumentOutOfRangeException(nameof(expectedPriceCents));

        Archetype = archetype;
        Need = need;
        tuning = Tunings[archetype];
        rng = new Random(seed);

        double jitter = 0.9 + rng.NextDouble() * 0.2; // orçamento varia ±10%
        profile = new CustomerProfile
        {
            customerId = customerId,
            budgetCents = (long)Math.Round(expectedPriceCents * tuning.BudgetMultiplier * jitter),
            priceSensitivity = tuning.PriceSensitivity,
            trust = tuning.StartTrust,
            productInterest = tuning.StartInterest
        };
        PatienceLeft = tuning.Patience;
    }

    // ------------------------------------------------------------------ ações do jogador

    public CustomerResponse Greet()
    {
        if (IsFinished) return Finished();
        AdjustTrust(0.03f);
        Log(NegotiationEventType.Greeted, 0);
        return Say(CustomerResponseType.Greeting);
    }

    /// <summary>Perguntar o que o cliente procura. Revela a necessidade e gera confiança.</summary>
    public CustomerResponse AskAboutNeeds()
    {
        if (IsFinished) return Finished();
        if (!needRevealed) AdjustTrust(0.08f);
        needRevealed = true;
        Log(NegotiationEventType.AskedNeeds, 0);
        return Say(CustomerResponseType.RevealNeed);
    }

    public CustomerResponse PresentProduct(ProductPitch pitch)
    {
        if (IsFinished) return Finished();
        if (pitch.ReferencePriceCents <= 0) throw new ArgumentOutOfRangeException(nameof(pitch));

        currentProduct = pitch;
        explainedBenefits.Clear();
        State = CustomerState.Negotiating;
        LastCounterOfferCents = 0;

        Log(NegotiationEventType.PresentedProduct, pitch.ReferencePriceCents);

        if (pitch.Category != Need)
        {
            profile.productInterest = Clamp01(tuning.StartInterest * 0.5f);
            ConsumePatience(1);
            return State == CustomerState.Left ? LeaveNow() : Say(CustomerResponseType.WantsOtherProduct);
        }

        // Apresentar o produto certo ajuda, mas o interesse real vem do atendimento (argumentos e confiança).
        float interest = tuning.StartInterest + 0.10f;
        if (pitch.Quality < tuning.MinQuality) interest -= 0.20f;
        else interest += (pitch.Quality - tuning.MinQuality) * 0.1f;
        profile.productInterest = Clamp01(interest);

        return Say(pitch.Quality < tuning.MinQuality
            ? CustomerResponseType.Doubtful
            : CustomerResponseType.Interested);
    }

    /// <summary>
    /// Explicar uma vantagem do produto. <paramref name="isTrue"/> vem dos dados do produto:
    /// o jogo sabe se o argumento é verdadeiro. Mentir pode funcionar na hora,
    /// mas é registrado e pode ser descoberto (e o mestre sempre vê).
    /// </summary>
    public CustomerResponse ExplainBenefit(string benefitId, bool isTrue)
    {
        if (IsFinished) return Finished();
        if (currentProduct == null) return Say(CustomerResponseType.Doubtful);

        bool repeated = !explainedBenefits.Add(benefitId ?? string.Empty);
        if (repeated)
        {
            ConsumePatience(1);
            if (State == CustomerState.Left) return LeaveNow();
            return Say(CustomerResponseType.Doubtful);
        }

        if (isTrue)
        {
            profile.productInterest = Clamp01(profile.productInterest + 0.10f);
            AdjustTrust(0.05f);
            Log(NegotiationEventType.ExplainedBenefit, 0);
            return Say(CustomerResponseType.Convinced);
        }

        Log(NegotiationEventType.MadeFalseClaim, 0);

        // Clientes desconfiados e exigentes percebem mais facilmente.
        double detectChance = Archetype == CustomerArchetype.Desconfiado ? 0.7
            : Archetype == CustomerArchetype.Exigente ? 0.5
            : 0.25;

        if (rng.NextDouble() < detectChance)
        {
            profile.trust = Clamp01(profile.trust - 0.30f);
            ConsumePatience(2);
            Log(NegotiationEventType.FalseClaimDetected, 0);
            if (State == CustomerState.Left) return LeaveNow();
            return Say(CustomerResponseType.Doubtful);
        }

        profile.productInterest = Clamp01(profile.productInterest + 0.10f);
        return Say(CustomerResponseType.Convinced);
    }

    /// <summary>Responder a uma objeção com informação útil (garantia, comparação honesta etc.).</summary>
    public CustomerResponse AnswerObjection()
    {
        if (IsFinished) return Finished();
        AdjustTrust(0.06f);
        Log(NegotiationEventType.AnsweredObjection, 0);
        return Say(CustomerResponseType.Convinced);
    }

    /// <summary>Pressionar o cliente ("é agora ou nunca"). Quase sempre piora a negociação.</summary>
    public CustomerResponse Pressure()
    {
        if (IsFinished) return Finished();
        profile.trust = Clamp01(profile.trust - 0.12f);
        ConsumePatience(Archetype == CustomerArchetype.Apressado ? 1 : 2);
        Log(NegotiationEventType.Pressured, 0);
        if (State == CustomerState.Left) return LeaveNow();
        return Say(CustomerResponseType.Doubtful);
    }

    /// <summary>O jogador propõe um preço para o produto apresentado.</summary>
    public CustomerResponse ProposePrice(long priceCents)
    {
        if (IsFinished) return Finished();
        if (currentProduct == null || priceCents <= 0) return Say(CustomerResponseType.Doubtful);

        round++;
        Log(NegotiationEventType.ProposedPrice, priceCents);

        var product = currentProduct.Value;
        float acceptance = AcceptanceFor(priceCents);

        if (product.Category == Need && acceptance >= tuning.AcceptThreshold)
            return AcceptAt(priceCents);

        long willingness = GetWillingnessCents();

        // Proposta absurda: muito acima do que o cliente aceitaria.
        if (willingness > 0 && priceCents > willingness * 1.6)
        {
            profile.trust = Clamp01(profile.trust - 0.05f);
            ConsumePatience(1);
        }

        if (Archetype == CustomerArchetype.Indeciso &&
            !askedForTime &&
            acceptance >= tuning.AcceptThreshold - 0.08f)
        {
            askedForTime = true;
            State = CustomerState.Thinking;
            Log(NegotiationEventType.CustomerAskedForTime, priceCents);
            return Say(CustomerResponseType.AskForTime, priceCents);
        }

        ConsumePatience(1);
        if (State == CustomerState.Left) return LeaveNow();

        if (product.Category != Need)
        {
            State = CustomerState.Rejected;
            Log(NegotiationEventType.CustomerRejected, priceCents);
            return Say(CustomerResponseType.Reject);
        }

        // Ainda não está convencido o bastante para pagar qualquer valor: precisa de mais argumentos.
        if (willingness <= 0)
            return Say(CustomerResponseType.Doubtful);

        // Contraproposta começa ancorada para baixo e sobe a cada rodada.
        float progress = Math.Min(1f, (float)round / tuning.Patience);
        float factor = tuning.Anchor + (1f - tuning.Anchor) * progress;
        long counter = RoundToReais((long)(willingness * factor));
        if (counter >= priceCents) counter = RoundToReais((long)(priceCents * 0.95));
        counter = Math.Max(100, Math.Min(counter, profile.budgetCents));

        LastCounterOfferCents = counter;
        Log(NegotiationEventType.CustomerCountered, counter);
        return Say(CustomerResponseType.CounterOffer, counter);
    }

    /// <summary>O jogador aceita a última contraproposta do cliente.</summary>
    public CustomerResponse AcceptCounterOffer()
    {
        if (IsFinished) return Finished();
        if (LastCounterOfferCents <= 0) return Say(CustomerResponseType.Doubtful);
        Log(NegotiationEventType.AcceptedCounter, LastCounterOfferCents);
        return AcceptAt(LastCounterOfferCents);
    }

    /// <summary>Depois de pedir um tempo, o cliente indeciso decide sobre a última proposta.</summary>
    public CustomerResponse Reconsider(long priceCents)
    {
        if (State != CustomerState.Thinking) return Say(CustomerResponseType.Doubtful);
        State = CustomerState.Negotiating;

        if (AcceptanceFor(priceCents) >= tuning.AcceptThreshold - 0.08f)
            return AcceptAt(priceCents);

        return ProposePrice(priceCents);
    }

    public CustomerResponse EndConversation()
    {
        if (IsFinished) return Finished();
        return LeaveNow();
    }

    // ------------------------------------------------------------------ cálculo

    /// <summary>Pontuação de aceitação (0 a 1) para um preço, no estado atual do cliente.</summary>
    public float AcceptanceFor(long priceCents)
    {
        if (currentProduct == null) return 0f;
        return NegotiationEngine.CalculateAcceptance(profile, priceCents, currentProduct.Value.ReferencePriceCents);
    }

    /// <summary>
    /// Maior preço que o cliente aceitaria agora (limitado pelo orçamento).
    /// Resolve a fórmula do NegotiationEngine para o limiar de aceitação do perfil.
    /// </summary>
    public long GetWillingnessCents()
    {
        if (currentProduct == null) return 0;
        long reference = currentProduct.Value.ReferencePriceCents;

        float nonPrice = profile.productInterest * 0.30f + profile.trust * 0.20f;
        float needed = tuning.AcceptThreshold - nonPrice;

        double maxRatio;
        if (needed <= 0f) maxRatio = double.MaxValue;
        else if (profile.priceSensitivity <= 0f) return 0;
        else
        {
            double priceScore = needed / profile.priceSensitivity;
            if (priceScore > 1.0) return 0; // nem de graça atinge o limiar
            maxRatio = 1.5 - priceScore;
        }

        if (maxRatio <= 0) return 0;
        double price = Math.Min(maxRatio * reference, profile.budgetCents);
        return (long)Math.Floor(price);
    }

    // ------------------------------------------------------------------ internos

    private CustomerResponse AcceptAt(long priceCents)
    {
        State = CustomerState.Accepted;
        AgreedPriceCents = priceCents;
        AdjustTrust(0.05f);
        Log(NegotiationEventType.SaleAgreed, priceCents);
        return Say(CustomerResponseType.Accept, priceCents);
    }

    private CustomerResponse LeaveNow()
    {
        State = CustomerState.Left;
        Log(NegotiationEventType.CustomerLeft, 0);
        return Say(CustomerResponseType.Leave);
    }

    private CustomerResponse Finished() =>
        new CustomerResponse(CustomerResponseType.Leave, 0, "(a conversa já terminou)");

    private void ConsumePatience(int amount)
    {
        PatienceLeft = Math.Max(0, PatienceLeft - amount);
        if (PatienceLeft == 0) State = CustomerState.Left;
    }

    private void AdjustTrust(float baseAmount)
    {
        profile.trust = Clamp01(profile.trust + baseAmount * tuning.TrustGainFactor);
    }

    private void Log(NegotiationEventType type, long price)
    {
        memory.Add(new NegotiationEvent(round, type, price, profile.trust, profile.productInterest));
    }

    private CustomerResponse Say(CustomerResponseType type, long price = 0)
    {
        return new CustomerResponse(type, price, PickLine(type, price));
    }

    private string PickLine(CustomerResponseType type, long price)
    {
        string[] pool = CustomerLines.For(type, Need);
        int index = rng.Next(pool.Length);
        // Evita repetir a mesma fala duas vezes seguidas.
        if (pool.Length > 1 && lastLineIndex.TryGetValue(type, out int last) && last == index)
            index = (index + 1) % pool.Length;
        lastLineIndex[type] = index;
        return pool[index].Replace("{preco}", FormatReais(price));
    }

    private static long RoundToReais(long cents) => (long)Math.Round(cents / 100.0) * 100;

    /// <summary>Formata centavos como "R$ 1.234,56", independente da cultura do sistema.</summary>
    public static string FormatReais(long cents)
    {
        string sign = cents < 0 ? "-" : "";
        ulong abs = cents < 0 ? (ulong)(-(cents + 1)) + 1 : (ulong)cents;
        string reais = (abs / 100).ToString(System.Globalization.CultureInfo.InvariantCulture);

        var grouped = new System.Text.StringBuilder();
        for (int i = 0; i < reais.Length; i++)
        {
            if (i > 0 && (reais.Length - i) % 3 == 0) grouped.Append('.');
            grouped.Append(reais[i]);
        }
        return $"{sign}R$ {grouped},{abs % 100:00}";
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}

/// <summary>Falas dos clientes em português. A camada de IA opcional pode substituí-las.</summary>
public static class CustomerLines
{
    private static readonly Dictionary<CustomerResponseType, string[]> Lines =
        new Dictionary<CustomerResponseType, string[]>
        {
            [CustomerResponseType.Greeting] = new[] { "Oi, tudo bem?", "Olá! Só dando uma olhada.", "Bom dia!" },
            [CustomerResponseType.RevealNeed] = new[]
            {
                "Estou procurando algo de {categoria}.",
                "Na verdade eu preciso de alguma coisa de {categoria}.",
                "Queria ver opções de {categoria}."
            },
            [CustomerResponseType.Interested] = new[]
            {
                "Hmm, isso parece interessante.",
                "Gostei. Me fala mais sobre ele.",
                "Era bem isso que eu estava procurando."
            },
            [CustomerResponseType.WantsOtherProduct] = new[]
            {
                "Não era bem isso que eu procurava.",
                "Você não tem outra coisa? Isso não me serve.",
                "Legal, mas não é o que eu preciso."
            },
            [CustomerResponseType.Convinced] = new[]
            {
                "Faz sentido.",
                "Ah, isso é bom saber.",
                "Ok, isso me deixa mais tranquilo."
            },
            [CustomerResponseType.Doubtful] = new[]
            {
                "Não sei não...",
                "Hmm, não me convenceu.",
                "Tem certeza disso?"
            },
            [CustomerResponseType.CounterOffer] = new[]
            {
                "Faço por {preco}.",
                "Consigo pagar {preco}. Fechamos?",
                "Se fizer por {preco} eu levo."
            },
            [CustomerResponseType.AskForTime] = new[]
            {
                "Deixa eu pensar um pouco...",
                "Preciso pensar. Posso te responder já já?",
                "Hmm, não sei... vou pensar."
            },
            [CustomerResponseType.Accept] = new[]
            {
                "Fechado! Levo por {preco}.",
                "Negócio fechado por {preco}.",
                "Combinado, {preco} então."
            },
            [CustomerResponseType.Reject] = new[]
            {
                "Não vai dar, obrigado.",
                "Vou passar dessa vez.",
                "Não, obrigado. Vou comparar com outras lojas."
            },
            [CustomerResponseType.Leave] = new[]
            {
                "Tenho que ir, até mais.",
                "Deixa pra lá. Tchau.",
                "Vou procurar em outro lugar."
            }
        };

    private static readonly Dictionary<ProductCategory, string> CategoryNames =
        new Dictionary<ProductCategory, string>
        {
            [ProductCategory.Electronics] = "eletrônicos",
            [ProductCategory.Accessories] = "acessórios",
            [ProductCategory.Home] = "casa",
            [ProductCategory.Tools] = "ferramentas",
            [ProductCategory.Sports] = "esportes"
        };

    public static string[] For(CustomerResponseType type, ProductCategory need)
    {
        var pool = Lines[type];
        if (type != CustomerResponseType.RevealNeed) return pool;

        var result = new string[pool.Length];
        for (int i = 0; i < pool.Length; i++)
            result[i] = pool[i].Replace("{categoria}", CategoryNames[need]);
        return result;
    }
}
