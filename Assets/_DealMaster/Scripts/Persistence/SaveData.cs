using System;
using System.Collections.Generic;

/// <summary>Jogo salvo da central (JSON versionado, compatível com JsonUtility).</summary>
[Serializable]
public class SaveData
{
    public int version;
    public string savedAtUtc;

    public long balanceCents;
    public int xp;
    public int day = 1;
    public float minutes;
    public int energy = 100;
    public int seed;

    public List<string> upgrades = new List<string>();
    public List<int> quotaDays = new List<int>();
    public List<string> missions = new List<string>();
    public List<SaleRecord> sales = new List<SaleRecord>();
    public List<LedgerEntry> ledger = new List<LedgerEntry>();
    public List<CallReport> reports = new List<CallReport>();
    public List<CallReport> pendingReviews = new List<CallReport>();
    public List<SupervisorReview> reviews = new List<SupervisorReview>();
    public List<Lead> queue = new List<Lead>();

    public float playerX, playerY, playerZ, playerYaw;
    public bool hasPlayerPosition;
}
