# Negócio Fechado — Central de Vendas

Jogo 3D em primeira pessoa (Unity 6 · PC Windows e Android) em que você trabalha numa **central de telemarketing** de verdade.

Você chega no escritório da *Conecta Vendas*, senta na sua mesa e usa o **computador virtual** para ligar para clientes. Na ligação você **digita o que quer falar**, e quem responde é uma **IA** (Claude) interpretando uma pessoa real, com nome, personalidade, problema e orçamento. Seu trabalho é **convencer de forma honesta**: descobrir o que a pessoa precisa, oferecer o serviço certo, responder às objeções e fechar o preço. Os serviços são fictícios.

A **Supervisora Regina** ouve as suas ligações e dá nota. **Venda feita com mentira é cancelada** e a comissão é estornada.

## O que tem no jogo

- **Escritório 3D:**
  - recepção;
  - salão com 18 mesas e baias, com colegas falando ao telefone;
  - sala de vidro da supervisora;
  - sala de reunião;
  - copa com cafeteira;
  - banheiros;
  - TV com o ranking do dia.
- **Computador virtual:**
  - *Discador*: lista de pessoas para ligar hoje;
  - *Ligação*: chat em que você digita, com barras de interesse e confiança do cliente;
  - *CRM*: histórico e transcrições;
  - *Catálogo*: o que é verdade e o que **não** prometer;
  - *Metas*, *Loja* (melhorias) e *Configurações*.
- **8 serviços fictícios:** internet NetVeloz, seguro de celular Protege+, plano odontológico SorrisoBom, streaming CineMax, seguro de vida VidaTranquila, inglês FalaFácil, alarme ProtegeCasa e energia SolarCasa.
- **Clientes:** cada um tem nome, idade, cidade, profissão, uma de 5 personalidades (econômico, exigente, indeciso, apressado, desconfiado), um problema real e um orçamento. Alguns pediram para **não ser incomodados**, e respeitar isso conta pontos.
- **Registro de venda no CRM:** só funciona se o cliente realmente aceitou, pelo valor combinado e dentro do desconto autorizado. A comissão cai na hora.
- **Supervisora:** avalia 9 critérios (abertura, descoberta, oferta adequada, argumentação, objeções, preço, transparência, respeito e fechamento), com observações e dicas.
- **Carreira:** de Operador(a) Júnior a Gerente Comercial, com serviços liberados por nível, meta diária com bônus, 8 missões, energia (o café recupera) e melhorias (headset, cadeira, segundo monitor, leads qualificados, cafeteira).
- **Expediente:** das 9h às 18h, com salvamento automático no fim do dia.

## Como abrir

1. Instale o **Unity 6 LTS** com *Windows Build Support (IL2CPP)* e *Android Build Support*.
2. Crie um projeto **Universal 3D (URP)** e copie a pasta `Assets/_DealMaster` deste repositório para a pasta `Assets/` do projeto.
3. Menu **DealMaster → Configurar projeto**. Se ele avisar que mudou o *Active Input Handling*, reinicie o Unity.
4. Menu **DealMaster → Construir escritório**.
5. Aperte **Play**.

Sem a IA instalada, o jogo já funciona com o **cliente offline**, que entende o que você escreve por palavras-chave.

## Ativar a IA de verdade (Claude)

1. Instale o [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) (Package Manager → *Add package from git URL* → `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`).
2. No menu **NuGet → Manage NuGet Packages**, procure **Anthropic** e instale (versão 13.6.0 ou mais nova).
3. Menu **DealMaster → Detectar SDK da IA (Anthropic)**. Isso liga a IA no projeto.
4. Crie uma chave em [console.anthropic.com](https://console.anthropic.com).
5. No jogo: sente na sua mesa → **Configurações**. Cole a chave, deixe "Usar IA de verdade" marcado e salve.

Como funciona:
- **Custo:** cada fala do cliente e cada avaliação da supervisora é uma chamada à API, que é cobrada. O modelo padrão é o **Claude Opus 5.5**; nas Configurações dá para trocar pelo Sonnet 5.5 ou pelo Haiku 5.5 (mais barato).
- **Se a IA falhar:** sem internet, com chave errada, com limite de uso atingido ou se a IA recusar, aquela fala é respondida pelo cliente offline e o jogo avisa.
- **Sua chave:** fica salva só no seu aparelho. **Nunca distribua um build com a sua chave dentro.**

## Usar os seus modelos 3D

1. Importe a sua pasta de assets para o projeto.
2. Menu **DealMaster → Lista de assets do escritório**.
3. Arraste os prefabs para os campos: mesa, cadeira, monitor, teclado, headset, divisória, colega, supervisora, recepcionista, sofá, planta, cafeteira, geladeira, mesa de reunião, cadeira, bebedouro, impressora e TV. Cada um tem **escala**, **giro** e **altura** para ajustar.
4. Menu **DealMaster → Construir escritório** de novo. O que ficou vazio continua com a forma simples.

## Controles

| | PC | Celular |
|---|---|---|
| Andar | WASD (Shift corre) | Joystick à esquerda |
| Olhar | Mouse | Arrastar à direita |
| Interagir (sentar, falar com a supervisora, café) | E ou clique | Botão "Interagir" |
| Falar na ligação | Digite e aperte Enter | Teclado do celular ou frases prontas |
| Soltar o mouse / fechar tela | Esc | Botão "Fechar" |

## Testes

No Unity: **Window → General → Test Runner → EditMode → Run All**. A lógica do jogo não depende de cena:
- analisador de mensagens;
- cliente offline;
- ligação;
- registro de venda;
- supervisora;
- carreira;
- jogo completo de um dia;
- leitura das respostas da IA (esses testes só rodam com o SDK instalado).

## Gerar os executáveis

- **Windows:** `DealMaster → Build Windows` gera `Builds/Windows/DealMaster.exe`.
- **Android:** `DealMaster → Build Android (APK)` gera `Builds/Android/DealMaster.apk`.

## Limitações atuais

- **Visual:** sem os seus assets, o escritório e as pessoas são formas simples. Os colegas não têm animação de esqueleto, só um balanço e balões de fala.
- **Interface:** é IMGUI, funcional e com escala para celular, mas visualmente simples.
- **Cliente offline:** entende por palavras-chave. Frases muito diferentes das esperadas podem não ser entendidas. Com a IA de verdade, a conversa é livre.
- **Verificação:** o código foi compilado contra os assemblies do Unity 2021 e contra o SDK oficial da Anthropic, e os testes rodaram fora do editor. **Ainda não foi aberto no Unity 6 de verdade** e a IA ainda não foi testada com uma chave real.
