using System;
using System.Collections.Generic;
using System.Linq;

public enum CustomerArchetype
{
    Economico,
    Exigente,
    Indeciso,
    Apressado,
    Desconfiado
}

/// <summary>Uma pessoa da lista de ligações. Os dados "ocultos" o jogador só descobre conversando.</summary>
[Serializable]
public sealed class Lead
{
    public string Id;
    public string Name;
    public bool Female;
    public int Age;
    public string City;
    public string Occupation;
    public string Household;
    public string Phone;
    public CustomerArchetype Archetype;

    // Ocultos
    public ServiceCategory Need;
    public string NeedServiceId;
    public string Pain;
    public long BudgetCents;
    public float PickupChance;
    public bool DoNotCall;
    public int Seed;

    public string Pronoun => Female ? "ela" : "ele";

    public string ArchetypeDescription()
    {
        switch (Archetype)
        {
            case CustomerArchetype.Economico: return "muito preocupada com preço, compara tudo e pede desconto";
            case CustomerArchetype.Exigente: return "exigente, quer detalhes e qualidade, não aceita respostas vagas";
            case CustomerArchetype.Indeciso: return "indecisa, tem medo de se arrepender e costuma pedir para pensar";
            case CustomerArchetype.Apressado: return "sem tempo, quer que o vendedor vá direto ao ponto";
            default: return "desconfiada de ligações de vendas, acha que pode ser golpe";
        }
    }
}

/// <summary>Gera leads variados e reproduzíveis (mesma semente = mesma lista).</summary>
public static class LeadGenerator
{
    private static readonly string[] MaleNames = { "Carlos", "Roberto", "Marcos", "José", "Rafael", "Bruno", "Diego", "Thiago", "Gustavo", "Paulo", "Felipe", "Rodrigo", "Antônio", "Eduardo", "Lucas", "Sérgio" };
    private static readonly string[] FemaleNames = { "Ana", "Juliana", "Patrícia", "Fernanda", "Camila", "Beatriz", "Larissa", "Renata", "Aline", "Mariana", "Cláudia", "Sandra", "Luciana", "Vanessa", "Débora", "Helena" };
    private static readonly string[] Surnames = { "Silva", "Souza", "Oliveira", "Santos", "Lima", "Pereira", "Costa", "Ferreira", "Almeida", "Rodrigues", "Carvalho", "Gomes", "Ribeiro", "Martins", "Barbosa", "Rocha" };
    private static readonly string[] Cities = { "Campinas (SP)", "Belo Horizonte (MG)", "Curitiba (PR)", "Recife (PE)", "Goiânia (GO)", "Salvador (BA)", "Fortaleza (CE)", "Porto Alegre (RS)", "Uberlândia (MG)", "Londrina (PR)", "Natal (RN)", "Santos (SP)" };
    private static readonly string[] Occupations = { "professor(a)", "motorista de aplicativo", "enfermeiro(a)", "dono(a) de padaria", "estudante", "aposentado(a)", "analista de sistemas", "vendedor(a) de loja", "cabeleireiro(a)", "engenheiro(a)", "autônomo(a)", "contador(a)" };
    private static readonly string[] Households = { "mora sozinho(a)", "mora com o(a) parceiro(a)", "mora com o(a) parceiro(a) e dois filhos pequenos", "mora com os pais", "mora com três colegas de quarto", "mora com o(a) filho(a) adolescente" };

    private static readonly Dictionary<ServiceCategory, string[]> Pains = new Dictionary<ServiceCategory, string[]>
    {
        [ServiceCategory.Internet] = new[] { "a internet de casa vive caindo durante o trabalho remoto", "a internet atual é de 50 mega e trava quando todo mundo usa", "paga caro numa internet lenta e o suporte nunca atende" },
        [ServiceCategory.Seguro] = new[] { "acabou de comprar um celular de R$ 3.000 e tem medo de assalto no ônibus", "já teve um celular roubado no ano passado", "deixou o celular cair e quebrar a tela duas vezes" },
        [ServiceCategory.Saude] = new[] { "está com dor de dente e não vai ao dentista há três anos", "gasta muito com dentista particular para a família", "quer cuidar dos dentes mas acha caro pagar cada consulta" },
        [ServiceCategory.Entretenimento] = new[] { "as crianças querem desenhos e a família briga pela TV", "cancelou a TV a cabo e sente falta de filmes", "viaja muito e queria ver séries no celular sem internet" },
        [ServiceCategory.Educacao] = new[] { "precisa de inglês para uma promoção no trabalho", "quer viajar para fora e tem vergonha de não falar inglês", "começou cursos de inglês e desistiu por falta de horário" },
        [ServiceCategory.Seguranca] = new[] { "a casa do vizinho foi assaltada mês passado", "fica fora o dia todo e se preocupa com a casa vazia", "tem um comércio pequeno e já tentaram arrombar" },
        [ServiceCategory.Energia] = new[] { "a conta de luz passou de R$ 600 com o ar-condicionado", "queria energia solar mas não pode fazer obra porque mora de aluguel", "a conta de luz subiu muito e está apertando o orçamento" },
    };

    public static Lead Generate(int seed, int playerLevel, bool qualifiedLeads = false)
    {
        var rng = new Random(seed);
        bool female = rng.Next(2) == 0;
        string first = female ? FemaleNames[rng.Next(FemaleNames.Length)] : MaleNames[rng.Next(MaleNames.Length)];

        var unlocked = ServiceCatalog.UnlockedAt(playerLevel).ToList();
        // Leads qualificados: a necessidade quase sempre bate com um serviço que você pode vender.
        ServiceInfo target = qualifiedLeads || rng.NextDouble() < 0.8
            ? unlocked[rng.Next(unlocked.Count)]
            : ServiceCatalog.All[rng.Next(ServiceCatalog.All.Length)];

        var pains = Pains[target.Category];
        var archetype = (CustomerArchetype)rng.Next(5);
        double budgetFactor = archetype == CustomerArchetype.Economico ? 0.85 + rng.NextDouble() * 0.2
            : archetype == CustomerArchetype.Exigente ? 1.0 + rng.NextDouble() * 0.3
            : 0.9 + rng.NextDouble() * 0.3;

        return new Lead
        {
            Id = $"L{seed:X}",
            Name = $"{first} {Surnames[rng.Next(Surnames.Length)]}",
            Female = female,
            Age = 19 + rng.Next(55),
            City = Cities[rng.Next(Cities.Length)],
            Occupation = Gender(Occupations[rng.Next(Occupations.Length)], female),
            Household = Gender(Households[rng.Next(Households.Length)], female),
            Phone = $"({11 + rng.Next(80)}) 9{rng.Next(1000, 9999)}-{rng.Next(1000, 9999)}",
            Archetype = archetype,
            Need = target.Category,
            NeedServiceId = target.Id,
            Pain = pains[rng.Next(pains.Length)],
            BudgetCents = (long)Math.Round(target.MonthlyPriceCents * budgetFactor / 10.0) * 10,
            PickupChance = (float)(qualifiedLeads ? 0.85 : 0.7 + rng.NextDouble() * 0.15),
            DoNotCall = rng.NextDouble() < 0.05,
            Seed = seed
        };
    }

    public static List<Lead> GenerateQueue(int baseSeed, int count, int playerLevel, bool qualified)
    {
        var list = new List<Lead>();
        for (int i = 0; i < count; i++) list.Add(Generate(baseSeed * 7919 + i, playerLevel, qualified));
        return list;
    }

    private static string Gender(string text, bool female) =>
        text.Replace("o(a)", female ? "a" : "o").Replace("(a)", female ? "a" : "")
            .Replace("sozinho(a)", female ? "sozinha" : "sozinho");
}
