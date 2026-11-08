using System.Linq;
using UnityEngine;

/// <summary>
/// Minigame de embalagem na bancada: selecionar, inspecionar, escolher a caixa, posicionar,
/// proteger, fechar, passar fita, etiquetar e confirmar. Funciona com mouse e toque.
/// </summary>
public class PackingBenchPanel : IModalPanel
{
    private enum Step { Select, Inspect, ChooseBox, Place, Pad, Close, Tape, Label, Confirm }

    private static readonly string[] StepNames =
        { "Produto", "Inspeção", "Caixa", "Posicionar", "Proteção", "Fechar", "Fita", "Etiqueta", "Confirmar" };

    private Step step = Step.Select;
    private PackagingInput input;
    private float startTime;
    private int inspectViews;
    private bool leftFlap, rightFlap;
    private string message;
    private Vector2 scroll;

    // Arraste (coordenadas virtuais da tela)
    private Vector2 productPos;
    private Vector2 labelPos;
    private bool dragging;
    private Vector2 dragOffset;
    private bool taping;
    private float tapeMinX, tapeMaxX, tapeDeviation;
    private int tapeSamples;
    private Rect lastBoxRect;

    public string Title => "Bancada de embalagem";

    public void OnClosed() { }

    public void DrawPanel(Rect area)
    {
        DrawSteps(new Rect(area.x, area.y, area.width, 30));
        var body = new Rect(area.x, area.y + 40, area.width, area.height - 40);
        float leftW = body.width * 0.4f;
        var left = new Rect(body.x, body.y, leftW, body.height);
        var right = new Rect(body.x + leftW + 20, body.y, body.width - leftW - 20, body.height);

        switch (step)
        {
            case Step.Select: DrawSelect(left, right); break;
            case Step.Inspect: DrawInspect(left, right); break;
            case Step.ChooseBox: DrawChooseBox(left, right); break;
            case Step.Place: DrawPlace(left, right); break;
            case Step.Pad: DrawPad(left, right); break;
            case Step.Close: DrawClose(left, right); break;
            case Step.Tape: DrawTape(left, right); break;
            case Step.Label: DrawLabel(left, right); break;
            case Step.Confirm: DrawConfirm(left, right); break;
        }

        if (!string.IsNullOrEmpty(message))
            GUI.Label(new Rect(left.x, left.yMax - 60, left.width, 56), message, UIStyles.Bad);
    }

    private void DrawSteps(Rect r)
    {
        float w = r.width / StepNames.Length;
        for (int i = 0; i < StepNames.Length; i++)
        {
            var style = i == (int)step ? UIStyles.Header : UIStyles.Small;
            string mark = i < (int)step ? "✓ " : "";
            GUI.Label(new Rect(r.x + i * w, r.y, w, r.height), mark + StepNames[i], style);
        }
    }

    // ---------------------------------------------------------------- passos

    private void DrawSelect(Rect left, Rect right)
    {
        var inv = GameServices.Inventory;
        GUI.Label(new Rect(left.x, left.y, left.width, 28), "Na bancada", UIStyles.Header);
        var bench = inv.GetStacks(StorageLocation.PackingBench).ToList();
        float y = left.y + 34;
        if (bench.Count == 0) GUI.Label(new Rect(left.x, y, left.width, 50), "Nada na bancada. Traga um produto da mochila.", UIStyles.Small);
        foreach (var s in bench)
        {
            if (GUI.Button(new Rect(left.x, y, left.width, 44), $"Embalar: {ProductDatabase.NameOf(s.ProductId)} ({s.Quantity})", UIStyles.ListItem))
                Begin(s.ProductId);
            y += 50;
        }

        GUI.Label(new Rect(right.x, right.y, right.width, 28), "Na mochila", UIStyles.Header);
        var backpack = inv.GetStacks(StorageLocation.Backpack).ToList();
        scroll = GUI.BeginScrollView(new Rect(right.x, right.y + 34, right.width, right.height - 34), scroll,
            new Rect(0, 0, right.width - 20, backpack.Count * 50));
        for (int i = 0; i < backpack.Count; i++)
        {
            var s = backpack[i];
            if (GUI.Button(new Rect(0, i * 50, right.width - 24, 44), $"Colocar na bancada: {ProductDatabase.NameOf(s.ProductId)} ({s.Quantity})", UIStyles.ListItem))
            {
                if (!inv.TryTransfer(StorageLocation.Backpack, StorageLocation.PackingBench, s.ProductId, 1, "Para a bancada"))
                    message = "A bancada está cheia.";
            }
        }
        GUI.EndScrollView();

        int ready = GameServices.Packaging.Orders.Count;
        GUI.Label(new Rect(left.x, left.yMax - 100, left.width, 30), $"Pedidos prontos para venda: {ready}", UIStyles.Good);
    }

    private void Begin(string productId)
    {
        var info = ProductDatabase.Get(productId);
        if (info == null) return;
        input = new PackagingInput { Product = info };
        startTime = Time.realtimeSinceStartup;
        inspectViews = 0;
        leftFlap = rightFlap = false;
        message = null;
        step = Step.Inspect;
    }

    private void DrawInspect(Rect left, Rect right)
    {
        string[] views = { "Frente: sem riscos.", "Laterais: encaixes firmes.", "Fundo: lacre e etiqueta de fábrica ok." };
        GUI.Label(new Rect(left.x, left.y, left.width, 60), $"Inspecione o {input.Product.Name.ToLower()} antes de embalar.", UIStyles.Text);
        for (int i = 0; i < inspectViews; i++)
            GUI.Label(new Rect(left.x, left.y + 70 + i * 30, left.width, 28), "✓ " + views[i], UIStyles.Good);

        DrawProductPreview(right, inspectViews * 120f);

        float y = left.y + 170;
        if (inspectViews < views.Length)
        {
            if (GUI.Button(new Rect(left.x, y, 240, 44), "Girar e inspecionar", UIStyles.Button)) inspectViews++;
            if (GUI.Button(new Rect(left.x, y + 54, 240, 40), "Pular inspeção", UIStyles.Button)) { input.Inspected = false; step = Step.ChooseBox; }
        }
        else
        {
            input.Inspected = true;
            if (GUI.Button(new Rect(left.x, y, 240, 44), "Próximo: escolher caixa", UIStyles.Button)) step = Step.ChooseBox;
        }
    }

    private void DrawChooseBox(Rect left, Rect right)
    {
        GUI.Label(new Rect(left.x, left.y, left.width, 50), $"Fragilidade do produto: {(int)(input.Product.Fragility * 100)}%. Escolha o tamanho da caixa.", UIStyles.Text);
        float y = left.y + 60;
        foreach (BoxSize size in new[] { BoxSize.Small, BoxSize.Medium, BoxSize.Large })
        {
            if (GUI.Button(new Rect(left.x, y, left.width, 48), $"Caixa {ShopPanel.BoxName(size)}  ·  {CustomerBrain.FormatReais(PackagingEvaluator.BoxCost(size))}", UIStyles.ListItem))
            {
                if ((int)size < (int)input.Product.Size) message = "O produto não cabe nessa caixa.";
                else
                {
                    input.Box = size;
                    message = null;
                    productPos = Vector2.zero;
                    step = Step.Place;
                }
            }
            y += 56;
        }
        DrawProductPreview(right, 0);
    }

    private void DrawPlace(Rect left, Rect right)
    {
        GUI.Label(new Rect(left.x, left.y, left.width, 80), "Arraste o produto para o centro da caixa.", UIStyles.Text);
        Rect box = BoxRect(right);
        Rect product = ProductRect(box);
        if (productPos == Vector2.zero) productPos = new Vector2(box.x + 20 + product.width / 2, box.y + 20 + product.height / 2);

        DrawBox(box, false);
        productPos = Drag(product.width, product.height, productPos, box);
        DrawProductAt(productPos, product.size);

        input.PlacementOffset = Offset(box, productPos);
        GUI.Label(new Rect(left.x, left.y + 90, left.width, 30), $"Distância do centro: {(int)(input.PlacementOffset * 100)}%", UIStyles.Small);
        if (GUI.Button(new Rect(left.x, left.y + 130, 240, 44), "Próximo: proteção", UIStyles.Button)) step = Step.Pad;
    }

    private void DrawPad(Rect left, Rect right)
    {
        GUI.Label(new Rect(left.x, left.y, left.width, 80), "Adicione plástico bolha. Produtos frágeis e caixas folgadas precisam de mais proteção.", UIStyles.Text);
        float y = left.y + 90;
        GUI.Label(new Rect(left.x, y, 160, 40), "Plástico bolha", UIStyles.Text);
        if (GUI.Button(new Rect(left.x + 170, y, 44, 40), "-", UIStyles.Button)) input.PaddingSheets = Mathf.Max(0, input.PaddingSheets - 1);
        GUI.Label(new Rect(left.x + 224, y, 40, 40), input.PaddingSheets.ToString(), UIStyles.Header);
        if (GUI.Button(new Rect(left.x + 264, y, 44, 40), "+", UIStyles.Button)) input.PaddingSheets = Mathf.Min(PackagingEvaluator.MaxPadding, input.PaddingSheets + 1);
        GUI.Label(new Rect(left.x, y + 46, left.width, 26), $"{CustomerBrain.FormatReais(PackagingEvaluator.PaddingSheetCents)} por folha", UIStyles.Small);

        Rect box = BoxRect(right);
        DrawBox(box, false);
        var bubble = new Color(0.75f, 0.9f, 1f, 0.5f);
        for (int i = 0; i < input.PaddingSheets; i++)
        {
            float inset = 6 + i * 7;
            GUI.color = bubble;
            GUI.Box(new Rect(box.x + inset, box.y + inset, box.width - inset * 2, box.height - inset * 2), GUIContent.none);
        }
        GUI.color = Color.white;
        DrawProductAt(productPos, ProductRect(box).size);

        if (GUI.Button(new Rect(left.x, y + 90, 240, 44), "Próximo: fechar caixa", UIStyles.Button)) step = Step.Close;
    }

    private void DrawClose(Rect left, Rect right)
    {
        GUI.Label(new Rect(left.x, left.y, left.width, 60), "Dobre as abas para fechar a caixa.", UIStyles.Text);
        if (!leftFlap && GUI.Button(new Rect(left.x, left.y + 70, 240, 44), "Dobrar aba esquerda", UIStyles.Button)) leftFlap = true;
        if (!rightFlap && GUI.Button(new Rect(left.x, left.y + 124, 240, 44), "Dobrar aba direita", UIStyles.Button)) rightFlap = true;

        Rect box = BoxRect(right);
        DrawBox(box, false);
        DrawProductAt(productPos, ProductRect(box).size);
        var flap = new Color(0.78f, 0.6f, 0.38f);
        if (leftFlap) Fill(new Rect(box.x, box.y, box.width / 2, box.height), flap);
        if (rightFlap) Fill(new Rect(box.center.x, box.y, box.width / 2, box.height), flap * 0.95f);

        input.FlapsClosed = leftFlap && rightFlap;
        if (input.FlapsClosed && GUI.Button(new Rect(left.x, left.y + 180, 240, 44), "Próximo: fita", UIStyles.Button))
            step = Step.Tape;
    }

    private void DrawTape(Rect left, Rect right)
    {
        GUI.Label(new Rect(left.x, left.y, left.width, 90),
            "Arraste ao longo da emenda (linha tracejada), de uma ponta à outra. Duas tiras bem retas costumam bastar.", UIStyles.Text);
        Rect box = BoxRect(right);
        DrawBox(box, true);
        lastBoxRect = box;

        float seamY = box.center.y;
        for (float x = box.x; x < box.xMax; x += 16) Fill(new Rect(x, seamY - 1, 8, 2), new Color(0.2f, 0.15f, 0.1f));

        // Tiras já aplicadas
        for (int i = 0; i < input.TapeStrips.Count; i++)
            Fill(new Rect(box.x, seamY - 10 + i * 3, box.width * input.TapeStrips[i], 14), new Color(0.85f, 0.75f, 0.5f, 0.85f));

        HandleTapeDrag(box, seamY);

        GUI.Label(new Rect(left.x, left.y + 100, left.width, 30),
            $"Tiras: {input.TapeStrips.Count}/{PackagingEvaluator.MaxTapeStrips}" +
            (input.TapeStrips.Count > 0 ? $"  ·  última: {(int)(input.TapeStrips[input.TapeStrips.Count - 1] * 100)}%" : ""),
            UIStyles.Small);

        if (input.TapeStrips.Count > 0 && GUI.Button(new Rect(left.x, left.y + 140, 240, 40), "Desfazer última tira", UIStyles.Button))
            input.TapeStrips.RemoveAt(input.TapeStrips.Count - 1);
        if (input.TapeStrips.Count > 0 && GUI.Button(new Rect(left.x, left.y + 190, 240, 44), "Próximo: etiqueta", UIStyles.Button))
        {
            labelPos = new Vector2(box.xMax - 70, box.yMax - 40);
            step = Step.Label;
        }
    }

    private void HandleTapeDrag(Rect box, float seamY)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && box.Contains(e.mousePosition) && input.TapeStrips.Count < PackagingEvaluator.MaxTapeStrips)
        {
            taping = true;
            tapeMinX = tapeMaxX = e.mousePosition.x;
            tapeDeviation = 0;
            tapeSamples = 0;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && taping)
        {
            float x = Mathf.Clamp(e.mousePosition.x, box.x, box.xMax);
            tapeMinX = Mathf.Min(tapeMinX, x);
            tapeMaxX = Mathf.Max(tapeMaxX, x);
            tapeDeviation += Mathf.Abs(e.mousePosition.y - seamY);
            tapeSamples++;
            e.Use();
        }
        else if ((e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && taping)
        {
            taping = false;
            float coverage = (tapeMaxX - tapeMinX) / box.width;
            float avgDev = tapeSamples > 0 ? tapeDeviation / tapeSamples : 99f;
            float straightness = Mathf.Clamp01(1f - avgDev / 40f);
            float quality = Mathf.Clamp01(coverage) * straightness;
            if (coverage > 0.1f) input.TapeStrips.Add(quality);
            e.Use();
        }

        if (taping) Fill(new Rect(tapeMinX, seamY - 7, tapeMaxX - tapeMinX, 14), new Color(0.9f, 0.8f, 0.55f, 0.6f));
    }

    private void DrawLabel(Rect left, Rect right)
    {
        GUI.Label(new Rect(left.x, left.y, left.width, 80), "Arraste a etiqueta para a área marcada no canto da caixa e cole.", UIStyles.Text);
        Rect box = BoxRect(right);
        DrawBox(box, true);
        Fill(new Rect(box.x, box.center.y - 7, box.width, 14), new Color(0.85f, 0.75f, 0.5f, 0.85f));

        Rect target = LabelTarget(box);
        GUI.Box(target, "etiqueta aqui", UIStyles.Small);

        var size = new Vector2(target.width, target.height);
        labelPos = Drag(size.x, size.y, labelPos, right);
        Rect label = new Rect(labelPos - size / 2, size);
        Fill(label, new Color(0.97f, 0.97f, 0.95f));
        GUI.Label(new Rect(label.x + 6, label.y + 4, label.width - 12, label.height - 8), "PED · DealMaster\nCliente final", UIStyles.Small);

        input.LabelAccuracy = Overlap(label, target);
        GUI.Label(new Rect(left.x, left.y + 90, left.width, 30), $"Alinhamento: {(int)(input.LabelAccuracy * 100)}%", UIStyles.Small);
        if (GUI.Button(new Rect(left.x, left.y + 130, 240, 44), "Colar etiqueta", UIStyles.Button))
        {
            if (input.LabelAccuracy < 0.2f) message = "A etiqueta precisa ficar sobre a caixa.";
            else
            {
                input.LabelApplied = true;
                input.SecondsSpent = Time.realtimeSinceStartup - startTime;
                message = null;
                step = Step.Confirm;
            }
        }
    }

    private void DrawConfirm(Rect left, Rect right)
    {
        var score = PackagingEvaluator.Evaluate(input);
        GUI.Label(new Rect(left.x, left.y, left.width, 28), input.Product.Name, UIStyles.Header);
        if (!score.Valid)
        {
            GUI.Label(new Rect(left.x, left.y + 40, left.width, 60), score.Problem, UIStyles.Bad);
            if (GUI.Button(new Rect(left.x, left.y + 110, 240, 44), "Refazer", UIStyles.Button)) Begin(input.Product.Id);
            return;
        }

        float y = left.y + 40;
        Row(left, ref y, "Proteção", score.Protection);
        Row(left, ref y, "Integridade (fita)", score.Integrity);
        Row(left, ref y, "Tamanho da caixa", score.SizeFit);
        Row(left, ref y, "Apresentação", score.Presentation);
        Row(left, ref y, "Uso de material", score.Efficiency);
        Row(left, ref y, "Tempo", score.Time);

        GUI.Label(new Rect(right.x, right.y, right.width, 34), $"Nota da embalagem: {score.Overall * 10f:0.0}/10", UIStyles.Header);
        GUI.Label(new Rect(right.x, right.y + 40, right.width, 28), $"Materiais: {CustomerBrain.FormatReais(score.MaterialsCostCents)}", UIStyles.Text);
        GUI.Label(new Rect(right.x, right.y + 70, right.width, 28), $"Risco de avaria na entrega: {(int)(score.DamageRisk * 100)}%",
            score.DamageRisk > 0.2f ? UIStyles.Bad : UIStyles.Good);
        GUI.Label(new Rect(right.x, right.y + 100, right.width, 28), $"Tempo: {input.SecondsSpent:0} s", UIStyles.Small);

        if (GUI.Button(new Rect(right.x, right.y + 150, 260, 48), "Confirmar pedido", UIStyles.Button))
        {
            if (GameServices.Packaging.TryFinalize(input, out var order, out var error))
            {
                GameUI.Toast($"Pedido {order.OrderId} pronto: {input.Product.Name}");
                step = Step.Select;
                input = null;
            }
            else message = error;
        }
        if (GUI.Button(new Rect(right.x, right.y + 208, 260, 40), "Desfazer e refazer", UIStyles.Button)) Begin(input.Product.Id);
    }

    // ---------------------------------------------------------------- desenho e arraste

    private static void Row(Rect area, ref float y, string label, float value)
    {
        GUI.Label(new Rect(area.x, y, 200, 26), label, UIStyles.Small);
        UIStyles.Bar(new Rect(area.x + 200, y + 4, area.width - 260, 18), value, value > 0.7f ? new Color(0.35f, 0.75f, 0.45f) : value > 0.4f ? new Color(0.9f, 0.7f, 0.3f) : new Color(0.9f, 0.4f, 0.35f));
        GUI.Label(new Rect(area.xMax - 50, y, 50, 26), $"{(int)(value * 100)}%", UIStyles.Small);
        y += 32;
    }

    private Rect BoxRect(Rect area)
    {
        float scale = input.Box == BoxSize.Small ? 0.55f : input.Box == BoxSize.Medium ? 0.75f : 0.95f;
        float w = Mathf.Min(area.width, area.height * 1.3f) * scale;
        float h = w * 0.7f;
        return new Rect(area.center.x - w / 2, area.y + (area.height - h) / 2, w, h);
    }

    private Rect ProductRect(Rect box)
    {
        // O produto ocupa a caixa do seu tamanho quase inteira; em caixas maiores, sobra espaço.
        float ratio = 0.62f * Mathf.Pow(0.73f, (int)input.Box - (int)input.Product.Size);
        return new Rect(0, 0, box.width * ratio, box.height * ratio);
    }

    private static float Offset(Rect box, Vector2 pos)
    {
        Vector2 d = pos - box.center;
        return Mathf.Clamp01(Mathf.Max(Mathf.Abs(d.x) / (box.width / 2), Mathf.Abs(d.y) / (box.height / 2)));
    }

    private Vector2 Drag(float w, float h, Vector2 pos, Rect bounds)
    {
        Event e = Event.current;
        var rect = new Rect(pos.x - w / 2, pos.y - h / 2, w, h);
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            dragging = true;
            dragOffset = pos - e.mousePosition;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && dragging)
        {
            pos = e.mousePosition + dragOffset;
            pos.x = Mathf.Clamp(pos.x, bounds.x + w / 2, bounds.xMax - w / 2);
            pos.y = Mathf.Clamp(pos.y, bounds.y + h / 2, bounds.yMax - h / 2);
            e.Use();
        }
        else if (e.rawType == EventType.MouseUp && dragging)
        {
            dragging = false;
        }
        return pos;
    }

    private static Rect LabelTarget(Rect box) => new Rect(box.x + box.width * 0.08f, box.y + box.height * 0.1f, box.width * 0.32f, box.height * 0.22f);

    private static float Overlap(Rect a, Rect b)
    {
        float x = Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.x, b.x));
        float y = Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.y, b.y));
        return Mathf.Clamp01(x * y / (a.width * a.height));
    }

    private static void DrawBox(Rect box, bool closed)
    {
        Fill(new Rect(box.x - 6, box.y - 6, box.width + 12, box.height + 12), new Color(0.55f, 0.4f, 0.25f));
        Fill(box, closed ? new Color(0.78f, 0.6f, 0.38f) : new Color(0.45f, 0.33f, 0.2f));
    }

    private void DrawProductAt(Vector2 center, Vector2 size)
    {
        Fill(new Rect(center - size / 2, size), new Color(0.25f, 0.3f, 0.45f));
        GUI.Label(new Rect(center.x - size.x / 2 + 6, center.y - 12, size.x - 12, 24), input.Product.Name, UIStyles.Small);
    }

    private void DrawProductPreview(Rect area, float angle)
    {
        var center = new Vector2(area.center.x, area.y + area.height * 0.4f);
        var size = new Vector2(180, 120);
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        Fill(new Rect(center - size / 2, size), new Color(0.25f, 0.3f, 0.45f));
        GUI.matrix = old;
        GUI.Label(new Rect(area.x, center.y + 90, area.width, 30), input?.Product?.Name ?? "", UIStyles.Text);
    }

    private static void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }
}
