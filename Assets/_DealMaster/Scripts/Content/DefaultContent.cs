using System.Collections.Generic;

/// <summary>
/// Conteúdo da primeira versão jogável: 10 produtos e 2 fornecedores.
/// Valores em centavos. É só um ponto de partida de balanceamento.
/// </summary>
public static class DefaultContent
{
    public const string ElectronicsSupplierId = "eletro_centro";
    public const string AccessoriesSupplierId = "casa_acessorios";
    public const string WholesalerId = "atacado_bom_preco";
    public const int WholesalerLevel = 6;

    public static readonly ProductInfo[] Products =
    {
        // Eletro Centro
        new ProductInfo("fone_bt", "Fone de ouvido sem fio", ProductCategory.Electronics, 8500, 15000, 0.3f, 0.5f, 0.75f, BoxSize.Small, 1,
            new[] { "Bateria dura cerca de 20 horas", "Garantia de 6 meses", "Cancelamento de ruído passivo" },
            new[] { "É à prova d'água para nadar", "É a mesma qualidade de marca famosa" }),
        new ProductInfo("caixa_som", "Caixa de som Bluetooth", ProductCategory.Electronics, 7000, 12900, 0.6f, 0.4f, 0.70f, BoxSize.Medium, 1,
            new[] { "Resistente a respingos", "Bateria de 10 horas", "Conecta dois aparelhos" },
            new[] { "Tem potência de caixa de festa", "Nunca descarrega" }),
        new ProductInfo("carregador", "Carregador turbo 20W", ProductCategory.Electronics, 2500, 5900, 0.15f, 0.2f, 0.65f, BoxSize.Small, 1,
            new[] { "Carrega 50% em 30 minutos", "Proteção contra superaquecimento" },
            new[] { "Carrega o celular em 5 minutos" }),
        new ProductInfo("power_bank", "Power bank 10.000 mAh", ProductCategory.Electronics, 4500, 9900, 0.25f, 0.3f, 0.70f, BoxSize.Small, 1,
            new[] { "Carrega o celular cerca de duas vezes", "Duas saídas USB", "Indicador de carga" },
            new[] { "Carrega um notebook inteiro" }),
        new ProductInfo("mouse_sf", "Mouse sem fio", ProductCategory.Electronics, 2800, 5900, 0.12f, 0.3f, 0.60f, BoxSize.Small, 1,
            new[] { "Pilha dura meses", "Silencioso", "Funciona sem instalar nada" },
            new[] { "É profissional para jogos de competição" }),

        // Casa dos Acessórios
        new ProductInfo("capinha", "Capinha de celular reforçada", ProductCategory.Accessories, 800, 2900, 0.05f, 0.05f, 0.60f, BoxSize.Small, 1,
            new[] { "Cantos reforçados contra quedas", "Não amarela fácil" },
            new[] { "Aguenta queda do terceiro andar" }),
        new ProductInfo("pelicula", "Película de vidro", ProductCategory.Accessories, 400, 1900, 0.02f, 0.8f, 0.55f, BoxSize.Small, 1,
            new[] { "Vidro temperado 9H", "Aplicação sem bolhas com o kit" },
            new[] { "É inquebrável" }),
        new ProductInfo("suporte_carro", "Suporte veicular magnético", ProductCategory.Accessories, 1200, 3500, 0.1f, 0.1f, 0.60f, BoxSize.Small, 1,
            new[] { "Imã forte", "Gira 360 graus", "Encaixa na saída de ar" },
            new[] { "Serve em qualquer carro e qualquer celular" }),
        new ProductInfo("garrafa", "Garrafa térmica 500 ml", ProductCategory.Home, 2500, 5900, 0.4f, 0.2f, 0.70f, BoxSize.Medium, 1,
            new[] { "Mantém gelado por 24 horas", "Aço inox", "Não vaza" },
            new[] { "Mantém quente por uma semana" }),
        new ProductInfo("luminaria", "Luminária LED de mesa", ProductCategory.Home, 3000, 6900, 0.7f, 0.6f, 0.65f, BoxSize.Large, 1,
            new[] { "Três níveis de brilho", "Recarregável por USB", "Braço flexível" },
            new[] { "Gasta zero de energia" }),
    };

    public static void RegisterProducts()
    {
        foreach (var p in Products) ProductDatabase.Register(p);
    }

    public static SupplierStock CreateElectronicsSupplier()
    {
        var s = new SupplierStock(ElectronicsSupplierId, 8, 18, maxDiscount: 0.08f,
            volumeDiscountMinQuantity: 5, volumeDiscount: 0.05f);
        foreach (var id in new[] { "fone_bt", "caixa_som", "carregador", "power_bank", "mouse_sf" })
            AddFromCatalog(s, id, stock: 8, restock: 4);
        return s;
    }

    public static SupplierStock CreateAccessoriesSupplier()
    {
        var s = new SupplierStock(AccessoriesSupplierId, 9, 19, maxDiscount: 0.15f,
            volumeDiscountMinQuantity: 6, volumeDiscount: 0.08f);
        foreach (var id in new[] { "capinha", "pelicula", "suporte_carro", "garrafa", "luminaria" })
            AddFromCatalog(s, id, stock: 12, restock: 6);
        return s;
    }

    /// <summary>Atacadista (nível 6): todos os produtos, mais baratos, com desconto por volume.</summary>
    public static SupplierStock CreateWholesaler()
    {
        var s = new SupplierStock(WholesalerId, 7, 17, maxDiscount: 0.05f,
            volumeDiscountMinQuantity: 10, volumeDiscount: 0.06f);
        foreach (var p in Products)
            s.AddCatalogItem(p.Id, (long)System.Math.Round(p.BaseCostCents * 0.85), 30, 15, WholesalerLevel);
        return s;
    }

    public static Dictionary<string, string> SupplierNames => new Dictionary<string, string>
    {
        [ElectronicsSupplierId] = "Eletro Centro",
        [AccessoriesSupplierId] = "Casa dos Acessórios",
        [WholesalerId] = "Atacado Bom Preço"
    };

    private static void AddFromCatalog(SupplierStock s, string id, int stock, int restock)
    {
        var p = ProductDatabase.Get(id) ?? FindLocal(id);
        s.AddCatalogItem(id, p.BaseCostCents, stock, restock, p.RequiredLevel);
    }

    private static ProductInfo FindLocal(string id)
    {
        foreach (var p in Products) if (p.Id == id) return p;
        throw new KeyNotFoundException(id);
    }
}
