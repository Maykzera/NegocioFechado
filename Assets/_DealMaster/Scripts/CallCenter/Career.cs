using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Experiência e cargo do vendedor na central.</summary>
public sealed class CareerService
{
    public int Xp { get; private set; }
    public int Level => LevelFromXp(Xp);

    public event Action<int> OnLevelUp;

    public static int XpForLevel(int level) => level <= 1 ? 0 : 60 * (level - 1) * level;

    public static int LevelFromXp(int xp)
    {
        int l = 1;
        while (xp >= XpForLevel(l + 1)) l++;
        return l;
    }

    public static string TitleFor(int level) =>
        level <= 2 ? "Operador(a) Júnior"
        : level <= 4 ? "Operador(a) Pleno"
        : level <= 6 ? "Operador(a) Sênior"
        : level <= 8 ? "Supervisor(a) de Vendas"
        : "Gerente Comercial";

    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        int before = Level;
        Xp += amount;
        for (int l = before + 1; l <= Level; l++) OnLevelUp?.Invoke(l);
    }

    public void Restore(int xp) => Xp = Math.Max(0, xp);

    public static int XpForCall(CallReport r) => r.Outcome == CallOutcome.NoAnswer ? 1 : 5;
    public static int XpForSale(SaleRecord s) => 20 + (int)Math.Min(40, s.CommissionCents / 300);
    public static int XpForReview(SupervisorReview r) => r.LiesFound.Count > 0 ? 0 : (int)Math.Round(r.Grade * 6);
}

public sealed class Mission
{
    public string Id, Title, Description;
    public int RewardXp;
    public bool Main;
    public Func<MissionContext, bool> Check;
}

public sealed class MissionContext
{
    public int CallsAnswered, Sales, BestGrade10, QuotaDays, ApprovedSales;
    public bool RespectedDoNotCall;
}

/// <summary>Missões da carreira na central.</summary>
public sealed class MissionBoard
{
    public static readonly Mission[] All =
    {
        new Mission { Id = "primeira_ligacao", Title = "Alô, alô", Description = "Faça uma ligação que seja atendida.", RewardXp = 15, Check = c => c.CallsAnswered >= 1 },
        new Mission { Id = "primeira_venda", Title = "Primeira venda", Description = "Registre uma venda no CRM.", RewardXp = 30, Check = c => c.Sales >= 1 },
        new Mission { Id = "principal", Title = "Negócio fechado", Description = "Faça uma venda honesta aprovada pelo supervisor (nota 7 ou mais, sem mentiras).", RewardXp = 100, Main = true, Check = c => c.ApprovedSales >= 1 },
        new Mission { Id = "meta", Title = "Bati a meta", Description = "Cumpra a meta de vendas de um dia.", RewardXp = 60, Check = c => c.QuotaDays >= 1 },
        new Mission { Id = "respeito", Title = "Respeito em primeiro lugar", Description = "Respeite um cliente que pediu para não ser incomodado.", RewardXp = 40, Check = c => c.RespectedDoNotCall },
        new Mission { Id = "dez_vendas", Title = "Máquina de vendas", Description = "Registre 10 vendas.", RewardXp = 120, Check = c => c.Sales >= 10 },
        new Mission { Id = "excelencia", Title = "Excelência", Description = "Receba nota 9 ou mais do supervisor.", RewardXp = 80, Check = c => c.BestGrade10 >= 90 },
        new Mission { Id = "tres_metas", Title = "Constância", Description = "Cumpra a meta em 3 dias.", RewardXp = 150, Check = c => c.QuotaDays >= 3 },
    };

    private readonly HashSet<string> done = new HashSet<string>();

    public IEnumerable<string> Completed => done;
    public bool IsDone(string id) => done.Contains(id);

    public event Action<Mission> OnCompleted;

    public List<Mission> Evaluate(MissionContext ctx)
    {
        var list = new List<Mission>();
        foreach (var m in All)
        {
            if (done.Contains(m.Id) || !m.Check(ctx)) continue;
            done.Add(m.Id);
            list.Add(m);
            OnCompleted?.Invoke(m);
        }
        return list;
    }

    public void Restore(IEnumerable<string> ids)
    {
        done.Clear();
        foreach (var id in ids ?? Enumerable.Empty<string>()) done.Add(id);
    }
}

[Serializable]
public sealed class Upgrade
{
    public string Id, Name, Description;
    public long PriceCents;
    public int RequiredLevel;
}

/// <summary>Melhorias da estação de trabalho, compradas com as comissões.</summary>
public static class UpgradeCatalog
{
    public const string Headset = "headset";
    public const string QualifiedLeads = "leads_qualificados";
    public const string ErgoChair = "cadeira";
    public const string SecondMonitor = "segundo_monitor";
    public const string CoffeeMaker = "cafeteira";

    public static readonly Upgrade[] All =
    {
        new Upgrade { Id = Headset, Name = "Headset profissional", Description = "Áudio limpo: os clientes começam com um pouco mais de confiança.", PriceCents = 15000, RequiredLevel = 1 },
        new Upgrade { Id = ErgoChair, Name = "Cadeira ergonômica", Description = "Cada ligação cansa a metade.", PriceCents = 20000, RequiredLevel = 2 },
        new Upgrade { Id = SecondMonitor, Name = "Segundo monitor", Description = "O CRM mostra a faixa de orçamento do cliente.", PriceCents = 30000, RequiredLevel = 3 },
        new Upgrade { Id = QualifiedLeads, Name = "Lista de leads qualificados", Description = "Mais gente atende e a necessidade sempre bate com o que você vende.", PriceCents = 45000, RequiredLevel = 4 },
        new Upgrade { Id = CoffeeMaker, Name = "Cafeteira na mesa", Description = "Café de graça a qualquer hora.", PriceCents = 25000, RequiredLevel = 2 },
    };

    public static Upgrade Get(string id) => All.FirstOrDefault(u => u.Id == id);
}
