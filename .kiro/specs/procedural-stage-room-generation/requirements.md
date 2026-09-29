# Requirements Document

## Introduction

Esta feature define a **geração procedural de fases baseada em salas** para o Tech-Guy, no estilo *Binding of Isaac*: cada run gera uma **fase** composta por um **grafo de salas** que o jogador pode percorrer de ida e volta (traversal bidirecional / backtracking). Cada fase contém uma sala inicial, salas de combate, salas especiais/secretas opcionais e uma sala de chefe; **derrotar o chefe da fase libera a próxima fase**.

O objetivo de design vem de um problema concreto observado: hoje as runs vêm com **poucos inimigos e pouca variedade**, deixando o combate entediante. Portanto esta feature também precisa **aumentar a densidade e a variedade de inimigos por sala**, compondo cada sala a partir dos **16 arquétipos de inimigo existentes** (Rush, Grunt, Heavy, Charger, Shooter, Spread_Shooter, Sniper, Bomber, Hazard_Caster, Hooker, Healer, Shield_Support, Swarm, Spawner, Fragile, Mirror), com composições variadas e ajustáveis.

A geração é **baseada em semente (seed)** para que cada run seja diferente da outra, e as salas especiais/secretas aparecem **de forma probabilística** — nem toda run terá todas as salas especiais, reforçando a variabilidade.

A implementação **estende os sistemas existentes** em vez de substituí-los:

- **Progression_Director** — a lógica atual do `FirstSectorDirector` (sequência finita de `Encounter`, gate de entrada/saída, drop de troféu, oferta de bênção, término da run, retorno ao Nexus) é generalizada de uma sequência linear autorada para um **grafo de salas gerado proceduralmente**.
- **Room_Gates** (`EncounterGates`) — selagem/abertura de entradas e saídas de sala é reutilizada para portas entre salas em ambos os sentidos.
- **Run_Boons** (`RunBoons`) — a seleção de bênção por sala continua sendo o sistema existente.
- **Reward_Trophy** (`RewardTrophy`) — o troféu reclamável que abre a seleção de bênção continua o mesmo.
- **Spawn_Point** (`EnemyRespawnPoint`) e **Archetype_System** — a instanciação e configuração de inimigos por arquétipo continuam sendo os sistemas existentes.
- **Sector_Boss** (`SectorBoss`) — o componente de chefe existente marca o inimigo da sala de chefe.
- **Scene_Portal** (`ScenePortal`) / gerenciamento de cena — a transição para o hub `NexusLobby` continua a mesma.

### Escopo

**No escopo:**

- Geração procedural de um grafo de salas por fase (sala inicial, salas de combate, sala de chefe, salas especiais/secretas opcionais).
- Traversal bidirecional entre salas conectadas, incluindo backtracking para salas já visitadas.
- Aleatoriedade por semente (seed) para que cada run seja única.
- Ponderação de tipos de sala para que salas especiais/secretas apareçam probabilisticamente (nem toda run as terá).
- Composição de inimigos por sala a partir dos 16 arquétipos existentes, com densidade e variedade ajustáveis (correção do "poucos inimigos / pouca variedade").
- Progressão de fase liberada pela derrota do chefe.
- Descoberta e probabilidade de aparição de salas secretas.

**Fora do escopo (explicitamente excluído):**

- Criação de novos arquétipos de inimigo (já coberto por `enemy-swarm-core-archetypes`).
- Nova arte, modelos 3D ou geometria de sala autorada à mão além dos blocos/prefabs já existentes.
- Mudanças na economia de meta-progressão além do `RunBoons`/`CurrencyWallet` existentes.
- Ferramenta de editor/UI de autoria de níveis (level editor).
- Rework das mecânicas de combate ou de armas em si.

## Glossary

- **Stage** (Fase): Uma unidade de progressão gerada proceduralmente, composta por um Room_Graph com exatamente uma Start_Room, uma ou mais Combat_Rooms, exatamente uma Boss_Room e zero ou mais Special_Rooms.
- **Run**: Uma sessão de jogo contínua do jogador através de uma ou mais Stages, iniciada a partir do hub e encerrada por morte ou por conclusão.
- **Stage_Generator**: O sistema responsável por gerar o Room_Graph de uma Stage a partir de uma Run_Seed e dos parâmetros de geração.
- **Progression_Director**: O sistema (evolução do `FirstSectorDirector`) que dirige a Run: ativa salas, sela/abre portas, dispara a oferta de bênção, gerencia o chefe e conduz a transição entre Stages e o retorno ao Nexus.
- **Room** (Sala): Um nó do Room_Graph com um tipo (Room_Type), uma posição central, limites (Room_Size), zero ou mais Room_Connections e, quando aplicável, uma Room_Composition.
- **Room_Type**: A classificação de uma Room. Tipos definidos: Start_Room, Combat_Room, Boss_Room, Treasure_Room, Secret_Room.
- **Start_Room**: A sala onde o jogador começa a Stage; não contém inimigos.
- **Combat_Room**: Uma sala que contém uma Room_Composition de inimigos e que precisa ser limpa para conceder recompensa.
- **Boss_Room**: A sala que contém o chefe da Stage, marcado pelo Sector_Boss.
- **Treasure_Room**: Uma sala especial de recompensa sem combate obrigatório.
- **Secret_Room**: Uma sala oculta que só é revelada após uma Discovery_Action e cuja existência na Stage é probabilística.
- **Special_Room**: Termo coletivo para Treasure_Room e Secret_Room.
- **Room_Graph**: O conjunto de Rooms de uma Stage e suas Room_Connections, formando um grafo conexo navegável.
- **Room_Connection**: Uma ligação bidirecional entre duas Rooms adjacentes, materializada por um par de portas do Room_Gates.
- **Room_Gates**: O sistema existente (`EncounterGates`) que sela e abre entradas/saídas de sala.
- **Run_Seed**: O valor inteiro que semeia toda a aleatoriedade da geração de uma Run, garantindo reprodutibilidade a partir de uma mesma semente.
- **Room_Composition**: A especificação dos inimigos de uma Combat_Room: quais Archetype_Ids, em que quantidade e com que variedade.
- **Archetype_System**: O sistema existente de inimigos (`EnemyAI`, `EnemyProfile`, `EnemyVariant`, `EnemyArchetype`, `ArchetypeId`, `CombatRole`), fonte dos 16 arquétipos.
- **Archetype_Id**: O identificador estável de um dos 16 arquétipos (`ArchetypeId`).
- **Spawn_Point**: O componente existente (`EnemyRespawnPoint`) usado para instanciar inimigos.
- **Run_Boons**: O sistema existente (`RunBoons`) que oferece a seleção de bênção por sala limpa.
- **Reward_Trophy**: O objeto reclamável existente (`RewardTrophy`) que, ao ser coletado, abre a seleção do Run_Boons.
- **Sector_Boss**: O componente existente (`SectorBoss`) que marca o inimigo-chefe.
- **Player_Actor**: O `Actor` que representa o jogador (`PlayerActor`).
- **Discovery_Action**: A ação do jogador que revela uma Secret_Room (ex.: aproximar-se de uma parede secreta e interagir).
- **Density_Budget**: O valor-alvo, por Combat_Room, que determina quantos inimigos são instanciados, escalonado pela profundidade/ordem da sala na Stage.
- **Variety_Target**: O número mínimo de Archetype_Ids distintos exigido na Room_Composition de uma Combat_Room, sujeito à disponibilidade.
- **Nexus_Scene**: A cena de hub `NexusLobby` para onde a Run retorna ao terminar.
- **Unity_MCP**: O conjunto de ferramentas do MCP for Unity (servidor `unity-mcp`) usado para criar, configurar, inspecionar e testar conteúdo do projeto diretamente dentro do Editor da Unity (cenas, prefabs, componentes, console, testes EditMode/PlayMode).

## Requirements

### Requisito 1: Geração procedural do grafo de salas

**User Story:** Como jogador, quero que cada fase seja um conjunto de salas gerado proceduralmente, para que cada run tenha um layout diferente e a exploração seja variada.

#### Critérios de Aceitação

1. WHEN uma Stage é iniciada, THE Stage_Generator SHALL gerar um Room_Graph conexo contendo exatamente uma Start_Room, exatamente uma Boss_Room e entre 1 e 20 Combat_Rooms.
2. THE Stage_Generator SHALL garantir que existe pelo menos um caminho navegável de Room_Connections da Start_Room até a Boss_Room.
3. WHEN duas Runs usam a mesma Run_Seed e os mesmos parâmetros de geração, THE Stage_Generator SHALL produzir um Room_Graph idêntico em número de Rooms, Room IDs, Room_Connections, Room_Types e Room_Composition de cada Room.
4. THE Stage_Generator SHALL posicionar cada Room de modo que exista uma separação mínima de 0 unidades entre os limites (Room_Size) de quaisquer duas Rooms do mesmo Room_Graph, permitindo que as bordas se toquem mas não se interpenetrem.
5. WHEN o Room_Graph é gerado, THE Stage_Generator SHALL criar exatamente um par de Room_Gates para cada Room_Connection, reutilizando o sistema `EncounterGates` existente.
6. THE Stage_Generator SHALL gerar cada Combat_Room contendo pelo menos 1 EnemyRespawnPoint dentro dos seus limites (Room_Size).
7. IF o Stage_Generator não conseguir produzir um Room_Graph conexo que ligue a Start_Room à Boss_Room em até 50 tentativas de geração, THEN THE Stage_Generator SHALL registrar uma mensagem de erro identificando a falha de geração, abortar a geração daquela Stage e não instanciar nenhuma Room parcial.
8. THE Stage_Generator SHALL resolver todas as referências de Rooms, Spawn_Points e Room_Gates usando exclusivamente referências serializadas ou componentes resolvidos explicitamente, sem GameObject.Find/FindObjectOfType.

### Requisito 2: Aleatoriedade por semente (variedade entre runs)

**User Story:** Como jogador, quero que cada run seja diferente da anterior, para que o jogo não fique repetitivo, mas quero que o mesmo resultado seja reproduzível a partir de uma mesma semente para testes.

#### Critérios de Aceitação

1. WHEN uma Run inicia, THE Progression_Director SHALL obter uma Run_Seed representada como um inteiro de 64 bits a partir da fonte de semente configurada e semear com ela toda a aleatoriedade da geração daquela Run.
2. IF a fonte de Run_Seed configurada estiver ausente ou fornecer um valor inválido para um inteiro de 64 bits, THEN THE Progression_Director SHALL registrar uma mensagem de erro identificando a falha de aquisição de semente e gerar uma Run_Seed de 64 bits válida a partir de uma fonte de fallback antes de iniciar a geração.
3. WHEN duas Runs usam a mesma Run_Seed e os mesmos parâmetros de geração, THE Stage_Generator SHALL produzir Room_Graphs iguais, definindo igualdade como idêntico número de Rooms, idênticos Room IDs, idênticas Room_Connections, idênticos Room_Types e idêntica Room_Composition de cada Room.
4. WHEN duas Runs usam Run_Seeds diferentes e os mesmos parâmetros de geração, THE Stage_Generator SHALL produzir Room_Graphs que divergem em pelo menos um valor mensurável entre: número de Rooms, conjunto de Room_Connections, conjunto de Room_Types atribuídos ou conjunto de Special_Rooms presentes.
5. THE Stage_Generator SHALL derivar toda decisão aleatória de geração de um gerador de números aleatórios isolado, semeado exclusivamente pela Run_Seed, sem consumir fontes de aleatoriedade global compartilhadas.
6. WHEN uma Run_Seed é adquirida, THE Progression_Director SHALL persistir e registrar a Run_Seed utilizada, de modo que a mesma Run_Seed possa ser reutilizada para reproduzir a geração.

### Requisito 3: Traversal bidirecional e backtracking

**User Story:** Como jogador, quero poder ir e voltar entre as salas já abertas, para explorar a fase livremente como em Binding of Isaac.

#### Critérios de Aceitação

1. WHERE uma Room_Connection liga duas Rooms adjacentes, THE Room_Gates SHALL permitir a passagem do jogador em ambos os sentidos quando a conexão estiver aberta.
2. WHEN o jogador limpa uma Combat_Room, THE Progression_Director SHALL abrir, em até 0,5 segundo, as Room_Connections elegíveis daquela Room, onde "elegíveis" são as Room_Connections que levam a Rooms adjacentes não bloqueadas por salas ainda não resolvidas.
3. WHEN o jogador retorna a uma Room previamente limpa, THE Progression_Director SHALL manter aquela Room marcada como limpa e não reinstanciar sua Room_Composition, preservando o estado idempotente da Room em cada reentrada.
4. WHILE o jogador estiver dentro de uma Combat_Room ainda não limpa, THE Room_Gates SHALL manter seladas as portas daquela Room para conter o combate, reutilizando o comportamento de selagem existente.
5. WHEN o jogador entra em uma Combat_Room ainda não limpa, THE Progression_Director SHALL ativar a Room_Composition daquela Room.
6. WHEN uma Combat_Room é marcada como limpa, THE Room_Gates SHALL abrir em até 0,5 segundo as portas elegíveis daquela Room, concluindo a transição de gate.

### Requisito 4: Tipos de sala e aparição probabilística de salas especiais

**User Story:** Como jogador, quero que segredos e salas especiais possam aparecer aleatoriamente, para que nem toda run seja igual e valha a pena explorar.

#### Critérios de Aceitação

1. THE Stage_Generator SHALL atribuir a cada Room exatamente um Room_Type dentre Start_Room, Combat_Room, Boss_Room, Treasure_Room e Secret_Room.
2. THE Stage_Generator SHALL gerar exatamente uma Start_Room e exatamente uma Boss_Room por Stage.
3. THE Stage_Generator SHALL determinar a presença de cada Special_Room por meio de um sorteio ponderado derivado exclusivamente da Run_Seed, de modo que a probabilidade de aparição de cada Special_Room seja de no mínimo 5% e no máximo 95% por Stage, e que a mesma Run_Seed produza sempre o mesmo resultado de sorteio.
4. WHEN a probabilidade sorteada de uma Special_Room fica abaixo do limiar definido para uma Stage, THE Stage_Generator SHALL gerar aquela Stage sem a Special_Room correspondente, mantendo o restante do Room_Graph acessível a partir da Start_Room.
5. THE Stage_Generator SHALL gerar no máximo uma Treasure_Room e no máximo uma Secret_Room por Stage.
6. THE Stage_Generator SHALL conectar toda Treasure_Room gerada ao Room_Graph por pelo menos uma Room_Connection acessível a partir da Start_Room.
7. IF o Stage_Generator não consegue posicionar uma Room_Connection acessível para uma Treasure_Room gerada, THEN THE Stage_Generator SHALL descartar aquela Treasure_Room daquela Stage e registrar a falha de geração, preservando as demais Rooms já geradas.
8. WHERE uma Secret_Room é gerada, THE Stage_Generator SHALL mantê-la oculta e com sua Room_Connection fechada até que o jogador execute a Discovery_Action correspondente.
9. WHEN o jogador executa a Discovery_Action a partir de uma Room diretamente adjacente à Secret_Room gerada (compartilhando uma Room_Connection oculta), THE Progression_Director SHALL revelar a Secret_Room e abrir a Room_Connection que dá acesso a ela em até 1 segundo, apresentando um indicador visual de revelação ao jogador.
10. IF o jogador executa a Discovery_Action sem estar em uma Room diretamente adjacente a uma Secret_Room gerada, THEN THE Progression_Director SHALL manter todas as Secret_Rooms ocultas e não abrir nenhuma Room_Connection.
11. WHERE uma Treasure_Room é gerada, THE Progression_Director SHALL conceder sua recompensa por meio do sistema Reward_Trophy / Run_Boons existente quando o jogador entra na Treasure_Room, sem exigir combate naquela Room.

### Requisito 5: Composição de inimigos por sala (densidade e variedade)

**User Story:** Como jogador, quero salas com mais inimigos e maior variedade de tipos, para que o combate seja intenso e deixe de ser entediante.

#### Critérios de Aceitação

1. WHEN uma Combat_Room é gerada, THE Stage_Generator SHALL montar sua Room_Composition selecionando Archetype_Ids dentre os 16 arquétipos existentes do Archetype_System.
2. THE Stage_Generator SHALL dimensionar a quantidade de inimigos de cada Combat_Room a partir de um Density_Budget configurável dentro do intervalo de 4 a 30 inimigos por sala, garantindo que a quantidade instanciada seja maior ou igual ao valor mínimo definido por esse orçamento e menor ou igual ao valor máximo definido.
3. THE Stage_Generator SHALL compor cada Combat_Room com um número de Archetype_Ids distintos maior ou igual ao Variety_Target, onde Variety_Target é configurável no intervalo de 2 a 6 arquétipos distintos, limitado à quantidade de arquétipos disponíveis para aquela sala quando esta for menor que o Variety_Target.
4. WHEN uma Combat_Room é ativada, THE Progression_Director SHALL instanciar os inimigos de sua Room_Composition por meio dos Spawn_Points existentes (`EnemyRespawnPoint`), aplicando a cada inimigo o Archetype_Id atribuído a ele na Room_Composition.
5. THE Stage_Generator SHALL escalonar o Density_Budget em função da profundidade da Combat_Room em relação à Start_Room, de modo que, para quaisquer duas salas A e B em que a profundidade de A seja maior que a de B, o Density_Budget de A seja maior ou igual ao de B, respeitando o máximo de 30 inimigos por sala.
6. WHEN um inimigo de uma Room_Composition é instanciado, THE Stage_Generator SHALL posicioná-lo em um ponto contido dentro dos limites (Room_Size) da respectiva Combat_Room e sobre um ponto válido da malha de navegação (NavMesh), realizando até 20 tentativas de amostragem de posição por inimigo.
7. IF um inimigo não obtiver um ponto válido de NavMesh dentro dos limites da sala após 20 tentativas, THEN THE Stage_Generator SHALL descartar esse inimigo específico da Room_Composition e registrar uma mensagem de aviso indicando a Combat_Room afetada e a quantidade de inimigos descartados.
8. IF a quantidade de inimigos efetivamente posicionados em uma Combat_Room for menor que o valor mínimo do Density_Budget (4 inimigos), THEN THE Stage_Generator SHALL registrar uma mensagem de aviso indicando a Combat_Room afetada, a quantidade alvo e a quantidade efetivamente posicionada.
9. IF uma Combat_Room não possuir nenhum Spawn_Point (`EnemyRespawnPoint`) válido dentro dos seus limites, THEN THE Stage_Generator SHALL deixar a Room_Composition sem inimigos e registrar uma mensagem de aviso indicando a Combat_Room afetada.

### Requisito 6: Recompensa por sala limpa (reuso de troféu e bênção)

**User Story:** Como jogador, quero receber uma bênção ao limpar cada sala de combate, para fortalecer meu personagem ao longo da fase, mantendo o fluxo de recompensa atual.

#### Critérios de Aceitação

1. WHEN o número de inimigos vivos de uma Combat_Room chega a zero, THE Progression_Director SHALL marcar a Combat_Room como limpa em até 1 segundo.
2. WHEN uma Combat_Room é marcada como limpa, THE Progression_Director SHALL instanciar em até 1 segundo um único Reward_Trophy sobre uma posição válida de NavMesh dentro dos limites da Combat_Room e alcançável pelo jogador por um caminho contínuo de NavMesh a partir da posição atual do jogador, reutilizando o comportamento de posicionamento sobre NavMesh existente.
3. IF nenhuma posição de NavMesh válida e alcançável for encontrada dentro dos limites da Combat_Room após até 5 tentativas de posicionamento, THEN THE Progression_Director SHALL posicionar o Reward_Trophy na posição de NavMesh válida e alcançável mais próxima do centro da Combat_Room e registrar a ocorrência para diagnóstico, mantendo a Combat_Room marcada como limpa.
4. WHEN o jogador aciona a interação de reivindicação enquanto está a até 2 metros do Reward_Trophy, THE Progression_Director SHALL abrir a seleção de bênção por meio do Run_Boons existente em até 1 segundo e remover o Reward_Trophy da cena.
5. WHILE a seleção de bênção do Run_Boons estiver ativa, THE Progression_Director SHALL manter seladas todas as Room_Connections de saída da Combat_Room recém-limpa.
6. IF a seleção de bênção do Run_Boons for encerrada sem que uma bênção seja escolhida, THEN THE Progression_Director SHALL manter seladas as Room_Connections de saída da Combat_Room e preservar o estado de "não reivindicada" para permitir nova tentativa de seleção, sem conceder nenhuma bênção.
7. WHEN o jogador escolhe uma bênção, THE Progression_Director SHALL aplicar a bênção escolhida e abrir, em até 1 segundo, todas as Room_Connections de saída da Combat_Room limpa que conectam a salas ainda não visitadas.

### Requisito 7: Progressão de fase por derrota do chefe

**User Story:** Como jogador, quero que derrotar o chefe de uma fase libere a próxima fase, para sentir progressão clara ao longo da run.

#### Critérios de Aceitação

1. WHEN a Boss_Room é gerada, THE Stage_Generator SHALL marcar exatamente um inimigo principal da Boss_Room com o componente Sector_Boss existente.
2. IF a Boss_Room não possuir nenhum inimigo elegível para ser marcado como inimigo principal, THEN THE Stage_Generator SHALL abortar a geração daquela Stage, preservar o estado da Run anterior à geração e registrar uma mensagem de erro identificando a ausência de inimigo principal na Boss_Room.
3. WHILE o chefe da Boss_Room não estiver derrotado, THE Progression_Director SHALL manter bloqueada a transição para a próxima Stage.
4. WHEN o chefe da Boss_Room é derrotado, THE Progression_Director SHALL liberar a transição para a próxima Stage em até 1 segundo após o registro da derrota.
5. WHEN o jogador aciona a transição de Stage liberada, THE Progression_Director SHALL gerar a próxima Stage com uma nova Run_Seed derivada deterministicamente da Run_Seed atual, de modo que a mesma Run_Seed atual produza sempre a mesma próxima Run_Seed.
6. WHEN não existe próxima Stage configurada após a derrota do chefe, THE Progression_Director SHALL concluir a Run e retornar o jogador à Nexus_Scene, reutilizando o fluxo de retorno existente.

### Requisito 8: Encerramento da run por morte do jogador

**User Story:** Como jogador, quero que a run termine de forma consistente quando eu morrer, para retornar ao Nexus como acontece hoje.

#### Critérios de Aceitação

1. WHEN o Player_Actor morre durante uma Stage, THE Progression_Director SHALL marcar a Run em andamento como encerrada em até 0,5 segundo e impedir qualquer outra transição de Stage ou spawn subsequente para essa Run.
2. WHEN a Run é encerrada por morte, THE Progression_Director SHALL iniciar o carregamento da Nexus_Scene reutilizando o fluxo de transição de cena existente, em até 1 segundo após o encerramento da Run.
3. WHEN a Nexus_Scene concluir o carregamento após uma morte, THE Progression_Director SHALL posicionar o jogador na Nexus_Scene e restaurar o estado de controle do jogador (input habilitado), sem preservar o estado transitório da Stage encerrada.
4. IF a Nexus_Scene não estiver disponível para carregamento, THEN THE Progression_Director SHALL registrar uma mensagem de erro identificando a cena ausente, interromper a tentativa de transição e manter a Run marcada como encerrada sem carregar outra cena.
5. IF o carregamento da Nexus_Scene falhar após ser iniciado, THEN THE Progression_Director SHALL registrar uma mensagem de erro indicando a falha de carregamento e realizar no máximo 1 nova tentativa de transição antes de interromper a operação.

### Requisito 9: Validação de dependências e conformidade estrutural

**User Story:** Como desenvolvedor, quero que a geração falhe de forma clara quando faltar uma dependência, para depurar o setup sem introduzir estados inconsistentes.

#### Critérios de Aceitação

1. IF uma dependência obrigatória (Player_Actor, Stage_Generator, referência de prefab de inimigo ou referência de Spawn_Point) estiver ausente ou nula durante `Awake` ou `OnValidate`, THEN THE Progression_Director SHALL registrar exatamente uma mensagem de erro que nomeia cada dependência ausente detectada.
2. IF uma dependência obrigatória estiver ausente ou nula durante `Awake`, THEN THE Progression_Director SHALL desabilitar-se (`enabled = false`) e não iniciar nenhuma geração de Stage ou sala, mantendo o estado da Run inalterado em relação ao estado anterior à execução do `Awake`.
3. WHILE ao menos uma dependência obrigatória estiver ausente ou nula, THE Progression_Director SHALL permanecer desabilitado e não executar lógica de progressão até que todas as dependências obrigatórias sejam atribuídas.
4. THE Progression_Director SHALL usar exclusivamente referências serializadas ou componentes resolvidos explicitamente para acessar Rooms, Room_Gates, Spawn_Points, Run_Boons e Sector_Boss, sem qualquer uso de `GameObject.Find`, `FindObjectOfType` ou strings mágicas na lógica de gameplay.
5. THE Progression_Director SHALL expor o estado de progressão da Run por meio de propriedades somente leitura (sem setter público), incluindo o Stage atual, a contagem de salas limpas e um indicador booleano de conclusão da Run, cada propriedade refletindo o valor corrente do estado interno no momento do acesso.

### Requisito 10: Fluxo de desenvolvimento e verificação via MCP da Unity

**User Story:** Como desenvolvedor, quero autorar e validar o sistema de geração procedural de fases/salas diretamente dentro do Editor da Unity por meio do Unity_MCP, para que cada mudança seja verificada em cenas, prefabs e testes reais em vez de aplicada às cegas apenas no código.

#### Critérios de Aceitação

1. WHEN Rooms, Room_Gates ou Spawn_Points (`EnemyRespawnPoint`) precisam ser adicionados ou reposicionados em uma cena ou prefab, THE Unity_MCP SHALL criar e posicionar esses GameObjects nas cenas e prefabs correspondentes por meio das ferramentas de manipulação de GameObject e prefab do Editor.
2. WHEN os componentes Progression_Director e Stage_Generator precisam ser configurados, THE Unity_MCP SHALL adicionar, configurar e conectar esses componentes e suas referências serializadas por meio das ferramentas de componente do Editor, sem introduzir `GameObject.Find`, `FindObjectOfType` ou strings mágicas.
3. WHEN uma Room_Composition precisa ser validada, THE Unity_MCP SHALL instanciar e verificar os inimigos por arquétipo utilizando os prefabs de arquétipo existentes e os Spawn_Points (`EnemyRespawnPoint`) da Combat_Room dentro do Editor.
4. WHEN um Room_Graph gerado precisa ser validado, THE Unity_MCP SHALL inspecionar a hierarquia da cena, capturar uma captura de tela do layout gerado e ler o console do Editor para confirmar as mensagens de aviso e de erro de geração definidas nos Requisitos 1, 5 e 7.
5. WHEN as propriedades de correção do sistema precisam ser verificadas, THE Unity_MCP SHALL executar os testes EditMode e PlayMode existentes do projeto por meio das ferramentas de teste do Editor e reportar o resultado de cada teste.
6. WHILE realiza edições dirigidas pelo Unity_MCP em assets do projeto, THE Unity_MCP SHALL preservar os arquivos `.meta` de cada asset movido, renomeado ou removido, mantendo as referências de GUID existentes.
7. IF uma edição dirigida pelo Unity_MCP violaria as restrições do AGENTS.md (uso de `GameObject.Find`, `FindObjectOfType` ou strings mágicas na lógica de gameplay), THEN THE Unity_MCP SHALL não aplicar a edição e registrar a violação identificada para correção antes de prosseguir.
8. WHERE o servidor `unity-mcp` estiver indisponível no Editor, THE Unity_MCP SHALL reportar a indisponibilidade e não marcar como verificada nenhuma autoração de cena, prefab ou componente que dependa da inspeção in-Editor.
