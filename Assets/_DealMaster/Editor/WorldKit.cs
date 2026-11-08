using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Peças básicas para montar o mundo com placeholders 3D: caixas, materiais, placas, prédios e portas.
/// Tudo é gerado por código para o projeto funcionar sem assets externos.
/// Depois é só trocar os placeholders por modelos definitivos.
/// </summary>
public static class WorldKit
{
    public const string MaterialFolder = "Assets/_DealMaster/Art/Materials/Generated";

    private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    public static void ClearCache() => cache.Clear();

    public static Material Mat(string name, Color color, float smoothness = 0.2f)
    {
        if (cache.TryGetValue(name, out var cached) && cached != null) return cached;

        Directory.CreateDirectory(MaterialFolder);
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(mat);
        cache[name] = mat;
        return mat;
    }

    public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat,
        bool collider = true, bool isStatic = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = isStatic;
        return go;
    }

    public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos,
        Vector3 scale, Material mat, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    public static GameObject Empty(string name, Transform parent, Vector3 localPos, float yaw = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    public static TextMesh Sign(string text, Transform parent, Vector3 localPos, float yaw, float size, Color color)
    {
        var go = Empty("Placa: " + text, parent, localPos, yaw);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = size;
        tm.fontSize = 64;
        tm.color = color;
        return tm;
    }

    public static Light PointLight(Transform parent, Vector3 localPos, float range, float intensity, Color color)
    {
        var go = Empty("Luz", parent, localPos);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = color;
        l.shadows = LightShadows.None;
        return l;
    }

    /// <summary>
    /// Prédio com interior acessível. Em espaço local, a fachada (com a porta) fica voltada para -Z.
    /// Retorna a raiz; o piso interno fica em y = 0.
    /// </summary>
    public static Transform Building(string name, Transform parent, Vector3 position, float yaw,
        float width, float depth, Color wallColor, string signText, out Door door)
    {
        const float height = 3.6f;
        const float t = 0.2f;
        const float doorWidth = 1.4f;
        const float doorHeight = 2.3f;

        var root = Empty(name, parent, position, yaw).transform;
        var wall = Mat("Parede_" + name, wallColor);
        var floor = Mat("Piso_Interno", new Color(0.62f, 0.6f, 0.56f), 0.4f);
        var roof = Mat("Telhado", new Color(0.25f, 0.25f, 0.27f));
        var trim = Mat("Acabamento", new Color(0.15f, 0.15f, 0.16f));

        Box("Piso", root, new Vector3(0, 0.025f, 0), new Vector3(width, 0.05f, depth), floor);
        Box("Telhado", root, new Vector3(0, height + t / 2, 0), new Vector3(width + 0.4f, t, depth + 0.4f), roof);
        Box("Parede Fundo", root, new Vector3(0, height / 2, depth / 2), new Vector3(width, height, t), wall);
        Box("Parede Esquerda", root, new Vector3(-width / 2, height / 2, 0), new Vector3(t, height, depth), wall);
        Box("Parede Direita", root, new Vector3(width / 2, height / 2, 0), new Vector3(t, height, depth), wall);

        float side = (width - doorWidth) / 2;
        Box("Fachada Esquerda", root, new Vector3(-width / 2 + side / 2, height / 2, -depth / 2), new Vector3(side, height, t), wall);
        Box("Fachada Direita", root, new Vector3(width / 2 - side / 2, height / 2, -depth / 2), new Vector3(side, height, t), wall);
        Box("Fachada Topo", root, new Vector3(0, (height + doorHeight) / 2, -depth / 2),
            new Vector3(doorWidth, height - doorHeight, t), wall);

        // Vitrines (decorativas, sem colisão extra)
        var glass = Mat("Vidro", new Color(0.55f, 0.7f, 0.8f), 0.9f);
        Box("Vitrine Esquerda", root, new Vector3(-width / 4 - doorWidth / 4, 1.6f, -depth / 2 - 0.11f), new Vector3(side * 0.6f, 1.4f, 0.02f), glass, false);
        Box("Vitrine Direita", root, new Vector3(width / 4 + doorWidth / 4, 1.6f, -depth / 2 - 0.11f), new Vector3(side * 0.6f, 1.4f, 0.02f), glass, false);

        // Porta: pivô na dobradiça, painel deslocado
        var hinge = Empty("Porta", root, new Vector3(-doorWidth / 2, 0, -depth / 2));
        var panel = Box("Folha", hinge.transform, new Vector3(doorWidth / 2, doorHeight / 2, 0),
            new Vector3(doorWidth - 0.04f, doorHeight - 0.02f, 0.06f), Mat("Porta", new Color(0.35f, 0.22f, 0.12f)), true, false);
        Box("Macaneta", panel.transform, new Vector3(0.4f, 0f, -0.9f), new Vector3(0.05f, 0.04f, 1.5f), trim, false, false);
        door = hinge.AddComponent<Door>();

        Sign(signText, root, new Vector3(0, height - 0.35f, -depth / 2 - 0.15f), 0f, 0.09f, Color.white);
        Box("Placa Fundo", root, new Vector3(0, height - 0.35f, -depth / 2 - 0.12f), new Vector3(Mathf.Min(width - 1, 6), 0.6f, 0.04f), trim, false);

        PointLight(root, new Vector3(0, height - 0.4f, 0), Mathf.Max(width, depth), 1.2f, new Color(1f, 0.95f, 0.85f));
        return root;
    }

    /// <summary>Prateleira com produtos placeholder (caixinhas coloridas).</summary>
    public static Transform Shelf(string name, Transform parent, Vector3 localPos, float yaw, Color productColor)
    {
        var root = Empty(name, parent, localPos, yaw).transform;
        var wood = Mat("Prateleira", new Color(0.45f, 0.33f, 0.22f));
        var item = Mat("Produto_" + ColorUtility.ToHtmlStringRGB(productColor), productColor, 0.5f);

        Box("Estrutura", root, new Vector3(0, 1f, 0.2f), new Vector3(2f, 2f, 0.05f), wood);
        for (int level = 0; level < 4; level++)
        {
            float y = 0.15f + level * 0.5f;
            Box("Nivel " + level, root, new Vector3(0, y, 0), new Vector3(2f, 0.04f, 0.45f), wood);
            for (int i = 0; i < 5; i++)
                Box("Item", root, new Vector3(-0.8f + i * 0.4f, y + 0.12f, 0), new Vector3(0.25f, 0.2f, 0.25f), item, false);
        }
        return root;
    }
}
