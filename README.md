# Negócio Fechado — DealMaster: Business Simulator

Simulador 3D realista de comércio e negociação (Unity 6 · PC Windows e Android).

O jogador começa com pouco dinheiro, compra mercadorias em lojas e fornecedores, embala os pedidos à mão, procura clientes, negocia de verdade e no fim passa pela avaliação de um **mestre negociador** que analisa toda a negociação. Não existe golpe como mecânica principal: o desafio é vender honestamente, convencer clientes, administrar o estoque e lucrar.

## Ciclo do jogo

1. **Comprar mercadorias:** visitar lojas, atacadistas e fornecedores em um mundo 3D, comparar preços, examinar produtos, negociar descontos e comprar estoque com dinheiro limitado.
2. **Preparar e embalar:** conferir, limpar, proteger e embalar cada produto usando caixas, fita adesiva, etiquetas e materiais de proteção.
3. **Procurar clientes e negociar:** conversar com NPCs de personalidades, necessidades e orçamentos diferentes; apresentar argumentos, responder objeções e fechar vendas.
4. **Avaliação do mestre:** um mentor analisa a negociação completa (persuasão, honestidade, margem de lucro, atendimento e satisfação do cliente) e dá uma nota justificada.
5. **Reinvestir** os lucros e liberar novos fornecedores, produtos e negócios.

O jogador não deve simplesmente clicar em "Vender". Ele precisa aprender a vender de verdade, e cada negociação deve ser uma experiência diferente.

## Documentação

| Documento | Conteúdo |
|---|---|
| [`docs/PROJETO_MESTRE.md`](docs/PROJETO_MESTRE.md) | Conceito, arquitetura, scripts, progressão, vertical slice, cronograma e regras |
| [`docs/PROMPT_MESTRE.md`](docs/PROMPT_MESTRE.md) | Prompt mestre completo para uma IA desenvolvedora com acesso ao projeto Unity |
| [`docs/simulador-negociacao.html`](docs/simulador-negociacao.html) | Exemplo interativo de negociação (abra no navegador) |

## Estado atual

- ✅ Estrutura de pastas Unity (`Assets/_DealMaster/`)
- ✅ Produtos e carteira: `ProductDefinition`, `WalletService` (+ interface `IWallet`)
- ✅ Estoque: `InventoryService`: mochila, depósito, bancada e pedido, com capacidade, reservas, custo médio, transferências atômicas e histórico
- ✅ Fornecedores: `SupplierDefinition` (ScriptableObject) e `SupplierStock`: catálogo, horário, estoque, reposição, desconto por volume e relacionamento
- ✅ Compras: `PurchaseService`: negociação de preço com o fornecedor (aceita, contraproposta ou recusa) e compra validada, em que dinheiro e estoque mudam juntos ou nada muda
- ✅ Clientes: `CustomerBrain`: 5 perfis (econômico, exigente, indeciso, apressado, desconfiado), com necessidade, orçamento, confiança, paciência, contrapropostas, pedido de tempo, detecção de mentiras e memória de eventos para o mestre
- ✅ 35 testes automatizados (EditMode) em `Assets/_DealMaster/Tests/EditMode`
- ⏳ Ainda faltam: cenas, prefabs, `SalesService`, embalagem, `MentorEvaluator`, progressão, salvamento, interface e builds. Ainda não existe executável.

## Testes

No Unity: **Window → General → Test Runner → EditMode → Run All**. A lógica de domínio não depende de cena, então os testes rodam sem abrir o jogo.

## Como abrir

1. Instale o **Unity 6 LTS** com os módulos *Windows Build Support* e *Android Build Support*.
2. Crie um projeto **Universal 3D (URP)** novo e copie a pasta `Assets/_DealMaster` para dentro dele (ou abra esta pasta como projeto pelo Unity Hub, e o Unity gera o `ProjectSettings/`).
3. Instale pelo Package Manager: *Input System*, *AI Navigation* e *Test Framework*.
