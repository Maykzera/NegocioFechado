using System;
using System.Collections.Generic;
using System.Linq;

public enum LedgerEntryType
{
    Purchase,
    PackagingMaterials,
    SaleRevenue,
    PaymentFee,
    Refund,
    OperatingCost
}

public readonly struct LedgerEntry
{
    public readonly int Day;
    public readonly LedgerEntryType Type;

    /// <summary>Positivo = entrada de dinheiro; negativo = saída.</summary>
    public readonly long AmountCents;

    public readonly string Description;

    public LedgerEntry(int day, LedgerEntryType type, long amountCents, string description)
    {
        Day = day;
        Type = type;
        AmountCents = amountCents;
        Description = description;
    }
}

/// <summary>
/// Livro-caixa: registra cada movimentação e calcula receita, custos, lucro bruto e lucro líquido.
/// As fórmulas econômicas ficam centralizadas aqui.
/// </summary>
public sealed class FinanceLedger
{
    private readonly List<LedgerEntry> entries = new List<LedgerEntry>();

    public IReadOnlyList<LedgerEntry> Entries => entries;

    public Func<int> DayProvider;

    public event Action<LedgerEntry> OnEntry;

    public void Record(LedgerEntryType type, long amountCents, string description)
    {
        var e = new LedgerEntry(DayProvider?.Invoke() ?? 1, type, amountCents, description);
        entries.Add(e);
        OnEntry?.Invoke(e);
    }

    public long Sum(LedgerEntryType type) => entries.Where(e => e.Type == type).Sum(e => e.AmountCents);

    /// <summary>Receita líquida de vendas (vendas − reembolsos).</summary>
    public long Revenue => Sum(LedgerEntryType.SaleRevenue) + Sum(LedgerEntryType.Refund);

    /// <summary>Total gasto em mercadorias.</summary>
    public long PurchasesCost => -Sum(LedgerEntryType.Purchase);

    /// <summary>Despesas: embalagem, taxas e custos operacionais.</summary>
    public long Expenses => -(Sum(LedgerEntryType.PackagingMaterials) + Sum(LedgerEntryType.PaymentFee) + Sum(LedgerEntryType.OperatingCost));

    /// <summary>Saldo do caixa no período (entradas − saídas).</summary>
    public long CashFlow => entries.Sum(e => e.AmountCents);

    public void Restore(IEnumerable<LedgerEntry> saved)
    {
        entries.Clear();
        entries.AddRange(saved);
    }
}
