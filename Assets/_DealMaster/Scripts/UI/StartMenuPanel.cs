using UnityEngine;

/// <summary>Tela inicial: continuar, novo jogo e como jogar.</summary>
public class StartMenuPanel : IModalPanel
{
    public static bool IsShowing { get; private set; }

    private bool showHelp;

    public string Title => "DealMaster — Negócio Fechado";

    public StartMenuPanel()
    {
        IsShowing = true;
    }

    public void OnClosed()
    {
        IsShowing = false;
    }

    public void DrawPanel(Rect area)
    {
        if (showHelp) { DrawHelp(area); return; }

        GUI.Label(new Rect(area.x, area.y, area.width, 80),
            "Você começa com R$ 1.500 e um depósito. Compre mercadorias, embale com capricho, negocie de verdade com os clientes e mostre ao Mestre Negociador que sabe vender.",
            UIStyles.Text);

        float y = area.y + 100;
        bool hasSave = SaveGameBridge.Service.HasSave;
        GUI.enabled = hasSave;
        if (GUI.Button(new Rect(area.x, y, 320, 54), "Continuar", UIStyles.Button))
        {
            if (SaveGameBridge.Instance != null && SaveGameBridge.Instance.LoadSaved()) GameUI.Close();
        }
        GUI.enabled = true;
        y += 64;
        if (GUI.Button(new Rect(area.x, y, 320, 54), hasSave ? "Novo jogo (apaga o salvo)" : "Novo jogo", UIStyles.Button))
        {
            if (hasSave && SaveGameBridge.Instance != null) SaveGameBridge.Instance.NewGame();
            else
            {
                GameUI.Close();
                GameUI.Toast("Missão principal: faça uma venda lucrativa e seja aprovado pelo mestre.", 6f);
            }
        }
        y += 64;
        if (GUI.Button(new Rect(area.x, y, 320, 54), "Como jogar", UIStyles.Button)) showHelp = true;
    }

    private void DrawHelp(Rect area)
    {
        string text =
            "<b>Objetivo:</b> comprar barato, embalar bem, vender com lucro e de forma honesta.\n\n" +
            "<b>1. Comprar</b> — Entre na Eletro Centro ou na Casa dos Acessórios e fale com o vendedor. Dá para pechinchar.\n" +
            "<b>2. Guardar</b> — No seu depósito (do outro lado da rua), use a estante para guardar produtos.\n" +
            "<b>3. Embalar</b> — Na bancada do depósito, inspecione, escolha a caixa certa, proteja, passe fita e etiquete.\n" +
            "<b>4. Vender</b> — Converse com os clientes na praça. Pergunte o que procuram, apresente o produto certo, use argumentos verdadeiros e negocie o preço.\n" +
            "<b>5. Mestre</b> — No escritório do Mestre Negociador, receba a nota de cada negociação.\n\n" +
            "<b>PC:</b> WASD mover · Mouse olhar · Shift correr · C agachar · E interagir · Tab mochila · F painel · Esc soltar cursor\n" +
            "<b>Celular:</b> joystick à esquerda · arraste à direita para olhar · botões Interagir, Agachar, Mochila e Painel";
        GUI.Label(new Rect(area.x, area.y, area.width, area.height - 60), text, UIStyles.Text);
        if (GUI.Button(new Rect(area.x, area.yMax - 50, 200, 46), "Voltar", UIStyles.Button)) showHelp = false;
    }
}

/// <summary>Mostra a tela inicial ao abrir o jogo.</summary>
public class StartMenu : MonoBehaviour
{
    private void Start()
    {
        GameUI.Open(new StartMenuPanel());
    }
}
