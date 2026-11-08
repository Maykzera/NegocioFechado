using UnityEngine;

/// <summary>Tranca uma porta até o jogador atingir o nível exigido.</summary>
[RequireComponent(typeof(Door))]
public class LevelGate : MonoBehaviour
{
    [SerializeField] private int requiredLevel = 6;
    [SerializeField] private string placeName = "Atacadista";

    public int RequiredLevel { get => requiredLevel; set => requiredLevel = value; }
    public string PlaceName { get => placeName; set => placeName = value; }

    public void Refresh(int level)
    {
        var door = GetComponent<Door>();
        door.Locked = level < requiredLevel;
        door.LockedMessage = $"{placeName}: disponível a partir do nível {requiredLevel}";
    }
}
