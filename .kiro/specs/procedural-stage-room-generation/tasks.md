# Implementation Plan: Geração Procedural de Fases Baseada em Salas

## Overview

Esta implementação segue uma abordagem **bottom-up** e **dirigida por testes**: primeiro o núcleo de dados puros e a RNG determinística, depois o algoritmo de geração e o planejamento de composição, seguidos pelos testes de propriedade (EditMode) que validam as 18 Correctness Properties do design. Só então entram as camadas de cena (extensão aditiva de `EncounterGates`, resolver de spawn e o `ProgressionDirector`), os testes de integração PlayMode e, por fim, a verificação in-Editor via Unity_MCP.

Cada tarefa constrói sobre as anteriores e termina integrando o novo código ao fluxo existente. Todo o novo código de gameplay fica sob o novo domínio `Assets/_Project/Scripts/Stages/`, seguindo o AGENTS.md (PascalCase para tipos/métodos, `_camelCase` para campos privados serializados, sem `GameObject.Find`/`FindObjectOfType`/strings mágicas, validação de dependências em `Awake`/`OnValidate`, preservação de `.meta`).

Linguagem de implementação: **C#** (o design usa C# explicitamente — nenhuma seleção de linguagem é necessária).

## Tasks

- [x] 1. Criar o modelo de dados puro do grafo de salas
  - [x] 1.1 Definir os enums `RoomType` e `Direction`
    - Criar `Assets/_Project/Scripts/Stages/Model/RoomType.cs` com `Start, Combat, Boss, Treasure, Secret`.
    - Criar `Assets/_Project/Scripts/Stages/Model/Direction.cs` com `North, South, East, West`.
    - Sem dependência de `UnityEngine` além de tipos de valor.
    - _Requisitos: 4.1_

  - [x] 1.2 Implementar `RoomComposition` e `ArchetypeSlot`
    - Criar `Assets/_Project/Scripts/Stages/Model/RoomComposition.cs` com `Slots` (`ArchetypeId` + `count`), `TargetDensity` e `DistinctArchetypes` como propriedades somente leitura.
    - Referenciar `ArchetypeId` do `Archetype_System` existente.
    - _Requisitos: 5.1, 5.2, 5.3_

  - [x] 1.3 Implementar `Room` e `RoomConnection`
    - Criar `Assets/_Project/Scripts/Stages/Model/Room.cs` com `Id`, `Type`, `Center`, `Size`, `Depth`, `Connections`, `Composition` e flags de runtime `Visited/Cleared/Revealed` (fora da igualdade de grafo).
    - Criar `Assets/_Project/Scripts/Stages/Model/RoomConnection.cs` com `RoomAId`, `RoomBId`, `SideFromA`, `Hidden`, `Open`.
    - _Requisitos: 1.3, 4.8_

  - [x] 1.4 Implementar `RoomGraph` com `StructurallyEquals`
    - Criar `Assets/_Project/Scripts/Stages/Model/RoomGraph.cs` com `Rooms`, `Connections`, `StartRoomId`, `BossRoomId`.
    - Implementar `StructurallyEquals(RoomGraph other)` comparando nº de Rooms, Room IDs, Room_Connections, Room_Types e Room_Composition de cada Room (base para igualdade de grafo).
    - _Requisitos: 1.3, 2.3_

  - [x] 1.5 Implementar `StageGenerationParams` com clamps e `OnValidate`
    - Criar `Assets/_Project/Scripts/Stages/StageGenerationParams.cs` `[Serializable]` com `[SerializeField] private` para `_minCombatRooms (Min 1)`, `_maxCombatRooms (clamp <=20)`, `_densityBudgetMin/_densityBudgetMax (Range 4–30)`, `_varietyTarget (Range 2–6)`, `_treasureProbability/_secretProbability (Range 0.05–0.95)`, `_generationRetryLimit (Min 1)`.
    - Expor propriedades somente leitura; `OnValidate` faz clamp de todos os intervalos.
    - _Requisitos: 1.1, 4.3, 5.2, 5.3, 1.7_

- [x] 2. Implementar a RNG determinística e a derivação de próxima seed
  - [x] 2.1 Criar a RNG isolada semeada pela Run_Seed
    - Criar `Assets/_Project/Scripts/Stages/SeededRng.cs` encapsulando `Unity.Mathematics.Random` (ou `System.Random` derivado) semeada exclusivamente por um `ulong` Run_Seed, sem consumir `UnityEngine.Random` global.
    - Expor helpers determinísticos (int em intervalo, roll de probabilidade, escolha ponderada).
    - _Requisitos: 2.5_

  - [x] 2.2 Implementar a derivação de próxima seed (splitmix64)
    - Adicionar função pura `SplitMix64(ulong currentSeed) -> ulong` para derivar `nextSeed` deterministicamente.
    - _Requisitos: 7.5_

  - [x] 2.3 Escrever teste de propriedade: determinismo da próxima seed
    - **Property 17: Determinismo da próxima seed**
    - _Validates: Requirements 7.5_
    - Usar o harness `PropertyCheck.ForAll` (padrão de `SoftGroupingBoundsPropertyTests`), ≥100 casos, em `Assets/_Project/Scripts/Tests/EditMode/Editor/`.

- [x] 3. Implementar o `RoomCompositionPlanner` (núcleo puro)
  - [x] 3.1 Implementar orçamento de densidade escalado por profundidade
    - Criar `Assets/_Project/Scripts/Stages/RoomCompositionPlanner.cs` que calcula `TargetDensity` em `[4,30]` escalado monotonicamente por `Depth` (respeitando teto 30).
    - _Requisitos: 5.2, 5.5_

  - [x] 3.2 Implementar seleção de arquétipos e distribuição por variedade
    - Selecionar `DistinctArchetypes >= Variety_Target` (2–6) dentre os 16 `ArchetypeId`, limitado à disponibilidade quando esta for menor que o `Variety_Target`; distribuir `TargetDensity` inimigos entre os arquétipos escolhidos.
    - Usar a `SeededRng` para todas as decisões.
    - _Requisitos: 5.1, 5.3_

  - [x] 3.3 Escrever teste de propriedade: densidade dentro do orçamento
    - **Property 12: Densidade dentro do orçamento**
    - _Validates: Requirements 5.2_

  - [x] 3.4 Escrever teste de propriedade: densidade monótona por profundidade
    - **Property 13: Densidade monótona por profundidade**
    - _Validates: Requirements 5.5_

  - [x] 3.5 Escrever teste de propriedade: variedade satisfeita
    - **Property 14: Variedade satisfeita**
    - _Validates: Requirements 5.3_

- [x] 4. Implementar o `StageGenerator` (núcleo puro)
  - [x] 4.1 Criar o esqueleto do `StageGenerator` e o resultado de geração
    - Criar `Assets/_Project/Scripts/Stages/StageGenerator.cs` com `Generate(StageGenerationParams parameters, ulong runSeed)` retornando `StageGenerationResult` (`Success`, `Graph`, `FailureReason`), sem tocar cena.
    - Instanciar a `SeededRng` a partir de `runSeed`.
    - _Requisitos: 1.3, 2.3, 2.5_

  - [x] 4.2 Sortear contagens e presença de salas especiais
    - Sortear `combatCount` em `[minCombat, min(maxCombat,20)]`; rolls ponderados em `[5%,95%]` para presença de Treasure e Secret, no máximo uma de cada.
    - _Requisitos: 1.1, 4.3, 4.4, 4.5_

  - [x] 4.3 Construir o grafo conexo com crescimento semeado e não-sobreposição
    - Crescer a partir da `Start_Room` (depth 0) escolhendo salas/direções livres; posicionar cada sala com separação mínima 0 (AABB no plano XZ, folga ≥0, sem interpenetração); criar uma `RoomConnection` por par adjacente; anexar a `Boss_Room` ao final de um ramo profundo garantindo caminho `Start → Boss`; computar `Depth` como caminho mais curto até a Start.
    - _Requisitos: 1.2, 1.4, 5.5_

  - [x] 4.4 Atribuir tipos de sala e posicionar Treasure/Secret
    - Exatamente 1 `Start`, 1 `Boss`, demais `Combat`; colocar Treasure/Secret sorteadas em nós-folha adequados.
    - Treasure recebe ≥1 conexão acessível a partir da Start; se não for possível conectar, descartar a Treasure (preservando demais salas) e sinalizar a falha no resultado.
    - Secret com `RoomConnection` marcada `Hidden=true` e fechada.
    - _Requisitos: 4.1, 4.2, 4.6, 4.7, 4.8_

  - [x] 4.5 Anexar composições e spawn points às Combat_Rooms
    - Para cada `Combat_Room`, invocar o `RoomCompositionPlanner` para montar a `RoomComposition` e garantir ≥1 `EnemyRespawnPoint` dentro dos limites (marcação no modelo; posicionamento efetivo ocorre na ativação em runtime).
    - _Requisitos: 1.6, 5.1, 5.2, 5.3_

  - [x] 4.6 Marcar o chefe elegível e implementar retentativas/aborto
    - Marcar exatamente um inimigo principal da `Boss_Room` para `SectorBoss`; se não houver inimigo elegível, retornar `Success=false` sem grafo parcial.
    - Repetir a geração até 50 tentativas; em falha de conectividade `Start → Boss`, retornar `Success=false` + `FailureReason` sem grafo parcial.
    - _Requisitos: 1.7, 7.1, 7.2_

  - [x] 4.7 Escrever teste de propriedade: determinismo da geração
    - **Property 1: Determinismo da geração**
    - _Validates: Requirements 1.3, 2.3_

  - [x] 4.8 Escrever teste de propriedade: seeds diferentes divergem
    - **Property 2: Seeds diferentes divergem**
    - _Validates: Requirements 2.4_

  - [x] 4.9 Escrever teste de propriedade: conectividade Start → Boss
    - **Property 3: Conectividade Start → Boss**
    - _Validates: Requirements 1.2_

  - [x] 4.10 Escrever teste de propriedade: limites de contagem de salas
    - **Property 4: Limites de contagem de salas**
    - _Validates: Requirements 1.1, 4.2_

  - [x] 4.11 Escrever teste de propriedade: tipo único por sala
    - **Property 5: Tipo único por sala**
    - _Validates: Requirements 4.1_

  - [x] 4.12 Escrever teste de propriedade: no máximo uma Treasure e uma Secret
    - **Property 6: No máximo uma Treasure e uma Secret**
    - _Validates: Requirements 4.5_

  - [x] 4.13 Escrever teste de propriedade: probabilidade de especiais em [5%,95%] e reprodutível
    - **Property 7: Probabilidade de especiais dentro de [5%, 95%] e reprodutível**
    - _Validates: Requirements 4.3_

  - [x] 4.14 Escrever teste de propriedade: Treasure acessível ou descartada
    - **Property 8: Treasure acessível ou descartada**
    - _Validates: Requirements 4.6, 4.7_

  - [x] 4.15 Escrever teste de propriedade: não-sobreposição de salas
    - **Property 9: Não-sobreposição de salas**
    - _Validates: Requirements 1.4_

  - [x] 4.16 Escrever teste de propriedade: um par de portas por conexão
    - **Property 10: Um par de portas por conexão**
    - _Validates: Requirements 1.5_

  - [x] 4.17 Escrever teste de propriedade: cada Combat_Room tem spawn point
    - **Property 11: Cada Combat_Room tem spawn point**
    - _Validates: Requirements 1.6_

  - [x] 4.18 Escrever teste de propriedade: chefe único e elegível
    - **Property 15: Chefe único e elegível**
    - _Validates: Requirements 7.1, 7.2_

  - [x] 4.19 Escrever teste de propriedade: aleatoriedade isolada da RNG global
    - **Property 18: Aleatoriedade isolada da RNG global**
    - _Validates: Requirements 2.5_
    - Alterar a semente global entre duas gerações com a mesma `Run_Seed` não deve alterar o `RoomGraph`.

  - [x] 4.20 Escrever testes de exemplo/edge do núcleo puro
    - Casos: "0 disponibilidade de arquétipos < Variety_Target", "combatCount no limite 20", "Treasure descartada quando sem conexão acessível".
    - _Requisitos: 5.3, 1.1, 4.7_

- [x] 5. Checkpoint — validar o núcleo puro
  - Garantir que todos os testes EditMode do núcleo (`StageGenerator`, `RoomCompositionPlanner`, `SeededRng`) passem; perguntar ao usuário se surgirem dúvidas.

- [x] 6. Estender `EncounterGates` com API direcional aditiva
  - [x] 6.1 Adicionar a API por direção preservando a API legada
    - Editar `Assets/_Project/Scripts/Core/EncounterGates.cs` (sem mover/renomear, sem alterar `.meta`) adicionando `ConfigureDirectional`, `AddDoor(Direction)`, `OpenDoor(Direction)`, `SealDoor(Direction)`, `IsDoorSealed(Direction)`, `SealAll`.
    - Reusar o método privado `Door(...)` + `NavMeshObstacle carving`; portas direcionais em dicionário/array por `Direction`, separadas dos campos `_entrance`/`_exit` legados.
    - Manter a API legada de 2 portas intacta (o `FirstSectorDirector` continua funcionando).
    - _Requisitos: 1.5, 3.1, 3.4, 3.6, 4.8, 6.5, 6.7_

  - [x] 6.2 Escrever testes EditMode da API direcional
    - Verificar idempotência por direção, selagem/abertura por lado e `SealAll`, sem regressão da API legada.
    - _Requisitos: 3.1, 3.4, 3.6_

- [x] 7. Implementar o `RoomCompositionResolver` (orquestração de spawn)
  - [x] 7.1 Amostrar NavMesh e instanciar inimigos por arquétipo
    - Criar `Assets/_Project/Scripts/Stages/RoomCompositionResolver.cs` com `Activate(Room, IReadOnlyList<EnemyRespawnPoint>, ...)` retornando `RoomActivationResult`.
    - Para cada inimigo: amostrar até 20 posições NavMesh dentro dos limites da sala (estilo `FindReachableSpot`); instanciar via `EnemyRespawnPoint.SpawnEnemy()`; aplicar arquétipo via `EnemyVariant.Configure(EnemyArchetype)` selecionado do catálogo pelo `ArchetypeId`.
    - _Requisitos: 5.4, 5.6_

  - [x] 7.2 Tratar descartes e avisos de composição
    - Descartar inimigo sem ponto NavMesh após 20 tentativas com aviso (sala + nº descartado); avisar quando total posicionado < mínimo do budget (4); deixar composição vazia com aviso quando não houver `EnemyRespawnPoint` válido.
    - Marcar o chefe adicionando/obtendo `SectorBoss` no inimigo principal e chamando `boss.Configure(_player)`.
    - _Requisitos: 5.7, 5.8, 5.9, 7.1_

  - [x] 7.3 Escrever testes EditMode dos avisos/descartes do resolver
    - Cobrir os três ramos de aviso (R5.7, R5.8, R5.9) com dublês/mocks de spawn point e amostragem.
    - _Requisitos: 5.7, 5.8, 5.9_

- [x] 8. Implementar o `ProgressionDirector` (MonoBehaviour de orquestração)
  - [x] 8.1 Criar o esqueleto com validação de dependências e estado somente leitura
    - Criar `Assets/_Project/Scripts/Stages/ProgressionDirector.cs` com `[SerializeField] private` para `_player`, `_generationParams`, `_seedSource`, `_enemyPrefab`, `_archetypeCatalog`, `_returnScene`, `_objective`.
    - `Awake`/`OnValidate` logam exatamente uma mensagem nomeando cada dependência ausente; em `Awake` com dep faltando, `enabled=false` sem iniciar geração.
    - Expor `CurrentStageIndex`, `ClearedRooms`, `IsRunComplete` como propriedades somente leitura.
    - _Requisitos: 1.8, 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 8.2 Adquirir a Run_Seed com fallback e persistência/log
    - Ler a `RunSeedSource`; se ausente/inválida para 64 bits, logar erro e gerar seed de fallback de 64 bits; persistir e logar a seed usada.
    - _Requisitos: 2.1, 2.2, 2.6_

  - [x] 8.3 Materializar o grafo na cena
    - Chamar `StageGenerator.Generate`; em `Success=false`, logar erro e abortar sem instanciar sala parcial.
    - Em sucesso, materializar um par de `RoomGates` por `RoomConnection`, posicionar o jogador na `Start_Room` e abrir as saídas da Start.
    - _Requisitos: 1.5, 1.7, 7.2_

  - [x] 8.4 Implementar entrada em Combat_Room, selagem e ativação de composição
    - Ao entrar em Combat_Room não limpa: selar as portas da sala e ativar a `Room_Composition` via `RoomCompositionResolver`.
    - _Requisitos: 3.4, 3.5, 5.4_

  - [x] 8.5 Implementar limpeza de sala, troféu e recompensa
    - Ao chegar a zero inimigos vivos, marcar `Cleared` em ≤1s; instanciar um `RewardTrophy` em NavMesh alcançável (≤5 tentativas, fallback ao ponto alcançável mais próximo do centro com diagnóstico); no claim (≤2m) abrir `RunBoons.OfferReward` e remover o troféu; manter saídas seladas enquanto a seleção estiver ativa ou for cancelada; abrir conexões elegíveis a salas não visitadas ao escolher bênção (≤1s).
    - _Requisitos: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 3.2_

  - [x] 8.6 Escrever teste de propriedade: idempotência de reentrada
    - **Property 16: Idempotência de reentrada**
    - _Validates: Requirements 3.3_

  - [x] 8.7 Implementar Treasure_Room e Secret_Room em runtime
    - Ao entrar na Treasure_Room, conceder recompensa via `RewardTrophy`/`RunBoons` sem exigir combate.
    - Na `Discovery_Action` a partir de sala adjacente à Secret oculta, revelar a Secret e abrir a conexão em ≤1s com indicador visual; se não adjacente, manter tudo oculto sem abrir conexão.
    - _Requisitos: 4.9, 4.10, 4.11_

  - [x] 8.8 Implementar progressão de fase por derrota do chefe
    - Manter bloqueada a transição enquanto o chefe não for derrotado; ao derrotar (evento `SectorBoss`/`Died`), liberar transição em ≤1s.
    - Na transição, gerar a próxima Stage com seed derivada via `SplitMix64`; se não houver próxima Stage, concluir a run e retornar ao Nexus reusando o fluxo existente.
    - _Requisitos: 7.3, 7.4, 7.5, 7.6_

  - [x] 8.9 Implementar encerramento da run por morte do jogador
    - No `Died` do `PlayerActor`, marcar run encerrada em ≤0,5s e bloquear spawns/transições subsequentes; iniciar `LoadSceneAsync(NexusLobby)` em ≤1s com guarda `Application.CanStreamedLevelBeLoaded`; em cena ausente, logar erro e interromper sem carregar; em falha de carregamento, logar e fazer no máximo 1 nova tentativa; ao concluir, posicionar o jogador e restaurar input sem preservar estado transitório.
    - _Requisitos: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 9. Checkpoint — validar orquestração e EditMode
  - Garantir que todos os testes EditMode (núcleo + resolver + diredirector) passem; perguntar ao usuário se surgirem dúvidas.

- [x] 10. Escrever testes de integração PlayMode
  - [x] 10.1 Testar ativação de sala e spawn de inimigos
    - Em `Assets/_Project/Scripts/Tests/PlayMode`, validar spawn via `EnemyRespawnPoint` e aplicação de arquétipo em uma Combat_Room real.
    - _Requisitos: 5.4, 5.6_

  - [x] 10.2 Testar selagem e abertura de portas
    - Validar selagem na entrada de Combat_Room não limpa e abertura das conexões elegíveis após limpeza.
    - _Requisitos: 3.4, 3.6, 6.7_

  - [x] 10.3 Testar fluxo de troféu e RunBoons
    - Validar drop do `RewardTrophy` em NavMesh alcançável, claim e abertura da seleção de bênção.
    - _Requisitos: 6.2, 6.4, 6.7_

  - [x] 10.4 Testar morte do jogador e retorno ao Nexus
    - Validar encerramento da run e transição para `NexusLobby`.
    - _Requisitos: 8.1, 8.2, 8.3_

- [x] 11. Verificação in-Editor via Unity_MCP (Requisito 10)
  - [x] 11.1 Autorar cena/prefab e conectar componentes via Unity_MCP
    - Criar/posicionar `Room`s, `RoomGates` e `EnemyRespawnPoint`s em cena/prefabs pelas ferramentas de GameObject/prefab; adicionar e conectar `ProgressionDirector`/`StageGenerator` por referências serializadas, sem `GameObject.Find`/`FindObjectOfType`/strings mágicas; preservar `.meta`; bloquear e registrar violações de AGENTS.md; se `unity-mcp` estiver indisponível, reportar e não marcar como verificado.
    - _Requisitos: 10.1, 10.2, 10.3, 10.6, 10.7, 10.8_

  - [x] 11.2 Validar grafo e executar testes via Unity_MCP
    - Inspecionar a hierarquia da cena, capturar screenshot do layout gerado e ler o console para confirmar as mensagens de aviso/erro dos Requisitos 1, 5 e 7; executar os testes EditMode e PlayMode pelas ferramentas de teste do Editor e reportar cada resultado.
    - _Requisitos: 10.4, 10.5_

- [x] 12. Checkpoint final — garantir que todos os testes passem
  - Garantir que todos os testes EditMode/PlayMode passem e que a verificação via Unity_MCP esteja concluída; perguntar ao usuário se surgirem dúvidas.

## Notes

- Tarefas marcadas com `*` são opcionais (testes) e podem ser puladas para um MVP mais rápido; as demais são implementação central e devem ser executadas.
- Cada tarefa referencia requisitos específicos para rastreabilidade; tarefas de teste de propriedade referenciam a Property correspondente do design.
- Os checkpoints garantem validação incremental do núcleo puro antes de tocar a camada de cena.
- Os testes de propriedade usam o harness semeado `PropertyCheck.ForAll` (padrão de `SoftGroupingBoundsPropertyTests`), ≥100 casos, em `Assets/_Project/Scripts/Tests/EditMode/Editor/` (assembly `TechGuy.Tests.EditMode`); FsCheck/CsCheck não estão disponíveis nesta máquina.
- `EncounterGates.cs` recebe apenas mudança aditiva no mesmo arquivo — não é movido nem renomeado — preservando GUID e o `FirstSectorDirector`.
- Todo o novo código de gameplay fica sob `Assets/_Project/Scripts/Stages/`, seguindo o AGENTS.md.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "1.4", "1.5", "2.1", "2.2"] },
    { "id": 1, "tasks": ["2.3", "3.1", "3.2", "6.1"] },
    { "id": 2, "tasks": ["3.3", "3.4", "3.5", "4.1", "6.2"] },
    { "id": 3, "tasks": ["4.2", "4.3", "4.4", "4.5", "4.6"] },
    { "id": 4, "tasks": ["4.7", "4.8", "4.9", "4.10", "4.11", "4.12", "4.13", "4.14", "4.15", "4.16", "4.17", "4.18", "4.19", "4.20", "7.1"] },
    { "id": 5, "tasks": ["7.2", "8.1"] },
    { "id": 6, "tasks": ["7.3", "8.2", "8.3"] },
    { "id": 7, "tasks": ["8.4", "8.5"] },
    { "id": 8, "tasks": ["8.6", "8.7", "8.8", "8.9"] },
    { "id": 9, "tasks": ["10.1", "10.2", "10.3", "10.4"] },
    { "id": 10, "tasks": ["11.1"] },
    { "id": 11, "tasks": ["11.2"] }
  ]
}
```
