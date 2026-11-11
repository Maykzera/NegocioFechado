using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum CallState
{
    Ringing,
    InCall,
    Ended
}

public enum CallOutcome
{
    None,
    NoAnswer,
    HungUp,
    Refused,
    PlayerEnded,
    Sale
}

/// <summary>
/// Uma ligação: discagem, conversa (com a IA ou o cliente offline) e encerramento.
/// Guarda tudo o que foi dito para o registro de venda e para o supervisor.
/// </summary>
public sealed class CallSession
{
    private readonly ICustomerAI ai;
    private readonly ICustomerAI fallback;
    private readonly List<CallLine> transcript = new List<CallLine>();
    private readonly List<string> rawReplies = new List<string>();
    private readonly List<MessageAnalysis> analyses = new List<MessageAnalysis>();
    private readonly List<CustomerTurn> turns = new List<CustomerTurn>();

    public string CallId { get; }
    public Lead Lead { get; }
    public CallContext Context { get; }
    public CallState State { get; private set; } = CallState.Ringing;
    public CallOutcome Outcome { get; private set; }
    public bool Busy { get; private set; }
    public int Day { get; }

    public IReadOnlyList<CallLine> Transcript => transcript;
    public IReadOnlyList<MessageAnalysis> Analyses => analyses;
    public IReadOnlyList<CustomerTurn> Turns => turns;
    public CustomerTurn LastTurn => turns.Count > 0 ? turns[turns.Count - 1] : null;

    /// <summary>Último aviso de falha da IA real (quando caiu para o modo offline).</summary>
    public string LastAiError { get; private set; }

    public CustomerTurn Agreement => turns.LastOrDefault(t => t.Decision == CustomerDecision.WantsToBuy);
    public bool SaleRegistered { get; internal set; }

    /// <summary>Serviço em conversa: o último citado pelo vendedor.</summary>
    public string FocusServiceId => analyses.SelectMany(a => a.ServicesMentioned).LastOrDefault();

    public event Action<CallSession> OnChanged;

    public CallSession(string callId, Lead lead, CallContext context, ICustomerAI ai, ICustomerAI fallback, int day)
    {
        CallId = callId;
        Lead = lead ?? throw new ArgumentNullException(nameof(lead));
        this.ai = ai ?? throw new ArgumentNullException(nameof(ai));
        this.fallback = fallback;
        Day = day;
        Context = context ?? new CallContext();
        Context.Lead = lead;
        Context.History = transcript;
        Context.PreviousRawReplies = rawReplies;
    }

    /// <param name="pickupRoll">Número de 0 a 1: se for maior que a chance de atender, ninguém atende.</param>
    public async Task<bool> DialAsync(double pickupRoll)
    {
        if (State != CallState.Ringing) return false;
        if (pickupRoll > Lead.PickupChance)
        {
            End(CallOutcome.NoAnswer);
            return false;
        }
        State = CallState.InCall;
        var turn = await Ask(() => ai.AnswerAsync(Context), () => fallback?.AnswerAsync(Context));
        AddCustomer(turn);
        return true;
    }

    public async Task<CustomerTurn> SendAsync(string playerMessage)
    {
        if (State != CallState.InCall || Busy || string.IsNullOrWhiteSpace(playerMessage)) return null;
        playerMessage = playerMessage.Trim();
        if (playerMessage.Length > 600) playerMessage = playerMessage.Substring(0, 600);

        var focus = turns.Count > 0 && FocusServiceId != null ? new[] { ServiceCatalog.Get(FocusServiceId) } : null;
        analyses.Add(MessageAnalyzer.Analyze(playerMessage, focus));
        transcript.Add(new CallLine(true, playerMessage));
        OnChanged?.Invoke(this);

        var turn = await Ask(() => ai.RespondAsync(Context, playerMessage), () => fallback?.RespondAsync(Context, playerMessage));
        AddCustomer(turn);

        if (turn.Decision == CustomerDecision.HungUp) End(CallOutcome.HungUp);
        else if (turn.Decision == CustomerDecision.Refused) End(CallOutcome.Refused);
        return turn;
    }

    /// <summary>O vendedor encerra a ligação.</summary>
    public void HangUp()
    {
        if (State == CallState.Ended) return;
        End(SaleRegistered ? CallOutcome.Sale : CallOutcome.PlayerEnded);
    }

    internal void MarkSale()
    {
        SaleRegistered = true;
        if (State == CallState.Ended && Outcome != CallOutcome.Sale) Outcome = CallOutcome.Sale;
    }

    private async Task<CustomerTurn> Ask(Func<Task<CustomerTurn>> primary, Func<Task<CustomerTurn>> backup)
    {
        Busy = true;
        OnChanged?.Invoke(this);
        try
        {
            return await primary();
        }
        catch (Exception e) when (backup != null)
        {
            LastAiError = e.Message;
            var t = await backup();
            return t;
        }
        finally
        {
            Busy = false;
        }
    }

    private void AddCustomer(CustomerTurn turn)
    {
        if (turn == null) return;
        turns.Add(turn);
        rawReplies.Add(turn.Raw ?? turn.Speech);
        transcript.Add(new CallLine(false, turn.Speech));
        OnChanged?.Invoke(this);
    }

    private void End(CallOutcome outcome)
    {
        State = CallState.Ended;
        Outcome = SaleRegistered ? CallOutcome.Sale : outcome;
        OnChanged?.Invoke(this);
    }
}
