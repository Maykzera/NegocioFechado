using System;

/// <summary>
/// Contrato do sistema financeiro. É a fonte única da verdade para o dinheiro:
/// nenhuma conversa com NPC pode alterar o saldo sem passar por aqui.
/// </summary>
public interface IWallet
{
    long BalanceCents { get; }

    event Action<long> OnBalanceChanged;

    bool TryDebit(long amountCents);

    bool TryCredit(long amountCents);
}
