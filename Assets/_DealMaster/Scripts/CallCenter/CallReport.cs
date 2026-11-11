using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Resumo de uma ligação encerrada: o que o supervisor analisa e o que fica salvo.</summary>
[Serializable]
public sealed class CallReport
{
    public string CallId;
    public int Day;
    public string LeadName;
    public CustomerArchetype Archetype;
    public ServiceCategory Need;
    public string NeedServiceId;
    public bool DoNotCall;
    public CallOutcome Outcome;

    public List<string> PlayerLines = new List<string>();
    public List<string> CustomerLines = new List<string>();
    public List<bool> LineIsPlayer = new List<bool>();

    // Sinais extraídos das falas do vendedor
    public bool Greeted, Introduced, AskedPermission;
    public int NeedQuestionsBeforePitch, NeedQuestions;
    public string FirstPitchServiceId;
    public bool PitchedFit;
    public List<string> Facts = new List<string>();
    public List<string> Lies = new List<string>();
    public int PressureCount, RudeCount, EmpathyCount, ObjectionsRaised, ObjectionsAnswered, OffTopicMessages;
    public bool RespectedDoNotCall;

    // Venda
    public bool SaleClosed;
    public string SoldServiceId;
    public long SoldPriceCents;
    public long ListPriceCents;
    public long CommissionCents;

    public int FinalTrust, FinalInterest;
    public string AiSource;

    public static CallReport From(CallSession call, SaleRecord sale)
    {
        var r = new CallReport
        {
            CallId = call.CallId,
            Day = call.Day,
            LeadName = call.Lead.Name,
            Archetype = call.Lead.Archetype,
            Need = call.Lead.Need,
            NeedServiceId = call.Lead.NeedServiceId,
            DoNotCall = call.Lead.DoNotCall,
            Outcome = call.Outcome,
            FinalTrust = call.LastTurn?.Trust ?? 0,
            FinalInterest = call.LastTurn?.Interest ?? 0,
            AiSource = call.Turns.Any(t => t.Source == "claude") ? "claude" : "offline"
        };

        foreach (var line in call.Transcript)
        {
            r.LineIsPlayer.Add(line.FromPlayer);
            (line.FromPlayer ? r.PlayerLines : r.CustomerLines).Add(line.Text);
        }

        bool pitched = false;
        bool pendingObjection = false;
        int turnIndex = 1; // a primeira fala do cliente é o "alô"
        foreach (var a in call.Analyses)
        {
            if (a.Has(PlayerIntent.Greeting)) r.Greeted = true;
            if (a.Has(PlayerIntent.Introduction)) r.Introduced = true;
            if (a.Has(PlayerIntent.AskPermission)) r.AskedPermission = true;
            if (a.Has(PlayerIntent.AskNeed)) { r.NeedQuestions++; if (!pitched) r.NeedQuestionsBeforePitch++; }
            if (a.ServicesMentioned.Count > 0 && !pitched)
            {
                pitched = true;
                r.FirstPitchServiceId = a.ServicesMentioned[0];
                var s = ServiceCatalog.Get(r.FirstPitchServiceId);
                r.PitchedFit = s != null && s.Category == call.Lead.Need;
            }
            foreach (var f in a.FactsUsed) if (!r.Facts.Contains(f)) r.Facts.Add(f);
            foreach (var l in a.LiesUsed) if (!r.Lies.Contains(l)) r.Lies.Add(l);
            if (a.Has(PlayerIntent.Pressure)) r.PressureCount++;
            if (a.Has(PlayerIntent.Rude)) r.RudeCount++;
            if (a.Has(PlayerIntent.Empathy)) r.EmpathyCount++;
            if (pendingObjection && (a.Has(PlayerIntent.Empathy) || a.Has(PlayerIntent.Benefit))) { r.ObjectionsAnswered++; pendingObjection = false; }
            if (a.Intents == PlayerIntent.None || a.Intents == PlayerIntent.Question) r.OffTopicMessages++;

            var t = turnIndex < call.Turns.Count ? call.Turns[turnIndex] : null;
            if (t != null && !string.IsNullOrEmpty(t.Objection)) { r.ObjectionsRaised++; pendingObjection = true; }
            turnIndex++;
        }

        if (call.Lead.DoNotCall)
        {
            r.RespectedDoNotCall = call.Outcome == CallOutcome.Refused || (call.Outcome == CallOutcome.PlayerEnded && call.Analyses.Count <= 2);
        }

        if (sale != null)
        {
            r.SaleClosed = true;
            r.SoldServiceId = sale.ServiceId;
            r.SoldPriceCents = sale.PriceCents;
            r.ListPriceCents = ServiceCatalog.Get(sale.ServiceId)?.MonthlyPriceCents ?? sale.PriceCents;
            r.CommissionCents = sale.CommissionCents;
        }
        return r;
    }

    public string TranscriptText(string playerLabel = "Vendedor", string customerLabel = "Cliente")
    {
        var sb = new System.Text.StringBuilder();
        int p = 0, c = 0;
        foreach (bool isPlayer in LineIsPlayer)
        {
            if (isPlayer) sb.Append(playerLabel).Append(": ").AppendLine(PlayerLines[p++]);
            else sb.Append(customerLabel).Append(": ").AppendLine(CustomerLines[c++]);
        }
        return sb.ToString();
    }
}
