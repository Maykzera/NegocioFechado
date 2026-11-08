using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Etapa 4: pontos de passeio e de entrada dos clientes na praça e nas calçadas.</summary>
[InitializeOnLoad]
public static class CustomersSceneSetup
{
    static CustomersSceneSetup()
    {
        DealMasterSceneBuilder.OnPopulate += Populate;
    }

    private static void Populate(DealMasterSceneBuilder.Layout layout)
    {
        var root = WorldKit.Empty("Clientes", layout.World, Vector3.zero).transform;

        var waypoints = new List<Transform>();
        // Praça
        for (int x = 0; x < 4; x++)
            for (int z = 0; z < 2; z++)
                waypoints.Add(WorldKit.Empty("Passeio Praca", root, new Vector3(3 + x * 4.5f, 0.06f, -10 - z * 6)).transform);
        // Calçadas
        for (int x = -6; x <= 38; x += 11)
        {
            waypoints.Add(WorldKit.Empty("Passeio Calcada Sul", root, new Vector3(x, 0.12f, -5)).transform);
            waypoints.Add(WorldKit.Empty("Passeio Calcada Norte", root, new Vector3(x - 30, 0.12f, 5)).transform);
        }

        var spawns = new List<Transform>
        {
            WorldKit.Empty("Entrada Leste", root, new Vector3(40, 0.12f, -5)).transform,
            WorldKit.Empty("Entrada Oeste", root, new Vector3(-40, 0.12f, 5)).transform,
            WorldKit.Empty("Entrada Praca", root, new Vector3(18, 0.06f, -18)).transform
        };

        var spawner = layout.Systems.gameObject.AddComponent<CustomerSpawner>();
        var so = new SerializedObject(spawner);
        SetArray(so.FindProperty("waypoints"), waypoints);
        SetArray(so.FindProperty("spawnPoints"), spawns);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(SerializedProperty prop, List<Transform> items)
    {
        prop.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }
}
