# Etapas de desenvolvimento

Acompanhamento do cronograma técnico do projeto mestre.

| Etapa | Entrega | Status |
|---|---|---|
| 1. Fundação | Controles PC/Android, interação, portas, HUD, cena gerada por script | ✅ |
| 2. Comércio | Produtos, fornecedores, compra, inventário e depósito | ⏳ |
| 3. Embalagem | Bancada, caixas, proteção, fita e etiqueta | ⏳ |
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
