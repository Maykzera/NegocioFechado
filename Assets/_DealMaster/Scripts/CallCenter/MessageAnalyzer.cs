using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[Flags]
public enum PlayerIntent
{
    None = 0,
    Greeting = 1 << 0,
    Introduction = 1 << 1,
    AskPermission = 1 << 2,
    AskNeed = 1 << 3,
    MentionService = 1 << 4,
    Benefit = 1 << 5,
    Lie = 1 << 6,
    Price = 1 << 7,
    Discount = 1 << 8,
    Pressure = 1 << 9,
    Empathy = 1 << 10,
    Close = 1 << 11,
    Rude = 1 << 12,
    Goodbye = 1 << 13,
    Question = 1 << 14,
}

/// <summary>O que foi identificado numa fala do vendedor.</summary>
public sealed class MessageAnalysis
{
    public PlayerIntent Intents;
    public List<string> ServicesMentioned = new List<string>();
    public List<string> FactsUsed = new List<string>();
    public List<string> LiesUsed = new List<string>();
    public long? PriceCents;

    public bool Has(PlayerIntent i) => (Intents & i) != 0;
}

/// <summary>
/// Entende, por palavras-chave, o que o vendedor escreveu em português.
/// Usado pelo cliente offline e pelo supervisor offline (com IA real, a IA lê o texto direto).
/// </summary>
public static class MessageAnalyzer
{
    private static readonly string[] Greetings = { "oi", "ola", "bom dia", "boa tarde", "boa noite", "alo", "tudo bem" };
    private static readonly string[] Intros = { "meu nome e", "me chamo", "aqui e", "falo da", "falo do", "sou da", "sou do", "estou ligando da", "estou ligando do", "em nome da" };
    private static readonly string[] Permission = { "tem um minuto", "tem um minutinho", "posso falar", "e um bom momento", "pode falar agora", "tem um tempinho", "posso tomar" };
    private static readonly string[] NeedWords = { "voce usa", "voce tem", "voce costuma", "como e", "como esta", "o que voce", "qual e", "quanto voce paga", "voce precisa", "te incomoda", "o que acha", "voce gostaria", "hoje voce", "sua internet", "seu celular", "sua casa", "sua conta", "voce mora", "voce trabalha", "conta pra mim", "me conta", "qual a sua", "qual o seu", "o que mais", "voce ja" };
    private static readonly string[] DiscountWords = { "desconto", "promocao", "mais barato", "abaixar", "baixar o preco", "condicao especial", "faco por", "consigo fazer por", "fazer por" };
    private static readonly string[] PressureWords = { "agora ou nunca", "so hoje", "ultima chance", "tem que decidir", "decide logo", "vai perder", "nao pode perder", "acaba hoje", "ultimas vagas", "rapido", "sem pensar" };
    private static readonly string[] EmpathyWords = { "entendo", "compreendo", "faz sentido", "com certeza", "tem razao", "imagino", "fique tranquil", "sem problema", "respeito" };
    private static readonly string[] CloseWords = { "vamos fechar", "posso fechar", "posso cadastrar", "posso confirmar", "fechamos", "vamos fazer", "posso ativar", "quer contratar", "vamos assinar", "posso seguir com", "confirma", "topa", "aceita" };
    private static readonly string[] RudeWords = { "idiota", "burro", "burra", "imbecil", "cala a boca", "otario", "otaria", "vagabund", "porra", "caralho", "merda", "foda-se", "palhaco" };
    private static readonly string[] GoodbyeWords = { "tchau", "ate logo", "ate mais", "obrigado pelo seu tempo", "obrigada pelo seu tempo", "tenha um bom dia", "tenha uma boa tarde" };

    private static readonly Regex PriceRegex = new Regex(@"(?:r\$\s*)?(\d{1,4})(?:[,.](\d{2}))?\s*(?:reais|real|por mes|/mes|mensais|ao mes)?", RegexOptions.Compiled);

    public static MessageAnalysis Analyze(string text, IEnumerable<ServiceInfo> services = null)
    {
        var a = new MessageAnalysis();
        string n = TextNormalizer.Normalize(text);
        if (n.Length == 0) return a;

        if (Any(n, Greetings)) a.Intents |= PlayerIntent.Greeting;
        if (Any(n, Intros)) a.Intents |= PlayerIntent.Introduction;
        if (Any(n, Permission)) a.Intents |= PlayerIntent.AskPermission;
        if (n.Contains("?")) a.Intents |= PlayerIntent.Question;
        if (a.Has(PlayerIntent.Question) && Any(n, NeedWords)) a.Intents |= PlayerIntent.AskNeed;
        if (Any(n, DiscountWords)) a.Intents |= PlayerIntent.Discount;
        if (Any(n, PressureWords)) a.Intents |= PlayerIntent.Pressure;
        if (Any(n, EmpathyWords)) a.Intents |= PlayerIntent.Empathy;
        if (Any(n, CloseWords)) a.Intents |= PlayerIntent.Close;
        if (RudeWords.Any(w => n.Contains(w))) a.Intents |= PlayerIntent.Rude;
        if (Any(n, GoodbyeWords)) a.Intents |= PlayerIntent.Goodbye;

        foreach (var s in ServiceCatalog.All)
        {
            bool mentioned = Any(n, s.Keywords) || n.Contains(TextNormalizer.Normalize(s.Company));
            if (mentioned) a.ServicesMentioned.Add(s.Id);
        }

        // Verdades e mentiras contam para os serviços em pauta: os citados agora e os já em conversa.
        var focus = new List<ServiceInfo>(services ?? Enumerable.Empty<ServiceInfo>());
        foreach (var id in a.ServicesMentioned)
            if (focus.All(f => f.Id != id)) focus.Add(ServiceCatalog.Get(id));
        foreach (var s in focus)
        {
            foreach (var lie in s.Lies)
                if (Any(n, lie.Keywords) && !a.LiesUsed.Contains(lie.Text)) a.LiesUsed.Add(lie.Text);
            foreach (var fact in s.Facts)
                if (Any(n, fact.Keywords) && !a.FactsUsed.Contains(fact.Text) && !ContradictedByLie(n, s, fact)) a.FactsUsed.Add(fact.Text);
        }
        if (a.ServicesMentioned.Count > 0) a.Intents |= PlayerIntent.MentionService;
        if (a.FactsUsed.Count > 0) a.Intents |= PlayerIntent.Benefit;
        if (a.LiesUsed.Count > 0) a.Intents |= PlayerIntent.Lie;

        a.PriceCents = ExtractPrice(n);
        if (a.PriceCents.HasValue) a.Intents |= PlayerIntent.Price;
        return a;
    }

    /// <summary>Encontra um valor em reais na frase ("R$ 89,90", "89 reais", "por 79,90").</summary>
    public static long? ExtractPrice(string normalized)
    {
        foreach (Match m in PriceRegex.Matches(normalized))
        {
            string whole = m.Value;
            bool looksLikeMoney = whole.Contains("r$") || whole.Contains("reais") || whole.Contains("real")
                || whole.Contains("mes") || m.Groups[2].Success;
            if (!looksLikeMoney) continue;
            long reais = long.Parse(m.Groups[1].Value);
            long cents = m.Groups[2].Success ? long.Parse(m.Groups[2].Value) : 0;
            long total = reais * 100 + cents;
            if (total >= 500 && total <= 500000) return total;
        }
        return null;
    }

    private static bool ContradictedByLie(string n, ServiceInfo s, ServiceFact fact) =>
        s.Lies.Any(l => Any(n, l.Keywords) && fact.Keywords.Any(k => l.Keywords.Contains(k)));

    private static bool Any(string normalized, IEnumerable<string> phrases) => phrases.Any(p => TextNormalizer.Has(normalized, p));
}
