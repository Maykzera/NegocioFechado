using UnityEditor;
using UnityEngine;

/// <summary>Etapa 3: bancada de embalagem com rolos de fita, plástico bolha e caixas.</summary>
[InitializeOnLoad]
public static class PackagingSceneSetup
{
    static PackagingSceneSetup()
    {
        DealMasterSceneBuilder.OnPopulate += Populate;
    }

    private static void Populate(DealMasterSceneBuilder.Layout layout)
    {
        var wood = WorldKit.Mat("Bancada", new Color(0.55f, 0.42f, 0.28f));
        var metal = WorldKit.Mat("Metal", new Color(0.5f, 0.5f, 0.52f), 0.6f);
        var cardboard = WorldKit.Mat("Papelao", new Color(0.72f, 0.56f, 0.36f));
        var bubble = WorldKit.Mat("Plastico Bolha", new Color(0.8f, 0.92f, 1f), 0.8f);
        var tape = WorldKit.Mat("Fita", new Color(0.85f, 0.75f, 0.5f), 0.7f);

        var bench = WorldKit.Empty("Bancada de Embalagem", layout.Warehouse, new Vector3(2.5f, 0, 0.5f), 0f).transform;
        WorldKit.Box("Tampo", bench, new Vector3(0, 0.92f, 0), new Vector3(2.4f, 0.08f, 1f), wood);
        foreach (var x in new[] { -1.1f, 1.1f })
            foreach (var z in new[] { -0.42f, 0.42f })
                WorldKit.Box("Perna", bench, new Vector3(x, 0.45f, z), new Vector3(0.08f, 0.9f, 0.08f), metal);

        WorldKit.Box("Caixa Pequena", bench, new Vector3(-0.8f, 1.06f, 0.2f), new Vector3(0.3f, 0.2f, 0.25f), cardboard, false);
        WorldKit.Box("Caixa Media", bench, new Vector3(-0.35f, 1.11f, 0.25f), new Vector3(0.45f, 0.3f, 0.35f), cardboard, false);
        WorldKit.Primitive(PrimitiveType.Cylinder, "Rolo de Plastico Bolha", bench, new Vector3(0.45f, 1.1f, 0.25f),
            new Vector3(0.3f, 0.18f, 0.3f), bubble, false);
        WorldKit.Primitive(PrimitiveType.Cylinder, "Fita", bench, new Vector3(0.9f, 0.99f, -0.2f),
            new Vector3(0.12f, 0.03f, 0.12f), tape, false);
        WorldKit.Sign("BANCADA", bench, new Vector3(0, 1.8f, 0.5f), 0f, 0.05f, Color.white);

        var trigger = bench.gameObject.AddComponent<BoxCollider>();
        trigger.center = new Vector3(0, 1.0f, 0);
        trigger.size = new Vector3(2.4f, 0.6f, 1f);
        trigger.isTrigger = true;
        bench.gameObject.AddComponent<PackingBench>();
    }
}
