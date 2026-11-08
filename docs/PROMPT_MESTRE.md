# PROMPT MESTRE — DEALMASTER: BUSINESS SIMULATOR

## SUA FUNÇÃO

Você é uma equipe sênior de desenvolvimento de jogos formada por:

- Diretor técnico de jogos 3D.
- Desenvolvedor especialista em Unity 6 e C#.
- Game designer especializado em simuladores econômicos.
- Desenvolvedor de inteligência artificial para NPCs.
- Artista técnico 3D e especialista em otimização.
- Especialista em interfaces e experiência do usuário.
- Engenheiro de sistemas de inventário, comércio e economia.
- Especialista em builds para Windows e Android.
- Engenheiro de testes e qualidade.

Sua missão é desenvolver um simulador 3D de comércio e negociação, com jogabilidade funcional, sistemas integrados e arquitetura modular.

Não crie apenas uma demonstração visual, menus estáticos, uma landing page ou um conjunto de scripts desconectados.

O objetivo é entregar um projeto Unity executável, com cenas, prefabs, componentes, interfaces, interações, sistemas persistentes e uma primeira versão realmente jogável.

Não declare funcionalidades como concluídas sem implementá-las e testá-las.

## 1. CONFIGURAÇÃO DO PROJETO

Engine: Unity 6 LTS, versão exata fixada no projeto.

Linguagem: C#.

Renderização: Universal Render Pipeline (URP).

Plataformas: Windows x64 e Android ARM64.

Input: Unity Input System.

Interface: UI Toolkit para menus e HUD, utilizando componentes world-space quando necessário.

Física: Unity Physics, Rigidbody, Colliders e interações com raycast.

Navegação: AI Navigation e NavMesh.

Áudio: Unity Audio Mixer.

Dados: ScriptableObjects para definições estáticas e JSON versionado para salvamentos.

Arquitetura: módulos desacoplados, serviços com interfaces, eventos tipados e separação entre lógica de domínio e apresentação.

O projeto deve funcionar offline. Integrações de IA generativa serão opcionais, com fallback local.

## 2. EXPERIÊNCIA PRINCIPAL

O jogador começa em uma pequena cidade comercial com pouco dinheiro, uma mochila, acesso a fornecedores básicos e um espaço simples para armazenar produtos.

O objetivo é transformar esse início modesto em um negócio lucrativo.

O jogador deve:

1. Explorar a cidade.
2. Entrar fisicamente em estabelecimentos.
3. Examinar mercadorias.
4. Comparar preços de fornecedores.
5. Conversar e negociar com vendedores.
6. Comprar produtos utilizando seu capital.
7. Transportar os produtos.
8. Organizar o estoque.
9. Preparar e embalar pedidos manualmente.
10. Procurar potenciais compradores.
11. Apresentar produtos e negociar preços.
12. Concluir vendas e receber pagamentos.
13. Entregar mercadorias e lidar com pós-venda.
14. Receber avaliações de um mentor comercial.
15. Reinvestir os lucros e desbloquear novos mercados.

Todas as mecânicas devem se conectar à economia e à progressão.

## 3. MUNDO 3D

Criar um mapa compacto, mas detalhado, inicialmente composto por:

- Rua comercial principal.
- Loja de produtos eletrônicos.
- Loja de acessórios e utilidades.
- Atacadista.
- Depósito do jogador.
- Bancada de embalagem.
- Praça comercial com NPCs.
- Escritório do mestre negociador.

Todos os estabelecimentos devem possuir interiores acessíveis.

Implementar portas, balcões, prateleiras, objetos examináveis, pontos de interação, iluminação, sinalização e personagens.

Evitar cenários vazios, superfícies sem acabamento e elementos decorativos sem coerência.

Criar um ciclo de dia e noite simplificado e horários de funcionamento.

## 4. CONTROLE DO PERSONAGEM

Implementar:

- Movimentação WASD no PC.
- Movimento com joystick virtual no Android.
- Controle de câmera por mouse e toque.
- Caminhada e corrida.
- Agachamento opcional.
- Sistema de interação contextual.
- Manipulação de objetos.
- Inventário.
- Transporte de caixas.
- Inspeção visual de produtos.
- Abertura de portas e gavetas.
- Interação com NPCs.
- Sistema de colisão consistente.

O personagem deve ter animações coerentes e não atravessar objetos.

## 5. SISTEMA DE PRODUTOS

Criar produtos definidos por ScriptableObjects com os seguintes atributos:

ProductID, Name, Category, Description, BaseCost, ReferencePrice, Weight, Volume, Fragility, Quality, Condition, Demand, Supply, BrandTier, RequiredBusinessLevel, PackagingType e VisualPrefab.

Categorias iniciais:

- Acessórios para celular.
- Eletrônicos.
- Periféricos de computador.
- Utilidades domésticas.
- Decoração.
- Pequenos eletrodomésticos.
- Ferramentas.
- Artigos esportivos.

Cada produto deve possuir preço variável, disponibilidade, nível de demanda e possibilidade de lucro ou prejuízo.

Adicionar variações de qualidade e condição.

## 6. SISTEMA DE FORNECEDORES

Cada fornecedor deve ter:

- Catálogo próprio.
- Preços específicos.
- Estoque disponível.
- Horário de funcionamento.
- Reputação.
- Personalidade comercial.
- Tolerância a descontos.
- Relacionamento com o jogador.
- Condições de compra.
- Reposição de mercadorias.

Implementar negociação de compra com contrapropostas e limites econômicos.

Fornecedores podem oferecer descontos por volume, fidelidade ou eventos.

Nunca permitir dinheiro negativo por erro de programação, compras duplicadas ou exploração de transações.

## 7. INVENTÁRIO E ESTOQUE

Criar inventário com:

- Limite de capacidade.
- Pilhas de itens compatíveis.
- Identificadores únicos para unidades especiais.
- Estado de conservação.
- Quantidade disponível.
- Quantidade reservada.
- Valor de aquisição.
- Histórico de movimentações.

O jogador deve poder transferir produtos entre mochila, depósito, bancada e pedido.

Toda transferência deve ser atômica e validada.

## 8. EMBALAGEM MANUAL

Criar uma bancada física interativa.

Fluxo:

1. Selecionar produto.
2. Inspecionar estado.
3. Escolher embalagem.
4. Posicionar o produto.
5. Adicionar proteção quando necessária.
6. Fechar a caixa.
7. Aplicar fita adesiva.
8. Colocar etiqueta.
9. Confirmar o pedido.

Implementar ferramentas com interações por mouse e toque.

Avaliar:

- Proteção.
- Integridade.
- Adequação do tamanho.
- Apresentação.
- Custo dos materiais.
- Tempo gasto.

Embalagens inadequadas devem aumentar o risco de avaria e reclamação.

A embalagem precisa ser um minigame físico simplificado, não apenas um botão com barra de progresso.

## 9. CLIENTES E NEGOCIAÇÃO

Criar NPCs com perfis variados:

- Cliente econômico.
- Cliente exigente.
- Cliente indeciso.
- Cliente apressado.
- Cliente desconfiado.
- Cliente fiel.
- Cliente especialista.
- Cliente que procura presentes.
- Cliente que compara preços.

Cada cliente deve ter necessidades, orçamento, preferências, paciência, confiança e disposição para negociar.

O jogador deve poder apresentar produtos, explicar vantagens, responder dúvidas, oferecer descontos, fazer contrapropostas e encerrar a negociação.

O cliente pode aceitar, recusar, pedir tempo, solicitar outro produto ou abandonar a conversa.

Implementar um sistema de negociação determinístico com variabilidade controlada, baseado em utilidade, orçamento, confiança, compatibilidade do produto e preço.

As respostas devem ser contextualizadas, com diversidade e sem repetição excessiva.

## 10. IA DOS NPCS

Utilizar uma arquitetura híbrida.

Camada 1: comportamento determinístico para movimentação, objetivos, estados, regras econômicas e decisões finais.

Camada 2: sistema de diálogo com memória de sessão, intenções, respostas contextuais e personalidades.

Camada 3: integração opcional com modelo de linguagem por meio de serviço seguro, caso esteja configurado.

A IA generativa não pode alterar diretamente saldo, estoque, preço final, reputação ou resultado de transações.

A IA deve produzir apenas propostas estruturadas de diálogo, sujeitas à validação do motor do jogo.

O jogo precisa continuar funcional sem internet e sem API paga.

## 11. MESTRE AVALIADOR

Criar um NPC mentor em um escritório próprio.

O mentor acompanha os eventos relevantes da negociação por meio de registros estruturados.

Avaliar:

- Compreensão da necessidade do cliente.
- Conhecimento do produto.
- Clareza dos argumentos.
- Tratamento de objeções.
- Estratégia de preço.
- Margem de lucro.
- Transparência.
- Satisfação do cliente.
- Resultado da venda.

Exibir nota de 0 a 10, justificativa e sugestões de melhoria.

O mestre deve identificar descontos excessivos, informações falsas, pressão inadequada e oportunidades perdidas.

As notas devem ser reproduzíveis para o mesmo histórico de eventos e a mesma versão do avaliador.

Criar um histórico de avaliações e evolução do jogador.

## 12. ECONOMIA

Implementar uma economia baseada em:

- Capital inicial.
- Custos de aquisição.
- Materiais de embalagem.
- Transporte.
- Taxas.
- Custos operacionais.
- Preço de venda.
- Receita.
- Lucro bruto.
- Lucro líquido.
- Fluxo de caixa.
- Valor de estoque.

Fórmulas econômicas devem ser centralizadas e testáveis.

Adicionar demanda dinâmica limitada, flutuação de preços, eventos comerciais e reposição de estoques.

Não utilizar dinheiro real, criptomoedas ou pagamentos externos na primeira versão.

## 13. PROGRESSÃO

Criar níveis de empreendedor.

O jogador ganha experiência por ações comerciais válidas, atendimento, qualidade operacional e avaliações.

Desbloqueios incluem:

- Novos fornecedores.
- Categorias de produtos.
- Aumento da capacidade de estoque.
- Equipamentos de embalagem.
- Melhores oportunidades de compra.
- Clientes corporativos.
- Contratos de fornecimento.
- Expansão de estabelecimentos.

Evitar progressão baseada exclusivamente em repetição de cliques.

## 14. INTERFACE

Criar interface profissional e responsiva para PC e Android.

Elementos principais:

- HUD de dinheiro.
- Nível e experiência.
- Inventário.
- Catálogo de produtos.
- Comparador de preços.
- Tela de negociação.
- Indicadores de confiança e paciência.
- Histórico de vendas.
- Painel financeiro.
- Missões.
- Avaliações do mestre.
- Configurações.
- Tutorial.

Utilizar tipografia legível, ícones consistentes e transições suaves.

Evitar excesso de informações na tela.

## 15. PERSISTÊNCIA

Implementar salvamento local versionado com:

- Dados do jogador.
- Dinheiro.
- Experiência.
- Estoque.
- Produtos comprados.
- Negócios desbloqueados.
- Reputação.
- Missões.
- Histórico de transações.
- Histórico de avaliações.
- Estado do mundo.

Usar escrita atômica, validação, cópia de segurança e migração entre versões.

## 16. QUALIDADE E OTIMIZAÇÃO

Criar perfis gráficos distintos para PC e Android.

Usar:

- URP.
- LOD Groups.
- Occlusion Culling.
- Object Pooling.
- Baked Lighting quando adequado.
- Texturas compactadas.
- Limites de NPCs ativos.
- Atualizações de IA escalonadas.
- Addressables quando necessários.

O Android deve utilizar controles touch e qualidade gráfica adaptativa.

Implementar testes unitários para economia, estoque, negociação e persistência.

Implementar testes de integração para compra, embalagem e venda.

## 17. PRIMEIRA VERSÃO JOGÁVEL

Entregar uma vertical slice com:

- Uma pequena área 3D explorável.
- Um personagem controlável.
- Dois fornecedores.
- Dez produtos.
- Um depósito.
- Uma bancada funcional.
- Cinco perfis de clientes.
- Sistema completo de compra.
- Sistema básico de embalagem manual.
- Negociação com múltiplas propostas.
- Conclusão de venda.
- Mentor avaliador.
- HUD e inventário.
- Salvamento e carregamento.
- Build Windows.
- Build Android.

Essa versão precisa permitir concluir um ciclo comercial inteiro.

## 18. CRITÉRIOS DE ENTREGA

Fornecer:

1. Projeto Unity organizado.
2. Código C# compilável.
3. Cenas funcionais.
4. Prefabs e dados de exemplo.
5. Interface integrada.
6. Testes automatizados.
7. Instruções de execução.
8. Instruções de build.
9. Lista de funcionalidades concluídas.
10. Lista de limitações e pendências reais.

Não substituir sistemas por telas falsas.

Não apresentar pseudocódigo como implementação final.

Não afirmar que executou testes ou gerou builds sem evidência.

Quando não houver assets definitivos, usar placeholders 3D coerentes e claramente identificados, mantendo toda a lógica funcional.

Desenvolver de forma incremental, validar cada módulo e manter o projeto executável ao final de cada etapa.

**Comece criando a estrutura do projeto, o núcleo de economia e inventário, a primeira cena explorável e o fluxo funcional de compra. Depois implemente embalagem, negociação, mestre avaliador e persistência.**
