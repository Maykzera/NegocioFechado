using UnityEngine;

/// <summary>
/// Colega de trabalho: fica na mesa ao telefone, às vezes fala uma frase (balão) e de vez em quando vai à copa.
/// </summary>
public class Coworker : MonoBehaviour
{
    private static readonly string[] Lines =
    {
        "Bom dia! Falo da Conecta Vendas, tem um minutinho?",
        "Entendo, senhora... posso explicar rapidinho?",
        "Isso! Instalação grátis e 500 mega.",
        "Pode ficar tranquilo, eu tiro seu número da lista.",
        "Fechou? Que ótimo, vou cadastrar!",
        "Alô? Alô? ... desligou.",
        "A fidelidade é de 12 meses, tá bom?",
        "Obrigado pelo seu tempo, tenha um bom dia!",
    };

    [SerializeField] private Transform body;
    [SerializeField] private TextMesh bubble;
    [SerializeField] private Transform breakPoint;

    private Vector3 seat;
    private float nextLine, hideAt, nextBreak, bob;
    private bool walking, returning;
    private CharacterPoses poses;

    public Transform Body { get => body; set => body = value; }
    public TextMesh Bubble { get => bubble; set => bubble = value; }
    public Transform BreakPoint { get => breakPoint; set => breakPoint = value; }

    private void Start()
    {
        seat = transform.position;
        poses = GetComponentInChildren<CharacterPoses>();
        nextLine = Time.time + Random.Range(3f, 15f);
        nextBreak = Time.time + Random.Range(60f, 180f);
        if (bubble != null) bubble.text = "";
    }

    private void Update()
    {
        if (walking) { Walk(); return; }

        bob += Time.deltaTime * 2f;
        if (body != null) body.localRotation = Quaternion.Euler(Mathf.Sin(bob) * 3f, Mathf.Sin(bob * 0.5f) * 8f, 0f);

        if (bubble != null)
        {
            if (Time.time > nextLine)
            {
                bubble.text = Lines[Random.Range(0, Lines.Length)];
                hideAt = Time.time + 4f;
                nextLine = Time.time + Random.Range(10f, 25f);
            }
            if (Time.time > hideAt) bubble.text = "";
        }

        if (breakPoint != null && Time.time > nextBreak)
        {
            walking = true;
            returning = false;
            if (poses != null) poses.Apply(false);
            if (bubble != null) bubble.text = "";
        }
    }

    private void Walk()
    {
        Vector3 target = returning ? seat : breakPoint.position;
        Vector3 to = target - transform.position;
        to.y = 0;
        if (to.magnitude < 0.2f)
        {
            if (returning)
            {
                walking = false;
                nextBreak = Time.time + Random.Range(90f, 240f);
                transform.rotation = Quaternion.identity;
                if (poses != null) poses.Apply(true);
            }
            else { returning = true; }
            return;
        }
        transform.position += to.normalized * Mathf.Min(1.2f * Time.deltaTime, to.magnitude);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 8f * Time.deltaTime);
    }
}
