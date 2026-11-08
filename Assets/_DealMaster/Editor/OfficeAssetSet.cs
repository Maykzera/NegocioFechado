using System;
using UnityEngine;

/// <summary>Um modelo 3D para um tipo de objeto do escritório.</summary>
[Serializable]
public class AssetSlot
{
    [Tooltip("Arraste aqui o prefab/modelo da sua pasta de assets. Vazio = forma simples gerada por código.")]
    public GameObject prefab;

    [Tooltip("Escala aplicada ao modelo (ajuste se ficar grande ou pequeno demais).")]
    public float scale = 1f;

    [Tooltip("Giro extra em graus, se o modelo vier virado para o lado errado.")]
    public float yawOffset;

    [Tooltip("Ajuste de altura em metros (ex.: modelos com o ponto de origem no centro).")]
    public float heightOffset;
}

/// <summary>
/// Lista dos modelos 3D usados pelo construtor do escritório.
/// Menu: DealMaster > Lista de assets do escritório. Depois de preencher, reconstrua a cena.
/// </summary>
[CreateAssetMenu(fileName = "OfficeAssets", menuName = "DealMaster/Lista de assets do escritório")]
public class OfficeAssetSet : ScriptableObject
{
    [Header("Estação de trabalho")]
    public AssetSlot desk = new AssetSlot();
    public AssetSlot officeChair = new AssetSlot();
    public AssetSlot monitor = new AssetSlot();
    public AssetSlot keyboard = new AssetSlot();
    public AssetSlot headset = new AssetSlot();
    public AssetSlot partition = new AssetSlot();

    [Header("Pessoas")]
    public AssetSlot coworker = new AssetSlot();
    public AssetSlot supervisor = new AssetSlot();
    public AssetSlot receptionist = new AssetSlot();

    [Header("Ambientes")]
    public AssetSlot receptionDesk = new AssetSlot();
    public AssetSlot sofa = new AssetSlot();
    public AssetSlot plant = new AssetSlot();
    public AssetSlot coffeeMachine = new AssetSlot();
    public AssetSlot fridge = new AssetSlot();
    public AssetSlot meetingTable = new AssetSlot();
    public AssetSlot chair = new AssetSlot();
    public AssetSlot waterCooler = new AssetSlot();
    public AssetSlot printer = new AssetSlot();
    public AssetSlot tv = new AssetSlot();
}
