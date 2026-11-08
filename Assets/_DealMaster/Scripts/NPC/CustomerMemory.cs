using System;
using System.Collections.Generic;

/// <summary>O que um cliente lembra das experiências com o jogador.</summary>
[Serializable]
public sealed class CustomerRecord
{
    public string Name;
    public int Purchases;
    public int Complaints;
    public int TimesLeftUnhappy;
    public int LiesDetected;
    public long TotalSpentCents;
}

/// <summary>
/// Memória dos clientes entre encontros: reconhece compras anteriores, descontos e problemas.
/// Lógica pura, salva junto com o jogo.
/// </summary>
public sealed class CustomerMemory
{
    private readonly Dictionary<string, CustomerRecord> records = new Dictionary<string, CustomerRecord>();

    public IEnumerable<CustomerRecord> Records => records.Values;

    public CustomerRecord Get(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("nome vazio");
        if (!records.TryGetValue(name, out var r))
        {
            r = new CustomerRecord { Name = name };
            records[name] = r;
        }
        return r;
    }

    public bool Knows(string name) => name != null && records.ContainsKey(name);

    /// <summary>Ajuste de confiança inicial com base no histórico (entre -0,3 e +0,25).</summary>
    public float TrustBonus(string name)
    {
        if (!Knows(name)) return 0f;
        var r = records[name];
        float bonus = r.Purchases * 0.06f - r.Complaints * 0.12f - r.TimesLeftUnhappy * 0.04f - r.LiesDetected * 0.1f;
        return Math.Max(-0.3f, Math.Min(0.25f, bonus));
    }

    public void RecordPurchase(string name, long priceCents) { var r = Get(name); r.Purchases++; r.TotalSpentCents += priceCents; }
    public void RecordComplaint(string name) => Get(name).Complaints++;
    public void RecordUnhappyExit(string name) => Get(name).TimesLeftUnhappy++;
    public void RecordLieDetected(string name) => Get(name).LiesDetected++;

    public void Restore(IEnumerable<CustomerRecord> saved)
    {
        records.Clear();
        foreach (var r in saved) if (!string.IsNullOrEmpty(r?.Name)) records[r.Name] = r;
    }

    /// <summary>Saudação de quem já conhece o jogador.</summary>
    public string Greeting(string name)
    {
        if (!Knows(name)) return null;
        var r = records[name];
        if (r.Complaints > 0) return "Da última vez o produto chegou com problema, hein...";
        if (r.LiesDetected > 0) return "Lembro de você. Espero que hoje seja tudo verdade.";
        if (r.Purchases > 0) return "Opa, você de novo! A última compra foi ótima.";
        return "Já nos falamos antes, né?";
    }
}
