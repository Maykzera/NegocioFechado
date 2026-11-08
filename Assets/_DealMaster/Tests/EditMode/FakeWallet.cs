using System;

/// <summary>Carteira em memória para testes (WalletService é MonoBehaviour).</summary>
public sealed class FakeWallet : IWallet
{
    public long BalanceCents { get; private set; }

    public event Action<long> OnBalanceChanged;

    public FakeWallet(long balanceCents)
    {
        BalanceCents = balanceCents;
    }

    public bool TryDebit(long amountCents)
    {
        if (amountCents <= 0 || amountCents > BalanceCents) return false;
        BalanceCents -= amountCents;
        OnBalanceChanged?.Invoke(BalanceCents);
        return true;
    }

    public bool TryCredit(long amountCents)
    {
        if (amountCents <= 0 || amountCents > long.MaxValue - BalanceCents) return false;
        BalanceCents += amountCents;
        OnBalanceChanged?.Invoke(BalanceCents);
        return true;
    }
}
