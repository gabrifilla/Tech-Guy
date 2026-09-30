# Requirements Document

## Introduction

Esta feature torna a **geração procedural de fases/salas** — que **já existe e já está testada** (ver `.kiro/specs/procedural-stage-room-generation/` e `Assets/_Project/Scripts/Stages/`) — **efetivamente jogável** dentro de uma cena real, e faz dela a **missão inicial** da run. Esta é uma spec de **autoração de cena + integração de mundo**, **não** uma reimplementação do gerador. O gerador (`StageGenerator`), o diretor (`ProgressionDirector`), os parâmetros (`StageGenerationParams`), a fonte de semente (`RunSeedSource`), as portas (`EncounterGates`), o troféu/bênção (`RewardTrophy`/`RunBoons`) e o chefe (`SectorBoss`) permanecem como a base; o que esta spec cobre é o **mundo em volta**: a cena jogável, a **geometria por sala + NavMesh assado**, câmera, HUD, jogador, portais de entrada/extração, o **selo do chefe por fragmentos de acesso** e a ligação da tecla de descoberta.

**A abordagem já implementada (realidade atual do projeto):** em vez de criar uma cena `ProceduralSector` nova e separada, o projeto **reautorou a própria cena `Assets/_Project/Scenes/FirstSector.unity`** para ser a fase procedural jogável. O construtor de código `FirstSectorBuilder` (`Assets/_Project/Scripts/Editor/FirstSectorBuilder.cs`) monta essa cena: move jogador + câmera para dentro, cria o GameObject "First sector procedural flow" com `SwarmAttackCoordinator` + `ProceduralStageEnvironment` + `RunSeedSource` + `ProgressionDirector`, conecta **todas** as referências serializadas do `ProgressionDirector` (`_player`, `_seedSource`, `_enemyPrefab` = prefab do Grunt, `_environment`, `_stageCount = 1`, `_requiredBossAccessFragments = 3`, `_returnScene = "NexusLobby"`, `_objective` via `SharedHudBuilder`, `_archetypeCatalog` com 16 `EnemyArchetype`, `_archetypePrefabs` com 16 `EnemyVariant`), define os `StageGenerationParams` (5 salas de combate, `roomSize` 24x24, densidade 8–16, variedade 4, tesouro 0.5 / secreto 0.3, retry 50), gera um grafo representativo (semente 1701), **assa o NavMesh via `environment.Build(graph)` e persiste-o em `Assets/_Project/Art/FirstSector/Navigation.asset`** (asset de NavMesh assado em edit time, GUID preservado via `CopySerialized`), adiciona iluminação, valida os caminhos, captura uma screenshot de visão geral e salva a cena. O portal de saída do Nexus **já** aponta para `"FirstSector"` e o Build Settings **já** inclui essa cena, portanto **não há repontamento de portal a fazer**.

**A geometria não é um chão único plano.** O componente `ProceduralStageEnvironment` (`Assets/_Project/Scripts/Core/ProceduralStageEnvironment.cs`) constrói **geometria real por sala** a partir do mesmo `RoomGraph` usado pelo combate: piso navegável, paredes com aberturas de porta (largura `DoorWidth = 4`), rótulos TMP, materiais por tipo de sala e um de **9 perfis de arena** (`Arena_Shape`: AccessAtrium, OpenDeck, Crossroads, Octagon, SplitLanes, PillarCourt, MemoryVault, HiddenArchive, CoreSanctum). Sobre essa geometria ele adiciona um `NavMeshSurface` e chama `BuildNavMesh()`. Em runtime, `ProgressionDirector.GenerateAndMaterializeStage` chama `_environment.Build(_graph)` para reconstruir geometria + navegação a partir da semente da run **antes** de materializar as portas.

**Nova mecânica central — selo do chefe por fragmentos de acesso.** A conexão da `Boss_Room` é **selada** por um `Boss_Seal` que exige N `Access_Fragment` (padrão 3, `Min 1`, de `_requiredBossAccessFragments`). Cada `Combat_Room` limpa concede exatamente **um** fragmento ao reclamar a recompensa (`_bossAccessFragments++`), atualiza os soquetes visíveis do selo e só quando `HasBossAccess` as conexões do chefe destravam. Isso é o coração do laço de exploração da run: o jogador precisa limpar salas suficientes antes de conseguir alcançar o chefe.

**O fluxo alvo:** o jogador sai do `NexusLobby` por um portal cujo destino é `FirstSector`; joga **uma única Stage** procedural de ponta a ponta (Start → salas de combate que concedem fragmentos → destravar o selo do chefe → chefe); ao derrotar o chefe, um **portal de extração** (`Extraction_Portal`) deve se materializar na `Boss_Room` sobre uma posição alcançável de NavMesh, e o jogador **entra nele** para voltar ao Nexus (não uma transição instantânea). A morte do jogador continua devolvendo ao Nexus pelo fluxo de morte existente.

**Por que a tuning-alvo é assim (contexto de design):** a diversão da run é construída sobre o sistema de modificadores `RunBoons` (`Assets/_Project/Scripts/Core/RunBoons.cs`), cujo apelo vem das **sinergias de cascata elemental** (ignite/frost aplicam elementos; conductor encadeia acertos; detonation explode ao matar; resonance/reactor amplificam). Essas cascatas brilham com **inimigos densos e agrupados** e com **escolhas de bênção suficientes** para montar um build antes do chefe. Por isso a Stage jogável é dimensionada para 5 combates densos com variedade de arquétipos: dar ao jogador escolhas de bênção o bastante e lutas densas o bastante para as cascatas "acenderem" antes do chefe. Os valores de tuning são expressos como configuração de `StageGenerationParams` no Inspector, dentro das faixas já suportadas pelo gerador — nunca como valores mágicos codificados.

### Escopo

**No escopo:**

- Reautorar `Assets/_Project/Scenes/FirstSector.unity` (via `FirstSectorBuilder`) como a `Procedural_Sector_Scene`: jogador, câmera, `Player_HUD`, `Progression_Director`, `ProceduralStageEnvironment`, `RunSeedSource` totalmente conectados por referências serializadas.
- Geometria **por sala** gerada pelo `ProceduralStageEnvironment` (piso navegável, paredes com aberturas de porta, 9 `Arena_Shape`) e um `Stage_Floor` = essa geometria + **NavMesh assado em edit time** persistido em `Assets/_Project/Art/FirstSector/Navigation.asset`, cobrindo todas as salas; em runtime a geometria + navegação são reconstruídas a partir da semente da run.
- **Selo do chefe por fragmentos de acesso** (`Boss_Seal` / `Access_Fragment`): a conexão da `Boss_Room` fica selada até o jogador reunir N fragmentos, um por `Combat_Room` limpa.
- Reuso da mesma câmera de jogo (`TechGuy.Cameras.TG_TopDown_Camera` com `m_Target` = jogador), do mesmo `Player_HUD` (via `SharedHudBuilder`, objetivo `TMP_Text`), do mesmo `PlayerActor`/controles/loadout de arma.
- Ligação da `Discovery_Action` da `Secret_Room` à tecla real `E`, já lida em `ProceduralStageEnvironment.Update()` chamando `ProgressionDirector.TryDiscoverSecret()`, incluindo geometria de `Secret_Room` oculta via `SetRevealed` até a descoberta.
- **Portal de extração pós-chefe** (`Extraction_Portal`) na `Boss_Room`, reutilizando `ScenePortal`, que devolve o jogador ao Nexus quando ele entra nele. **Esta é a principal lacuna de implementação restante.**
- Tuning jogável de uma **única Stage** por run, expresso como valores configurados de `StageGenerationParams` dentro das faixas existentes.
- **Aposentar os validadores acoplados à antiga missão linear:** retirar `FirstSectorValidation` (totalmente acoplado ao `FirstSectorDirector`/`ClearedEncounters`/tamanhos fixos, que não existem mais) e **desacoplar** `ArsenalValidation` da missão linear (mantendo sua verificação de persistência de arma através do carregamento de cena).
- Autoração e verificação in-Editor via `Unity_MCP`.

**Fora do escopo (explicitamente excluído):**

- Reimplementar ou alterar a lógica de geração procedural (grafo, semente, densidade, variedade, portas) — já entregue e testada na spec `procedural-stage-room-generation`.
- Múltiplos chefes, múltiplas Stages por run, meta-progressão ou desbloqueáveis adicionais (`_stageCount` permanece 1); ficam para depois.
- Novos arquétipos de inimigo, nova arte/modelos 3D autorados à mão ou rework de combate/armas.
- Bake de NavMesh em runtime (o NavMesh é assado em edit time; em runtime a geometria + navegação são reconstruídas pelo `ProceduralStageEnvironment` a partir da semente, sem depender de bake in-play manual).
- Alterar código de terceiros (`Assets/_ThirdParty`, `Assets/RealToon`).

## Glossary

Termos reutilizados da spec `procedural-stage-room-generation`:

- **Stage** (Fase): Uma unidade de progressão gerada proceduralmente, composta por um Room_Graph com exatamente uma Start_Room, uma ou mais Combat_Rooms, exatamente uma Boss_Room e zero ou mais Special_Rooms.
- **Run**: Uma sessão de jogo contínua do jogador, iniciada a partir do Nexus_Scene e encerrada por morte ou por extração após o chefe.
- **Progression_Director**: O componente `ProgressionDirector` (`Assets/_Project/Scripts/Stages/ProgressionDirector.cs`) que dirige a Run via referências serializadas (`_player`, `_generationParams`, `_seedSource`, `_enemyPrefab`, `_archetypeCatalog`, `_archetypePrefabs`, `_environment`, `_requiredBossAccessFragments`, `_returnScene`, `_objective`, `_stageCount`) e expõe `TryDiscoverSecret()`, `CurrentGraph`, `BossAccessFragments`, `RequiredBossAccessFragments`, `HasBossAccess` e as propriedades somente leitura `CurrentStageIndex`, `ClearedRooms`, `IsRunComplete`, `ActiveRunSeed`, `IsStageBuilt`.
- **Stage_Generator**: O sistema puro (`StageGenerator`) que gera o Room_Graph a partir da Run_Seed e dos `StageGenerationParams`.
- **Room_Graph**: O conjunto de Rooms de uma Stage e suas Room_Connections, formando um grafo conexo navegável.
- **Room_Type**: A classificação de uma Room: Start_Room, Combat_Room, Boss_Room, Treasure_Room, Secret_Room.
- **Special_Room**: Termo coletivo para Treasure_Room e Secret_Room.
- **Room_Connection**: Uma ligação bidirecional entre duas Rooms adjacentes, materializada por uma porta direcional do Room_Gates.
- **Room_Gates**: O sistema existente (`EncounterGates`) que sela e abre entradas/saídas de sala; as portas são GameObjects de runtime com `NavMeshObstacle` fazendo carving. Suporta portas direcionais (`ConfigureDirectional`, `AddDoor`, `OpenDoor`, `SealDoor`) e o selo do chefe (`ConfigureBossSeal`, `SetBossSealProgress`, `IsBossSealReady`).
- **Run_Seed**: O valor inteiro de 64 bits que semeia toda a aleatoriedade da geração de uma Run, obtido do `RunSeedSource`.
- **Run_Boons**: O sistema existente (`RunBoons`, `Assets/_Project/Scripts/Core/RunBoons.cs`) que oferece a seleção de bênção por sala limpa.
- **Reward_Trophy**: O objeto reclamável existente (`RewardTrophy`) que, ao ser coletado, abre a seleção do Run_Boons.
- **Sector_Boss**: O componente existente (`SectorBoss`) que marca o inimigo-chefe.
- **Player_Actor**: O `PlayerActor` que representa o jogador, com seus controles e loadout de arma atuais.
- **Discovery_Action**: A ação lógica que revela uma Secret_Room, exposta pelo `ProgressionDirector.TryDiscoverSecret()`.
- **Density_Budget**: O orçamento configurável de inimigos por Combat_Room, escalonado pela profundidade da sala, dentro do intervalo [4, 30].
- **Variety_Target**: O número mínimo de Archetype_Ids distintos por Combat_Room, configurável dentro do intervalo [2, 6].
- **Nexus_Scene**: A cena de hub `NexusLobby` de onde a Run parte e para onde retorna.
- **Unity_MCP**: O conjunto de ferramentas do MCP for Unity (servidor `unity-mcp`) usado para criar, configurar, inspecionar e testar conteúdo do projeto diretamente dentro do Editor da Unity.

Termos novos ou redefinidos desta spec:

- **Procedural_Sector_Scene**: A cena jogável `Assets/_Project/Scenes/FirstSector.unity`, **reautorada** pelo `FirstSectorBuilder` para hospedar o Progression_Director, o ProceduralStageEnvironment, o RunSeedSource, o Player_Actor, a câmera, o Player_HUD e os portais, e onde a Run procedural acontece de ponta a ponta. **Não** é uma cena nova separada.
- **Stage_Floor**: A **geometria por sala gerada** pelo `ProceduralStageEnvironment` (piso navegável, paredes com aberturas de porta, perfis de arena) **mais o NavMesh** assado sobre ela — persistido em edit time em `Assets/_Project/Art/FirstSector/Navigation.asset` e reconstruído em runtime a partir da semente. **Não** é um único plano liso.
- **Arena_Shape**: Um dos 9 perfis de geometria por sala aplicados pelo `ProceduralStageEnvironment` (AccessAtrium, OpenDeck, Crossroads, Octagon, SplitLanes, PillarCourt, MemoryVault, HiddenArchive, CoreSanctum), resolvido por `ResolveArenaShape` conforme o Room_Type/Id.
- **Player_HUD**: O HUD existente (vida/mana e objetivo) instalado por `SharedHudBuilder`, com o objetivo exibido em um `TMP_Text` associado ao `_objective` do Progression_Director.
- **Discovery_Input**: A tecla **`E`** lida em `ProceduralStageEnvironment.Update()` (`Keyboard.current.eKey`) que aciona a Discovery_Action chamando `ProgressionDirector.TryDiscoverSecret()`.
- **Secret_Wall**: A parede/geometria oculta da Secret_Room, cujos `Renderer` são desligados por `ProceduralStageEnvironment.SetRevealed(roomId, false)` até a descoberta e religados na revelação.
- **Extraction_Portal**: O portal de extração pós-chefe, um `ScenePortal` a ser materializado na Boss_Room ao derrotar o chefe, cujo destino é a Nexus_Scene e no qual o jogador entra para retornar ao Nexus. **Lacuna de implementação restante.**
- **Boss_Seal**: O selo que trava a(s) conexão(ões) da Boss_Room, exigindo `RequiredBossAccessFragments` fragmentos; materializado por `EncounterGates.ConfigureBossSeal` com soquetes visíveis atualizados por `SetBossSealProgress`.
- **Access_Fragment**: A unidade de progresso do Boss_Seal, concedida (uma por vez) ao reclamar a recompensa de cada Combat_Room limpa (`_bossAccessFragments`), até `HasBossAccess`.

## Requirements

### Requisito 1: Cena procedural jogável de ponta a ponta (FirstSector reautorada)

**User Story:** Como jogador, quero uma cena real onde a fase procedural roda de verdade, para que eu consiga jogar uma run inteira do começo ao fim.

#### Critérios de Aceitação

1. THE Procedural_Sector_Scene SHALL ser o asset de cena `Assets/_Project/Scenes/FirstSector.unity`, reautorado pelo `FirstSectorBuilder`, contendo exatamente um Progression_Director, exatamente um Player_Actor, exatamente uma câmera de jogo e exatamente um Player_HUD.
2. THE Procedural_Sector_Scene SHALL não conter nenhum `FirstSectorDirector`, de modo que a antiga missão linear não coexista com o fluxo procedural.
3. THE Progression_Director da Procedural_Sector_Scene SHALL ter todas as suas referências serializadas obrigatórias atribuídas (`_player`, `_generationParams`, `_enemyPrefab`, `_archetypeCatalog`, `_archetypePrefabs`, `_environment`, `_objective`), a referência `_seedSource` atribuída via `RunSeedSource` na mesma cena, `_stageCount` = 1 e `_requiredBossAccessFragments` = 3.
4. THE Progression_Director e os componentes conectados SHALL resolver todas as referências exclusivamente por referências serializadas ou componentes resolvidos explicitamente, sem uso de `GameObject.Find`, `FindObjectOfType` ou strings mágicas na lógica de gameplay.
5. WHEN a Procedural_Sector_Scene entra em Play, THE Progression_Director SHALL adquirir a Run_Seed, gerar e materializar uma Stage (incluindo a chamada a `ProceduralStageEnvironment.Build`), posicionar o Player_Actor na Start_Room e abrir as portas de saída da Start_Room, reutilizando o pipeline de geração existente.
6. WHEN a Stage é materializada com sucesso, THE Progression_Director SHALL conduzir a Run de ponta a ponta pelo grafo (entrada em salas, combate, recompensa por sala limpa, selo do chefe, chefe) sem intervenção de autoração adicional.
7. IF a geração da Stage abortar durante o `Awake` do Progression_Director, THEN THE Progression_Director SHALL registrar a falha, não instanciar nenhuma Room parcial e manter `IsStageBuilt` falso.

### Requisito 2: Geometria por sala e NavMesh assado em edit time

**User Story:** Como jogador, quero pisar em um mundo sólido onde os inimigos aparecem no chão certo, para que a fase seja navegável e o combate funcione.

#### Critérios de Aceitação

1. WHEN uma Stage é materializada, THE ProceduralStageEnvironment SHALL construir a geometria por sala do Stage_Floor a partir do Room_Graph (piso navegável, paredes com aberturas de porta de largura `DoorWidth`, e um Arena_Shape por sala), cobrindo todas as Rooms geradas.
2. THE ProceduralStageEnvironment SHALL adicionar um `NavMeshSurface` sobre a geometria gerada e construir a navegação (`BuildNavMesh()`), de modo que o NavMesh cubra as áreas navegáveis de todas as Rooms.
3. THE Procedural_Sector_Scene SHALL ter um NavMesh assado em edit time persistido em `Assets/_Project/Art/FirstSector/Navigation.asset`, com o GUID do asset preservado entre reconstruções (via `CopySerialized`).
4. WHEN a Procedural_Sector_Scene entra em Play, THE ProceduralStageEnvironment SHALL reconstruir a geometria e a navegação a partir da semente da run, sem depender de um bake de NavMesh disparado manualmente em runtime.
5. WHEN uma Combat_Room é ativada, THE Progression_Director SHALL conseguir amostrar posições válidas de NavMesh dentro dos limites da sala para spawn de inimigos, de modo que os inimigos instanciados fiquem sobre o NavMesh.
6. WHEN uma Combat_Room é marcada como limpa, THE Progression_Director SHALL conseguir posicionar o Reward_Trophy sobre uma posição válida e alcançável de NavMesh dentro dos limites da sala.
7. IF o NavMesh não estiver assado ou não cobrir as Rooms do Room_Graph representativo, THEN a Procedural_Sector_Scene SHALL ser considerada não verificada e o problema SHALL ser registrado para correção antes de marcar a cena como jogável.

### Requisito 3: Entrada do Nexus e Build Settings

**User Story:** Como jogador, quero que o portal do Nexus me leve à fase procedural, para que a run procedural seja a missão inicial, sem quebrar o resto do projeto.

#### Critérios de Aceitação

1. THE Build Settings (`ProjectSettings/EditorBuildSettings.asset`) SHALL incluir `Assets/_Project/Scenes/FirstSector.unity` como cena habilitada.
2. THE Build Settings SHALL incluir a Nexus_Scene (`NexusLobby`) habilitada, de modo que o retorno ao Nexus e a extração permaneçam carregáveis.
3. THE ScenePortal de saída do NexusLobby SHALL ter seu destino configurado como `"FirstSector"` via `ScenePortal.Configure(player, "FirstSector")`, sem repontamento adicional necessário.
4. WHEN o Player_Actor entra no raio do ScenePortal de saída do NexusLobby, THE ScenePortal SHALL carregar a Procedural_Sector_Scene reutilizando o carregamento guardado existente (`Application.CanStreamedLevelBeLoaded` antes de `LoadSceneAsync`).
5. IF a Procedural_Sector_Scene não estiver disponível no Build Settings quando o ScenePortal do Nexus for acionado, THEN THE ScenePortal SHALL registrar uma mensagem de erro identificando a cena de destino ausente e não iniciar o carregamento.

### Requisito 4: Reuso de câmera, HUD, jogador, controles e loadout

**User Story:** Como jogador, quero que a fase procedural jogue exatamente como a missão atual, com o mesmo HUD e controles, mas com a câmera um pouco mais afastada para eu enxergar melhor a arena e a horda ao meu redor, no estilo top-down de action-RPG, para não ter que reaprender nada e ainda ler bem o ambiente e os inimigos.

#### Critérios de Aceitação

1. THE Procedural_Sector_Scene SHALL reutilizar a câmera `TechGuy.Cameras.TG_TopDown_Camera` com seu `m_Target` apontado para o Player_Actor por referência serializada.
2. THE Procedural_Sector_Scene SHALL reutilizar o Player_HUD (vida/mana e objetivo) instalado por `SharedHudBuilder`, com o objetivo vinculado ao `_objective` (`TMP_Text`) do Progression_Director.
3. THE Procedural_Sector_Scene SHALL reutilizar o mesmo Player_Actor, com os mesmos controles (`CharControlScript` com `mainCamera` serializado) e o mesmo loadout de arma da missão atual.
4. WHERE o Player_Actor persiste a partir do Nexus, THE Procedural_Sector_Scene SHALL preservar o estado de controle do jogador (input habilitado) ao iniciar a Run.
5. WHEN a câmera é inicializada na Procedural_Sector_Scene, THE câmera SHALL ter seu alvo apontado para o Player_Actor por referência serializada, sem `GameObject.Find`/`FindObjectOfType`.
6. THE `TG_TopDown_Camera` SHALL ser configurada com um enquadramento mais afastado que o padrão atual (valores de `m_Height` e `m_Distance` maiores que os padrões 9 e 10, respectivamente), de modo que o jogador visualize a maior parte de uma Combat_Room de 24x24 unidades e a horda ao seu redor, no estilo top-down de action-RPG (referência: Diablo 4 / Lost Ark), com os campos `m_Height`, `m_Distance` e `m_Angle` permanecendo expostos e ajustáveis no Inspector; um ponto de partida sugerido (não obrigatório) é `m_Height` ~16 e `m_Distance` ~16, deixando o ajuste fino para a autoração/verificação em Play.
7. WHEN o `FirstSectorBuilder` autora a Procedural_Sector_Scene, THE autoração SHALL configurar o `TG_TopDown_Camera` com esses valores de enquadramento afastado por meio dos campos serializados da câmera (`m_Height`, `m_Distance`, `m_Angle`), em vez de depender apenas de uma posição de transform fixa, mantendo `m_Target` = Player_Actor.

### Requisito 5: Tuning jogável de uma única Stage (configuração de StageGenerationParams)

**User Story:** Como jogador, quero uma fase de tamanho gostoso — nem curta demais, nem arrastada — com combates densos e variados até o chefe, para que a run seja divertida e me deixe montar um build.

#### Critérios de Aceitação

1. THE Progression_Director SHALL manter `_stageCount` igual a 1, de modo que a Run seja composta por exatamente uma Stage.
2. THE StageGenerationParams configurado na Procedural_Sector_Scene SHALL definir `_minCombatRooms` e `_maxCombatRooms` como valores iguais dentro da faixa existente (mínimo >= 1, máximo <= 20) que produzam exatamente 5 Combat_Rooms.
3. THE Stage gerada SHALL conter no mínimo 5 Rooms no total (1 Start_Room + 5 Combat_Rooms + 1 Boss_Room + Special_Rooms eventuais), resultando em um grafo-alvo de aproximadamente 7 Rooms com as salas especiais.
4. THE StageGenerationParams configurado SHALL definir os limites de `Density_Budget` dentro da faixa existente [4, 30] em valores de aproximadamente 8 a 16 inimigos por sala, com a densidade crescendo monotonicamente pela profundidade (garantia já provida pelo gerador).
5. THE StageGenerationParams configurado SHALL definir `Variety_Target` dentro da faixa existente [2, 6] em um valor de aproximadamente 4 arquétipos distintos por Combat_Room.
6. THE StageGenerationParams configurado SHALL definir `RoomSize` como 24x24 unidades (`_roomSize`) e as probabilidades de Special_Room (`_treasureProbability` ~0.5, `_secretProbability` ~0.3) dentro da faixa existente [0.05, 0.95].
7. THE Progression_Director SHALL obter todos esses valores de tuning de configuração serializada de `StageGenerationParams` (valores de Inspector), sem strings mágicas nem constantes de tuning codificadas na lógica de gameplay.

### Requisito 6: Selo do chefe por fragmentos de acesso

**User Story:** Como jogador, quero que a sala do chefe fique trancada até eu limpar salas suficientes e reunir fragmentos de acesso, para que explorar e combater tenham um propósito claro antes do confronto final.

#### Critérios de Aceitação

1. WHEN o Room_Graph é materializado, THE Progression_Director SHALL selar a(s) conexão(ões) da Boss_Room com um Boss_Seal que exige `RequiredBossAccessFragments` (padrão 3, `Min 1`, de `_requiredBossAccessFragments`) Access_Fragments, materializado por `EncounterGates.ConfigureBossSeal` com um soquete visível por fragmento.
2. WHEN o jogador reclama a recompensa de uma Combat_Room limpa (escolhe uma bênção), THE Progression_Director SHALL conceder exatamente um Access_Fragment (`_bossAccessFragments++`) e atualizar os soquetes visíveis do Boss_Seal (`SetBossSealProgress`), enquanto `HasBossAccess` for falso.
3. WHILE o número de Access_Fragments for menor que `RequiredBossAccessFragments`, THE Progression_Director SHALL manter a(s) conexão(ões) da Boss_Room seladas, de modo que a porta do chefe não abra mesmo que seja alcançada.
4. WHEN o número de Access_Fragments atinge `RequiredBossAccessFragments` E a sala de aproximação ao chefe está limpa ou é a Start_Room, THE Progression_Director SHALL destravar e abrir a(s) conexão(ões) do chefe (`TryUnlockBossConnections`).
5. THE Progression_Director SHALL comunicar o progresso dos fragmentos no Player_HUD como "FRAGMENTOS DE ACESSO x/N" junto à dica de descoberta enquanto `HasBossAccess` for falso, e sinalizar a abertura do selo quando `HasBossAccess` for verdadeiro (`UpdateExplorationObjective`).

### Requisito 7: Descoberta de sala secreta e parede secreta

**User Story:** Como jogador, quero encontrar e abrir salas secretas apertando uma tecla perto da parede certa, para ser recompensado por explorar.

#### Critérios de Aceitação

1. WHERE uma Secret_Room é gerada, THE ProceduralStageEnvironment SHALL manter a geometria dessa Secret_Room oculta (`SetRevealed(roomId, false)` desliga seus `Renderer`) até a descoberta, apresentando uma Secret_Wall que oculta a Room_Connection de acesso.
2. THE Procedural_Sector_Scene SHALL vincular a Discovery_Input à tecla `E`, lida em `ProceduralStageEnvironment.Update()`, que chama `ProgressionDirector.TryDiscoverSecret()`.
3. WHEN o jogador aciona a Discovery_Input estando em uma Room diretamente adjacente a uma Secret_Room gerada (compartilhando uma Room_Connection oculta), com nenhum inimigo vivo e nenhuma recompensa pendente, THE Progression_Director SHALL revelar a Secret_Room (`_environment.SetRevealed(secret.Id, true)`) e abrir a Room_Connection correspondente em até 1 segundo, exibindo um indicador visual de revelação.
4. IF o jogador aciona a Discovery_Input sem estar em uma Room diretamente adjacente a uma Secret_Room gerada, THEN THE Progression_Director SHALL manter todas as Secret_Rooms ocultas e não abrir nenhuma Room_Connection.
5. WHEN a Secret_Room é revelada, THE Room_Connection correspondente SHALL deixar de bloquear a passagem, permitindo o traversal bidirecional.

### Requisito 8: Portal de extração pós-chefe

**User Story:** Como jogador, quero um portal de extração que aparece quando eu derroto o chefe e no qual eu entro para voltar ao Nexus, para encerrar a run com uma saída clara em vez de um corte de cena instantâneo.

#### Critérios de Aceitação

1. WHILE o chefe da Boss_Room não estiver derrotado, THE Progression_Director SHALL manter o Extraction_Portal não materializado ou inativo.
2. WHEN o chefe da Boss_Room é derrotado, THE Progression_Director SHALL materializar o Extraction_Portal (um `ScenePortal`) dentro dos limites da Boss_Room, sobre uma posição alcançável de NavMesh, em até 1 segundo após o registro da derrota.
3. THE Extraction_Portal SHALL ter seu destino configurado como a Nexus_Scene por configuração explícita (`ScenePortal.Configure(player, destination)`), sem string mágica na lógica de gameplay.
4. WHEN o Player_Actor entra no raio do Extraction_Portal, THE Extraction_Portal SHALL carregar a Nexus_Scene reutilizando o carregamento guardado existente (`Application.CanStreamedLevelBeLoaded` antes de `LoadSceneAsync`).
5. IF a Nexus_Scene não estiver disponível para carregamento quando o Extraction_Portal for acionado, THEN THE Extraction_Portal SHALL registrar uma mensagem de erro identificando a cena de destino ausente e não iniciar o carregamento.
6. WHEN o Player_Actor morre durante a Stage, THE Progression_Director SHALL retornar o jogador à Nexus_Scene reutilizando o fluxo de retorno por morte existente, independentemente do Extraction_Portal.

### Requisito 9: Salas especiais fora do caminho principal

**User Story:** Como jogador, quero que salas de tesouro fiquem fora da rota principal e me deem uma bênção sem luta, para recompensar quem explora sem obrigar combate extra.

#### Critérios de Aceitação

1. WHERE uma Treasure_Room é gerada, THE Stage_Generator SHALL posicioná-la fora do caminho principal Start_Room → Boss_Room, conectada ao Room_Graph por pelo menos uma Room_Connection acessível a partir da Start_Room.
2. WHEN o Player_Actor entra em uma Treasure_Room, THE Progression_Director SHALL conceder a recompensa por meio do sistema Reward_Trophy / Run_Boons existente, sem exigir combate naquela Room.
3. WHERE uma Secret_Room é gerada, THE Stage_Generator SHALL posicioná-la fora do caminho principal, com sua Room_Connection oculta até a Discovery_Action correspondente.
4. THE Stage_Generator SHALL determinar a presença de cada Special_Room de forma probabilística a partir da Run_Seed, de modo que nem toda Run apresente todas as Special_Rooms.

### Requisito 10: Aposentar validadores acoplados à antiga missão linear

**User Story:** Como desenvolvedor, quero remover a validação acoplada à missão linear que não existe mais, para que o projeto compile e os testes reflitam a fase procedural real.

#### Critérios de Aceitação

1. THE projeto SHALL retirar (remover ou desabilitar) `Assets/_Project/Scripts/Editor/FirstSectorValidation.cs`, que depende de `FirstSectorDirector`, `ClearedEncounters`, `IsComplete`, `Boons` e de tamanhos fixos de encontro que não existem mais no fluxo procedural.
2. THE `Assets/_Project/Scripts/Editor/ArsenalValidation.cs` SHALL ser desacoplado da antiga missão linear: suas suposições de "FirstSector como missão linear" SHALL ser removidas/atualizadas de modo que ele continue validando a persistência de arma/loadout através do carregamento de `"FirstSector"`, sem depender de `FirstSectorDirector`.
3. WHERE o desacoplamento de `ArsenalValidation` do carregamento de `"FirstSector"` não puder ser feito de forma limpa mantendo a verificação de persistência de arma, THE projeto SHALL retirar apenas o trecho acoplado à missão linear, preservando as verificações de arsenal independentes de cena.
4. THE código de validação de Editor SHALL não referenciar `FirstSectorDirector` após a retirada.
5. WHEN a retirada e o desacoplamento estiverem concluídos, THE projeto SHALL compilar sem erros e os testes EditMode/PlayMode existentes (incluindo `FirstSectorProceduralTests`) SHALL passar.

### Requisito 11: Conformidade estrutural e AGENTS.md na autoração de cena

**User Story:** Como desenvolvedor, quero que a autoração da cena respeite as regras do projeto, para não corromper GUIDs, referências ou assets existentes.

#### Critérios de Aceitação

1. WHEN qualquer asset (cena, prefab, material, NavMesh) é criado, movido, renomeado ou removido durante a autoração, THE código de autoração SHALL preservar os arquivos `.meta` correspondentes, mantendo os GUIDs existentes — incluindo o GUID de `Assets/_Project/Art/FirstSector/Navigation.asset`, preservado via `CopySerialized`.
2. THE código de autoração de cena SHALL reutilizar os assets existentes (Player_Actor, câmera, Player_HUD, ScenePortal, prefabs de inimigo, arquétipos) movendo/instanciando-os em vez de recriá-los.
3. THE código de autoração e os componentes conectados SHALL evitar `GameObject.Find`, `FindObjectOfType` e strings mágicas na lógica de gameplay, usando referências serializadas ou componentes resolvidos explicitamente.
4. THE autoração SHALL não modificar código de terceiros (`Assets/_ThirdParty`, `Assets/RealToon`) nem alterar GUIDs, nomes de assets referenciados ou caminhos especiais da Unity sem verificar as cenas, prefabs e ScriptableObjects dependentes.
5. THE assets em `Assets/Resources` SHALL ser tratados como gerenciados pela Unity, movidos apenas após confirmar que não são usados por `Resources.Load`.

### Requisito 12: Fluxo de desenvolvimento e verificação via MCP da Unity

**User Story:** Como desenvolvedor, quero autorar e validar a cena procedural jogável diretamente dentro do Editor da Unity por meio do Unity_MCP, para que cada mudança seja verificada em cenas, NavMesh e testes reais em vez de aplicada às cegas apenas no código.

#### Critérios de Aceitação

1. WHEN a Procedural_Sector_Scene, a geometria por sala, os portais e o Player_HUD precisam ser criados ou posicionados, THE Unity_MCP SHALL criar e configurar esses GameObjects e componentes na cena por meio das ferramentas de manipulação de GameObject, componente e cena do Editor.
2. WHEN o Progression_Director, o ProceduralStageEnvironment e o StageGenerationParams precisam ser configurados, THE Unity_MCP SHALL conectar todas as referências serializadas e definir os valores de tuning por meio das ferramentas de componente do Editor, sem introduzir `GameObject.Find`, `FindObjectOfType` ou strings mágicas.
3. WHEN o NavMesh precisa ser validado, THE Unity_MCP SHALL capturar uma captura de tela do NavMesh assado e do layout gerado e confirmar por inspeção in-Editor que o NavMesh cobre as Rooms do Room_Graph e que o enquadramento afastado da câmera (a Combat_Room de 24x24 e a horda ao redor visíveis) está de acordo com o Requisito 4.6.
4. WHEN a Run precisa ser validada, THE Unity_MCP SHALL entrar em Play e confirmar que os inimigos aparecem sobre o NavMesh e que a Run é jogável de ponta a ponta (fragmentos → selo do chefe → chefe → Extraction_Portal → Nexus), e que a morte do jogador retorna ao Nexus.
5. WHEN o console precisa ser inspecionado, THE Unity_MCP SHALL ler o console do Editor para confirmar as mensagens de aviso e de erro de geração definidas nos Requisitos 1, 5, 6 e 8.
6. WHEN as propriedades de correção do sistema precisam ser verificadas, THE Unity_MCP SHALL executar os testes EditMode e PlayMode existentes do projeto (incluindo `FirstSectorProceduralTests`) por meio das ferramentas de teste do Editor e reportar o resultado de cada teste.
7. WHILE realiza edições dirigidas pelo Unity_MCP em assets do projeto, THE Unity_MCP SHALL preservar os arquivos `.meta` de cada asset movido, renomeado ou removido, mantendo as referências de GUID existentes.
8. IF uma edição dirigida pelo Unity_MCP violaria as restrições do AGENTS.md (uso de `GameObject.Find`, `FindObjectOfType` ou strings mágicas na lógica de gameplay), THEN THE Unity_MCP SHALL não aplicar a edição e registrar a violação identificada para correção antes de prosseguir.
9. WHERE o servidor `unity-mcp` estiver indisponível no Editor, THE Unity_MCP SHALL reportar a indisponibilidade e não marcar como verificada nenhuma autoração de cena, prefab, componente, NavMesh, portal de extração ou fluxo que dependa da inspeção in-Editor.
