using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Guarda as negociações à espera de análise e o histórico de avaliações do jogador.</summary>
public sealed class MentorService
{
    private readonly List<NegotiationRecord> pending = new List<NegotiationRecord>();
    private readonly List<MentorEvaluation> history = new List<MentorEvaluation>();

    public IReadOnlyList<NegotiationRecord> Pending => pending;
    public IReadOnlyList<MentorEvaluation> History => history;

    public event Action<MentorEvaluation> OnEvaluated;

    public void Submit(NegotiationRecord record)
    {
        if (record == null) return;
        pending.Add(record);
    }

    public MentorEvaluation ReviewNext()
    {
        if (pending.Count == 0) return null;
        var record = pending[0];
        pending.RemoveAt(0);
        var eval = MentorEvaluator.Evaluate(record);
        history.Add(eval);
        OnEvaluated?.Invoke(eval);
        return eval;
    }

    /// <summary>Média das últimas <paramref name="count"/> notas (evolução do jogador).</summary>
    public float RecentAverage(int count = 5)
    {
        if (history.Count == 0) return 0f;
        return (float)Math.Round(history.Skip(Math.Max(0, history.Count - count)).Average(e => e.Grade), 1);
    }

    public bool HasApprovedProfitableSale =>
        history.Any(e => e.SaleClosed && MentorEvaluator.IsApproved(e));

    public void Restore(IEnumerable<MentorEvaluation> savedHistory, IEnumerable<NegotiationRecord> savedPending)
    {
        history.Clear();
        history.AddRange(savedHistory);
        pending.Clear();
        pending.AddRange(savedPending);
    }

    /// <summary>Monta o registro a partir do resultado da negociação e da venda (se houver).</summary>
    public static NegotiationRecord BuildRecord(NegotiationOutcome outcome, SaleRecord sale, int day)
    {
        var product = ProductDatabase.Get(outcome.ProductId);
        return new NegotiationRecord
        {
            Day = day,
            CustomerName = outcome.CustomerName,
            Archetype = outcome.Archetype,
            Need = outcome.Need,
            ProductId = outcome.ProductId,
            ProductCategory = product?.Category,
            ReferencePriceCents = product?.ReferencePriceCents ?? 0,
            ProductCostCents = sale?.ProductCostCents ?? product?.BaseCostCents ?? 0,
            MaterialsCostCents = sale?.MaterialsCostCents ?? 0,
            FeeCents = sale?.FeeCents ?? 0,
            SaleClosed = outcome.SaleClosed && sale != null,
            AgreedPriceCents = sale?.PriceCents ?? 0,
            Packed = sale?.Packed ?? outcome.Packed,
            PackagingScore = sale?.PackagingScore ?? 0f,
            FinalTrust = outcome.Brain?.Trust ?? 0f,
            FinalState = outcome.Brain?.State ?? CustomerState.Left,
            Events = outcome.Brain != null ? outcome.Brain.Memory.ToList() : new List<NegotiationEvent>()
        };
    }
}
