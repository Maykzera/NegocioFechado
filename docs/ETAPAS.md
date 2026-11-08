# Etapas de desenvolvimento

Acompanhamento do cronograma técnico do projeto mestre.

| Etapa | Entrega | Status |
|---|---|---|
| 1. Fundação | Controles PC/Android, interação, portas, HUD, cena gerada por script | ✅ |
| 2. Comércio | Produtos, fornecedores, compra, inventário e depósito | ✅ |
| 3. Embalagem | Bancada, caixas, proteção, fita e etiqueta | ✅ |
| 4. NPCs | Clientes andando, perfis e negociação | ✅ |
| 5. Vendas | Pagamento, entrega e pós-venda | ✅ |
| 6. Mestre | Histórico, avaliação e feedback | ✅ |
| 7. Progressão | XP, níveis, desbloqueios, missões e relógio | ✅ |
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

## Etapa 4 — NPCs

- **`CustomerSpawner`:** mantém até 6 clientes no PC e 4 no Android, cada um com nome, um dos 5 perfis e uma necessidade sorteada entre os produtos liberados no seu nível.
- **`CustomerAgent`:** o cliente anda entre pontos da praça e das calçadas, para e olha para o jogador durante a conversa e vai embora quando a negociação termina.
- **Negociação (`NegotiationPanel`):**
  - barras de **confiança, interesse e paciência**;
  - histórico da conversa;
  - ações: cumprimentar, perguntar o que procura, apresentar produto (embalado ou da mochila), argumentos verdadeiros misturados com exageros, responder dúvida, pressionar, propor preço, aceitar a contraproposta, aguardar o indeciso e encerrar.
- **`CustomerMemory`:** o cliente lembra compras, reclamações, saídas insatisfeitas e mentiras descobertas. Isso muda a confiança inicial e a saudação no próximo encontro.
- **`NegotiationHooks`:** liga a negociação ao sistema de vendas (Etapa 5) e ao mestre (Etapa 6).

## Etapa 5 — Vendas

- **`SalesService`:** é o único caminho pelo qual uma venda mexe em dinheiro e estoque.
  - Tira o pedido embalado (ou o item da mochila, sem embalagem) e recebe o pagamento menos a **taxa da maquininha de 2%**.
  - Na entrega, o produto pode chegar **avariado**, conforme o risco calculado na embalagem. Sem embalagem, o risco é bem maior.
  - Calcula o lucro direto de cada venda: preço − taxa − custo − embalagem − reembolsos.
- **Pós-venda:** reclamações de produto avariado podem ser resolvidas com reembolso total, reembolso de metade ou recusa. Cada opção afeta a **reputação** e a memória do cliente.
- **Reputação (0 a 100):** sobe com entregas boas e cai com avarias e recusas. Muda a confiança inicial dos novos clientes.
- **`FinanceLedger`:** livro-caixa com todas as compras, embalagens, vendas, taxas e reembolsos. Calcula receita, compras, despesas e fluxo de caixa.
- **Painel do negócio (F, ou botão "Painel" no celular):** abas de resumo, vendas, caixa e reclamações.

## Etapa 6 — Mestre

- **`MentorEvaluator`** dá uma nota de 0 a 10 a partir do registro estruturado da negociação. É **determinístico**: o mesmo histórico, na mesma versão do avaliador, gera sempre a mesma nota. Os 9 critérios são os do projeto mestre:
  - necessidade;
  - conhecimento do produto;
  - clareza;
  - objeções;
  - estratégia de preço;
  - margem;
  - transparência;
  - satisfação;
  - resultado.
- **Problemas que ele aponta:** desconto excessivo, prejuízo, mentiras (o mestre sempre vê, mesmo quando o cliente não percebe), pressão, oportunidade perdida, dúvidas sem resposta, entrega sem embalagem e embalagem fraca.
- **Sugestões:** saem dos 3 critérios mais fracos.
- **Escritório do mestre:** o Mestre Otávio, na mesa dele (`MentorDesk`), analisa as negociações pendentes uma a uma. A tela (`MentorPanel`) mostra a nota, as barras dos critérios, as observações e um **histórico com gráfico de evolução**.

## Etapa 7 — Progressão

- **XP** por ações comerciais válidas: compra, embalagem (proporcional à qualidade), venda (proporcional ao lucro) e avaliação do mestre (proporcional à nota). Não há XP por clique repetido.
- **Níveis** com os títulos do projeto: Vendedor iniciante (1–5), Revendedor profissional (6–10), Empreendedor (11–20), Empresário (21–30) e Magnata comercial (31+).
- **Desbloqueios:**
  - mochila de 14 itens (nível 2) e de 20 itens (nível 5);
  - depósito de 100 itens e bancada maior (nível 3);
  - 30 pedidos prontos ao mesmo tempo (nível 4);
  - **Atacado Bom Preço (nível 6)**, com todos os produtos 15% mais baratos, desconto por volume e o Seu Antônio no balcão;
  - depósito de 200 itens (nível 8).
- **8 missões**, incluindo a principal, ★ **"Negócio fechado"**: fazer uma venda lucrativa e obter a aprovação do mestre.
- **Relógio do jogo:** o expediente vai das 8h às 22h (cerca de 7 minutos reais). As lojas têm horário de funcionamento, o sol se move e os postes acendem à noite. No **fim do dia**, é cobrado o aluguel do depósito (R$ 20), os fornecedores reabastecem e aparece um resumo do dia.
- **Painel (F) → aba Progresso:** mostra nível, barra de XP, missões e desbloqueios.
