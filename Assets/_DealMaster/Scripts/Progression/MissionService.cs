using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Estado do negócio usado para verificar as missões.</summary>
public sealed class MissionContext
{
    public int Purchases;
    public int PackedOrders;
    public int Sales;
    public long TotalProfitCents;
    public bool ApprovedProfitableSale;
    public float Reputation;
    public float BestGrade;
}

public sealed class Mission
{
    public string Id;
    public string Title;
    public string Description;
    public int RewardXp;
    public Func<MissionContext, bool> IsComplete;
    public bool Completed;
    public bool Main;
}

/// <summary>Missões da primeira versão jogável.</summary>
public sealed class MissionService
{
    private readonly List<Mission> missions = new List<Mission>
    {
        new Mission { Id = "comprar", Title = "Primeiro estoque", Description = "Compre um produto em um fornecedor.", RewardXp = 20, IsComplete = c => c.Purchases >= 1 },
        new Mission { Id = "embalar", Title = "Caprichando", Description = "Embale um pedido na bancada.", RewardXp = 25, IsComplete = c => c.PackedOrders >= 1 },
        new Mission { Id = "vender", Title = "Primeira venda", Description = "Feche uma venda com um cliente.", RewardXp = 30, IsComplete = c => c.Sales >= 1 },
        new Mission { Id = "principal", Title = "Negócio fechado", Description = "Faça uma venda lucrativa e obtenha a aprovação do mestre negociador (nota 7 ou mais).", RewardXp = 100, Main = true, IsComplete = c => c.ApprovedProfitableSale },
        new Mission { Id = "cinco", Title = "Pegando o ritmo", Description = "Feche 5 vendas.", RewardXp = 60, IsComplete = c => c.Sales >= 5 },
        new Mission { Id = "reputacao", Title = "Bem falado", Description = "Alcance reputação 60.", RewardXp = 60, IsComplete = c => c.Reputation >= 60f },
        new Mission { Id = "lucro", Title = "No azul", Description = "Acumule R$ 300,00 de lucro direto em vendas.", RewardXp = 80, IsComplete = c => c.TotalProfitCents >= 30000 },
        new Mission { Id = "excelencia", Title = "Excelência", Description = "Receba nota 8,5 ou mais do mestre.", RewardXp = 80, IsComplete = c => c.BestGrade >= 8.5f },
    };

    public IReadOnlyList<Mission> Missions => missions;

    public event Action<Mission> OnCompleted;

    public Mission MainMission => missions.First(m => m.Main);

    /// <summary>Verifica as missões e retorna as que acabaram de ser concluídas.</summary>
    public List<Mission> Check(MissionContext context)
    {
        var done = new List<Mission>();
        foreach (var m in missions)
        {
            if (m.Completed || !m.IsComplete(context)) continue;
            m.Completed = true;
            done.Add(m);
            OnCompleted?.Invoke(m);
        }
        return done;
    }

    public IEnumerable<string> CompletedIds => missions.Where(m => m.Completed).Select(m => m.Id);

    public void Restore(IEnumerable<string> completedIds)
    {
        var set = new HashSet<string>(completedIds ?? Enumerable.Empty<string>());
        foreach (var m in missions) m.Completed = set.Contains(m.Id);
    }
}
