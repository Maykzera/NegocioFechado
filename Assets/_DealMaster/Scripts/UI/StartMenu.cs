using UnityEngine;

/// <summary>Mostra a tela inicial ao abrir o jogo.</summary>
public class StartMenu : MonoBehaviour
{
    private void Start() => GameUI.Open(new StartMenuPanel());
}
