using System;
using System.Collections.Generic;
using System.Linq;

public enum ServiceCategory
{
    Internet,
    Seguro,
    Saude,
    Educacao,
    Entretenimento,
    Energia,
    Seguranca
}

/// <summary>Um argumento verdadeiro sobre o serviço, com palavras que o identificam numa frase.</summary>
public sealed class ServiceFact
{
    public string Text { get; }
    public string[] Keywords { get; }

    public ServiceFact(string text, params string[] keywords)
    {
        Text = text;
        Keywords = keywords;
    }
}

/// <summary>Uma promessa falsa (que o serviço NÃO cumpre). Usá-la é mentir para o cliente.</summary>
public sealed class ServiceLie
{
    public string Text { get; }
    public string[] Keywords { get; }

    public ServiceLie(string text, params string[] keywords)
    {
        Text = text;
        Keywords = keywords;
    }
}

/// <summary>Serviço fictício vendido pela central. Valores mensais em centavos.</summary>
public sealed class ServiceInfo
{
    public string Id { get; }
    public string Name { get; }
    public string Company { get; }
    public ServiceCategory Category { get; }
    public long MonthlyPriceCents { get; }

    /// <summary>Menor mensalidade que a empresa autoriza (desconto máximo).</summary>
    public long MinPriceCents { get; }

    /// <summary>Comissão do vendedor: múltiplo da mensalidade fechada.</summary>
    public float CommissionRate { get; }

    public int FidelityMonths { get; }
    public int RequiredLevel { get; }
    public string Pitch { get; }
    public string[] Keywords { get; }
    public IReadOnlyList<ServiceFact> Facts { get; }
    public IReadOnlyList<ServiceLie> Lies { get; }

    public ServiceInfo(string id, string name, string company, ServiceCategory category,
        long monthlyPriceCents, long minPriceCents, float commissionRate, int fidelityMonths, int requiredLevel,
        string pitch, string[] keywords, ServiceFact[] facts, ServiceLie[] lies)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("id vazio");
        if (minPriceCents > monthlyPriceCents) throw new ArgumentException("preço mínimo maior que o de tabela");
        Id = id;
        Name = name;
        Company = company;
        Category = category;
        MonthlyPriceCents = monthlyPriceCents;
        MinPriceCents = minPriceCents;
        CommissionRate = commissionRate;
        FidelityMonths = fidelityMonths;
        RequiredLevel = Math.Max(1, requiredLevel);
        Pitch = pitch;
        Keywords = keywords ?? Array.Empty<string>();
        Facts = facts ?? Array.Empty<ServiceFact>();
        Lies = lies ?? Array.Empty<ServiceLie>();
    }

    public long CommissionFor(long priceCents) => (long)Math.Round(priceCents * CommissionRate);

    /// <summary>Resumo do serviço para o catálogo e para a IA.</summary>
    public string Describe()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"{Name} ({Company}) - {Money.Format(MonthlyPriceCents)}/mês");
        sb.Append(FidelityMonths > 0 ? $", fidelidade de {FidelityMonths} meses" : ", sem fidelidade");
        sb.Append(". ").Append(Pitch);
        sb.Append(" Verdades: ").Append(string.Join("; ", Facts.Select(f => f.Text))).Append('.');
        sb.Append(" NÃO é verdade: ").Append(string.Join("; ", Lies.Select(l => l.Text))).Append('.');
        return sb.ToString();
    }
}

public static class Money
{
    /// <summary>Formata centavos como "R$ 1.234,56", independente da cultura.</summary>
    public static string Format(long cents)
    {
        string sign = cents < 0 ? "-" : "";
        ulong abs = cents < 0 ? (ulong)(-(cents + 1)) + 1 : (ulong)cents;
        string reais = (abs / 100).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var grouped = new System.Text.StringBuilder();
        for (int i = 0; i < reais.Length; i++)
        {
            if (i > 0 && (reais.Length - i) % 3 == 0) grouped.Append('.');
            grouped.Append(reais[i]);
        }
        return $"{sign}R$ {grouped},{abs % 100:00}";
    }
}

/// <summary>Os serviços fictícios da central de vendas.</summary>
public static class ServiceCatalog
{
    public static readonly ServiceInfo[] All =
    {
        new ServiceInfo("netveloz_500", "NetVeloz Fibra 500 Mega", "NetVeloz", ServiceCategory.Internet,
            9990, 8490, 1.0f, 12, 1,
            "Internet de fibra óptica para casa.",
            new[] { "internet", "fibra", "netveloz", "wi-fi", "wifi", "mega" },
            new[]
            {
                new ServiceFact("500 mega de velocidade", "500 mega", "500mb", "500 mb", "500 megas"),
                new ServiceFact("instalação grátis", "instalacao gratis", "instalacao gratuita", "sem custo de instalacao", "instalacao sem custo"),
                new ServiceFact("roteador Wi-Fi 6 incluso", "wi-fi 6", "wifi 6", "roteador incluso", "roteador gratis"),
                new ServiceFact("suporte técnico 24 horas", "suporte 24", "24 horas", "24h"),
            },
            new[]
            {
                new ServiceLie("não tem fidelidade", "sem fidelidade", "nao tem fidelidade", "sem multa", "cancela quando quiser"),
                new ServiceLie("velocidade garantida 100% do tempo", "100% do tempo", "nunca cai", "nunca oscila", "garantida sempre"),
                new ServiceLie("primeiro ano grátis", "ano gratis", "primeiro ano de graca", "gratis para sempre"),
            }),
        new ServiceInfo("protege_celular", "Protege+ Seguro de Celular", "Protege+", ServiceCategory.Seguro,
            2990, 2490, 1.5f, 0, 1,
            "Seguro para smartphone contra roubo, furto e quebra.",
            new[] { "seguro de celular", "seguro do celular", "protege", "seguro para o celular", "seguro celular" },
            new[]
            {
                new ServiceFact("cobre roubo e furto qualificado", "roubo", "furto"),
                new ServiceFact("cobre quebra acidental de tela", "quebra", "tela quebrada", "tela"),
                new ServiceFact("franquia de 20% no sinistro", "franquia de 20", "20% de franquia", "franquia"),
                new ServiceFact("sem fidelidade, cancela quando quiser", "sem fidelidade", "cancela quando quiser", "cancelar quando quiser"),
            },
            new[]
            {
                new ServiceLie("não tem franquia", "sem franquia", "nao tem franquia", "zero de franquia"),
                new ServiceLie("cobre perda ou esquecimento", "perda", "perdeu", "esqueceu", "esquecimento"),
                new ServiceLie("cobre qualquer dano, até água", "qualquer dano", "agua", "molhou", "tudo coberto"),
            }),
        new ServiceInfo("sorriso_bom", "SorrisoBom Plano Odontológico", "SorrisoBom", ServiceCategory.Saude,
            3990, 3290, 1.2f, 12, 1,
            "Plano odontológico individual com rede credenciada.",
            new[] { "dentista", "odontologico", "odonto", "plano dental", "sorrisobom", "sorriso" },
            new[]
            {
                new ServiceFact("consultas e limpezas sem limite", "limpeza", "consultas", "sem limite de consultas"),
                new ServiceFact("urgência odontológica 24 horas", "urgencia", "emergencia", "24 horas", "24h"),
                new ServiceFact("mais de 2 mil dentistas na rede", "2 mil dentistas", "rede credenciada", "dentistas na rede"),
                new ServiceFact("carência de 30 dias para canal e extração", "carencia de 30", "30 dias de carencia"),
            },
            new[]
            {
                new ServiceLie("não tem carência nenhuma", "sem carencia", "nao tem carencia", "carencia zero"),
                new ServiceLie("aparelho ortodôntico de graça", "aparelho gratis", "aparelho de graca", "ortodontia gratis"),
                new ServiceLie("implante incluso", "implante"),
            }),
        new ServiceInfo("cinemax_familia", "CineMax Streaming Família", "CineMax", ServiceCategory.Entretenimento,
            3490, 2790, 1.0f, 0, 1,
            "Streaming de filmes e séries para a família.",
            new[] { "streaming", "filmes", "series", "cinemax", "assistir" },
            new[]
            {
                new ServiceFact("4 telas ao mesmo tempo", "4 telas", "quatro telas"),
                new ServiceFact("perfil infantil com controle dos pais", "infantil", "controle dos pais", "criancas"),
                new ServiceFact("baixar para ver sem internet", "baixar", "download", "offline"),
                new ServiceFact("sem fidelidade", "sem fidelidade", "cancela quando quiser"),
            },
            new[]
            {
                new ServiceLie("tem futebol ao vivo", "futebol", "jogo ao vivo", "canais ao vivo"),
                new ServiceLie("é grátis no primeiro ano", "ano gratis", "gratis por um ano"),
            }),
        new ServiceInfo("vidatranquila", "VidaTranquila Seguro de Vida", "VidaTranquila", ServiceCategory.Seguro,
            4990, 3990, 1.5f, 12, 2,
            "Seguro de vida individual com assistências.",
            new[] { "seguro de vida", "vidatranquila", "vida tranquila" },
            new[]
            {
                new ServiceFact("indenização de R$ 100 mil", "100 mil", "cem mil", "indenizacao"),
                new ServiceFact("assistência funeral familiar", "funeral", "assistencia funeral"),
                new ServiceFact("sem carência para acidentes", "sem carencia para acidente", "acidente sem carencia"),
            },
            new[]
            {
                new ServiceLie("cobre doenças que a pessoa já tem", "doenca preexistente", "doencas preexistentes", "doenca que ja tem"),
                new ServiceLie("rende como investimento", "rende", "investimento", "poupanca", "devolve o dinheiro"),
            }),
        new ServiceInfo("falafacil", "FalaFácil Inglês Online", "FalaFácil", ServiceCategory.Educacao,
            7990, 5990, 1.0f, 6, 3,
            "Curso de inglês online com aulas ao vivo.",
            new[] { "ingles", "curso", "falafacil", "aulas", "idioma" },
            new[]
            {
                new ServiceFact("aulas ao vivo duas vezes por semana", "ao vivo", "duas vezes por semana", "2 vezes"),
                new ServiceFact("certificado de conclusão", "certificado"),
                new ServiceFact("aplicativo para praticar", "aplicativo", "app"),
                new ServiceFact("primeira aula experimental grátis", "aula experimental", "experimental", "aula gratis"),
            },
            new[]
            {
                new ServiceLie("fluência garantida em 3 meses", "fluencia garantida", "fluente em 3 meses", "garantido em 3 meses"),
                new ServiceLie("diploma reconhecido pelo MEC", "mec", "diploma reconhecido"),
            }),
        new ServiceInfo("protegecasa", "ProtegeCasa Alarme Monitorado", "ProtegeCasa", ServiceCategory.Seguranca,
            8990, 7490, 1.2f, 12, 4,
            "Alarme residencial com monitoramento.",
            new[] { "alarme", "monitorado", "protegecasa", "seguranca", "camera" },
            new[]
            {
                new ServiceFact("monitoramento 24 horas", "24 horas", "24h", "monitoramento"),
                new ServiceFact("controle pelo aplicativo", "aplicativo", "app", "celular"),
                new ServiceFact("instalação inclusa", "instalacao inclusa", "instalacao gratis", "instalacao gratuita"),
            },
            new[]
            {
                new ServiceLie("polícia chega em 5 minutos garantido", "policia", "5 minutos", "cinco minutos"),
                new ServiceLie("indeniza qualquer roubo", "indeniza", "reembolsa o roubo", "paga o roubo"),
            }),
        new ServiceInfo("solarcasa", "SolarCasa Energia por Assinatura", "SolarCasa", ServiceCategory.Energia,
            14990, 12990, 0.8f, 12, 5,
            "Assinatura de energia solar compartilhada, sem obras.",
            new[] { "energia", "solar", "conta de luz", "solarcasa", "luz" },
            new[]
            {
                new ServiceFact("economia de até 15% na conta de luz", "15%", "quinze por cento", "economia"),
                new ServiceFact("sem obras nem placas no telhado", "sem obra", "sem placas", "sem instalacao no telhado"),
                new ServiceFact("sem taxa de adesão", "sem taxa", "sem adesao"),
            },
            new[]
            {
                new ServiceLie("zera a conta de luz", "zera", "conta zerada", "nao paga mais luz", "conta de luz de graca"),
                new ServiceLie("não tem fidelidade", "sem fidelidade", "nao tem fidelidade"),
            }),
    };

    public static ServiceInfo Get(string id) => All.FirstOrDefault(s => s.Id == id);

    public static IEnumerable<ServiceInfo> UnlockedAt(int level) => All.Where(s => s.RequiredLevel <= level);
}
