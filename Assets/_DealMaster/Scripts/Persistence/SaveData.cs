using System;
using System.Collections.Generic;

/// <summary>
/// Formato do jogo salvo (JSON versionado). Só campos públicos simples,
/// para funcionar com o JsonUtility da Unity.
/// </summary>
[Serializable]
public class SaveData
{
    public int version;
    public string savedAtUtc;

    public long balanceCents;
    public int xp;
    public int day;
    public float minutes;
    public float reputation = 50f;

    public float playerX, playerY, playerZ, playerYaw;
    public bool hasPlayerPosition;

    public List<StackData> inventory = new List<StackData>();
    public List<OrderData> packedOrders = new List<OrderData>();
    public int nextOrderNumber = 1;
    public List<SaleRecord> sales = new List<SaleRecord>();
    public List<SupplierData> suppliers = new List<SupplierData>();
    public List<LedgerData> ledger = new List<LedgerData>();
    public List<EvaluationData> mentorHistory = new List<EvaluationData>();
    public List<RecordData> mentorPending = new List<RecordData>();
    public List<CustomerRecord> customers = new List<CustomerRecord>();
    public List<string> completedMissions = new List<string>();
}

[Serializable] public class StackData { public int location; public string productId; public int quantity; public long totalCostCents; }

[Serializable]
public class OrderData
{
    public string orderId, productId;
    public long productCostCents, materialsCostCents;
    public float protection, integrity, sizeFit, presentation, efficiency, time, overall, damageRisk;
}

[Serializable] public class SupplierData { public string id; public float relationship; public List<string> productIds = new List<string>(); public List<int> stocks = new List<int>(); }

[Serializable] public class LedgerData { public int day; public int type; public long amountCents; public string description; }

[Serializable]
public class EvaluationData
{
    public int evaluatorVersion, day;
    public string customerName, productId, verdict;
    public bool saleClosed;
    public long agreedPriceCents;
    public float grade;
    public List<string> criteriaNames = new List<string>();
    public List<float> criteriaValues = new List<float>();
    public List<string> observations = new List<string>();
    public List<string> suggestions = new List<string>();
}

[Serializable] public class EventData { public int round, type; public long priceCents; public float trust, interest; }

[Serializable]
public class RecordData
{
    public int day, archetype, need, productCategory = -1, finalState;
    public string customerName, productId;
    public long referencePriceCents, productCostCents, materialsCostCents, feeCents, agreedPriceCents;
    public bool saleClosed, packed;
    public float packagingScore, finalTrust;
    public List<EventData> events = new List<EventData>();
}

/// <summary>Conversões entre os objetos do jogo e o formato salvo.</summary>
public static class SaveMapping
{
    public static OrderData ToData(PackedOrder o) => new OrderData
    {
        orderId = o.OrderId, productId = o.ProductId, productCostCents = o.ProductCostCents,
        materialsCostCents = o.Score.MaterialsCostCents, protection = o.Score.Protection, integrity = o.Score.Integrity,
        sizeFit = o.Score.SizeFit, presentation = o.Score.Presentation, efficiency = o.Score.Efficiency,
        time = o.Score.Time, overall = o.Score.Overall, damageRisk = o.Score.DamageRisk
    };

    public static PackedOrder FromData(OrderData d) => new PackedOrder(d.orderId, d.productId, d.productCostCents,
        new PackagingScore(d.protection, d.integrity, d.sizeFit, d.presentation, d.efficiency, d.time, d.overall,
            d.damageRisk, d.materialsCostCents, true, null));

    public static LedgerData ToData(LedgerEntry e) => new LedgerData { day = e.Day, type = (int)e.Type, amountCents = e.AmountCents, description = e.Description };
    public static LedgerEntry FromData(LedgerData d) => new LedgerEntry(d.day, (LedgerEntryType)d.type, d.amountCents, d.description);

    public static EvaluationData ToData(MentorEvaluation e)
    {
        var d = new EvaluationData
        {
            evaluatorVersion = e.EvaluatorVersion, day = e.Day, customerName = e.CustomerName, productId = e.ProductId,
            verdict = e.Verdict, saleClosed = e.SaleClosed, agreedPriceCents = e.AgreedPriceCents, grade = e.Grade,
            observations = new List<string>(e.Observations), suggestions = new List<string>(e.Suggestions)
        };
        foreach (var c in e.Criteria) { d.criteriaNames.Add(c.Name); d.criteriaValues.Add(c.Value); }
        return d;
    }

    public static MentorEvaluation FromData(EvaluationData d)
    {
        var e = new MentorEvaluation
        {
            EvaluatorVersion = d.evaluatorVersion, Day = d.day, CustomerName = d.customerName, ProductId = d.productId,
            Verdict = d.verdict, SaleClosed = d.saleClosed, AgreedPriceCents = d.agreedPriceCents, Grade = d.grade,
            Observations = new List<string>(d.observations ?? new List<string>()),
            Suggestions = new List<string>(d.suggestions ?? new List<string>())
        };
        int n = Math.Min(d.criteriaNames?.Count ?? 0, d.criteriaValues?.Count ?? 0);
        for (int i = 0; i < n; i++) e.Criteria.Add(new CriterionScore(d.criteriaNames[i], d.criteriaValues[i]));
        return e;
    }

    public static RecordData ToData(NegotiationRecord r)
    {
        var d = new RecordData
        {
            day = r.Day, archetype = (int)r.Archetype, need = (int)r.Need, productCategory = r.ProductCategory.HasValue ? (int)r.ProductCategory.Value : -1,
            finalState = (int)r.FinalState, customerName = r.CustomerName, productId = r.ProductId,
            referencePriceCents = r.ReferencePriceCents, productCostCents = r.ProductCostCents, materialsCostCents = r.MaterialsCostCents,
            feeCents = r.FeeCents, agreedPriceCents = r.AgreedPriceCents, saleClosed = r.SaleClosed, packed = r.Packed,
            packagingScore = r.PackagingScore, finalTrust = r.FinalTrust
        };
        foreach (var e in r.Events)
            d.events.Add(new EventData { round = e.Round, type = (int)e.Type, priceCents = e.PriceCents, trust = e.TrustAfter, interest = e.InterestAfter });
        return d;
    }

    public static NegotiationRecord FromData(RecordData d)
    {
        var r = new NegotiationRecord
        {
            Day = d.day, Archetype = (CustomerArchetype)d.archetype, Need = (ProductCategory)d.need,
            ProductCategory = d.productCategory >= 0 ? (ProductCategory?)d.productCategory : null,
            FinalState = (CustomerState)d.finalState, CustomerName = d.customerName, ProductId = d.productId,
            ReferencePriceCents = d.referencePriceCents, ProductCostCents = d.productCostCents, MaterialsCostCents = d.materialsCostCents,
            FeeCents = d.feeCents, AgreedPriceCents = d.agreedPriceCents, SaleClosed = d.saleClosed, Packed = d.packed,
            PackagingScore = d.packagingScore, FinalTrust = d.finalTrust
        };
        foreach (var e in d.events ?? new List<EventData>())
            r.Events.Add(new NegotiationEvent(e.round, (NegotiationEventType)e.type, e.priceCents, e.trust, e.interest));
        return r;
    }
}
