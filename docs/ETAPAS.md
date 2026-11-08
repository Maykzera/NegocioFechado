# Etapas de desenvolvimento

Acompanhamento do cronograma técnico do projeto mestre.

| Etapa | Entrega | Status |
|---|---|---|
| 1. Fundação | Controles PC/Android, interação, portas, HUD, cena gerada por script | ✅ |
| 2. Comércio | Produtos, fornecedores, compra, inventário e depósito | ✅ |
| 3. Embalagem | Bancada, caixas, proteção, fita e etiqueta | ✅ |
| 4. NPCs | Clientes andando, perfis e negociação | ⏳ |
| 5. Vendas | Pagamento, entrega e pós-venda | ⏳ |
| 6. Mestre | Histórico, avaliação e feedback | ⏳ |
| 7. Progressão | XP, níveis, desbloqueios, missões e relógio | ⏳ |
| 8. Finalização | Salvamento, qualidade gráfica e builds | ⏳ |

## Etapa 1 — Fundação

- **Cena gerada por código:** menu `DealMaster > Construir cena da Vertical Slice` cria `Scenes/CommercialDistrict.unity`. A cena tem:
  - rua comercial com calçadas e postes;
  - Loja Eletro Centro, Casa dos Acessórios, Escritório do Mestre e Atacadista (trancado até o nível 6);
  - Depósito do jogador e Praça comercial.
- **Personagem:** `PlayerController` em primeira pessoa (caminhar, correr com Shift, agachar com C).
- **Controles:**
  - PC: WASD, mouse, E para interagir, Tab para mochila, Esc para soltar o cursor.
  - Android: `TouchControls` com joystick à esquerda, arrastar à direita para olhar e botões Interagir, Agachar e Mochila.
- **Interação:** `InteractionController` usa um raycast pelo centro da tela e a interface `IInteractable`.
- **Portas:** `Door` abre e fecha e pode ser trancada.
- **Interface:** `GameUI` e `GameHUDController` controlam o HUD (dinheiro, mira, texto de interação, avisos) e as telas modais. A UI é IMGUI com escala automática, igual em PC e Android.

## Etapa 2 — Comércio

- **10 produtos** (`DefaultContent`), cada um com custo, preço de mercado, fragilidade, qualidade, tamanho de caixa, argumentos verdadeiros e exageros. Nenhuma caneta.
- **2 fornecedores:**
  - Eletro Centro, com o Seu Jorge: fone, caixa de som, carregador, power bank e mouse;
  - Casa dos Acessórios, com a Dona Célia: capinha, película, suporte veicular, garrafa térmica e luminária.
- **Tela de compra (`ShopPanel`):** catálogo, quantidade, desconto por volume e pechincha (até 3 tentativas, com contraproposta do vendedor), além de horário de funcionamento e nível exigido.
- **Mochila (Tab):** mostra os produtos carregados, a capacidade e o custo médio.
- **Depósito:** a estante do estoque abre a transferência entre mochila e depósito.
- **`ProductDefinition`:** novos produtos podem ser criados como ScriptableObject e adicionados no `GameBootstrap`.

## Etapa 3 — Embalagem

- **Bancada física** no depósito (`PackingBench`). O minigame (`PackingBenchPanel`) tem 9 passos:
  1. levar o produto da mochila para a bancada;
  2. inspecionar o produto, girando-o (dá para pular, mas a apresentação piora);
  3. escolher a caixa (pequena, média ou grande; se for pequena demais, o produto não cabe);
  4. **arrastar** o produto até o centro da caixa;
  5. adicionar plástico bolha;
  6. dobrar as abas;
  7. **passar fita arrastando** ao longo da emenda, onde a cobertura e a retidão contam;
  8. **arrastar a etiqueta** até a área marcada;
  9. ver a nota e confirmar.
- **`PackagingEvaluator`:** dá a nota de proteção, integridade, tamanho, apresentação, uso de material e tempo, calcula o custo dos materiais e o **risco de avaria na entrega**.
- **`PackagingService`:** cobra os materiais e move a unidade para "pedidos prontos". A operação é tudo ou nada.
