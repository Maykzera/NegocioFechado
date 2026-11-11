using System;
using System.Collections.Generic;
using System.Linq;

public enum LedgerEntryType
{
    Commission,
    Chargeback,
    QuotaBonus,
    Coffee,
    Upgrade
}

[Serializable]
public struct LedgerEntry
{
    public int Day;
    public LedgerEntryType Type;

    /// <summary>Positivo = entrada de dinheiro; negativo = saída.</summary>
    public long AmountCents;

    public string Description;

    public LedgerEntry(int day, LedgerEntryType type, long amountCents, string description)
    {
        Day = day;
        Type = type;
        AmountCents = amountCents;
        Description = description;
    }
}

/// <summary>Extrato do vendedor: comissões, estornos, bônus e gastos.</summary>
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
    public long SumOnDay(int day) => entries.Where(e => e.Day == day).Sum(e => e.AmountCents);

    /// <summary>Ganhos líquidos: comissões e bônus menos estornos.</summary>
    public long NetEarnings => Sum(LedgerEntryType.Commission) + Sum(LedgerEntryType.QuotaBonus) + Sum(LedgerEntryType.Chargeback);

    public void Restore(IEnumerable<LedgerEntry> saved)
    {
        entries.Clear();
        entries.AddRange(saved);
    }
}
