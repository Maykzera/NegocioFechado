using UnityEngine;

/// <summary>Quadro de ranking do dia: colegas e você.</summary>
public class RankingBoard : MonoBehaviour
{
    [SerializeField] private TextMesh text;
    private static readonly string[] Names = { "Paula", "Jefferson", "Marta", "Kleber", "Simone", "Diego" };
    private int lastDay = -1;
    private int[] coworkerSales = new int[6];
    private float timer;

    public TextMesh Text { get => text; set => text = value; }

    private void Update()
    {
        var g = GameServices.Game;
        if (g == null || text == null) return;
        if (lastDay != g.Day)
        {
            lastDay = g.Day;
            for (int i = 0; i < coworkerSales.Length; i++) coworkerSales[i] = 0;
        }
        timer += Time.deltaTime;
        if (timer > 25f)
        {
            timer = 0;
            int i = Random.Range(0, coworkerSales.Length);
            if (Random.value < 0.35f) coworkerSales[i]++;
        }

        var rows = new System.Collections.Generic.List<(string name, int sales)>();
        for (int i = 0; i < Names.Length; i++) rows.Add((Names[i], coworkerSales[i]));
        rows.Add(("VOCÊ (" + g.PlayerName + ")", g.SalesToday));
        rows.Sort((a, b) => b.sales.CompareTo(a.sales));
        var sb = new System.Text.StringBuilder("RANKING DO DIA\n");
        for (int i = 0; i < rows.Count; i++) sb.Append($"{i + 1}. {rows[i].name}  {rows[i].sales}\n");
        sb.Append($"\nMETA: {g.Quota} vendas");
        text.text = sb.ToString();
    }
}
