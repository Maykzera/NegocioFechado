using System;
using System.Collections.Generic;

/// <summary>Desbloqueio obtido ao atingir um nível.</summary>
public readonly struct LevelUnlock
{
    public readonly int Level;
    public readonly string Description;

    public LevelUnlock(int level, string description)
    {
        Level = level;
        Description = description;
    }
}

/// <summary>
/// Experiência e níveis do empreendedor. A XP vem de ações comerciais válidas,
/// qualidade operacional e avaliações do mestre (não de cliques repetidos).
/// </summary>
public sealed class ProgressionService
{
    public static readonly LevelUnlock[] Unlocks =
    {
        new LevelUnlock(2, "Mochila maior (14 itens)"),
        new LevelUnlock(3, "Depósito ampliado (100 itens) e bancada maior"),
        new LevelUnlock(4, "Mais pedidos prontos ao mesmo tempo (30)"),
        new LevelUnlock(5, "Mochila profissional (20 itens)"),
        new LevelUnlock(6, "Atacadista liberado: preços de atacado"),
        new LevelUnlock(8, "Depósito grande (200 itens)"),
    };

    public int Xp { get; private set; }
    public int Level { get; private set; } = 1;

    public event Action<int> OnLevelUp;
    public event Action<int, string> OnXpGained;

    /// <summary>XP total necessária para chegar ao nível informado.</summary>
    public static int XpForLevel(int level) => level <= 1 ? 0 : 50 * (level - 1) * level;

    public static string TitleFor(int level) =>
        level <= 5 ? "Vendedor iniciante"
        : level <= 10 ? "Revendedor profissional"
        : level <= 20 ? "Empreendedor"
        : level <= 30 ? "Empresário"
        : "Magnata comercial";

    public string Title => TitleFor(Level);

    public float ProgressToNext
    {
        get
        {
            int cur = XpForLevel(Level), next = XpForLevel(Level + 1);
            return next == cur ? 1f : (float)(Xp - cur) / (next - cur);
        }
    }

    public int XpToNext => XpForLevel(Level + 1) - Xp;

    public void AddXp(int amount, string reason)
    {
        if (amount <= 0) return;
        Xp += amount;
        OnXpGained?.Invoke(amount, reason);
        while (Xp >= XpForLevel(Level + 1))
        {
            Level++;
            OnLevelUp?.Invoke(Level);
        }
    }

    public IEnumerable<LevelUnlock> UnlocksAt(int level)
    {
        foreach (var u in Unlocks) if (u.Level == level) yield return u;
    }

    // Regras de XP, centralizadas
    public static int XpForPurchase(PurchaseReceipt r) => 5;
    public static int XpForPackaging(PackagingScore s) => (int)Math.Round(5 + s.Overall * 20);
    public static int XpForSale(SaleRecord s) => s.ProfitCents > 0 ? 20 + (int)Math.Min(30, s.ProfitCents / 200) : 8;
    public static int XpForEvaluation(MentorEvaluation e) => (int)Math.Round(e.Grade * 6);

    public void Restore(int xp)
    {
        Xp = Math.Max(0, xp);
        Level = 1;
        while (Xp >= XpForLevel(Level + 1)) Level++;
    }

    /// <summary>Capacidades de estoque para um nível.</summary>
    public static (int backpack, int warehouse, int bench, int orders) CapacitiesFor(int level) =>
    (
        level >= 5 ? 20 : level >= 2 ? 14 : 10,
        level >= 8 ? 200 : level >= 3 ? 100 : 60,
        level >= 3 ? 6 : 4,
        level >= 4 ? 30 : 20
    );
}
