using System.Collections.Generic;
using System.Threading.Tasks;

public enum CustomerDecision
{
    Talking,
    WantsToBuy,
    Refused,
    HungUp
}

/// <summary>Resposta do cliente numa ligação: a fala e os sinais que o jogo usa.</summary>
public sealed class CustomerTurn
{
    public string Speech;
    public int Interest;      // 0 a 100
    public int Trust;         // 0 a 100
    public string Mood;
    public string Objection;
    public CustomerDecision Decision;
    public string AcceptedServiceId;
    public long? AcceptedPriceCents;

    /// <summary>De onde veio a resposta: "claude" ou "offline".</summary>
    public string Source;

    /// <summary>Resposta exata da IA (JSON), reenviada no histórico das próximas falas.</summary>
    public string Raw;
}

/// <summary>Uma fala na ligação.</summary>
public readonly struct CallLine
{
    public readonly bool FromPlayer;
    public readonly string Text;

    public CallLine(bool fromPlayer, string text)
    {
        FromPlayer = fromPlayer;
        Text = text;
    }
}

/// <summary>O que a IA do cliente precisa saber para responder.</summary>
public sealed class CallContext
{
    public Lead Lead;
    public string PlayerName;
    public string CompanyName;
    public int PlayerLevel;
    public bool HasHeadsetUpgrade;
    public IReadOnlyList<CallLine> History;

    /// <summary>Respostas anteriores do cliente, na mesma ordem das falas dele (para a IA manter o histórico exato).</summary>
    public IReadOnlyList<string> PreviousRawReplies;
}

/// <summary>Quem interpreta o cliente: a IA real (Claude) ou o cliente offline.</summary>
public interface ICustomerAI
{
    string Name { get; }

    /// <summary>Primeira fala ao atender.</summary>
    Task<CustomerTurn> AnswerAsync(CallContext context);

    /// <summary>Resposta a uma fala do vendedor.</summary>
    Task<CustomerTurn> RespondAsync(CallContext context, string playerMessage);
}
