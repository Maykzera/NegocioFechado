using System;

/// <summary>Resultado final de uma conversa com cliente, entregue aos sistemas de venda e ao mestre.</summary>
public sealed class NegotiationOutcome
{
    public string CustomerName;
    public CustomerArchetype Archetype;
    public ProductCategory Need;
    public string ProductId;
    public bool Packed;
    public long AgreedPriceCents;
    public bool SaleClosed;
    public CustomerBrain Brain;
}

/// <summary>
/// Ganchos entre a tela de negociação e os sistemas de venda (Etapa 5) e do mestre (Etapa 6).
/// A negociação só decide; quem mexe em dinheiro e estoque é o SalesService.
/// </summary>
public static class NegotiationHooks
{
    /// <summary>Tenta concluir a venda acordada. Retorna mensagem de erro, ou null em caso de sucesso.</summary>
    public static Func<NegotiationOutcome, string> CompleteSale;

    /// <summary>Disparado ao terminar uma conversa (com ou sem venda).</summary>
    public static event Action<NegotiationOutcome> OnNegotiationEnded;

    public static void RaiseEnded(NegotiationOutcome outcome) => OnNegotiationEnded?.Invoke(outcome);
}
