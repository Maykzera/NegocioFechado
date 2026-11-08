using UnityEditor;
using UnityEngine;

/// <summary>Etapa 2: balcões dos fornecedores com vendedores e o ponto de estoque do depósito.</summary>
[InitializeOnLoad]
public static class CommerceSceneSetup
{
    static CommerceSceneSetup()
    {
        DealMasterSceneBuilder.OnPopulate += Populate;
    }

    private static void Populate(DealMasterSceneBuilder.Layout layout)
    {
        Counter(layout.ElectronicsShop, DefaultContent.ElectronicsSupplierId, "Seu Jorge", new Color(0.2f, 0.35f, 0.6f));
        Counter(layout.AccessoriesShop, DefaultContent.AccessoriesSupplierId, "Dona Célia", new Color(0.7f, 0.3f, 0.35f));

        // Depósito: estantes de estoque com ponto de interação
        var shelf = WorldKit.Shelf("Estante do Estoque", layout.Warehouse, new Vector3(-4f, 0, 3.6f), 0f, new Color(0.65f, 0.5f, 0.3f));
        var point = WorldKit.Box("Ponto de Estoque", shelf, new Vector3(0, 1f, -0.1f), new Vector3(2.1f, 2f, 0.6f),
            WorldKit.Mat("Invisivel", Color.clear), true, false);
        Object.DestroyImmediate(point.GetComponent<MeshRenderer>());
        point.GetComponent<BoxCollider>().isTrigger = true;
        point.AddComponent<StoragePoint>().Location = StorageLocation.Warehouse;

        // Pilhas de caixas decorativas
        var cardboard = WorldKit.Mat("Papelao", new Color(0.72f, 0.56f, 0.36f));
        for (int i = 0; i < 6; i++)
            WorldKit.Box("Caixa", layout.Warehouse, new Vector3(4.2f - (i % 2) * 0.7f, 0.3f + (i / 2) * 0.6f, 3.8f), new Vector3(0.6f, 0.6f, 0.6f), cardboard);

        layout.Systems.gameObject.AddComponent<PlayerMenus>();
    }

    private static void Counter(Transform shop, string supplierId, string clerk, Color shirt)
    {
        var wood = WorldKit.Mat("Balcao", new Color(0.4f, 0.3f, 0.22f));
        var top = WorldKit.Mat("Tampo", new Color(0.85f, 0.85f, 0.82f), 0.6f);
        var skin = WorldKit.Mat("Pele", new Color(0.8f, 0.62f, 0.48f));
        var shirtMat = WorldKit.Mat("Camisa_" + supplierId, shirt);

        var counter = WorldKit.Empty("Balcao", shop, new Vector3(0, 0, 0.5f)).transform;
        WorldKit.Box("Base", counter, new Vector3(0, 0.5f, 0), new Vector3(3.2f, 1f, 0.8f), wood);
        WorldKit.Box("Tampo", counter, new Vector3(0, 1.02f, 0), new Vector3(3.3f, 0.05f, 0.9f), top);
        WorldKit.Box("Caixa Registradora", counter, new Vector3(0.9f, 1.15f, 0.1f), new Vector3(0.4f, 0.22f, 0.35f),
            WorldKit.Mat("Registradora", new Color(0.15f, 0.15f, 0.17f)), false);

        var npc = WorldKit.Empty("Vendedor " + clerk, counter, new Vector3(0, 0, 1.0f), 180f).transform;
        WorldKit.Primitive(PrimitiveType.Capsule, "Corpo", npc, new Vector3(0, 0.95f, 0), new Vector3(0.55f, 0.75f, 0.4f), shirtMat);
        WorldKit.Primitive(PrimitiveType.Sphere, "Cabeca", npc, new Vector3(0, 1.75f, 0), new Vector3(0.3f, 0.32f, 0.3f), skin, false);
        WorldKit.Sign(clerk, npc, new Vector3(0, 2.15f, 0), 0f, 0.035f, Color.white);

        var supplierCounter = counter.gameObject.AddComponent<SupplierCounter>();
        supplierCounter.SupplierId = supplierId;
        supplierCounter.ClerkName = clerk;
    }
}
