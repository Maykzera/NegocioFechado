using UnityEditor;
using UnityEngine;

/// <summary>Etapa 6: escritório do mestre com mesa, cadeiras, estante e o mestre.</summary>
[InitializeOnLoad]
public static class MentorSceneSetup
{
    static MentorSceneSetup()
    {
        DealMasterSceneBuilder.OnPopulate += Populate;
    }

    private static void Populate(DealMasterSceneBuilder.Layout layout)
    {
        var office = layout.MentorOffice;
        var wood = WorldKit.Mat("Mesa Mestre", new Color(0.3f, 0.2f, 0.12f), 0.5f);
        var leather = WorldKit.Mat("Couro", new Color(0.15f, 0.12f, 0.1f), 0.4f);
        var suit = WorldKit.Mat("Terno", new Color(0.12f, 0.13f, 0.18f));
        var skin = WorldKit.Mat("Pele Mestre", new Color(0.7f, 0.52f, 0.4f));
        var rug = WorldKit.Mat("Tapete", new Color(0.45f, 0.15f, 0.15f));

        WorldKit.Box("Tapete", office, new Vector3(0, 0.06f, 0), new Vector3(5f, 0.01f, 4f), rug, false);

        var desk = WorldKit.Empty("Mesa do Mestre", office, new Vector3(0, 0, 1.6f)).transform;
        WorldKit.Box("Tampo", desk, new Vector3(0, 0.78f, 0), new Vector3(2.4f, 0.08f, 1.1f), wood);
        WorldKit.Box("Base", desk, new Vector3(0, 0.38f, 0.25f), new Vector3(2.3f, 0.76f, 0.5f), wood);
        WorldKit.Box("Pasta", desk, new Vector3(-0.5f, 0.84f, -0.1f), new Vector3(0.35f, 0.04f, 0.25f), WorldKit.Mat("Pasta", new Color(0.2f, 0.3f, 0.5f)), false);
        desk.gameObject.AddComponent<MentorDesk>();

        var mentor = WorldKit.Empty("Mestre", desk, new Vector3(0, 0, 0.9f), 180f).transform;
        WorldKit.Box("Cadeira", mentor, new Vector3(0, 0.5f, -0.3f), new Vector3(0.6f, 1.2f, 0.12f), leather);
        WorldKit.Primitive(PrimitiveType.Capsule, "Corpo", mentor, new Vector3(0, 0.95f, 0), new Vector3(0.6f, 0.75f, 0.45f), suit);
        WorldKit.Primitive(PrimitiveType.Sphere, "Cabeca", mentor, new Vector3(0, 1.75f, 0), new Vector3(0.3f, 0.32f, 0.3f), skin, false);
        WorldKit.Sign("Mestre Otávio", mentor, new Vector3(0, 2.15f, 0), 0f, 0.035f, new Color(1f, 0.9f, 0.6f));

        foreach (float x in new[] { -0.7f, 0.7f })
            WorldKit.Box("Cadeira Visitante", office, new Vector3(x, 0.45f, 0.3f), new Vector3(0.55f, 0.9f, 0.55f), leather);

        WorldKit.Shelf("Estante de Livros", office, new Vector3(-3.5f, 0, 4f), 0f, new Color(0.5f, 0.2f, 0.2f));
        WorldKit.Shelf("Estante de Troféus", office, new Vector3(3.5f, 0, 4f), 0f, new Color(0.85f, 0.7f, 0.2f));
    }
}
