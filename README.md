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
- ✅ Scripts base: `ProductDefinition.cs`, `WalletService.cs`, `NegotiationEngine.cs`
- ⏳ Cenas, prefabs, demais serviços, testes e builds ainda não foram criados. Ainda não existe projeto Unity compilado nem executável.

## Como abrir

1. Instale o **Unity 6 LTS** com os módulos *Windows Build Support* e *Android Build Support*.
2. Crie um projeto **Universal 3D (URP)** novo e copie a pasta `Assets/_DealMaster` para dentro dele (ou abra esta pasta como projeto pelo Unity Hub, e o Unity gera o `ProjectSettings/`).
3. Instale pelo Package Manager: *Input System* e *AI Navigation*.
