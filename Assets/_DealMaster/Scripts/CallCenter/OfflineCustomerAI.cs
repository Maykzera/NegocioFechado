using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Cliente simulado sem internet. Entende a fala do vendedor por palavras-chave
/// e reage com confiança, interesse e paciência. Uma instância por ligação.
/// </summary>
public sealed class OfflineCustomerAI : ICustomerAI
{
    private sealed class Tuning
    {
        public float Sensitivity, Trust, Interest, Threshold, Anchor, LieDetect;
        public int Patience;
    }

    private static readonly Dictionary<CustomerArchetype, Tuning> Tunings = new Dictionary<CustomerArchetype, Tuning>
    {
        [CustomerArchetype.Economico] = new Tuning { Sensitivity = 0.9f, Trust = 0.40f, Interest = 0.20f, Threshold = 0.72f, Anchor = 0.80f, LieDetect = 0.35f, Patience = 7 },
        [CustomerArchetype.Exigente] = new Tuning { Sensitivity = 0.4f, Trust = 0.40f, Interest = 0.15f, Threshold = 0.62f, Anchor = 0.90f, LieDetect = 0.60f, Patience = 6 },
        [CustomerArchetype.Indeciso] = new Tuning { Sensitivity = 0.6f, Trust = 0.45f, Interest = 0.20f, Threshold = 0.64f, Anchor = 0.88f, LieDetect = 0.30f, Patience = 8 },
        [CustomerArchetype.Apressado] = new Tuning { Sensitivity = 0.4f, Trust = 0.50f, Interest = 0.25f, Threshold = 0.52f, Anchor = 0.95f, LieDetect = 0.25f, Patience = 4 },
        [CustomerArchetype.Desconfiado] = new Tuning { Sensitivity = 0.6f, Trust = 0.15f, Interest = 0.20f, Threshold = 0.62f, Anchor = 0.85f, LieDetect = 0.75f, Patience = 6 },
    };

    private readonly Lead lead;
    private readonly Tuning t;
    private readonly Random rng;
    private readonly HashSet<string> factsHeard = new HashSet<string>();
    private readonly Dictionary<string, int> lastLine = new Dictionary<string, int>();

    private float trust, interest;
    private int patience;
    private bool needRevealed, greeted, askedTime, fidelityObjection, priceObjection, doNotCallWarned;
    private string focusServiceId;
    private long lastCounter;
    private int detailIndex;
    private CustomerDecision decision = CustomerDecision.Talking;
    private long? acceptedPrice;

    public string Name => "offline";

    public float TrustValue => trust;
    public float InterestValue => interest;
    public int Patience => patience;

    public OfflineCustomerAI(Lead lead, float trustBonus = 0f)
    {
        this.lead = lead ?? throw new ArgumentNullException(nameof(lead));
        t = Tunings[lead.Archetype];
        rng = new Random(lead.Seed);
        trust = Clamp01(t.Trust + trustBonus);
        interest = t.Interest;
        patience = t.Patience;
    }

    public Task<CustomerTurn> AnswerAsync(CallContext context) => Task.FromResult(Answer());

    public Task<CustomerTurn> RespondAsync(CallContext context, string playerMessage) => Task.FromResult(Respond(playerMessage));

    public CustomerTurn Answer()
    {
        if (lead.DoNotCall)
            return Turn("Alô? ... Olha, eu já pedi pra tirar meu número dessa lista de ligações.", "irritado");
        return Turn(Pick("alo", "Alô?", "Pois não?", "Alô, quem fala?", $"Alô, {lead.Name.Split(' ')[0]} falando."), "neutro");
    }

    public CustomerTurn Respond(string message)
    {
        if (decision == CustomerDecision.HungUp || decision == CustomerDecision.Refused)
            return Turn("(a ligação já terminou)", "neutro");

        var focus = focusServiceId != null ? new[] { ServiceCatalog.Get(focusServiceId) } : null;
        var a = MessageAnalyzer.Analyze(message, focus);
        var reply = new List<string>();
        string mood = "neutro";

        // 1. Falta de educação: desliga na hora.
        if (a.Has(PlayerIntent.Rude)) return HangUp("Que falta de educação! Não ligue mais pra cá.", "irritado");

        // 2. Pediu para não ser incomodado.
        if (lead.DoNotCall)
        {
            string n = TextNormalizer.Normalize(message);
            bool respects = (n.Contains("tirar") || n.Contains("retirar") || n.Contains("remover") || n.Contains("excluir"))
                            && (n.Contains("numero") || n.Contains("lista") || n.Contains("cadastro"));
            if (respects || a.Has(PlayerIntent.Goodbye))
            {
                decision = CustomerDecision.Refused;
                return Turn("Obrigado(a) por respeitar. Tenha um bom dia.".Replace("Obrigado(a)", lead.Female ? "Obrigada" : "Obrigado"), "aliviado");
            }
            if (doNotCallWarned) return HangUp("Eu já falei que não quero! Vou denunciar essas ligações.", "irritado");
            doNotCallWarned = true;
            return Turn("Moço(a), eu pedi pra não me ligarem. Pode tirar meu número, por favor?".Replace("Moço(a)", "Olha"), "irritado");
        }

        if (a.Has(PlayerIntent.Goodbye) && !a.Has(PlayerIntent.Close))
        {
            decision = CustomerDecision.Refused;
            return Turn(Pick("bye", "Tá bom, tchau.", "Obrigado(a), até mais.".Replace("Obrigado(a)", lead.Female ? "Obrigada" : "Obrigado")), "neutro");
        }

        bool progress = false;

        // 3. Pressão
        if (a.Has(PlayerIntent.Pressure))
        {
            trust = Clamp01(trust - 0.12f);
            patience -= lead.Archetype == CustomerArchetype.Apressado ? 1 : 2;
            reply.Add(Pick("pressure", "Calma, não gosto de ser pressionado(a).", "Se é pra decidir agora, então a resposta é não.", "Pra que tanta pressa?").Replace("pressionado(a)", lead.Female ? "pressionada" : "pressionado"));
            mood = "incomodado";
        }

        // 4. Mentira
        if (a.Has(PlayerIntent.Lie))
        {
            if (rng.NextDouble() < t.LieDetect)
            {
                trust = Clamp01(trust - 0.3f);
                patience -= 2;
                reply.Add(Pick("lie", "Isso não é verdade. Eu já li sobre isso e não é assim.", "Hum... meu cunhado tem esse serviço e não é bem assim, não.", "Agora fiquei com o pé atrás. Você está me enrolando?"));
                mood = "desconfiado";
            }
            else
            {
                interest = Clamp01(interest + 0.08f);
                reply.Add("Sério? Isso é bom.");
            }
            progress = true;
        }

        // 5. Abertura educada
        if (!greeted && (a.Has(PlayerIntent.Greeting) || a.Has(PlayerIntent.Introduction)))
        {
            greeted = true;
            trust = Clamp01(trust + (a.Has(PlayerIntent.Introduction) ? 0.06f : 0.03f));
            if (a.Has(PlayerIntent.AskPermission)) trust = Clamp01(trust + 0.04f);
            if (reply.Count == 0)
                reply.Add(lead.Archetype == CustomerArchetype.Desconfiado ? "Ok... e do que se trata?"
                    : lead.Archetype == CustomerArchetype.Apressado ? "Fala rápido que estou ocupado(a).".Replace("ocupado(a)", lead.Female ? "ocupada" : "ocupado")
                    : Pick("greet", "Oi, tudo bem. Pode falar.", "Boa tarde. Do que se trata?", "Oi. Diga."));
            progress = true;
        }

        // 6. Perguntas sobre a necessidade
        if (a.Has(PlayerIntent.AskNeed))
        {
            if (!needRevealed)
            {
                needRevealed = true;
                trust = Clamp01(trust + 0.08f);
                interest = Clamp01(interest + 0.05f);
                reply.Add($"Olha, na verdade {lead.Pain}.");
            }
            else
            {
                trust = Clamp01(trust + 0.03f);
                string[] details = { $"Eu {lead.Household}.", $"Trabalho como {lead.Occupation}.", $"Meu orçamento é apertado, não posso gastar muito por mês.", "Já tive experiência ruim com empresa que prometeu e não cumpriu." };
                reply.Add(details[Math.Min(detailIndex++, details.Length - 1)]);
            }
            progress = true;
        }

        // 7. Serviço apresentado
        foreach (var id in a.ServicesMentioned)
        {
            var s = ServiceCatalog.Get(id);
            if (s == null || id == focusServiceId) continue;
            focusServiceId = id;
            progress = true;
            if (s.Category == lead.Need)
            {
                interest = Clamp01(interest + (needRevealed ? 0.2f : 0.12f));
                reply.Add(Pick("fit", $"Hum, {s.Name}... isso pode me interessar. Como funciona?", "Isso é justamente o que eu estava precisando. Me explica melhor.", "Interessante. E o que tem de bom?"));
            }
            else
            {
                interest = Clamp01(interest - 0.05f);
                patience -= 1;
                reply.Add(Pick("nofit", "Isso eu não preciso, não.", "Não tenho interesse nisso.", "Hum, acho que não é pra mim."));
                mood = "desinteressado";
            }
        }

        var service = focusServiceId != null ? ServiceCatalog.Get(focusServiceId) : null;
        bool fits = service != null && service.Category == lead.Need;

        // 8. Argumentos verdadeiros
        int newFacts = 0;
        foreach (var f in a.FactsUsed)
        {
            if (factsHeard.Add(f)) newFacts++;
        }
        if (newFacts > 0 && fits)
        {
            interest = Clamp01(interest + 0.09f * newFacts);
            trust = Clamp01(trust + 0.04f * newFacts);
            if (!a.Has(PlayerIntent.Lie)) reply.Add(Pick("fact", "Ah, isso é bom saber.", "Gostei disso.", "Hum, faz sentido.", "Isso me deixa mais tranquilo(a).".Replace("tranquilo(a)", lead.Female ? "tranquila" : "tranquilo")));
            progress = true;

            // Objeções aparecem quando o interesse cresce.
            if (service.FidelityMonths > 0 && !fidelityObjection && interest > 0.45f)
            {
                fidelityObjection = true;
                reply.Add("Mas tem fidelidade? Não quero ficar preso(a) a contrato.".Replace("preso(a)", lead.Female ? "presa" : "preso"));
            }
        }
        else if (a.FactsUsed.Count > 0 && newFacts == 0)
        {
            patience -= 1;
            reply.Add("Isso você já falou.");
        }

        // 9. Empatia e resposta a objeções
        if (a.Has(PlayerIntent.Empathy))
        {
            trust = Clamp01(trust + (fidelityObjection || priceObjection ? 0.08f : 0.03f));
            if (priceObjection || fidelityObjection) { priceObjection = false; fidelityObjection = false; reply.Add("Ok, entendi."); }
            progress = true;
        }
        string norm = TextNormalizer.Normalize(message);
        if (service != null && service.FidelityMonths > 0 && norm.Contains("fidelidade") && norm.Contains(service.FidelityMonths.ToString()) && !a.Has(PlayerIntent.Lie))
        {
            trust = Clamp01(trust + 0.06f);
            fidelityObjection = false;
            reply.Add("Pelo menos você foi sincero(a) sobre a fidelidade.".Replace("sincero(a)", "honesto"));
            progress = true;
        }

        // 10. Preço e fechamento
        if (a.PriceCents.HasValue || a.Has(PlayerIntent.Close))
        {
            progress = true;
            if (service == null)
            {
                reply.Add("Mas você nem me explicou o que está vendendo.");
            }
            else if (!fits)
            {
                decision = CustomerDecision.Refused;
                reply.Add("Não, obrigado(a). Isso não serve pra mim.".Replace("obrigado(a)", lead.Female ? "obrigada" : "obrigado"));
                return Turn(string.Join(" ", reply), "desinteressado");
            }
            else
            {
                long price = a.PriceCents ?? (lastCounter > 0 && a.Has(PlayerIntent.Close) ? lastCounter : service.MonthlyPriceCents);
                float acc = Acceptance(price, service);
                long will = Willingness(service);

                if (acc >= t.Threshold && interest >= 0.4f && trust >= 0.35f)
                {
                    decision = CustomerDecision.WantsToBuy;
                    acceptedPrice = price;
                    reply.Add(Pick("yes", $"Tá bom, por {Money.Format(price)} por mês eu fecho.", $"Fechado! Pode fazer por {Money.Format(price)}.", $"Ok, me convenceu. {Money.Format(price)} por mês, pode cadastrar."));
                    return Turn(string.Join(" ", reply), "satisfeito");
                }

                if (lead.Archetype == CustomerArchetype.Indeciso && !askedTime && acc >= t.Threshold - 0.1f)
                {
                    askedTime = true;
                    reply.Add("Ai, não sei... deixa eu pensar um pouquinho.");
                    return Turn(string.Join(" ", reply), "indeciso");
                }

                patience -= 1;
                if (will <= 0 || interest < 0.3f)
                {
                    reply.Add(Pick("notyet", "Ainda não me convenci. Por que eu deveria contratar isso?", "Não sei se vale a pena. O que tem de diferente?"));
                }
                else
                {
                    priceObjection = true;
                    long counter = (long)Math.Round(Math.Min(will, lead.BudgetCents) * (t.Anchor + (1 - t.Anchor) * (1 - patience / (float)t.Patience)) / 100.0) * 100 - 10;
                    if (counter >= price) counter = (long)Math.Round(price * 0.92 / 100.0) * 100 - 10;
                    counter = Math.Max(990, counter);
                    lastCounter = counter;
                    reply.Add(Pick("counter", $"Está caro. Se fizer por {Money.Format(counter)} eu penso no caso.", $"Consigo pagar uns {Money.Format(counter)} por mês, no máximo.", $"Por {Money.Format(counter)} eu fecharia."));
                }
                mood = "negociando";
            }
        }

        if (!progress)
        {
            patience -= 1;
            reply.Add(Pick("filler", "Hum... e então?", "Não entendi muito bem, pode explicar?", "Certo... e o que mais?", "Tá, mas o que você quer me oferecer?"));
        }

        if (patience <= 0) return HangUp("Olha, não tenho mais tempo pra isso. Tchau.", "impaciente");

        return Turn(string.Join(" ", reply.Distinct()), mood);
    }

    public float Acceptance(long priceCents, ServiceInfo service)
    {
        if (priceCents <= 0 || priceCents > lead.BudgetCents * 1.05) return 0f;
        float ps = Clamp01(1.5f - (float)priceCents / service.MonthlyPriceCents);
        return Clamp01(ps * t.Sensitivity + interest * 0.3f + trust * 0.2f);
    }

    public long Willingness(ServiceInfo service)
    {
        float needed = t.Threshold - (interest * 0.3f + trust * 0.2f);
        double max;
        if (needed <= 0) max = double.MaxValue;
        else
        {
            double ps = needed / t.Sensitivity;
            if (ps > 1) return 0;
            max = 1.5 - ps;
        }
        return (long)Math.Floor(Math.Min(max * service.MonthlyPriceCents, lead.BudgetCents * 1.05));
    }

    private CustomerTurn HangUp(string speech, string mood)
    {
        decision = CustomerDecision.HungUp;
        return Turn(speech + " (desligou)", mood);
    }

    private CustomerTurn Turn(string speech, string mood) => new CustomerTurn
    {
        Speech = speech,
        Interest = (int)Math.Round(interest * 100),
        Trust = (int)Math.Round(trust * 100),
        Mood = mood,
        Decision = decision,
        AcceptedServiceId = decision == CustomerDecision.WantsToBuy ? focusServiceId : null,
        AcceptedPriceCents = decision == CustomerDecision.WantsToBuy ? acceptedPrice : null,
        Objection = fidelityObjection ? "fidelidade" : priceObjection ? "preço" : null,
        Source = "offline"
    };

    private string Pick(string key, params string[] options)
    {
        int i = rng.Next(options.Length);
        if (options.Length > 1 && lastLine.TryGetValue(key, out int last) && last == i) i = (i + 1) % options.Length;
        lastLine[key] = i;
        return options[i];
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
