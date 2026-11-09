using UnityEngine;

/// <summary>Tela inicial: continuar, novo jogo e como jogar.</summary>
public class StartMenuPanel : IModalPanel
{
    public static bool IsShowing { get; private set; }

    private bool showHelp;

    public string Title => "Negócio Fechado · Central de Vendas";

    public StartMenuPanel() { IsShowing = true; }

    public void OnClosed() { IsShowing = false; }

    public void DrawPanel(Rect area)
    {
        if (showHelp) { DrawHelp(area); return; }

        GUI.Label(new Rect(area.x, area.y, area.width, 110),
            $"Seu primeiro dia na <b>{CallCenterGame.CompanyName}</b>. Sente na sua mesa, ligue para os clientes e converse de verdade: " +
            "descubra o que cada pessoa precisa, apresente o serviço certo e convença sem mentir. A Supervisora Regina ouve suas ligações " +
            "e venda feita com mentira é cancelada.", UIStyles.Text);

        float y = area.y + 120;
        bool hasSave = SaveGameBridge.Service.HasSave;
        GUI.enabled = hasSave;
        if (GUI.Button(new Rect(area.x, y, 340, 54), "Continuar", UIStyles.Button))
            if (SaveGameBridge.Instance != null && SaveGameBridge.Instance.LoadSaved()) GameUI.Close();
        GUI.enabled = true;
        y += 64;
        if (GUI.Button(new Rect(area.x, y, 340, 54), hasSave ? "Novo jogo (apaga o salvo)" : "Começar o expediente", UIStyles.Button))
        {
            if (hasSave && SaveGameBridge.Instance != null) SaveGameBridge.Instance.NewGame();
            else { GameUI.Close(); GameUI.Toast("Sua mesa tem a placa SUA MESA. Sente lá e abra o Discador.", 6f); }
        }
        y += 64;
        if (GUI.Button(new Rect(area.x, y, 340, 54), "Como jogar", UIStyles.Button)) showHelp = true;
        y += 70;
        GUI.Label(new Rect(area.x, y, area.width, 50), ClaudeAvailability.Status(GameServices.AI ?? new AISettings()), UIStyles.Small);
    }

    private void DrawHelp(Rect area)
    {
        string text =
            "<b>1. Sua mesa:</b> sente e use o computador. No <b>Discador</b> estão as pessoas para ligar hoje.\n" +
            "<b>2. A ligação:</b> digite o que você falaria. Apresente-se, pergunte como a pessoa está, descubra o problema dela.\n" +
            "<b>3. A oferta:</b> consulte o <b>Catálogo</b>. Ofereça o serviço que resolve o problema e use só argumentos verdadeiros.\n" +
            "<b>4. O fechamento:</b> negocie o preço (sem passar do mínimo autorizado). Quando o cliente disser sim, use <b>Registrar venda</b>.\n" +
            "<b>5. A supervisora:</b> na sala de vidro, a Regina ouve suas ligações e dá nota. Mentira cancela a venda.\n" +
            "<b>6. Energia:</b> cada ligação cansa. Tome café na copa. O expediente vai das 9h às 18h.\n\n" +
            "<b>PC:</b> WASD anda · mouse olha · E interage · Esc solta o mouse.  <b>Celular:</b> joystick à esquerda, arraste à direita, botão Interagir.";
        GUI.Label(new Rect(area.x, area.y, area.width, area.height - 60), text, UIStyles.Text);
        if (GUI.Button(new Rect(area.x, area.yMax - 50, 200, 46), "Voltar", UIStyles.Button)) showHelp = false;
    }
}
