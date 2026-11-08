# Negócio Fechado — DealMaster: Business Simulator

Simulador 3D de comércio e negociação em primeira pessoa (Unity 6 · PC Windows e Android).

Você começa com **R$ 1.500** e um depósito. Compra mercadorias nas lojas da cidade, **embala os pedidos à mão** na bancada, procura clientes na praça e **negocia de verdade**. Não existe botão "Vender": é preciso entender o que o cliente quer, argumentar, responder dúvidas e chegar a um preço. No fim, o **Mestre Negociador** analisa cada negociação e dá uma nota justificada.

## Como abrir e jogar

1. Instale o **Unity 6 LTS** pelo Unity Hub, com os módulos *Windows Build Support (IL2CPP)* e *Android Build Support*.
2. No Unity Hub: **New project → Universal 3D (URP)**.
3. Copie a pasta `Assets/_DealMaster` deste repositório para dentro da pasta `Assets/` do projeto novo.
4. No Unity, use o menu **DealMaster → Configurar projeto**. Se ele avisar que mudou o *Active Input Handling*, reinicie o Unity.
5. Use o menu **DealMaster → Construir cena da Vertical Slice**. A cidade inteira é criada em `Scenes/CommercialDistrict.unity`.
6. Aperte **Play**.

### Controles

| | PC | Celular |
|---|---|---|
| Mover | WASD (Shift corre, C agacha) | Joystick à esquerda |
| Olhar | Mouse | Arrastar à direita |
| Interagir | E ou clique | Botão "Interagir" |
| Mochila | Tab | Botão "Mochila" |
| Painel (progresso, finanças, reclamações) | F | Botão "Painel" |
| Soltar cursor / fechar tela | Esc | — |

### O ciclo do jogo

1. **Comprar:** na *Eletro Centro* (Seu Jorge) ou na *Casa dos Acessórios* (Dona Célia). Dá para pechinchar.
2. **Guardar:** no seu depósito, do outro lado da rua, a estante guarda o estoque.
3. **Embalar:** na bancada, inspecione, escolha a caixa, arraste o produto, proteja, passe a fita arrastando, etiquete e confirme. Embalagem ruim aumenta o risco de avaria.
4. **Vender:** converse com os clientes na praça e nas calçadas. Cada um tem perfil, necessidade, orçamento, confiança e paciência.
5. **Mestre:** no escritório do Mestre Otávio, receba a nota de 0 a 10 de cada negociação, com observações e sugestões.
6. **Evoluir:** ganhe XP, suba de nível, amplie o estoque e libere o *Atacado Bom Preço* no nível 6.

**Missão principal:** fazer uma venda lucrativa e ser aprovado pelo mestre (nota 7 ou mais).

## Testes

No Unity: **Window → General → Test Runner → EditMode → Run All**. São 81 testes cobrindo:
- economia e estoque;
- fornecedores e compras;
- clientes e negociação;
- embalagem;
- vendas e pós-venda;
- mestre;
- progressão, missões e relógio;
- salvamento.

A lógica do jogo é separada da cena, então os testes rodam sem abrir o jogo.

## Gerar os executáveis

- **Windows:** `DealMaster → Build Windows` gera `Builds/Windows/DealMaster.exe`.
- **Android:** `DealMaster → Build Android (APK)` gera `Builds/Android/DealMaster.apk`.

Linha de comando (CI):

```bash
Unity -batchmode -quit -projectPath <projeto> -executeMethod DealMasterBuild.BuildWindows
Unity -batchmode -quit -projectPath <projeto> -executeMethod DealMasterBuild.BuildAndroid
```

## Documentação

| Documento | Conteúdo |
|---|---|
| [`docs/ETAPAS.md`](docs/ETAPAS.md) | O que foi feito em cada uma das 8 etapas |
| [`docs/PROJETO_MESTRE.md`](docs/PROJETO_MESTRE.md) | Conceito, arquitetura, progressão, cronograma e regras |
| [`docs/PROMPT_MESTRE.md`](docs/PROMPT_MESTRE.md) | Prompt mestre original |
| [`docs/simulador-negociacao.html`](docs/simulador-negociacao.html) | Exemplo interativo de negociação |

## Estrutura do código

```text
Assets/_DealMaster/
├── Scripts/            (assembly DealMaster)
│   ├── Core/           GameBootstrap, GameServices, qualidade por plataforma
│   ├── Player/         movimento, input PC/touch, interação
│   ├── World/          portas, utilitários
│   ├── Content/        produtos e fornecedores padrão
│   ├── Economy/        carteira e livro-caixa
│   ├── Inventory/      estoque, mochila, depósito
│   ├── Suppliers/      fornecedores, compra e pechincha
│   ├── Packaging/      avaliação e minigame de embalagem
│   ├── NPC/            cérebro, memória, agentes e spawner de clientes
│   ├── Negotiation/    motor e tela de negociação
│   ├── Sales/          vendas, pós-venda, painel financeiro
│   ├── Mentor/         avaliador, histórico e escritório do mestre
│   ├── Progression/    XP, níveis, missões, relógio, dia/noite
│   ├── Persistence/    salvamento versionado
│   ├── Audio/          sons gerados por código
│   └── UI/             HUD, telas, menu inicial
├── Editor/             construtor da cena e builds
└── Tests/EditMode/     testes automatizados
```

## Limitações atuais (honestas)

- **Visual de protótipo:** prédios, móveis, produtos e personagens são formas simples (placeholders). A lógica toda funciona, e os modelos 3D definitivos podem substituir as formas sem mexer no código.
- **Interface simples:** a UI é IMGUI, funcional e com escala para celular, mas visualmente simples. Pode ser trocada por UI Toolkit.
- **Personagens:** os clientes andam em linha reta entre pontos (sem NavMesh) e não têm animação.
- **Verificação:** o código foi compilado contra os assemblies do Unity 2021 e os testes rodaram fora do editor. **Ainda não foi aberto no Unity 6 de verdade**, então pode haver avisos ou pequenos ajustes de API na primeira abertura.
