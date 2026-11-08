using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Constrói a cena jogável "CommercialDistrict" com placeholders 3D.
/// Menu: DealMaster > Construir cena da Vertical Slice.
/// Pode ser executado de novo a qualquer momento: a cena é recriada do zero.
/// </summary>
public static class DealMasterSceneBuilder
{
    public const string ScenePath = "Assets/_DealMaster/Scenes/CommercialDistrict.unity";

    /// <summary>Pontos de referência da cena, usados pelas etapas seguintes ao montar os sistemas.</summary>
    public sealed class Layout
    {
        public Transform World;
        public Transform Systems;
        public Transform ElectronicsShop;
        public Transform AccessoriesShop;
        public Transform Wholesaler;
        public Transform Warehouse;
        public Transform MentorOffice;
        public Transform Plaza;
        public GameObject Player;
    }

    /// <summary>Extensões das etapas seguintes (fornecedores, NPCs, embalagem...) se registram aqui.</summary>
    public static event System.Action<Layout> OnPopulate;

    [MenuItem("DealMaster/Construir cena da Vertical Slice", priority = 0)]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        WorldKit.ClearCache();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var layout = new Layout
        {
            World = new GameObject("Mundo").transform,
            Systems = new GameObject("Sistemas").transform
        };

        BuildEnvironment(layout);
        BuildStreet(layout.World);
        BuildBuildings(layout);
        BuildPlayer(layout);
        BuildSystems(layout);

        OnPopulate?.Invoke(layout);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        Debug.Log("[DealMaster] Cena criada em " + ScenePath + ". Aperte Play para jogar.");
    }

    private static void BuildEnvironment(Layout layout)
    {
        var sunGo = new GameObject("Sol");
        sunGo.transform.SetParent(layout.World, false);
        sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft;
        sun.color = new Color(1f, 0.96f, 0.88f);
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.8f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.2f);
    }

    private static void BuildStreet(Transform world)
    {
        var asphalt = WorldKit.Mat("Asfalto", new Color(0.18f, 0.18f, 0.2f));
        var sidewalk = WorldKit.Mat("Calcada", new Color(0.6f, 0.6f, 0.58f));
        var grass = WorldKit.Mat("Grama", new Color(0.3f, 0.45f, 0.25f));
        var stripe = WorldKit.Mat("Faixa", new Color(0.95f, 0.85f, 0.3f));

        var street = WorldKit.Empty("Rua Comercial", world, Vector3.zero).transform;
        WorldKit.Box("Terreno", street, new Vector3(0, -0.06f, 0), new Vector3(120, 0.1f, 80), grass);
        WorldKit.Box("Asfalto", street, new Vector3(0, -0.005f, 0), new Vector3(90, 0.02f, 7), asphalt);
        WorldKit.Box("Calcada Norte", street, new Vector3(0, 0.06f, 5), new Vector3(90, 0.12f, 3), sidewalk);
        WorldKit.Box("Calcada Sul", street, new Vector3(0, 0.06f, -5), new Vector3(90, 0.12f, 3), sidewalk);
        for (int i = -10; i <= 10; i++)
            WorldKit.Box("Faixa", street, new Vector3(i * 4f, 0.01f, 0), new Vector3(2f, 0.005f, 0.15f), stripe, false);

        // Postes de luz
        var pole = WorldKit.Mat("Poste", new Color(0.2f, 0.2f, 0.22f));
        for (int i = -4; i <= 4; i++)
        {
            foreach (float z in new[] { 6.2f, -6.2f })
            {
                var p = WorldKit.Primitive(PrimitiveType.Cylinder, "Poste", street, new Vector3(i * 10f + 5f, 2f, z),
                    new Vector3(0.12f, 2f, 0.12f), pole);
                WorldKit.PointLight(p.transform, new Vector3(0, 1.1f, 0), 10f, 0.6f, new Color(1f, 0.85f, 0.6f));
            }
        }

        // Limites invisíveis do mapa
        foreach (var (pos, size) in new[]
                 {
                     (new Vector3(0, 2, 30), new Vector3(100, 4, 1)), (new Vector3(0, 2, -30), new Vector3(100, 4, 1)),
                     (new Vector3(46, 2, 0), new Vector3(1, 4, 60)), (new Vector3(-46, 2, 0), new Vector3(1, 4, 60))
                 })
        {
            var wall = WorldKit.Box("Limite", street, pos, size, asphalt);
            Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
        }
    }

    private static void BuildBuildings(Layout layout)
    {
        var w = layout.World;
        const float north = 12.5f;
        const float south = -12.5f;

        layout.ElectronicsShop = WorldKit.Building("Loja Eletro Centro", w, new Vector3(-24, 0, north), 0f, 12, 12,
            new Color(0.72f, 0.78f, 0.86f), "ELETRO CENTRO", out _);
        WorldKit.Shelf("Prateleira 1", layout.ElectronicsShop, new Vector3(-4.5f, 0, 3.5f), 0f, new Color(0.15f, 0.15f, 0.18f));
        WorldKit.Shelf("Prateleira 2", layout.ElectronicsShop, new Vector3(4.5f, 0, 3.5f), 0f, new Color(0.2f, 0.4f, 0.8f));

        layout.AccessoriesShop = WorldKit.Building("Casa dos Acessorios", w, new Vector3(-8, 0, north), 0f, 12, 12,
            new Color(0.9f, 0.8f, 0.62f), "CASA DOS ACESSORIOS", out _);
        WorldKit.Shelf("Prateleira 1", layout.AccessoriesShop, new Vector3(-4.5f, 0, 3.5f), 0f, new Color(0.85f, 0.3f, 0.3f));
        WorldKit.Shelf("Prateleira 2", layout.AccessoriesShop, new Vector3(4.5f, 0, 3.5f), 0f, new Color(0.3f, 0.7f, 0.4f));

        layout.MentorOffice = WorldKit.Building("Escritorio do Mestre", w, new Vector3(8, 0, north), 0f, 10, 10,
            new Color(0.55f, 0.5f, 0.6f), "MESTRE NEGOCIADOR", out _);

        layout.Wholesaler = WorldKit.Building("Atacadista", w, new Vector3(25, 0, north), 0f, 14, 12,
            new Color(0.6f, 0.62f, 0.55f), "ATACADO BOM PRECO", out var wholesaleDoor);
        wholesaleDoor.Locked = true;
        wholesaleDoor.LockedMessage = "Atacadista: disponível a partir do nível 6";
        var gate = wholesaleDoor.gameObject.AddComponent<LevelGate>();
        gate.RequiredLevel = DefaultContent.WholesalerLevel;
        gate.PlaceName = "Atacadista";

        layout.Warehouse = WorldKit.Building("Deposito do Jogador", w, new Vector3(-16, 0, south), 180f, 12, 10,
            new Color(0.75f, 0.55f, 0.4f), "MEU DEPOSITO", out _);

        layout.Plaza = WorldKit.Empty("Praca Comercial", w, new Vector3(10, 0, -14)).transform;
        var paving = WorldKit.Mat("Piso Praca", new Color(0.7f, 0.66f, 0.6f));
        var bench = WorldKit.Mat("Banco", new Color(0.4f, 0.28f, 0.18f));
        var tree = WorldKit.Mat("Arvore", new Color(0.2f, 0.45f, 0.2f));
        var trunk = WorldKit.Mat("Tronco", new Color(0.35f, 0.25f, 0.15f));
        WorldKit.Box("Piso", layout.Plaza, new Vector3(0, 0.03f, 0), new Vector3(20, 0.06f, 14), paving);
        for (int i = 0; i < 4; i++)
        {
            float x = -7 + i * 4.7f;
            WorldKit.Box("Banco", layout.Plaza, new Vector3(x, 0.45f, 4), new Vector3(1.8f, 0.1f, 0.5f), bench);
            WorldKit.Primitive(PrimitiveType.Cylinder, "Tronco", layout.Plaza, new Vector3(x, 1f, -4), new Vector3(0.3f, 1f, 0.3f), trunk);
            WorldKit.Primitive(PrimitiveType.Sphere, "Copa", layout.Plaza, new Vector3(x, 2.6f, -4), new Vector3(2.2f, 2.2f, 2.2f), tree, false);
        }
        WorldKit.Sign("PRACA COMERCIAL", layout.Plaza, new Vector3(0, 2.4f, 6.5f), 180f, 0.08f, Color.white);
    }

    private static void BuildPlayer(Layout layout)
    {
        var player = new GameObject("Jogador");
        player.transform.position = new Vector3(-16, 0.1f, -4.5f);
        player.transform.rotation = Quaternion.Euler(0, 0, 0);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.35f;

        player.AddComponent<PlayerInputReader>();
        player.AddComponent<PlayerController>();
        player.AddComponent<InteractionController>();

        var camGo = new GameObject("Camera do Jogador");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0, 1.68f, 0);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.fieldOfView = 70f;
        camGo.AddComponent<AudioListener>();

        layout.Player = player;
    }

    private static void BuildSystems(Layout layout)
    {
        var s = layout.Systems.gameObject;
        s.AddComponent<WalletService>();
        s.AddComponent<GameBootstrap>();
        s.AddComponent<TouchControls>();
        s.AddComponent<GameHUDController>();
    }
}
