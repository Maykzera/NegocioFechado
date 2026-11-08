using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;

public class SaveGameServiceTests
{
    private sealed class MemoryStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
        public bool Exists(string p) => Files.ContainsKey(p);
        public string Read(string p) => Files[p];
        public void Write(string p, string c) => Files[p] = c;
        public void Delete(string p) => Files.Remove(p);
        public void Move(string a, string b) { Files[b] = Files[a]; Files.Remove(a); }
    }

    /// <summary>Serializador de teste (na Unity é usado o JsonUtility).</summary>
    private sealed class XmlTestSerializer : ISaveSerializer
    {
        private readonly XmlSerializer xml = new XmlSerializer(typeof(SaveData));
        public string Serialize(SaveData d) { using var w = new StringWriter(); xml.Serialize(w, d); return w.ToString(); }
        public SaveData Deserialize(string t) { using var r = new StringReader(t); return (SaveData)xml.Deserialize(r); }
    }

    private MemoryStorage storage;
    private SaveGameService service;

    [SetUp]
    public void SetUp()
    {
        storage = new MemoryStorage();
        service = new SaveGameService(storage, new XmlTestSerializer(), "save.json");
    }

    private static SaveData Sample(long balance = 150000) => new SaveData
    {
        balanceCents = balance, xp = 120, day = 3, minutes = 600, reputation = 55,
        inventory = new List<StackData> { new StackData { location = 0, productId = "fone_bt", quantity = 2, totalCostCents = 17000 } },
        completedMissions = new List<string> { "comprar" }
    };

    [Test]
    public void SaveAndLoad_RoundTrip()
    {
        Assert.IsTrue(service.Save(Sample(), out _));
        var r = service.Load();
        Assert.IsTrue(r.Success);
        Assert.AreEqual(150000, r.Data.balanceCents);
        Assert.AreEqual(SaveGameService.CurrentVersion, r.Data.version);
        Assert.AreEqual("fone_bt", r.Data.inventory[0].productId);
        Assert.IsFalse(storage.Exists("save.json.tmp"));
    }

    [Test]
    public void SecondSave_KeepsPreviousAsBackup()
    {
        service.Save(Sample(100), out _);
        service.Save(Sample(200), out _);
        Assert.IsTrue(storage.Exists("save.json.bak"));
        Assert.AreEqual(200, service.Load().Data.balanceCents);
    }

    [Test]
    public void CorruptedMain_FallsBackToBackup()
    {
        service.Save(Sample(100), out _);
        service.Save(Sample(200), out _);
        storage.Files["save.json"] = "<<lixo>>";
        var r = service.Load();
        Assert.IsTrue(r.Success);
        Assert.IsTrue(r.FromBackup);
        Assert.AreEqual(100, r.Data.balanceCents);
    }

    [Test]
    public void InvalidData_IsNotSaved()
    {
        Assert.IsFalse(service.Save(Sample(-5), out var error));
        Assert.IsNotNull(error);
        Assert.IsFalse(service.HasSave);
    }

    [Test]
    public void OldVersion_IsMigrated()
    {
        var old = Sample();
        old.version = 0;
        old.reputation = 0;
        old.day = 0;
        old.packedOrders = null;
        storage.Files["save.json"] = new XmlTestSerializer().Serialize(old);

        var r = service.Load();
        Assert.IsTrue(r.Success);
        Assert.IsTrue(r.Migrated);
        Assert.AreEqual(2, r.Data.version);
        Assert.AreEqual(50f, r.Data.reputation);
        Assert.AreEqual(1, r.Data.day);
        Assert.IsNotNull(r.Data.packedOrders);
    }

    [Test]
    public void NewerVersion_IsRejected()
    {
        var future = Sample();
        future.version = 99;
        storage.Files["save.json"] = new XmlTestSerializer().Serialize(future);
        Assert.IsFalse(service.Load().Success);
    }

    [Test]
    public void Mapping_RoundTripsEvaluationsAndRecords()
    {
        var rec = new NegotiationRecord { Day = 2, CustomerName = "Ana", ProductId = "fone_bt", ProductCategory = ProductCategory.Electronics, SaleClosed = true, AgreedPriceCents = 14000 };
        rec.Events.Add(new NegotiationEvent(1, NegotiationEventType.ProposedPrice, 14000, 0.5f, 0.6f));
        var back = SaveMapping.FromData(SaveMapping.ToData(rec));
        Assert.AreEqual(ProductCategory.Electronics, back.ProductCategory);
        Assert.AreEqual(14000, back.Events[0].PriceCents);

        var eval = MentorEvaluator.Evaluate(rec);
        var evalBack = SaveMapping.FromData(SaveMapping.ToData(eval));
        Assert.AreEqual(eval.Grade, evalBack.Grade);
        Assert.AreEqual(eval.Criteria.Count, evalBack.Criteria.Count);
    }

    [Test]
    public void Inventory_RestoreStack_KeepsExactCost()
    {
        var inv = new InventoryService();
        inv.RestoreStack(StorageLocation.Warehouse, "a", 3, 302);
        Assert.AreEqual(302, inv.GetTotalStockValueCents());
        inv.Clear();
        Assert.AreEqual(0, inv.GetTotalStockValueCents());
    }
}
