using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Mantém clientes circulando pela praça e calçadas, com perfis e necessidades variados.</summary>
public class CustomerSpawner : MonoBehaviour
{
    private static readonly string[] Names =
    {
        "Rafael", "Juliana", "Marcos", "Patrícia", "Bruno", "Camila", "Diego", "Fernanda",
        "Gustavo", "Larissa", "Thiago", "Aline", "Rodrigo", "Beatriz", "Felipe", "Renata"
    };

    [SerializeField] private Transform[] waypoints = new Transform[0];
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField] private int maxCustomersPC = 6;
    [SerializeField] private int maxCustomersMobile = 4;
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private int seed = 1234;

    private readonly List<CustomerAgent> active = new List<CustomerAgent>();
    private System.Random rng;
    private float nextSpawn;
    private int spawnCount;

    public static CustomerMemory Memory { get; } = new CustomerMemory();

    public IReadOnlyList<CustomerAgent> Active => active;

    private int MaxCustomers => Application.isMobilePlatform ? maxCustomersMobile : maxCustomersPC;

    private void Start()
    {
        rng = new System.Random(seed);
        for (int i = 0; i < Mathf.Min(3, MaxCustomers); i++) Spawn();
    }

    private void Update()
    {
        if (Time.time < nextSpawn || active.Count >= MaxCustomers) return;
        nextSpawn = Time.time + spawnInterval;
        Spawn();
    }

    public void Despawn(CustomerAgent agent)
    {
        active.Remove(agent);
        Destroy(agent.gameObject);
    }

    private void Spawn()
    {
        if (waypoints.Length == 0 || spawnPoints.Length == 0 || !ProductDatabase.All.Any()) return;

        string name = Names.Where(n => active.All(a => a.CustomerName != n)).OrderBy(_ => rng.Next()).FirstOrDefault();
        if (name == null) return;

        var archetype = (CustomerArchetype)rng.Next(5);
        var products = ProductDatabase.All.Where(p => p.RequiredLevel <= GameServices.PlayerLevel).ToList();
        var wanted = products[rng.Next(products.Count)];
        int customerSeed = seed * 31 + spawnCount++;

        var brain = new CustomerBrain(name, archetype, wanted.Category, wanted.ReferencePriceCents, customerSeed);
        brain.ApplyRelationship(Memory.TrustBonus(name));
        if (GameServices.Sales != null) brain.ApplyRelationship((GameServices.Sales.Reputation - 50f) / 400f);

        Vector3 start = spawnPoints[rng.Next(spawnPoints.Length)].position;
        Vector3 exit = spawnPoints[rng.Next(spawnPoints.Length)].position;

        var go = BuildBody(name, archetype);
        go.transform.position = start;
        var agent = go.AddComponent<CustomerAgent>();
        agent.Setup(this, name, brain, waypoints.Select(w => w.position).ToArray(), exit, customerSeed);
        active.Add(agent);
    }

    private static GameObject BuildBody(string name, CustomerArchetype archetype)
    {
        var root = new GameObject("Cliente " + name);
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Corpo";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0, 0.95f, 0);
        body.transform.localScale = new Vector3(0.55f, 0.75f, 0.4f);
        body.GetComponent<Renderer>().material.color = ShirtColor(archetype, name);

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Cabeca";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0, 1.75f, 0);
        head.transform.localScale = new Vector3(0.3f, 0.32f, 0.3f);
        Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().material.color = new Color(0.8f, 0.62f, 0.48f);

        var label = new GameObject("Nome");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0, 2.1f, 0);
        var tm = label.AddComponent<TextMesh>();
        tm.text = name;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.characterSize = 0.035f;
        tm.fontSize = 64;
        TextMeshUtil.ApplyFont(tm);
        label.AddComponent<FaceCamera>();
        return root;
    }

    private static Color ShirtColor(CustomerArchetype archetype, string name)
    {
        float hue = (Mathf.Abs(name.GetHashCode()) % 360) / 360f;
        return Color.HSVToRGB(hue, 0.5f, 0.75f);
    }
}

/// <summary>Faz o texto do nome sempre olhar para a câmera.</summary>
public class FaceCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
