using System;
using UnityEngine;

public class WalletService : MonoBehaviour
{
    [SerializeField]
    private long startingBalanceCents = 150000;

    public long BalanceCents { get; private set; }

    public event Action<long> OnBalanceChanged;

    private void Awake()
    {
        BalanceCents = startingBalanceCents;
    }

    public bool TryDebit(long amountCents)
    {
        if (amountCents <= 0 ||
            amountCents > BalanceCents)
            return false;

        BalanceCents -= amountCents;
        OnBalanceChanged?.Invoke(BalanceCents);
        return true;
    }

    public bool TryCredit(long amountCents)
    {
        if (amountCents <= 0 ||
            amountCents > long.MaxValue - BalanceCents)
            return false;

        BalanceCents += amountCents;
        OnBalanceChanged?.Invoke(BalanceCents);
        return true;
    }

    public void Restore(long savedBalanceCents)
    {
        BalanceCents = Math.Max(0, savedBalanceCents);
        OnBalanceChanged?.Invoke(BalanceCents);
    }
}
