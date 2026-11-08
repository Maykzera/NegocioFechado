using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Constrói a cena do escritório da central de vendas.
/// Menu: DealMaster > Construir escritório. Pode ser executado de novo a qualquer momento.
/// Usa os modelos da "Lista de assets do escritório" quando preenchidos; senão, formas simples.
/// </summary>
public static class DealMasterSceneBuilder
{
    public const string ScenePath = "Assets/_DealMaster/Scenes/CallCenter.unity";
    public const string AssetSetPath = "Assets/_DealMaster/Settings/OfficeAssets.asset";
    private const float H = 3.2f;

    private static OfficeAssetSet assets;
    private static Transform world;

    [MenuItem("DealMaster/Construir escritório", priority = 0)]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        WorldKit.ClearCache();
        assets = LoadOrCreateAssetSet();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        world = new GameObject("Escritório").transform;
        var systems = new GameObject("Sistemas");

        Lighting();
        Shell();
        Reception();
        var breakPoint = BreakRoom();
        CallFloor(breakPoint);
        SupervisorOffice();
        MeetingRoom();
        Restrooms();
        var player = Player();
        Systems(systems);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;
        Debug.Log("[DealMaster] Escritório criado em " + ScenePath + ". Aperte Play.");
    }

    [MenuItem("DealMaster/Lista de assets do escritório", priority = 1)]
    public static void SelectAssetSet()
    {
        Selection.activeObject = LoadOrCreateAssetSet();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    private static OfficeAssetSet LoadOrCreateAssetSet()
    {
        var set = AssetDatabase.LoadAssetAtPath<OfficeAssetSet>(AssetSetPath);
        if (set != null) return set;
        Directory.CreateDirectory(Path.GetDirectoryName(AssetSetPath));
        set = ScriptableObject.CreateInstance<OfficeAssetSet>();
        AssetDatabase.CreateAsset(set, AssetSetPath);
        AssetDatabase.SaveAssets();
        return set;
    }

    // ------------------------------------------------------------------ ambientes

    private static void Lighting()
    {
        var sunGo = new GameObject("Sol (janelas)");
        sunGo.transform.SetParent(world, false);
        sunGo.transform.rotation = Quaternion.Euler(45f, -40f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.7f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.68f);

        var lamps = WorldKit.Empty("Luminárias", world, Vector3.zero).transform;
        var panel = WorldKit.Mat("Luminaria", new Color(0.97f, 0.97f, 0.95f), 0.9f);
        for (int x = -2; x <= 2; x++)
            for (int z = -1; z <= 1; z++)
            {
                var p = new Vector3(x * 6f, H - 0.03f, z * 7f);
                WorldKit.Box("Painel de luz", lamps, p, new Vector3(1.2f, 0.04f, 0.6f), panel, false);
                if ((x + z) % 2 == 0) WorldKit.PointLight(lamps, p - Vector3.up * 0.2f, 9f, 0.9f, new Color(1f, 0.98f, 0.92f));
            }
    }

    private static void Shell()
    {
        var shell = WorldKit.Empty("Prédio", world, Vector3.zero).transform;
        var carpet = WorldKit.Mat("Carpete", new Color(0.32f, 0.36f, 0.42f));
        var wall = WorldKit.Mat("Parede", new Color(0.88f, 0.87f, 0.84f));
        var ceiling = WorldKit.Mat("Forro", new Color(0.93f, 0.93f, 0.92f));
        var glass = WorldKit.Mat("Janela", new Color(0.6f, 0.75f, 0.85f), 0.95f);
        var sidewalk = WorldKit.Mat("Calcada", new Color(0.6f, 0.6f, 0.58f));

        WorldKit.Box("Piso", shell, new Vector3(0, -0.05f, 0), new Vector3(30, 0.1f, 24), carpet);
        WorldKit.Box("Forro", shell, new Vector3(0, H + 0.05f, 0), new Vector3(30, 0.1f, 24), ceiling, false);
        WorldKit.Box("Calçada", shell, new Vector3(0, -0.06f, -15), new Vector3(30, 0.1f, 6), sidewalk);

        // Paredes externas com faixa de janelas
        foreach (var (pos, size) in new[]
                 {
                     (new Vector3(0, 0, 12), new Vector3(30, 0, 0.2f)),
                     (new Vector3(-15, 0, 0), new Vector3(0.2f, 0, 24)),
                     (new Vector3(15, 0, 0), new Vector3(0.2f, 0, 24)),
                 })
        {
            WorldKit.Box("Parede", shell, pos + Vector3.up * 0.5f, size + Vector3.up * 1f, wall);
            WorldKit.Box("Janela", shell, pos + Vector3.up * 1.7f, size + Vector3.up * 1.4f, glass);
            WorldKit.Box("Parede", shell, pos + Vector3.up * 2.8f, size + Vector3.up * 0.8f, wall);
        }
        // Frente com a entrada (vidro)
        WorldKit.Box("Fachada", shell, new Vector3(-8.25f, H / 2, -12), new Vector3(13.5f, H, 0.2f), glass);
        WorldKit.Box("Fachada", shell, new Vector3(8.25f, H / 2, -12), new Vector3(13.5f, H, 0.2f), glass);
        WorldKit.Box("Fachada topo", shell, new Vector3(0, H - 0.4f, -12), new Vector3(3f, 0.8f, 0.2f), wall);
        WorldKit.Sign("CONECTA VENDAS", shell, new Vector3(0, H - 0.4f, -12.15f), 180f, 0.09f, new Color(0.15f, 0.3f, 0.6f));
    }

    private static void Reception()
    {
        var r = WorldKit.Empty("Recepção", world, Vector3.zero).transform;
        var glass = WorldKit.Mat("Divisória de vidro", new Color(0.7f, 0.82f, 0.9f), 0.95f);
        // Divisória entre recepção e salão, com passagem no meio
        WorldKit.Box("Divisória", r, new Vector3(-8.5f, H / 2, -7f), new Vector3(13f, H, 0.08f), glass);
        WorldKit.Box("Divisória", r, new Vector3(8.5f, H / 2, -7f), new Vector3(13f, H, 0.08f), glass);

        Place(assets.receptionDesk, r, new Vector3(-5f, 0, -9.5f), 0f, p =>
        {
            WorldKit.Box("Balcão", p, new Vector3(0, 0.55f, 0), new Vector3(3f, 1.1f, 0.7f), WorldKit.Mat("Balcão recepção", new Color(0.25f, 0.3f, 0.4f)));
            WorldKit.Box("Tampo", p, new Vector3(0, 1.12f, 0), new Vector3(3.1f, 0.05f, 0.8f), WorldKit.Mat("Tampo branco", new Color(0.95f, 0.95f, 0.95f), 0.6f));
        });
        var receptionist = Person(assets.receptionist, r, new Vector3(-5f, 0, -8.7f), 180f, new Color(0.6f, 0.3f, 0.5f), "Recepcionista Bia");
        AddCoworker(receptionist, null);

        Place(assets.sofa, r, new Vector3(5f, 0, -10.8f), 0f, SofaFallback);
        Place(assets.plant, r, new Vector3(7.2f, 0, -10.8f), 0f, PlantFallback);
        Place(assets.plant, r, new Vector3(-8f, 0, -8f), 0f, PlantFallback);
        WorldKit.Sign("BEM-VINDO(A) À CONECTA VENDAS", r, new Vector3(0, 2.6f, -7.1f), 180f, 0.05f, new Color(0.15f, 0.3f, 0.6f));
    }

    private static void CallFloor(Transform breakPoint)
    {
        var floor = WorldKit.Empty("Salão de atendimento", world, Vector3.zero).transform;
        float[] xs = { -7.5f, -4.5f, -1.5f, 1.5f, 4.5f, 7.5f };
        float[] zs = { -4.5f, -1f, 2.5f };
        int index = 0;
        for (int row = 0; row < zs.Length; row++)
            for (int col = 0; col < xs.Length; col++)
            {
                bool isPlayer = row == 1 && col == 3;
                var desk = Workstation(floor, new Vector3(xs[col], 0, zs[row]), isPlayer, out var seat, out var monitor);
                if (isPlayer)
                {
                    var pd = desk.gameObject.AddComponent<PlayerDesk>();
                    pd.SeatPoint = seat;
                    pd.Monitor = monitor;
                    WorldKit.Sign("SUA MESA", desk, new Vector3(0, 1.55f, 0.38f), 180f, 0.04f, new Color(1f, 0.85f, 0.3f));
                }
                else if (index++ % 4 != 3)
                {
                    var p = Person(assets.coworker, floor, seat.position, 0f, Color.HSVToRGB((index * 0.137f) % 1f, 0.45f, 0.7f), CoworkerName(index));
                    AddCoworker(p, index % 3 == 0 ? breakPoint : null);
                }
            }

        // Quadro de ranking (TV) virado para o salão
        var tvRoot = Place(assets.tv, floor, new Vector3(-4.5f, 0, 6.85f), 180f, p =>
        {
            WorldKit.Box("TV", p, new Vector3(0, 2.1f, 0), new Vector3(2.4f, 1.4f, 0.08f), WorldKit.Mat("Tela", new Color(0.05f, 0.07f, 0.1f), 0.9f), false);
        });
        var board = WorldKit.Sign("RANKING", tvRoot.transform, new Vector3(0, 2.1f, -0.06f), 0f, 0.03f, new Color(0.6f, 1f, 0.7f));
        board.anchor = TextAnchor.MiddleCenter;
        board.alignment = TextAlignment.Left;
        board.gameObject.AddComponent<RankingBoard>().Text = board;

        Place(assets.waterCooler, floor, new Vector3(-8.5f, 0, 6.3f), 180f, p =>
        {
            WorldKit.Box("Base", p, new Vector3(0, 0.5f, 0), new Vector3(0.4f, 1f, 0.4f), WorldKit.Mat("Bebedouro", new Color(0.85f, 0.85f, 0.88f)));
            WorldKit.Primitive(PrimitiveType.Cylinder, "Galão", p, new Vector3(0, 1.25f, 0), new Vector3(0.3f, 0.25f, 0.3f), WorldKit.Mat("Água", new Color(0.5f, 0.75f, 0.95f), 0.9f), false);
        });
        Place(assets.printer, floor, new Vector3(-1f, 0, 6.3f), 180f, p =>
        {
            WorldKit.Box("Móvel", p, new Vector3(0, 0.4f, 0), new Vector3(0.9f, 0.8f, 0.6f), WorldKit.Mat("Móvel cinza", new Color(0.5f, 0.5f, 0.52f)));
            WorldKit.Box("Impressora", p, new Vector3(0, 0.95f, 0), new Vector3(0.6f, 0.3f, 0.5f), WorldKit.Mat("Impressora", new Color(0.9f, 0.9f, 0.9f)), false);
        });
    }

    private static Transform Workstation(Transform parent, Vector3 pos, bool highlight, out Transform seat, out Transform monitorPoint)
    {
        var root = WorldKit.Empty(highlight ? "Mesa do jogador" : "Estação", parent, pos).transform;
        Place(assets.desk, root, Vector3.zero, 0f, p =>
        {
            WorldKit.Box("Tampo", p, new Vector3(0, 0.74f, 0), new Vector3(1.5f, 0.05f, 0.75f), WorldKit.Mat(highlight ? "Mesa destaque" : "Mesa", highlight ? new Color(0.7f, 0.55f, 0.3f) : new Color(0.75f, 0.72f, 0.66f)));
            WorldKit.Box("Pé", p, new Vector3(-0.7f, 0.37f, 0), new Vector3(0.05f, 0.74f, 0.7f), WorldKit.Mat("Metal", new Color(0.3f, 0.3f, 0.32f)));
            WorldKit.Box("Pé", p, new Vector3(0.7f, 0.37f, 0), new Vector3(0.05f, 0.74f, 0.7f), WorldKit.Mat("Metal", new Color(0.3f, 0.3f, 0.32f)));
        });
        Place(assets.partition, root, new Vector3(0, 0, 0.42f), 0f, p =>
        {
            var fabric = WorldKit.Mat("Baia", new Color(0.35f, 0.45f, 0.55f));
            WorldKit.Box("Divisória fundo", p, new Vector3(0, 0.65f, 0), new Vector3(1.55f, 1.3f, 0.05f), fabric);
            WorldKit.Box("Divisória lado", p, new Vector3(0.77f, 0.55f, -0.4f), new Vector3(0.05f, 1.1f, 0.8f), fabric);
        });
        var mon = Place(assets.monitor, root, new Vector3(0, 0.77f, 0.18f), 180f, p =>
        {
            WorldKit.Box("Tela", p, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.36f, 0.04f), WorldKit.Mat("Monitor", new Color(0.08f, 0.08f, 0.1f), 0.8f), false);
            WorldKit.Box("Imagem", p, new Vector3(0, 0.3f, 0.025f), new Vector3(0.55f, 0.31f, 0.005f), WorldKit.Mat(highlight ? "Tela ligada" : "Tela azul", highlight ? new Color(0.35f, 0.65f, 0.95f) : new Color(0.25f, 0.4f, 0.6f), 0.9f), false);
            WorldKit.Box("Base", p, new Vector3(0, 0.06f, 0), new Vector3(0.18f, 0.12f, 0.12f), WorldKit.Mat("Monitor", new Color(0.08f, 0.08f, 0.1f)), false);
        });
        Place(assets.keyboard, root, new Vector3(0, 0.77f, -0.12f), 0f, p =>
            WorldKit.Box("Teclado", p, new Vector3(0, 0.015f, 0), new Vector3(0.45f, 0.03f, 0.15f), WorldKit.Mat("Teclado", new Color(0.15f, 0.15f, 0.17f)), false));
        Place(assets.headset, root, new Vector3(0.5f, 0.77f, 0.05f), 0f, p =>
            WorldKit.Primitive(PrimitiveType.Sphere, "Headset", p, new Vector3(0, 0.06f, 0), new Vector3(0.18f, 0.12f, 0.18f), WorldKit.Mat("Headset", new Color(0.1f, 0.1f, 0.1f)), false));
        Place(assets.officeChair, root, new Vector3(0, 0, -0.65f), 0f, p =>
        {
            var black = WorldKit.Mat("Cadeira", new Color(0.12f, 0.12f, 0.14f));
            WorldKit.Box("Assento", p, new Vector3(0, 0.48f, 0), new Vector3(0.5f, 0.08f, 0.5f), black, false);
            WorldKit.Box("Encosto", p, new Vector3(0, 0.85f, -0.23f), new Vector3(0.48f, 0.6f, 0.06f), black, false);
            WorldKit.Primitive(PrimitiveType.Cylinder, "Coluna", p, new Vector3(0, 0.24f, 0), new Vector3(0.06f, 0.24f, 0.06f), black, false);
        });

        seat = WorldKit.Empty("Assento", root, new Vector3(0, 0, -0.78f)).transform;
        monitorPoint = mon.transform;
        return root;
    }

    private static Transform BreakRoom()
    {
        var c = WorldKit.Empty("Copa", world, Vector3.zero).transform;
        var tile = WorldKit.Mat("Piso copa", new Color(0.78f, 0.76f, 0.7f));
        WorldKit.Box("Piso", c, new Vector3(12.5f, -0.04f, -0.5f), new Vector3(5f, 0.02f, 11f), tile, false);
        WorldKit.Box("Meia parede", c, new Vector3(10f, 0.55f, -3f), new Vector3(0.15f, 1.1f, 6f), WorldKit.Mat("Parede", new Color(0.88f, 0.87f, 0.84f)));
        WorldKit.Sign("COPA", c, new Vector3(10f, 2.4f, -1f), 270f, 0.06f, new Color(0.2f, 0.2f, 0.25f));

        var counter = WorldKit.Mat("Bancada", new Color(0.4f, 0.32f, 0.25f));
        WorldKit.Box("Bancada", c, new Vector3(14.4f, 0.45f, -2.5f), new Vector3(0.9f, 0.9f, 5f), counter);
        var machine = Place(assets.coffeeMachine, c, new Vector3(14.4f, 0.9f, -3.5f), 270f, p =>
        {
            WorldKit.Box("Cafeteira", p, new Vector3(0, 0.25f, 0), new Vector3(0.4f, 0.5f, 0.35f), WorldKit.Mat("Cafeteira", new Color(0.15f, 0.12f, 0.1f)), false);
            WorldKit.Primitive(PrimitiveType.Cylinder, "Xícara", p, new Vector3(0.12f, 0.05f, -0.1f), new Vector3(0.08f, 0.05f, 0.08f), WorldKit.Mat("Xícara", Color.white), false);
        });
        var hit = WorldKit.Box("Área da cafeteira", machine.transform, new Vector3(0, 0.3f, 0), new Vector3(0.8f, 0.8f, 0.8f), WorldKit.Mat("Invisivel", Color.clear), true, false);
        UnityEngine.Object.DestroyImmediate(hit.GetComponent<MeshRenderer>());
        hit.GetComponent<BoxCollider>().isTrigger = true;
        hit.AddComponent<CoffeeMachine>();
        WorldKit.Sign("CAFÉ ☕", c, new Vector3(14.9f, 1.9f, -3.5f), 270f, 0.04f, new Color(0.3f, 0.2f, 0.1f));

        Place(assets.fridge, c, new Vector3(14.4f, 0, 0.6f), 270f, p =>
            WorldKit.Box("Geladeira", p, new Vector3(0, 0.9f, 0), new Vector3(0.75f, 1.8f, 0.7f), WorldKit.Mat("Geladeira", new Color(0.92f, 0.92f, 0.94f), 0.7f)));
        Place(assets.meetingTable, c, new Vector3(12.2f, 0, 2.5f), 0f, p =>
            WorldKit.Primitive(PrimitiveType.Cylinder, "Mesa redonda", p, new Vector3(0, 0.72f, 0), new Vector3(1.1f, 0.03f, 1.1f), WorldKit.Mat("Mesa copa", new Color(0.85f, 0.83f, 0.78f))));
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            Place(assets.chair, c, new Vector3(12.2f + Mathf.Sin(a) * 0.85f, 0, 2.5f + Mathf.Cos(a) * 0.85f), i * 120f + 180f, ChairFallback);
        }
        Place(assets.sofa, c, new Vector3(12.5f, 0, -5.4f), 0f, SofaFallback);
        return WorldKit.Empty("Ponto da copa", c, new Vector3(13.2f, 0, -2f)).transform;
    }

    private static void SupervisorOffice()
    {
        var o = WorldKit.Empty("Sala da supervisora", world, Vector3.zero).transform;
        var glass = WorldKit.Mat("Divisória de vidro", new Color(0.7f, 0.82f, 0.9f), 0.95f);
        var frame = WorldKit.Mat("Caixilho", new Color(0.2f, 0.2f, 0.22f));
        WorldKit.Box("Vidro", o, new Vector3(-10f, H / 2, 2.5f), new Vector3(0.08f, H, 3f), glass);
        WorldKit.Box("Vidro", o, new Vector3(-10f, H / 2, 9.2f), new Vector3(0.08f, H, 5.6f), glass);
        WorldKit.Box("Vidro topo", o, new Vector3(-10f, H - 0.4f, 5f), new Vector3(0.08f, 0.8f, 1.6f), glass, false);
        WorldKit.Box("Parede", o, new Vector3(-12.5f, H / 2, 1f), new Vector3(5f, H, 0.12f), frame);
        WorldKit.Sign("SUPERVISÃO · Regina", o, new Vector3(-9.95f, 2.75f, 5f), 90f, 0.035f, new Color(0.15f, 0.3f, 0.6f));

        var desk = Place(assets.desk, o, new Vector3(-12.5f, 0, 7.5f), 180f, p =>
        {
            WorldKit.Box("Tampo", p, new Vector3(0, 0.76f, 0), new Vector3(2f, 0.06f, 0.9f), WorldKit.Mat("Mesa chefe", new Color(0.35f, 0.24f, 0.15f), 0.5f));
            WorldKit.Box("Base", p, new Vector3(0, 0.37f, -0.3f), new Vector3(1.9f, 0.74f, 0.3f), WorldKit.Mat("Mesa chefe", new Color(0.35f, 0.24f, 0.15f), 0.5f));
        });
        Place(assets.monitor, o, new Vector3(-12.5f, 0.79f, 7.8f), 0f, p =>
            WorldKit.Box("Tela", p, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.36f, 0.04f), WorldKit.Mat("Monitor", new Color(0.08f, 0.08f, 0.1f), 0.8f), false));
        Person(assets.supervisor, o, new Vector3(-12.5f, 0, 8.4f), 180f, new Color(0.15f, 0.18f, 0.3f), "Supervisora Regina");
        var hit = WorldKit.Box("Atendimento da supervisora", desk.transform, new Vector3(0, 1f, 0), new Vector3(2.4f, 2f, 1.6f), WorldKit.Mat("Invisivel", Color.clear), true, false);
        UnityEngine.Object.DestroyImmediate(hit.GetComponent<MeshRenderer>());
        hit.GetComponent<BoxCollider>().isTrigger = true;
        hit.AddComponent<SupervisorDesk>();
        for (int i = 0; i < 2; i++) Place(assets.chair, o, new Vector3(-13f + i, 0, 6.3f), 0f, ChairFallback);
        Place(assets.plant, o, new Vector3(-14.4f, 0, 11.4f), 0f, PlantFallback);
    }

    private static void MeetingRoom()
    {
        var m = WorldKit.Empty("Sala de reunião", world, Vector3.zero).transform;
        var glass = WorldKit.Mat("Divisória de vidro", new Color(0.7f, 0.82f, 0.9f), 0.95f);
        WorldKit.Box("Vidro", m, new Vector3(2.5f, H / 2, 7f), new Vector3(3f, H, 0.08f), glass);
        WorldKit.Box("Vidro", m, new Vector3(7.7f, H / 2, 7f), new Vector3(4.6f, H, 0.08f), glass);
        WorldKit.Box("Vidro", m, new Vector3(1f, H / 2, 9.5f), new Vector3(0.08f, H, 5f), glass);
        WorldKit.Box("Vidro", m, new Vector3(10f, H / 2, 9.5f), new Vector3(0.08f, H, 5f), glass);
        Place(assets.meetingTable, m, new Vector3(5.5f, 0, 9.6f), 0f, p =>
            WorldKit.Box("Mesa de reunião", p, new Vector3(0, 0.74f, 0), new Vector3(4f, 0.06f, 1.4f), WorldKit.Mat("Mesa reunião", new Color(0.3f, 0.22f, 0.16f), 0.5f)));
        for (int i = 0; i < 4; i++)
        {
            Place(assets.chair, m, new Vector3(4f + i, 0, 8.5f), 0f, ChairFallback);
            Place(assets.chair, m, new Vector3(4f + i, 0, 10.7f), 180f, ChairFallback);
        }
        WorldKit.Sign("REUNIÃO", m, new Vector3(5.5f, 2.75f, 6.95f), 180f, 0.04f, new Color(0.15f, 0.3f, 0.6f));
    }

    private static void Restrooms()
    {
        var b = WorldKit.Empty("Banheiros", world, Vector3.zero).transform;
        var wall = WorldKit.Mat("Parede", new Color(0.88f, 0.87f, 0.84f));
        WorldKit.Box("Parede", b, new Vector3(-10f, H / 2, -6.5f), new Vector3(0.15f, H, 1f), wall);
        WorldKit.Box("Parede", b, new Vector3(-10f, H / 2, -3f), new Vector3(0.15f, H, 2.6f), wall);
        WorldKit.Box("Parede", b, new Vector3(-10f, H / 2, 0.3f), new Vector3(0.15f, H, 1.6f), wall);
        WorldKit.Box("Parede", b, new Vector3(-12.5f, H / 2, 1f), new Vector3(5f, H, 0.12f), wall);
        foreach (var (z, label) in new[] { (-5f, "FEMININO"), (-1f, "MASCULINO") })
        {
            var hinge = WorldKit.Empty("Porta " + label, b, new Vector3(-10f, 0, z - 0.6f), 90f);
            WorldKit.Box("Folha", hinge.transform, new Vector3(0.6f, 1.1f, 0), new Vector3(1.2f, 2.2f, 0.05f), WorldKit.Mat("Porta banheiro", new Color(0.5f, 0.55f, 0.6f)), true, false);
            var door = hinge.AddComponent<Door>();
            door.Locked = true;
            door.LockedMessage = "Ocupado. Melhor voltar para as ligações!";
            WorldKit.Sign(label, b, new Vector3(-9.9f, 2.45f, z), 90f, 0.03f, new Color(0.2f, 0.2f, 0.25f));
        }
    }

    // ------------------------------------------------------------------ jogador e sistemas

    private static GameObject Player()
    {
        var player = new GameObject("Jogador");
        player.transform.position = new Vector3(0, 0.05f, -10f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.3f;
        player.AddComponent<PlayerInputReader>();
        player.AddComponent<PlayerController>();
        player.AddComponent<InteractionController>();
        var camGo = new GameObject("Câmera do jogador");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0, 1.65f, 0);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.fieldOfView = 70f;
        camGo.AddComponent<AudioListener>();
        return player;
    }

    private static void Systems(GameObject s)
    {
        s.AddComponent<WalletService>();
        s.AddComponent<GameBootstrap>();
        s.AddComponent<TouchControls>();
        s.AddComponent<GameHUDController>();
        s.AddComponent<CallCenterDirector>();
        s.AddComponent<CallAudio>();
        s.AddComponent<PlatformQuality>();
        s.AddComponent<SaveGameBridge>();
        s.AddComponent<StartMenu>();
    }

    // ------------------------------------------------------------------ peças

    private static readonly string[] CoworkerNames = { "Paula", "Jefferson", "Marta", "Kleber", "Simone", "Diego", "Rose", "Wesley", "Tati", "Marcão", "Lu", "Fábio", "Cris", "Neide" };

    private static string CoworkerName(int i) => CoworkerNames[i % CoworkerNames.Length];

    private static GameObject Person(AssetSlot slot, Transform parent, Vector3 pos, float yaw, Color shirt, string name)
    {
        var root = WorldKit.Empty(name, parent, pos, yaw);
        Transform body;
        if (slot != null && slot.prefab != null)
        {
            body = Place(slot, root.transform, Vector3.zero, 0f, null).transform;
        }
        else
        {
            body = WorldKit.Empty("Corpo", root.transform, Vector3.zero).transform;
            var skin = WorldKit.Mat("Pele " + ((int)(shirt.r * 10)) % 3, new[] { new Color(0.85f, 0.67f, 0.52f), new Color(0.6f, 0.42f, 0.3f), new Color(0.4f, 0.27f, 0.18f) }[((int)(shirt.r * 10)) % 3]);
            var shirtMat = WorldKit.Mat("Camisa_" + ColorUtility.ToHtmlStringRGB(shirt), shirt);
            WorldKit.Primitive(PrimitiveType.Capsule, "Tronco", body, new Vector3(0, 0.95f, 0), new Vector3(0.5f, 0.6f, 0.35f), shirtMat);
            WorldKit.Primitive(PrimitiveType.Sphere, "Cabeça", body, new Vector3(0, 1.62f, 0), new Vector3(0.28f, 0.3f, 0.28f), skin, false);
            WorldKit.Primitive(PrimitiveType.Sphere, "Headset", body, new Vector3(0.14f, 1.64f, 0), new Vector3(0.06f, 0.1f, 0.1f), WorldKit.Mat("Headset", new Color(0.1f, 0.1f, 0.1f)), false);
        }
        var label = WorldKit.Sign(name, root.transform, new Vector3(0, 2.05f, 0), 0f, 0.025f, Color.white);
        label.gameObject.AddComponent<FaceCameraLabel>();
        return root;
    }

    private static void AddCoworker(GameObject person, Transform breakPoint)
    {
        var bubble = WorldKit.Sign("", person.transform, new Vector3(0, 2.35f, 0), 0f, 0.022f, new Color(1f, 0.95f, 0.7f));
        bubble.gameObject.AddComponent<FaceCameraLabel>();
        var c = person.AddComponent<Coworker>();
        c.Body = person.transform.Find("Corpo") ?? person.transform.GetChild(0);
        c.Bubble = bubble;
        c.BreakPoint = breakPoint;
    }

    /// <summary>Coloca o modelo do slot; se estiver vazio, monta a forma simples.</summary>
    private static GameObject Place(AssetSlot slot, Transform parent, Vector3 localPos, float yaw, Action<Transform> fallback)
    {
        if (slot != null && slot.prefab != null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(slot.prefab, parent);
            go.transform.localPosition = localPos + Vector3.up * slot.heightOffset;
            go.transform.localRotation = Quaternion.Euler(0f, yaw + slot.yawOffset, 0f);
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, slot.scale);
            EnsureCollider(go);
            return go;
        }
        var root = WorldKit.Empty(slot?.prefab == null ? "Peça" : slot.prefab.name, parent, localPos, yaw);
        fallback?.Invoke(root.transform);
        return root;
    }

    private static void EnsureCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        var box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        var s = go.transform.lossyScale;
        box.size = new Vector3(b.size.x / Mathf.Max(0.0001f, s.x), b.size.y / Mathf.Max(0.0001f, s.y), b.size.z / Mathf.Max(0.0001f, s.z));
    }

    private static void SofaFallback(Transform p)
    {
        var fabric = WorldKit.Mat("Sofá", new Color(0.3f, 0.4f, 0.5f));
        WorldKit.Box("Assento", p, new Vector3(0, 0.25f, 0), new Vector3(2f, 0.5f, 0.8f), fabric);
        WorldKit.Box("Encosto", p, new Vector3(0, 0.65f, -0.3f), new Vector3(2f, 0.6f, 0.2f), fabric, false);
    }

    private static void PlantFallback(Transform p)
    {
        WorldKit.Primitive(PrimitiveType.Cylinder, "Vaso", p, new Vector3(0, 0.25f, 0), new Vector3(0.45f, 0.25f, 0.45f), WorldKit.Mat("Vaso", new Color(0.55f, 0.35f, 0.25f)));
        WorldKit.Primitive(PrimitiveType.Sphere, "Folhas", p, new Vector3(0, 0.95f, 0), new Vector3(0.8f, 0.9f, 0.8f), WorldKit.Mat("Folhas", new Color(0.2f, 0.5f, 0.25f)), false);
    }

    private static void ChairFallback(Transform p)
    {
        var mat = WorldKit.Mat("Cadeira simples", new Color(0.25f, 0.25f, 0.3f));
        WorldKit.Box("Assento", p, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.06f, 0.45f), mat, false);
        WorldKit.Box("Encosto", p, new Vector3(0, 0.75f, -0.2f), new Vector3(0.45f, 0.55f, 0.05f), mat, false);
    }
}
