using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura o escritório com o POLYGON Office Pack: converte os materiais para URP,
/// gera folhas de pré-visualização e preenche a Lista de assets.
/// </summary>
public static class DealMasterOfficeSetup
{
    private const string Root = "Assets/PolygonOffice";
    private const string Furniture = Root + "/Prefabs/Props/Furniture/";
    private const string Desk = Root + "/Prefabs/Props/Desk Props/";
    private const string Kitchen = Root + "/Prefabs/Props/Kitchen Props/";
    private const string Misc = Root + "/Prefabs/Props/Misc/";
    private const string Wall = Root + "/Prefabs/Props/Wall Props/";
    private const string Chars = Root + "/Prefabs/Characters/";

    [MenuItem("DealMaster/Converter materiais do Office Pack para URP", priority = 30)]
    public static void ConvertMaterials()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var before = new Dictionary<string, int>();
        int converted = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            string shaderName = mat.shader != null ? mat.shader.name : "(null)";
            before[shaderName] = before.TryGetValue(shaderName, out var n) ? n + 1 : 1;
            if (shaderName.StartsWith("Universal Render Pipeline/")) continue;

            Texture main = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Texture normal = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            Texture emission = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

            mat.shader = lit;
            mat.SetTexture("_BaseMap", main);
            mat.SetColor("_BaseColor", color);
            if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
            if (emission != null || emissionColor.maxColorComponent > 0.01f)
            {
                mat.SetTexture("_EmissionMap", emission);
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(mat);
            converted++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Materiais convertidos: " + converted + " | antes: " +
                  string.Join(", ", before.Select(k => k.Key + "=" + k.Value)));
    }

    /// <summary>Batch: converte os materiais e gera a folha de candidatos em Logs/preview.png.</summary>
    public static void Preview()
    {
        ConvertMaterials();
        var names = new List<string>();
        foreach (var n in new[] { "01", "02", "03", "04", "05", "06" }) names.Add(Furniture + "SM_Prop_Desk_" + n);
        foreach (var n in new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11" }) names.Add(Furniture + "SM_Prop_Chair_" + n);
        names.Add(Furniture + "SM_Prop_Divider_01");
        names.Add(Furniture + "SM_Prop_Divider_Half_01");
        names.Add(Furniture + "SM_Prop_Couch_01");
        names.Add(Furniture + "SM_Prop_Couch_02");
        names.Add(Furniture + "SM_Prop_Table_Conference_01");
        names.Add(Furniture + "SM_Prop_Table_Conference_02");
        names.Add(Desk + "SM_Prop_Computer_Monitor_01");
        names.Add(Desk + "SM_Prop_Computer_Keyboard_01");
        names.Add(Desk + "SM_Prop_Headset_01");
        names.Add(Kitchen + "SM_Prop_CoffeeMachine_01");
        names.Add(Kitchen + "SM_Prop_Fridge_01");
        names.Add(Misc + "SM_Prop_WaterCooler_01");
        names.Add(Misc + "SM_Prop_Photocopier_01");
        names.Add(Misc + "SM_Prop_Plant_01");
        names.Add(Wall + "SM_Prop_TV_Wall_01");
        names.Add(Chars + "SM_Chr_Business_Male_01");
        names.Add(Chars + "SM_Chr_Business_Female_01");
        names.Add(Chars + "SM_Chr_Boss_Female_01");
        names.Add(Chars + "SM_Chr_Developer_Male_01");
        RenderSheet(names, "Logs/preview.png");
        EditorApplication.Exit(0);
    }

    private static GameObject Load(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab");
        if (prefab == null) Debug.LogError("[Setup] Prefab nao encontrado: " + path);
        return prefab;
    }

    private static void Slot(AssetSlot slot, GameObject prefab, float scale = 1f, float yaw = 0f, float height = 0f)
    {
        slot.prefab = prefab;
        slot.scale = scale;
        slot.yawOffset = yaw;
        slot.heightOffset = height;
    }

    private static GameObject CenteredPartition()
    {
        const string path = "Assets/_DealMaster/Prefabs/Divisoria_Centralizada.prefab";
        var root = new GameObject("Divisoria_Centralizada");
        var child = (GameObject)PrefabUtility.InstantiatePrefab(Load(Furniture + "SM_Prop_Divider_Half_01"), root.transform);
        child.transform.localPosition = new Vector3(-0.98f, 0f, 0.04f);
        child.transform.localScale = new Vector3(1.96f / 1.25f, 1.3f / 1.5f, 1f);
        var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return saved;
    }

    [MenuItem("DealMaster/Preencher lista com o Office Pack", priority = 2)]
    public static void FillAssetList()
    {
        DealMasterSceneBuilder.SelectAssetSet();
        var set = AssetDatabase.LoadAssetAtPath<OfficeAssetSet>(DealMasterSceneBuilder.AssetSetPath);

        Slot(set.desk, Load(Furniture + "SM_Prop_Desk_01"));
        Slot(set.officeChair, Load(Furniture + "SM_Prop_Chair_04"));
        Slot(set.monitor, Load(Desk + "SM_Prop_Computer_Monitor_01"), 0.9f, 0f, -0.02f);
        Slot(set.keyboard, Load(Desk + "SM_Prop_Computer_Keyboard_01"), 0.7f, 0f, -0.02f);
        Slot(set.headset, Load(Desk + "SM_Prop_Headset_01"), 0.7f, 0f, 0.07f);
        Slot(set.partition, CenteredPartition());

        Slot(set.coworker, Load(Chars + "SM_Chr_Business_Male_01"), 0.95f);
        // Mesma ordem dos nomes: Paula, Jefferson, Marta, Kleber, Simone, Diego, Rose, Wesley, Tati, Marcão, Lu, Fábio, Cris, Neide.
        set.coworkerVariants = new[]
        {
            "Business_Female_03", "Business_Male_01", "Business_Female_04", "Business_Male_02", "Developer_Female_01",
            "Business_Male_03", "Developer_Female_02", "Business_Male_04", "Business_Female_03", "Developer_Male_01",
            "Business_Female_04", "Developer_Male_02", "Developer_Female_01", "Developer_Female_02"
        }.Select(n => Load(Chars + "SM_Chr_" + n)).ToArray();
        Slot(set.supervisor, Load(Chars + "SM_Chr_Boss_Female_01"), 0.95f);
        Slot(set.receptionist, Load(Chars + "SM_Chr_Business_Female_02"), 0.95f);

        Slot(set.receptionDesk, Load(Furniture + "SM_Prop_Desk_06"), 1f, 180f);
        Slot(set.sofa, Load(Furniture + "SM_Prop_Couch_02"));
        Slot(set.plant, Load(Misc + "SM_Prop_Plant_01"));
        Slot(set.coffeeMachine, Load(Kitchen + "SM_Prop_CoffeeMachine_Small_01"));
        Slot(set.fridge, Load(Kitchen + "SM_Prop_Fridge_01"), 0.85f);
        Slot(set.meetingTable, Load(Furniture + "SM_Prop_Table_Conference_02"));
        Slot(set.breakTable, Load(Furniture + "SM_Prop_Table_Round_01"));
        Slot(set.chair, Load(Furniture + "SM_Prop_Chair_01"));
        Slot(set.waterCooler, Load(Misc + "SM_Prop_WaterCooler_01"));
        Slot(set.printer, Load(Misc + "SM_Prop_Photocopier_01"));
        Slot(set.tv, Load(Wall + "SM_Prop_TV_Wall_01"), 1.55f, 0f, 2.1f);

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Lista de assets preenchida.");
    }

    /// <summary>Batch: materiais, lista, configuração do projeto e cena.</summary>
    public static void SetupAll()
    {
        ConvertMaterials();
        FillAssetList();
        DealMasterSceneBuilder.Build();
        DealMasterBuild.ConfigureProject();
        Shots();
    }

    /// <summary>Batch: renderiza vistas de dentro do escritório em Logs/shots.png.</summary>
    public static void Shots()
    {
        var views = new (Vector3 pos, Vector3 look)[]
        {
            (new Vector3(0, 1.7f, -10), new Vector3(0, 1.5f, 0)),
            (new Vector3(1.5f, 1.7f, -1.78f), new Vector3(1.5f, 0.95f, -0.82f)),
            (new Vector3(-9, 2.8f, -6), new Vector3(3, 0.5f, 3)),
            (new Vector3(-4.5f, 1.7f, 1), new Vector3(-4.5f, 2.1f, 6.85f)),
            (new Vector3(11, 1.7f, -5.5f), new Vector3(14, 1, 0)),
            (new Vector3(-11, 1.7f, 4), new Vector3(-12.5f, 1, 8)),
            (new Vector3(2, 2.3f, 7.5f), new Vector3(6, 0.7f, 10)),
            (new Vector3(0, 1.7f, -8), new Vector3(-5, 1, -9.5f)),
            (new Vector3(-2.9f, 1.4f, -6.6f), new Vector3(-1.5f, 0.9f, -5.3f)),
        };
        const int w = 600, h = 338, cols = 3;
        EditorSceneManager.OpenScene(DealMasterSceneBuilder.ScenePath, OpenSceneMode.Single);
        int rows = Mathf.CeilToInt(views.Length / (float)cols);
        var sheet = new Texture2D(w * cols, h * rows, TextureFormat.RGB24, false);
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.enabled = false;
        var camGo = new GameObject("CameraFoto");
        var cam = camGo.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 80f;
        for (int i = 0; i < views.Length; i++)
        {
            camGo.transform.position = views[i].pos;
            camGo.transform.LookAt(views[i].look);
            cam.Render();
            RenderTexture.active = rt;
            sheet.ReadPixels(new Rect(0, 0, w, h), (i % cols) * w, (rows - 1 - i / cols) * h);
            RenderTexture.active = null;
        }
        sheet.Apply();
        File.WriteAllBytes("Logs/shots.png", sheet.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(camGo);
        Debug.Log("[Setup] Vistas salvas.");
        var missing = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .SelectMany(r => r.sharedMaterials).Count(m => m == null);
        Debug.Log("[Setup] Materiais vazios na cena: " + missing);
        EditorApplication.Exit(0);
    }

    /// <summary>Batch: imprime no log a hierarquia de ossos de um personagem.</summary>
    public static void DumpBones()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Chars + "SM_Chr_Business_Male_01.prefab");
        var lines = new List<string>();
        void Walk(Transform t, int depth)
        {
            lines.Add(new string(' ', depth * 2) + t.name + " pos=" + t.localPosition.ToString("F3") + " rot=" + t.localEulerAngles.ToString("F0"));
            if (depth < 9) foreach (Transform c in t) Walk(c, depth + 1);
        }
        Walk(prefab.transform, 0);
        var anim = prefab.GetComponent<Animator>();
        Debug.Log("[Bones] animator=" + (anim != null) + " avatar=" + (anim != null && anim.avatar != null) + " human=" + (anim != null && anim.isHuman) + "\n" + string.Join("\n", lines));
        EditorApplication.Exit(0);
    }

    private static void RenderSheet(List<string> paths, string output)
    {
        const int tile = 256, cols = 7;
        int rows = Mathf.CeilToInt(paths.Count / (float)cols);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sheet = new Texture2D(tile * cols, tile * rows, TextureFormat.RGB24, false);
        var rt = new RenderTexture(tile, tile, 24, RenderTextureFormat.ARGB32);
        var lightGo = new GameObject("Luz");
        lightGo.AddComponent<Light>().type = LightType.Directional;
        lightGo.transform.rotation = Quaternion.Euler(45f, 200f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);

        var camGo = new GameObject("Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.backgroundColor = new Color(0.45f, 0.5f, 0.55f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.targetTexture = rt;

        for (int i = 0; i < paths.Count; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i] + ".prefab");
            if (prefab == null) { Debug.LogWarning("[Setup] Nao achei " + paths[i]); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var bounds = MeasureAtOrigin(go);
            Debug.Log($"[Setup] #{i} {Path.GetFileName(paths[i])} tam={bounds.size:F2} min={bounds.min:F2} colisor={go.GetComponentInChildren<Collider>() != null} skinned={go.GetComponentInChildren<SkinnedMeshRenderer>() != null}");

            // Câmera do lado +Z: se a frente do modelo aparecer, a frente aponta para +Z.
            float radius = Mathf.Max(0.05f, bounds.extents.magnitude);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = radius * 10f;
            camGo.transform.position = bounds.center + new Vector3(0.45f, 0.35f, 1f).normalized * radius * 2.7f;
            camGo.transform.LookAt(bounds.center);
            cam.Render();

            RenderTexture.active = rt;
            sheet.ReadPixels(new Rect(0, 0, tile, tile), (i % cols) * tile, (rows - 1 - i / cols) * tile);
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(go);
        }
        sheet.Apply();
        File.WriteAllBytes(output, sheet.EncodeToPNG());
        Debug.Log("[Setup] Folha salva em " + output);
    }

    private static Bounds MeasureAtOrigin(GameObject go)
    {
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 0.1f);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
