# Requirements Document

## Introduction

Esta especificação cobre a **Fase 1 — Combat Foundation** (o backlog P0 descrito na seção 54 do documento `Docs/Tech-Guy_Gameplay_Rework.md`, detalhado nas seções 3, 4, 5, 6, 7, 44 e nas partes de combate das seções 34 e 57) da reestruturação de gameplay do action-roguelite **Tech-Guy** (Unity 3D, C#).

O objetivo desta fase é refazer o **núcleo do combate** para que ele deixe de ser automático e rígido e passe a ser responsivo e controlado pelo jogador, sem descartar os sistemas existentes.

Esta fase entrega uma **fundação de combate compartilhável (Nucleo_Compartilhado)** e a **valida na Manopla** — a arma do vertical slice (doc §41) — sobre seu ataque básico, suas quatro habilidades Q/W/E/R e seu dash. As mecânicas do Nucleo_Compartilhado (estado de ação, autorização de transição, buffer de input, identificação de execução e despacho de eventos) são genéricas e projetadas para serem reaproveitadas pelas três armas, pois todas compartilham `CharControlScript` e `AbilityHolder`.

A migração e o ajuste fino de **Arco** e **Lança** são adiados para fases posteriores. Nesta fase, Arco e Lança devem permanecer **funcionais** — equipar, mover, atacar, usar habilidades e dash — podendo manter o fluxo legado ou operar por meio de **adaptadores de compatibilidade (Adaptador_de_Compatibilidade)** sobre o Nucleo_Compartilhado. Requisitos de comportamento **específicos da Manopla** são permitidos fora do Nucleo_Compartilhado (p.ex. socos, avanços, geração de energia Asura). Comportamento específico de arma pode residir em definições de arma, executores ou adaptadores; apenas o Nucleo_Compartilhado é caminho único.

Esta especificação cobre exatamente sete áreas:

1. Remoção do auto-combate persistente (doc §3.1).
2. Fases de ação startup → active → recovery (doc §4.2).
3. Categorias de compromisso de habilidade Fluid / Committed / Channel (doc §5).
4. Janelas de cancelamento por destino (cancel rules) (doc §6).
5. Buffer de input (doc §7).
6. Revisão do dash como ferramenta defensiva de cancelamento (doc §44 P0 "revisar dash").
7. Fundação de feedback de acerto: hit-stop e reação básica de inimigo (doc §34 perguntas 1-2, §44 "hit-stop" / "reação básica dos inimigos").

### Fora de escopo (adiado para fases posteriores)

Os itens abaixo **não** possuem requisitos neste documento e são explicitamente adiados:

- Experimento de remoção de mana universal — Fase P1 (doc §8). A mana e a energia Asura permanecem sob as condições atuais nesta fase.
- Pilar de postura/stagger e taxonomia de reações (stagger, knockback, knockdown, launch, pull, stance break) e feedback pesado de stance break (flash, VFX, launch, hit-stop específico de quebra) — Fase 2 (doc §9, §10). Esta fase inclui **apenas** o hit-stop mínimo e a reação básica de inimigo necessários para o combate parecer sólido, e **não** amplia nem remove reações de postura existentes (p.ex. o stun do E da Manopla).
- Identidade e reworks de Arco e Lança — Fases 6 e 7 (doc §13, §14, §49, §50). A migração e o ajuste fino de Arco e Lança ao Nucleo_Compartilhado são adiados.
- Manopla 2.0 e trabalho de boons de arma — Fase 3 (doc §46).
- Estrutura de run, rotas, HP como recurso, tipos de sala — Fase 4 (doc §24–§27, §47).
- Sistema de Protocols — Fase 5 (doc §48).
- Meta progressão, geração procedural, reorganização da taxonomia de boons — Fases P3+ (doc §51, §52).

### Compatibilidade retroativa

As mudanças desta fase devem **integrar-se** aos canais de evento e APIs existentes por **contrato**, preservando assinaturas e o comportamento observável de que os consumidores dependem; a ordenação interna do novo sistema pode mudar desde que a semântica observável seja preservada ou explicitamente adaptada. Especificamente:

- `CharControlScript.BasicAttackPerformed`, `CharControlScript.CancelCombo()` e `CharControlScript.RequireAttackRelease()` continuam existindo e funcionando.
- `AbilityHolder.AbilityUsed`, `AbilityHolder.AbilityRejected`, `AbilityHolder.AttackHitsResolved`, `AbilityHolder.IsCasting`, `AbilityHolder.MovementAllowedWhileCasting`, `AbilityHolder.ReduceCooldowns(float)` e `HookBus.OnBasicHit` continuam existindo e disparando conforme os boons já esperam.
- Adaptadores de compatibilidade são permitidos e devem ser documentados.

## Glossary

- **Jogador (Player)**: o personagem controlado, orquestrado por `CharControlScript` (locomoção/ataque básico) e `AbilityHolder` (habilidades Q/W/E/R e dash).
- **Sistema_de_Combate (Combat_System)**: conjunto coordenado de `CharControlScript`, `AbilityHolder` e classes auxiliares puras (sem `MonoBehaviour`) que esta fase introduz para fases, janelas de cancelamento e buffer. É o sujeito da maioria dos requisitos.
- **Nucleo_Compartilhado (Shared_Core)**: subconjunto do Sistema_de_Combate que é caminho único de código, independente de arma: estado de ação, avanço da linha do tempo, autorização de transição/cancelamento, Buffer_de_Input, identificação de execução e despacho de eventos.
- **Adaptador_de_Compatibilidade (Compatibility_Adapter)**: componente que conecta comportamento legado ou específico de arma (Arco, Lança) ao Nucleo_Compartilhado sem forçar migração completa nesta fase.
- **Ação_Ofensiva (Offensive_Action)**: um ataque básico ou uma das quatro habilidades (Q/W/E/R). Cada ação ofensiva possui uma linha do tempo de fases (Startup, Active, Recovery). Para ações de duração fixa, a linha do tempo é normalizada em `[0, 1]`.
- **Startup**: a primeira fase da linha do tempo de uma ação ofensiva, de preparação/antecipação. PODE conter VFX, SFX e deslocamento de antecipação, mas nenhum ImpactEvent nem ImpactWindow dessa ação está ativo.
- **Active (Ativa)**: a fase intermediária em que a ação pode emitir um ou mais ImpactEvents e abrir uma ou mais ImpactWindows.
- **Recovery (Recuperação)**: a fase final, posterior aos impactos, durante a qual parte dos cancelamentos é permitida e efeitos já emitidos podem continuar conforme sua própria política de vida.
- **ImpactEvent (Evento_de_Impacto)**: uma emissão/impacto discreto de uma Ação_Ofensiva — p.ex. um soco, um disparo, uma explosão, um pulso. Cada ImpactEvent pode referenciar os dados de que precisa (dano, postura, hitbox/projétil, VFX, SFX, deslocamento, perfil de Hit_Stop) sem duplicar dados que as definições existentes já contêm.
- **ImpactWindow (Janela_de_Impacto)**: uma janela de detecção contínua dentro da fase Active — p.ex. uma varredura (sweep) — durante a qual contatos podem ser resolvidos.
- **Compromisso (Commitment)**: o grau em que uma ação prende o jogador (bloqueando movimento e/ou novas ações) durante sua execução.
- **Categoria_de_Compromisso (Commitment_Category)**: perfil de intenção de uma ação — `Fluid`, `Committed` ou `Channel` — que fornece valores padrão de mobilidade e cancelabilidade. Mobilidade e cancelamento concretos derivam da configuração por ação/por fase, não de relações rígidas entre categorias.
- **Fluid**: perfil de ações rápidas e responsivas (padrões de recovery curto, movimento parcial permitido, cancel cedo).
- **Committed**: perfil de ações fortes com startup legível e janela de cancel limitada.
- **Channel**: perfil de ações sustentadas que continuam enquanto ativas, canceláveis por dash quando a CancelRule de dash o permitir.
- **Modo_de_Terminacao_de_Channel (Channel_Termination_Mode)**: forma como uma Ação_Ofensiva Channel encerra sua fase Active — `Hold` (encerra ao soltar o controle, conforme sua regra de Recovery), `Timed` (uma ativação executa uma sequência de duração fixa) ou `Condition` (encerra por condição explícita). Nem todos os modos precisam ser implementados nesta fase.
- **CancelRule (Regra_de_Cancelamento)**: regra independente por destino que define para um destino de cancelamento (`Dash`, `Basic` ou `Skill`) um intervalo `[Start, End]` (frações da fase/linha do tempo) durante o qual o cancelamento naquele destino é permitido. A ausência de CancelRule para um destino significa que o cancelamento naquele destino NÃO é permitido.
- **Destino_de_Cancelamento (Cancel_Target)**: o tipo de ação para o qual se tenta cancelar — `Dash`, `Basic` ou `Skill`.
- **Resolvedor_de_Cancelamento (Cancel_Resolver)**: lógica única e compartilhada que avalia as CancelRules e a disponibilidade do destino para autorizar ou rejeitar uma transição; usada por todas as categorias e também pelo dash.
- **Buffer_de_Input (Input_Buffer)**: mecanismo de slot único que armazena temporariamente a intenção de um comando (básico, dash, Q, W, E, R) cujo único impedimento é temporal, por uma `BufferDuration` configurável, para dispará-lo assim que a janela aplicável abrir ou a ação atual terminar.
- **BufferDuration**: valor serializado e ajustável (em segundos) que define por quanto tempo uma intenção bufferizada permanece válida. Valor inicial 120 ms; a faixa 100–180 ms é uma sugestão, não um limite rígido; 0 desativa o buffer.
- **Janela_de_i-frames (Iframe_Window)**: intervalo configurável, independente da duração de deslocamento, durante o qual o Jogador é imune a dano ao executar um dash.
- **Duracao_de_Deslocamento (Displacement_Duration)**: duração do movimento físico do dash, parâmetro independente da Janela_de_i-frames.
- **Hit_Stop**: pausa muito curta e configurável aplicada em resposta a um ImpactEvent (ou grupo de impactos simultâneos), para dar peso ao golpe.
- **Reação_de_Acerto_Básica (Basic_Hit_Reaction)**: reação visível mínima e cosmética do inimigo ao sofrer um acerto (p.ex. um breve "flinch"/pisca de recuo), sem interromper a IA do inimigo e distinta de qualquer reação mecânica de quebra de postura (reservada à Fase 2).
- **Alvo_Interno (Internal_Target)**: referência de inimigo mantida pelo Sistema_de_Combate apenas para tracking e seleção de animação. Sua existência, por si só, não autoriza ataque nem perseguição indefinida.
- **Intencao_de_Comando (Command_Intent)**: a intenção associada a um input, incluindo o alvo ou direção explicitamente derivado do cursor/input no momento da emissão.
- **Ataque_Alvo (Targeted_Attack)**: comando que seleciona um inimigo elegível sob o cursor, aproxima o Jogador e executa um ataque.
- **Ataque_Direcional (Directional_Attack)**: comando que executa um ataque básico parado na direção do cursor, sem auto-aproximação e sem selecionar inimigos (o Shift+ataque existente descrito no README).
- **Ordem_de_Movimento (Move_Order)**: comando que move o Jogador para um ponto do terreno e substitui qualquer intenção pendente de aproximação/ataque.
- **Toque_de_Ataque (Attack_Tap)**: um acionamento discreto do controle de ataque (pressionar e soltar, sem segurar) que autoriza exatamente uma execução.
- **Segurar_Ataque (Attack_Hold)**: manter o controle de ataque pressionado continuamente, autorizando repetições enquanto o controle estiver pressionado e a intenção/alvo permanecer válida.
- **Alcance_de_Ataque (Attack_Range)**: distância máxima configurada dentro da qual o Jogador pode conectar um ataque em um inimigo.
- **Alcance_de_Deteccao (Detection_Range)**: distância configurada dentro da qual um inimigo é considerado elegível para seleção pelo cursor.
- **Intervalo_de_Ataque (Attack_Interval)**: tempo configurado entre ataques básicos consecutivos durante Segurar_Ataque.
- **Zona_Morta_de_Movimento (Movement_Deadzone)**: raio configurado em torno do avatar dentro do qual um clique não gera Ordem_de_Movimento, preservando a Intencao_de_Comando corrente.
- **startupEnd**: fronteira serializada por Ação_Ofensiva de duração fixa, no intervalo fechado de 0.0 a 1.0, que marca o fim da fase Startup e o início da fase Active.
- **activeEnd**: fronteira serializada por Ação_Ofensiva de duração fixa, no intervalo fechado de 0.0 a 1.0, que marca o fim da fase Active e o início da fase Recovery.
- **Quadro_de_Simulacao (Simulation_Frame)**: um passo de `FixedUpdate` da simulação Unity. Salvo indicação em contrário, "dentro de um quadro" nos requisitos de ativação/desativação de impacto e de devolução de controle refere-se a um Quadro_de_Simulacao (FixedUpdate).

## Requirements

### Requirement 1: Remoção do auto-combate persistente

**User Story:** Como jogador, quero que cada ataque seja uma ação que eu executo, derivada do cursor/input, não uma ordem persistente nem uma mira automática por proximidade, para que eu controle o momento-a-momento do combate.

#### Acceptance Criteria

1. WHEN o jogador emite um Ataque_Alvo sobre um inimigo elegível sob o cursor dentro do Alcance_de_Deteccao, THE Sistema_de_Combate SHALL registrar esse inimigo como alvo da Intencao_de_Comando, mover o Jogador até o Alcance_de_Ataque e executar exatamente um ataque.
2. WHEN o jogador emite um Ataque_Direcional, THE Sistema_de_Combate SHALL executar um ataque básico parado na direção do cursor, sem auto-aproximação e sem selecionar inimigos.
3. WHILE o jogador mantém Segurar_Ataque e a Intencao_de_Comando e o alvo permanecem válidos, THE Sistema_de_Combate SHALL executar ataques repetidos respeitando o Intervalo_de_Ataque configurado.
4. WHEN o jogador solta o controle após Segurar_Ataque, THE Sistema_de_Combate SHALL remover a autorização de repetição e a aproximação sustentada antes do próximo ciclo de ataque, sem interromper um golpe já iniciado.
5. WHEN o jogador solta o controle após um Toque_de_Ataque, THE Sistema_de_Combate SHALL preservar a única execução já autorizada por esse toque.
6. WHEN um Toque_de_Ataque executa um ataque e nenhum input de ataque adicional é emitido, THE Sistema_de_Combate SHALL permanecer sem executar ataques adicionais.
7. THE Sistema_de_Combate SHALL manter um Alvo_Interno exclusivamente para tracking e seleção de animação, e a mera existência de um Alvo_Interno SHALL NOT autorizar um ataque nem uma perseguição indefinida.
8. WHEN um ataque é executado com sucesso, THE Sistema_de_Combate SHALL disparar o evento `BasicAttackPerformed` conforme o contrato observável existente para esse tipo de ataque.
9. WHEN o jogador emite uma Ordem_de_Movimento para um ponto do terreno fora da Zona_Morta_de_Movimento, THE Sistema_de_Combate SHALL mover o Jogador para esse ponto e substituir qualquer Intencao_de_Comando de aproximação/ataque pendente.
10. WHEN o jogador clica sobre o próprio avatar ou dentro da Zona_Morta_de_Movimento, THE Sistema_de_Combate SHALL preservar a Intencao_de_Comando corrente sem emitir nova Ordem_de_Movimento.
11. IF o jogador emite um Ataque_Alvo e nenhum inimigo elegível está sob o cursor dentro do Alcance_de_Deteccao, THEN THE Sistema_de_Combate SHALL não executar ataque, não disparar `BasicAttackPerformed` e preservar o estado atual, sem selecionar um inimigo por proximidade.
12. IF o alvo da Intencao_de_Comando deixa de ser elegível (morte, saída de alcance ou perda de linha de visão) antes da execução, THEN THE Sistema_de_Combate SHALL invalidar a Intencao_de_Comando associada e limpar o Alvo_Interno, sem selecionar outro inimigo por proximidade.

### Requirement 2: Fases de ação (startup → active → recovery)

**User Story:** Como designer de combate, quero que toda ação ofensiva seja dividida em startup, active e recovery, para que o compromisso de cada ação seja intencional e legível, enquanto impactos discretos são governados por eventos.

#### Acceptance Criteria

1. THE Sistema_de_Combate SHALL modelar cada Ação_Ofensiva com uma linha do tempo de fases dividida em exatamente três fases contíguas e não sobrepostas, na ordem Startup, Active e Recovery, onde as fases governam o compromisso do Jogador.
2. THE Sistema_de_Combate SHALL expor, para cada Ação_Ofensiva de duração fixa, duas fronteiras serializadas e ajustáveis, startupEnd e activeEnd, cada uma no intervalo fechado de 0.0 a 1.0, onde Startup ocupa [0.0, startupEnd), Active ocupa [startupEnd, activeEnd) e Recovery ocupa [activeEnd, 1.0].
3. IF uma Ação_Ofensiva de duração fixa possui fronteiras serializadas que violam a restrição 0.0 <= startupEnd <= activeEnd <= 1.0, THEN THE Sistema_de_Combate SHALL rejeitar os valores durante a validação em editor com uma mensagem clara indicando a fronteira inválida e aplicar um fallback seguro em runtime, sem mutar silenciosamente os dados de design.
4. WHILE uma Ação_Ofensiva está na fase Startup, THE Sistema_de_Combate SHALL manter sem emitir qualquer ImpactEvent e sem abrir qualquer ImpactWindow dessa ação, permitindo VFX, SFX e deslocamento de antecipação.
5. WHEN uma Ação_Ofensiva está na fase Active, THE Sistema_de_Combate SHALL emitir cada ImpactEvent e abrir cada ImpactWindow dessa ação nos instantes definidos por sua configuração, dentro de no máximo um Quadro_de_Simulacao após o instante configurado, suportando o kit da Manopla (Q: avanço + dois socos; W: sequência de socos + finalizador; E: dois impactos; R: rajada + finalizador circular).
6. WHEN uma Ação_Ofensiva entra na fase Recovery, THE Sistema_de_Combate SHALL encerrar as ImpactWindows ainda abertas dessa ação e deixar de emitir ImpactEvents futuros dessa ação dentro de no máximo um Quadro_de_Simulacao, enquanto efeitos já emitidos (projéteis, zonas persistentes) continuam conforme sua própria política de vida.
7. WHEN uma Ação_Ofensiva de duração fixa alcança o fim da fase Recovery sem ser cancelada, THE Sistema_de_Combate SHALL devolver ao Jogador, dentro de no máximo um Quadro_de_Simulacao, o controle total de locomoção e a permissão para iniciar novas ações.
8. THE Sistema_de_Combate SHALL migrar e validar o modelo de três fases nas cinco Ações_Ofensivas da Manopla (ataque básico e habilidades Q, W, E e R) e no dash da Manopla; Arco e Lança SHALL permanecer funcionais (equipar, mover, atacar, usar habilidades e dash), por fluxo legado ou Adaptador_de_Compatibilidade, sem regressão nos caminhos afetados; a migração e o ajuste fino completos de Arco e Lança NÃO são exigidos para concluir esta fase.
9. IF uma mudança no Nucleo_Compartilhado altera o comportamento de Arco ou Lança, THEN THE Sistema_de_Combate SHALL documentar a alteração e exigir uma verificação de regressão proporcional ao caminho afetado.

### Requirement 3: Categorias de compromisso de habilidade

**User Story:** Como designer de combate, quero que cada habilidade declare uma categoria de compromisso como perfil de intenção, para que o peso e a cancelabilidade sejam definidos por dado configurável, não por relações rígidas entre categorias.

#### Acceptance Criteria

1. THE Sistema_de_Combate SHALL permitir que cada Ação_Ofensiva declare exatamente uma Categoria_de_Compromisso entre Fluid, Committed e Channel, usada como perfil de valores padrão de mobilidade e cancelabilidade.
2. THE Sistema_de_Combate SHALL derivar a mobilidade e os cancelamentos efetivos de uma Ação_Ofensiva de sua configuração concreta por ação e por fase, incluindo uma fração de velocidade de movimento serializada e ajustável no intervalo de 0 a 1 por fase quando aplicável.
3. THE Sistema_de_Combate SHALL avaliar o cancelamento de toda Ação_Ofensiva, incluindo o dash, por meio do mesmo Resolvedor_de_Cancelamento, sem relações rígidas obrigatórias de ordenação entre categorias.
4. WHERE uma Ação_Ofensiva é declarada Committed, THE Sistema_de_Combate SHALL aplicar como padrão o comprometimento do Jogador durante as fases Startup e Active (bloqueando locomoção e início de novas ações) e CancelRules limitadas à fase Recovery, salvo configuração concreta em contrário.
5. WHERE uma Ação_Ofensiva é declarada Channel, THE Sistema_de_Combate SHALL suportar um Modo_de_Terminacao_de_Channel entre Hold, Timed e Condition, não sendo exigida a implementação dos três modos nesta fase, e Hold NÃO SHALL ser um requisito global de todas as ações Channel.
6. WHERE uma Ação_Ofensiva Channel usa o modo Hold, WHEN o controle que a disparou é solto, THE Sistema_de_Combate SHALL encerrar a fase Active conforme a regra de Recovery dessa ação.
7. WHERE uma Ação_Ofensiva Channel possui duração variável, THE Sistema_de_Combate SHALL representar Startup, Active e Recovery sem depender de uma duração total conhecida, permitindo que a posição de cancelamento use fase mais progresso/tempo local da fase, enquanto a linha do tempo global [0,1] permanece válida para ações de duração fixa.
8. WHERE uma Ação_Ofensiva Channel é cancelável por dash durante toda a fase Active, THE Sistema_de_Combate SHALL exigir uma CancelRule de destino Dash que cubra essa fase, sem exceção oculta no controlador.
9. THE Sistema_de_Combate SHALL preservar o controle atual da Rajada Asura (R da Manopla) até uma mudança de design deliberada.
10. IF uma Ação_Ofensiva não possui Categoria_de_Compromisso declarada, THEN THE Sistema_de_Combate SHALL resolver sua categoria como Committed como salvaguarda de compatibilidade e SHALL emitir um aviso de validação em editor indicando a ausência de categoria.

### Requirement 4: Janelas de cancelamento por destino (CancelRules)

**User Story:** Como jogador, quero cancelar ações apenas em janelas específicas e por destino, para que Dash, Básico e Habilidade possam abrir em momentos diferentes e o combate encadeie de forma fluida sem perder o peso das animações.

#### Acceptance Criteria

1. THE Sistema_de_Combate SHALL permitir que cada Ação_Ofensiva defina CancelRules independentes por Destino_de_Cancelamento (Dash, Basic, Skill), cada CancelRule contendo Target, Start e End como frações no intervalo fechado de 0 a 1, com Start menor ou igual a End.
2. IF uma Ação_Ofensiva não possui CancelRule para um dado Destino_de_Cancelamento, THEN THE Sistema_de_Combate SHALL rejeitar o cancelamento para esse destino.
3. WHEN o jogador solicita um cancelamento para um Destino_de_Cancelamento, THE Sistema_de_Combate SHALL, antes de encerrar a ação atual, verificar em ordem: (1) que a CancelRule do destino está aberta no progresso atual; (2) que o comando ainda é válido; (3) que a ação de destino está disponível (cooldown, recurso, energia Asura e demais condições existentes); (4) que a transição pode ser realizada; e SHALL cancelar a ação atual e iniciar a ação de destino somente quando todas as verificações passarem.
4. IF qualquer das verificações de ordenação segura falha (CancelRule fechada, comando inválido, destino indisponível ou transição inviável), THEN THE Sistema_de_Combate SHALL rejeitar o cancelamento e manter a ação atual em execução sem alterar sua fase, de modo que uma habilidade sem mana ou um dash em cooldown NÃO interrompa a ação atual deixando o Jogador sem ação.
5. WHEN uma Ação_Ofensiva é cancelada por uma CancelRule aberta e um destino disponível, THE Sistema_de_Combate SHALL encerrar a ação cancelada sem emitir seus ImpactEvents futuros ainda não emitidos e sem abrir suas ImpactWindows futuras.
6. WHEN uma Ação_Ofensiva termina naturalmente ao fim da fase Recovery, THE Sistema_de_Combate SHALL permitir o início de novas ações sem exigir uma CancelRule, sujeito a outros estados bloqueantes existentes.
7. THE Sistema_de_Combate SHALL substituir o bloqueio total de input anterior pelo modelo de CancelRules, preservando o comportamento observável de `AbilityHolder.IsCasting` e `AbilityHolder.MovementAllowedWhileCasting` para os consumidores existentes.
8. IF uma CancelRule define Start ou End fora do intervalo de 0 a 1, ou define Start maior que End, THEN THE Sistema_de_Combate SHALL sinalizar o erro na validação em editor com uma mensagem clara e aplicar um fallback seguro em runtime, sem mutar silenciosamente os dados de design.

### Requirement 5: Buffer de input

**User Story:** Como jogador, quero que comandos cujo único impedimento é o momento sejam lembrados, para que o combate aceite minhas intenções sem exigir timing perfeito, sem re-mirar inimigos arbitrários nem esperar indefinidamente por cooldown.

#### Acceptance Criteria

1. THE Sistema_de_Combate SHALL expor um valor serializado e ajustável `BufferDuration`, inicializado em 120 milissegundos, onde a faixa de 100 a 180 milissegundos é apenas uma sugestão e não um limite rígido, o valor 0 desativa o buffer, e valores negativos ou inválidos SHALL ser rejeitados.
2. WHEN o jogador emite um comando (ataque básico, dash, Q, W, E ou R), THE Sistema_de_Combate SHALL primeiro tentar executá-lo imediatamente ou transicioná-lo via cancelamento, e SHALL armazenar a Intencao_de_Comando no Buffer_de_Input apenas se o único impedimento for temporal e a ação puder tornar-se elegível, preservando o alvo/direção da intenção para não re-mirar um inimigo arbitrário.
3. WHEN uma Intencao_de_Comando bufferizada existe e a janela aplicável ao comando abre, THE Sistema_de_Combate SHALL reavaliar expiração, alvo, cooldown e recurso e disparar o comando dentro de um quadro após a abertura da janela quando elegível.
4. WHEN uma Intencao_de_Comando bufferizada existe e a Ação_Ofensiva atual termina naturalmente, THE Sistema_de_Combate SHALL reavaliar expiração, alvo, cooldown e recurso e disparar o comando dentro de um quadro após o término quando elegível.
5. IF o tempo decorrido desde a emissão de uma Intencao_de_Comando bufferizada excede `BufferDuration`, THEN THE Sistema_de_Combate SHALL descartar essa intenção sem executá-la.
6. WHEN uma Intencao_de_Comando bufferizada é disparada, THE Sistema_de_Combate SHALL removê-la do buffer de modo que ela seja executada no máximo uma vez.
7. WHEN uma nova Intencao_de_Comando é bufferizada enquanto outra ainda está válida, THE Sistema_de_Combate SHALL substituir a anterior pela mais recente em um único slot, usando um critério de desempate determinístico para comandos de mesmo instante.
8. WHEN o jogador emite um comando elegível e nenhuma Ação_Ofensiva está em curso, THE Sistema_de_Combate SHALL executá-lo imediatamente sem bufferizá-lo nem introduzir espera artificial.
9. IF um comando é recusado por indisponibilidade de recurso ou cooldown e não apenas por impedimento temporal de janela, THEN THE Sistema_de_Combate SHALL não mantê-lo em espera indefinida por cooldown ou recurso.
10. WHEN o jogo está em pausa de menu, THE Sistema_de_Combate SHALL não consumir a validade de intenções bufferizadas; and WHILE um Hit_Stop está ativo, THE Sistema_de_Combate SHALL continuar capturando input sem descartar acidentalmente input legítimo.
11. WHEN o jogador solta o controle após Segurar_Ataque, THE Sistema_de_Combate SHALL remover repetições futuras derivadas de Hold sem remover uma execução já autorizada por um Toque_de_Ataque.
12. THE Sistema_de_Combate SHALL não emitir `AbilityRejected` a cada reavaliação interna de uma intenção bufferizada, emitindo no máximo uma notificação de rejeição terminal conforme o contrato aplicável.

### Requirement 6: Dash como ferramenta defensiva de cancelamento

**User Story:** Como jogador, quero usar o dash para escapar de ações quando a regra de cancelamento permitir, para que o dash seja uma ferramenta defensiva confiável, com invulnerabilidade separada da duração do deslocamento.

#### Acceptance Criteria

1. THE Sistema_de_Combate SHALL manter o dash padrão existente, acionado pelo controle de dash configurado, sem consumir recurso de habilidade e sujeito apenas ao seu próprio cooldown.
2. WHEN o jogador aciona o dash durante uma Ação_Ofensiva, THE Sistema_de_Combate SHALL avaliar o cancelamento pelo mesmo Resolvedor_de_Cancelamento das demais ações, verificando a CancelRule de destino Dash e a disponibilidade do dash antes de cancelar a ação atual.
3. WHEN o Resolvedor_de_Cancelamento autoriza um cancelamento por dash (CancelRule de Dash aberta e dash disponível), THE Sistema_de_Combate SHALL cancelar a ação atual e iniciar a execução do dash a partir de um input elegível, tendo como meta inicial 50 milissegundos sob as condições de teste declaradas.
4. IF o jogador aciona o dash enquanto a CancelRule de Dash ainda está fechada e o único impedimento é temporal, com o dash disponível, THEN THE Sistema_de_Combate SHALL reter a Intencao_de_Comando no Buffer_de_Input e executar o cancelamento por dash no primeiro quadro em que a CancelRule de Dash abrir.
5. IF o jogador aciona o dash fora de qualquer CancelRule de Dash e fora da janela de buffer, THEN THE Sistema_de_Combate SHALL rejeitar o cancelamento por dash, manter a ação atual sem alterar sua fase e sinalizar a rejeição pelo canal `AbilityRejected` existente conforme o contrato aplicável.
6. IF o dash está em cooldown ou indisponível quando acionado, THEN THE Sistema_de_Combate SHALL rejeitar o dash antes de cancelar a ação atual, preservar o cooldown restante inalterado, manter a ação atual e sinalizar a falha pelo canal `AbilityRejected` existente conforme o contrato aplicável.
7. THE Sistema_de_Combate SHALL tratar a Duracao_de_Deslocamento e a Janela_de_i-frames como parâmetros independentes e configuráveis, permitindo dash sem i-frames, com i-frames parciais ou com i-frames por toda a duração, onde o valor inicial de i-frames NÃO é um contrato de gameplay obrigatório.
8. WHEN um dash é cancelado, o Jogador morre, a sala muda ou o deslocamento do dash termina, THE Sistema_de_Combate SHALL encerrar qualquer imunidade a dano concedida pelo dash, sem imunidade residual.
9. WHEN um dash é executado com sucesso, THE Sistema_de_Combate SHALL conceder imunidade a dano durante a Janela_de_i-frames configurada e disparar os eventos de dash existentes conforme o contrato observável existente.

### Requirement 7: Fundação de feedback de acerto

**User Story:** Como jogador, quero sentir que meus golpes acertam, para que o combate base tenha peso mesmo antes do sistema de postura, com feedback cosmético distinto de stagger mecânico.

#### Acceptance Criteria

1. WHEN um ImpactEvent resolve dano em pelo menos um inimigo, THE Sistema_de_Combate SHALL autorizar um Hit_Stop para esse ImpactEvent ou grupo de impactos simultâneos, conforme seu perfil de Hit_Stop configurado.
2. THE Sistema_de_Combate SHALL expor o perfil de Hit_Stop como valores serializados e ajustáveis, onde a duração está no intervalo de 0 a 1 segundo e o valor 0 desativa o Hit_Stop sem alterar a escala de tempo do jogo.
3. WHEN múltiplos impactos de um mesmo ataque ocorrem simultaneamente ou um único ImpactEvent atinge vários inimigos, THE Sistema_de_Combate SHALL agrupar as solicitações em um único feedback e limitar a frequência/intensidade por uma política explícita (p.ex. intensidade máxima), sem somar as durações por alvo.
4. WHERE um ImpactEvent é um soco intermediário, THE Sistema_de_Combate SHALL aplicar um micro Hit_Stop opcional; and WHERE um ImpactEvent é um finalizador, THE Sistema_de_Combate SHALL aplicar um Hit_Stop mais forte conforme o perfil configurado.
5. WHERE um ImpactEvent é um impacto secundário ou de dano ao longo do tempo (DoT), THE Sistema_de_Combate SHALL aplicar Hit_Stop reduzido ou nenhum conforme o perfil configurado.
6. WHEN um inimigo sofre um acerto de ataque básico ou habilidade, THE Sistema_de_Combate SHALL acionar nesse inimigo exatamente uma Reação_de_Acerto_Básica cosmética e distinta de qualquer reação de quebra de postura, encerrando-a automaticamente ao fim de sua duração serializada e sem interromper a IA do inimigo.
7. THE Sistema_de_Combate SHALL distinguir feedback cosmético de stagger mecânico, de modo que inimigos elite ou boss NÃO recebam interrupção de ataque apenas por uma Reação_de_Acerto_Básica.
8. IF um ataque básico ou habilidade não resolve dano em nenhum inimigo, THEN THE Sistema_de_Combate SHALL não aplicar Hit_Stop nem acionar qualquer Reação_de_Acerto_Básica.
9. THE Sistema_de_Combate SHALL limitar o feedback desta fase ao Hit_Stop e à Reação_de_Acerto_Básica, sem acionar feedback de quebra de postura, flash de stance break, knockdown ou launch, e sem ampliar nem remover reações de postura existentes (p.ex. o stun do E da Manopla).
10. WHERE o Hit_Stop altera a escala de tempo global, THE Sistema_de_Combate SHALL preservar a pausa e demais modificadores de tempo ativos ao término do Hit_Stop, sem restaurar incondicionalmente a escala de tempo para 1.
11. WHEN um acerto é resolvido, THE Sistema_de_Combate SHALL disparar os canais de notificação de acerto existentes (`HookBus.OnBasicHit` e `AbilityHolder.AttackHitsResolved`) conforme a granularidade e o contrato observável que os consumidores atuais esperam.

### Requirement 8: Compatibilidade com sistemas existentes

**User Story:** Como desenvolvedor, quero que os boons e sistemas atuais continuem funcionando por contrato, para que a reestruturação do combate não quebre conteúdo já implementado, mesmo que a ordenação interna do novo sistema mude.

#### Acceptance Criteria

1. THE Sistema_de_Combate SHALL manter as APIs públicas `CharControlScript.BasicAttackPerformed`, `CharControlScript.CancelCombo()` e `CharControlScript.RequireAttackRelease()` com as mesmas assinaturas (nome, parâmetros e tipo de retorno) existentes antes desta fase.
2. THE Sistema_de_Combate SHALL manter as APIs públicas `AbilityHolder.AbilityUsed`, `AbilityHolder.AbilityRejected`, `AbilityHolder.AttackHitsResolved`, `AbilityHolder.IsCasting`, `AbilityHolder.MovementAllowedWhileCasting`, `AbilityHolder.ReduceCooldowns(float)` e `HookBus.OnBasicHit` com as mesmas assinaturas (nome, parâmetros e tipo de retorno) existentes antes desta fase.
3. WHEN um input é recebido, uma ação é iniciada, um ImpactEvent é emitido, um acerto é resolvido, um dash é executado ou uma ação é cancelada, THE Sistema_de_Combate SHALL preservar a semântica observável de cada ocorrência que os consumidores de fato utilizam — input recebido = intenção; ação iniciada = execução autorizada; impacto emitido = um evento que pode não acertar; acerto resolvido = contato/dano na granularidade do contrato; ação cancelada = interrompe eventos futuros dependentes sem desfazer acertos já resolvidos nem efeitos independentes já emitidos — identificando as relações de ordem que os boons realmente usam e preservando-as ou adaptando-as explicitamente.
4. WHERE o contrato legado não comprova disparo por golpe individual, THE Sistema_de_Combate SHALL não emitir `AbilityUsed` por soco de uma mesma habilidade, e SHALL manter a granularidade real de `AttackHitsResolved`, que pode ser um lote de alvos.
5. IF uma Ação_Ofensiva é cancelada antes de emitir um ImpactEvent, THEN THE Sistema_de_Combate SHALL não disparar os eventos de notificação de acerto (`AttackHitsResolved`, `HookBus.OnBasicHit`) para esse ImpactEvent não emitido; and WHERE um projétil foi emitido antes do cancelamento, THE Sistema_de_Combate SHALL permitir que ele produza um acerto válido após o cancelamento conforme sua própria política de vida.
6. WHEN uma intenção é bufferizada e reavaliada ou uma transição por cancelamento ocorre, THE Sistema_de_Combate SHALL não duplicar custos, cooldowns, geração de energia Asura nem procs.
7. THE Sistema_de_Combate SHALL implementar a lógica de fases, CancelRules, Buffer_de_Input e identificação de execução do Nucleo_Compartilhado em classes C# sem herança de `MonoBehaviour`, exercitáveis em testes unitários sem instanciar `GameObject` ou `MonoBehaviour`, extraindo apenas o que o núcleo determinístico precisa, sem forçar uma reescrita estrutural completa de `CharControlScript` e `AbilityHolder` nesta fase, seguindo o padrão de classes auxiliares como `ChargedShot`.
8. THE Sistema_de_Combate SHALL manter o Nucleo_Compartilhado (estado de ação, autorização de transição, buffer, identificação de execução e despacho de eventos) como caminho único de código, permitindo que comportamento específico de arma (socos, avanços, estocadas, projéteis, sequências, geração de energia Asura) resida em definições de arma, executores ou Adaptadores_de_Compatibilidade.
9. THE Sistema_de_Combate SHALL exigir que ImpactEvents de uma mesma execução possuam identidade suficiente para deduplicação, com uma única fonte de avanço da linha do tempo e uma estratégia explícita de sincronização com a animação, de modo que um relógio lógico e Animation Events não disparem o mesmo impacto em duplicidade.

### Requirement 9: Critério de conclusão por playtest da Manopla

**User Story:** Como designer, quero que a conclusão desta fase dependa de um playtest de sensação, para que a correção técnica não seja confundida com qualidade de gameplay.

#### Acceptance Criteria

1. THE Sistema_de_Combate SHALL exigir, para a conclusão da Fase 1, um playtest da Manopla sem boons em um sandbox com 3 a 5 inimigos, com acesso às habilidades Q/W/E/R e reinício rápido disponível.
2. THE playtest de conclusão SHALL incluir uma situação de múltiplos acertos, uma aproximação a inimigo e um dash durante uma ação, em uma sessão de aproximadamente 10 minutos com condições e observações registradas.
3. THE Sistema_de_Combate SHALL testar a mana e a energia Asura sob as condições atuais, garantindo que a Rajada Asura (R) seja alcançável durante o playtest, sem remover a mana silenciosamente.
4. IF o projeto apenas compila e os testes automatizados passam mas o playtest de conclusão não foi executado e registrado, THEN a Fase 1 SHALL NOT ser considerada concluída.

## Correctness Properties (candidatas a Property-Based Testing)

As propriedades abaixo descrevem invariantes puras das classes auxiliares do Nucleo_Compartilhado e são adequadas para teste baseado em propriedades (lógica própria, determinística, de baixo custo). Elas não testam serviços externos nem o motor Unity.

- **Tap produz no máximo um ataque (R1.1, R1.5, R1.6, R1.11, R1.12):** sob as precondições de uma execução válida — alvo elegível, aproximação/execução concluída e comando não substituído — um Toque_de_Ataque produz no máximo um ataque, e exatamente um quando a aproximação/execução conclui com sucesso. (A propriedade não vale para sequências com alvo inelegível, intenção invalidada por perda de alvo ou comando substituído, cujos caminhos de rejeição/perda de alvo cessam a execução.)
- **Cancelamento por destino dentro da janela (R4.1, R4.2, R4.3, R4.4):** para qualquer progresso `p` e CancelRule `[start, end]` de um destino, o cancelamento para esse destino é permitido se e somente se existe CancelRule para o destino, `start <= p <= end`, e as verificações de validade e disponibilidade do destino passam. (Metamórfica / condição de borda.)
- **Ausência de regra proíbe destino (R4.2):** se não existe CancelRule para um destino, nenhum valor de `p` autoriza o cancelamento para esse destino.
- **Indisponibilidade preserva a ação atual (R4.4):** se o destino está indisponível (cooldown, recurso, Asura), a ação atual permanece em execução sem alterar sua fase e sem deixar o Jogador sem ação.
- **CancelRule válida (R4.1):** para toda CancelRule, `0 <= start <= end <= 1` (invariante preservada após validação).
- **Buffer nunca dispara após expirar (R5.5):** para qualquer tempo de emissão `t0` e tempo corrente `t`, se `t - t0 > BufferDuration` então a intenção bufferizada nunca é disparada.
- **Buffer dispara no máximo uma vez (R5.6):** para qualquer sequência de atualizações, uma única intenção bufferizada produz no máximo um disparo.
- **Buffer retém o mais recente (R5.7):** dado um conjunto de intenções bufferizadas, a selecionada para disparo é sempre a de emissão mais recente, com desempate determinístico para instantes iguais.
- **Buffer não re-mira (R5.2):** uma intenção bufferizada preserva o alvo/direção original e nunca seleciona um inimigo arbitrário por proximidade ao ser reavaliada.
- **Buffer não aguarda indisponibilidade (R5.9):** um comando recusado por recurso/cooldown, e não apenas por impedimento temporal de janela, não permanece em espera indefinida.
- **Não duplicação de custos (R8.6):** reavaliação de buffer ou transição por cancelamento não duplica custo, cooldown, geração de energia Asura nem procs de uma mesma execução.
- **Hit-stop desativável e não-negativo (R7.2):** para qualquer duração de Hit_Stop configurada, o valor efetivo é maior ou igual a zero, e zero resulta em nenhuma pausa aplicada.
- **Hit-stop agrupado não soma por alvo (R7.3):** um único ImpactEvent que atinge N inimigos produz um feedback agrupado cuja intensidade/duração segue a política explícita (p.ex. máximo), nunca a soma das N durações.
- **Preservação de tempo após hit-stop (R7.10):** se o Hit_Stop altera a escala de tempo global, a escala restaurada ao término respeita a pausa e os demais modificadores ativos, em vez de ser fixada incondicionalmente em 1.
- **Dedup de impactos por execução (R8.9):** para uma mesma execução, um ImpactEvent é contabilizado uma única vez mesmo quando relógio lógico e Animation Events coincidem.
- **Padrão de categoria (R3.10):** uma Ação_Ofensiva sem Categoria_de_Compromisso declarada resolve sempre para Committed e gera um aviso de validação.
