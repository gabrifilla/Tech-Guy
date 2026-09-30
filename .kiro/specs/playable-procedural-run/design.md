# Design Document

## Overview

Esta feature **não** reimplementa a geração procedural — ela é uma feature de **integração de cena + autoração de mundo** construída sobre o núcleo de geração já entregue e testado (`StageGenerator`, `ProgressionDirector`, `ProceduralStageEnvironment`, `EncounterGates`, `RewardTrophy`/`RunBoons`, `SectorBoss`; ver `.kiro/specs/procedural-stage-room-generation/`). A cena jogável é a `Assets/_Project/Scenes/FirstSector.unity` **reautorada** pelo construtor de código `FirstSectorBuilder` para hospedar o fluxo procedural de ponta a ponta.

**A maior parte dos 12 requisitos já está satisfeita pelo código atual.** O `ProgressionDirector` já adquire a semente, gera e materializa a Stage, constrói geometria + NavMesh via `ProceduralStageEnvironment`, posiciona o jogador, sela/abre portas por conexão, materializa o selo do chefe por fragmentos de acesso, concede um fragmento por `Combat_Room` limpa, revela salas secretas pela tecla `E`, conduz o combate até o chefe e devolve ao Nexus na morte. O `FirstSectorBuilder` já autora a cena, conecta todas as referências serializadas, define os `StageGenerationParams` (5 combates, 24x24, densidade 8–16, variedade 4, tesouro 0.5 / secreto 0.3), assa o NavMesh preservando o GUID de `Navigation.asset` e aponta o portal do Nexus para `"FirstSector"`; o Build Settings já lista `FirstSector` e `NexusLobby`.

Portanto este design cumpre dois papéis distintos e explicitamente separados ao longo do documento:

1. **Documentar a arquitetura "já implementada" (as-built)** — descrever, com nomes e assinaturas reais, como os componentes existentes cumprem os requisitos, para que a spec reflita a realidade do projeto.
2. **Especificar as 4 mudanças "a implementar"** que fecham as lacunas reais restantes:
   - **(A) Extraction_Portal** — materializar um `ScenePortal` na `Boss_Room` ao derrotar o chefe, substituindo o retorno instantâneo ao Nexus por uma saída física na qual o jogador entra (R8).
   - **(B) Afastamento de câmera** — o `FirstSectorBuilder` configura `TG_TopDown_Camera` (`m_Height`/`m_Distance` ~16) via campos serializados, em vez de só posicionar o transform (R4.6/R4.7).
   - **(C) Aposentar/desacoplar validadores** — retirar `FirstSectorValidation` (acoplado ao antigo `FirstSectorDirector`) e desacoplar `ArsenalValidation` da moldura de missão linear, mantendo a verificação de persistência de arma através do carregamento de cena (R10).
   - **(D) Verificação in-Editor ponta a ponta** — via Unity_MCP: NavMesh, enquadramento, Play até o portal, console, testes (R12).

### Mapa de requisitos → estado

| Requisito | Estado | Onde vive |
| --- | --- | --- |
| R1 Cena procedural jogável ponta a ponta | **Já implementado** | `ProgressionDirector`, `FirstSectorBuilder` |
| R2 Geometria por sala + NavMesh assado | **Já implementado** | `ProceduralStageEnvironment.Build`, `FirstSectorBuilder` (`CopySerialized`) |
| R3 Entrada do Nexus + Build Settings | **Já implementado** | `ScenePortal`, `FirstSectorBuilder.ConnectLobby`, `EditorBuildSettings.asset` |
| R4 Reuso câmera/HUD/jogador/loadout | **Parcial** — reuso já feito; **(B)** afastamento de câmera a implementar | `TG_TopDown_Camera`, `FirstSectorBuilder` |
| R5 Tuning de uma única Stage | **Já implementado** | `StageGenerationParams` no `FirstSectorBuilder` |
| R6 Selo do chefe por fragmentos | **Já implementado** | `ProgressionDirector`, `EncounterGates` (boss seal) |
| R7 Descoberta de sala secreta | **Já implementado** | `ProceduralStageEnvironment.Update` (`E`), `ProgressionDirector.TryDiscoverSecret` |
| R8 Portal de extração pós-chefe | **A implementar (A)** — hoje é retorno instantâneo | `ProgressionDirector` (novo ponto de inserção), `ScenePortal` |
| R9 Salas especiais fora do caminho | **Já implementado** (garantido pelo `StageGenerator`) | núcleo de geração |
| R10 Aposentar validadores | **A implementar (C)** | `FirstSectorValidation`, `ArsenalValidation` |
| R11 Conformidade AGENTS.md | **Já implementado / contínuo** | todo o código de autoração |
| R12 Fluxo de verificação Unity_MCP | **A implementar (D)** | processo de verificação |

## Architecture

O `NexusLobby` leva ao `FirstSector` (a `Procedural_Sector_Scene`) por um `ScenePortal`. Dentro dela, o `ProgressionDirector` orquestra o núcleo puro `StageGenerator` (gera o `RoomGraph`) e a camada de cena: `ProceduralStageEnvironment` (geometria + NavMesh), `EncounterGates` (portas + `Boss_Seal`), o `RoomCompositionResolver` (spawn de inimigos), `RewardTrophy`/`RunBoons` (recompensa + bênção), `SectorBoss` (chefe) e — **a implementar** — o `Extraction_Portal`, que devolve o jogador ao Nexus.

```mermaid
flowchart TD
    NX[NexusLobby] -->|ScenePortal destino=FirstSector| FS

    subgraph FS["FirstSector (Procedural_Sector_Scene)"]
        PD[ProgressionDirector<br/>orquestrador de cena]
        PA[PlayerActor + CharControlScript]
        CAM[TG_TopDown_Camera<br/>m_Target=jogador<br/>m_Height/m_Distance ~16 (B)]
        HUD[Player_HUD + objetivo TMP]

        subgraph Puro["Núcleo puro (inalterado)"]
            SG[StageGenerator]
            GRAPH[RoomGraph]
        end

        ENV[ProceduralStageEnvironment<br/>geometria 9 ArenaShape + NavMesh]
        GATES[EncounterGates<br/>portas direcionais + Boss_Seal]
        RES[RoomCompositionResolver<br/>spawn via EnemyRespawnPoint]
        RT[RewardTrophy] --> RB[RunBoons]
        SB[SectorBoss]
        EP[Extraction_Portal<br/>ScenePortal (A) - a implementar]
    end

    PD -->|params + seed| SG --> GRAPH
    PD -->|Build graph| ENV
    PD -->|1 porta por conexão + boss seal| GATES
    PD -->|ativa composição| RES
    PD -->|sala limpa: troféu| RT
    RB -->|RewardChosen: +1 fragmento| PD
    PD -->|marca chefe| SB
    SB -->|Died| PD
    PD -->|derrota do chefe, sem próxima Stage| EP
    EP -->|jogador entra: LoadSceneAsync guardado| NX
    PA -.Died.-> PD
    PD -.morte: ReturnToNexus direto (ignora portal).-> NX
    CAM --> PA
```

### Sequência de runtime (run completa)

```mermaid
sequenceDiagram
    participant P as PlayerActor
    participant PD as ProgressionDirector
    participant SG as StageGenerator
    participant ENV as ProceduralStageEnvironment
    participant G as EncounterGates
    participant RB as RunBoons
    participant SB as SectorBoss
    participant EP as Extraction_Portal

    Note over PD: Awake: valida deps (R1.3/R9). Falta obrigatória -> enabled=false (R1.7)
    PD->>PD: AcquireRunSeed (fonte + fallback + log) (R1.5)
    PD->>SG: Generate(params, seed)
    SG-->>PD: RoomGraph (5 combate + 1 chefe + especiais)
    PD->>ENV: Build(graph)  (geometria 9 ArenaShape + NavMesh)
    PD->>G: MaterializeGates (1 porta/conexão; ConfigureBossSeal+SetBossSealProgress na conexão do chefe) (R6.1)
    PD->>P: PositionPlayerAtStart + OpenStartRoomDoors (R1.5)

    loop Laço de combate (RunCombatLoop, ~0.15s)
        P->>PD: entra numa Combat_Room
        PD->>G: SealRoomDoors (R3/contenção)
        PD->>PD: ativa composição -> spawn no NavMesh (R2.5)
        P->>PD: inimigos == 0 -> ClearRoom
        PD->>PD: dropa RewardTrophy em ponto alcançável (R2.6)
        P->>RB: reclama -> OfferReward
        RB-->>PD: OnRewardChosen -> _bossAccessFragments++ (R6.2)
        PD->>G: UpdateBossSealProgress; se HasBossAccess: TryUnlockBossConnections (R6.4)
        PD->>PD: UpdateExplorationObjective ("FRAGMENTOS x/N" / "SELO ABERTO") (R6.5)
    end

    P->>PD: selo aberto -> entra na Boss_Room
    PD->>SB: chefe marcado; combate
    SB-->>PD: Boss_Room alive==0 -> ClearRoom -> OnBossDefeated (R8.2)
    Note over PD: (A) ponto de inserção do portal
    PD->>PD: StartCoroutine(ReleaseStageTransition ~0.75s) -> AdvanceToNextStageOrConclude
    alt sem próxima Stage (_stageCount==1) [A: hoje = ReturnToNexus instantâneo]
        PD->>EP: MaterializeExtractionPortal na Boss_Room (ponto NavMesh alcançável) + Configure(player, _returnScene)
        PD->>PD: objetivo HUD "SETOR PURIFICADO / entre no portal" + IsRunComplete=true
        P->>EP: entra no raio
        EP->>NX: CanStreamedLevelBeLoaded -> LoadSceneAsync(NexusLobby)
    end

    Note over P,PD: Ramo de morte (independe do portal)
    P->>PD: Died
    PD->>PD: IsRunComplete=true -> ReturnToNexusAfterDeath (~0.5s) -> ReturnToNexus() (R8.6)
```

### Camadas

- **Núcleo puro (inalterado):** `StageGenerator` + modelo `RoomGraph` — sem cena, testável em EditMode. Esta feature **não toca** nesta camada.
- **Camada de cena (onde a feature vive):** `ProgressionDirector` + `ProceduralStageEnvironment` + `EncounterGates`/portais. As 4 mudanças (A–D) são todas nesta camada ou no processo de autoração/verificação; nenhuma altera a geração.

## Components and Interfaces

Todos os scripts seguem o AGENTS.md: PascalCase para tipos/métodos/propriedades públicas; `_camelCase` para campos privados serializados com `[SerializeField] private`; validação de dependências em `Awake`/`OnValidate`; sem `GameObject.Find`/`FindObjectOfType`/strings mágicas na lógica de gameplay.

### 1. ProgressionDirector — as-built + mudança (A) Extraction_Portal

**Já implementado (as-built).** O orquestrador de cena (`Assets/_Project/Scripts/Stages/ProgressionDirector.cs`) expõe as referências serializadas `_player`, `_generationParams`, `_seedSource`, `_enemyPrefab`, `_archetypeCatalog`, `_archetypePrefabs`, `_environment`, `_requiredBossAccessFragments` (=3), `_returnScene` (="NexusLobby"), `_objective`, `_stageCount` (=1); e o estado somente-leitura `CurrentGraph`, `BossAccessFragments`, `RequiredBossAccessFragments`, `HasBossAccess`, `CurrentStageIndex`, `ClearedRooms`, `IsRunComplete`, `ActiveRunSeed`, `IsStageBuilt`. O fluxo de materialização é `GenerateAndMaterializeStage` → `_environment.Build(_graph)` → `MaterializeGates` (`EncounterGates.ConfigureDirectional(center, size, DoorWidth)`, `AddDoor` e, para a conexão do chefe, `ConfigureBossSeal` + `SetBossSealProgress`) → `PositionPlayerAtStart` → `OpenStartRoomDoors`. Cada `Combat_Room` limpa concede um fragmento em `OnRewardChosen` (`_bossAccessFragments++`), e `UpdateBossSealProgress`/`TryUnlockBossConnections`/`UpdateExplorationObjective` conduzem o selo.

**Lacuna atual (o que muda em A).** Hoje a derrota do chefe segue: `ClearRoom` → (sala do chefe) `OnBossDefeated` → `StartCoroutine(ReleaseStageTransition())` (espera `BossTransitionDelay` ~0.75s) → `AdvanceToNextStageOrConclude()`. Como `_stageCount == 1`, `hasNextStage == false`, então o método hoje define `IsRunComplete = true` e chama `ReturnToNexus()`, que faz um `LoadSceneAsync` **instantâneo e não-aditivo** para o Nexus (via `LoadNexusScene()`, guardado por `Application.CanStreamedLevelBeLoaded` + flag `_leaving` de sentido único + no máximo 1 retry). Isso viola R8.2/R8.4 (deveria ser um portal físico no qual o jogador entra), embora o ramo de morte já esteja correto.

**Mudança (A): materializar o Extraction_Portal em vez do retorno instantâneo.**

- **Reutilizar `ScenePortal` construído por código** (não um prefab serializado novo). Justificativa: `EncounterGates`, `RewardTrophy` (`RewardTrophy.Spawn(position, player, onClaimed)`) e as portas já são construídos em runtime por código, sem prefab. Um `_extractionPortalPrefab` serializado seria uma dependência a mais para autorar/validar em `Awake`, contra a garantia de "materializar sob demanda" já usada no diretor. Portanto o diretor cria um `GameObject`, adiciona `ScenePortal` e chama `Configure`.
- **Ponto de inserção exato.** Em `AdvanceToNextStageOrConclude`, no ramo `!hasNextStage` (sem próxima Stage), **substituir a chamada direta a `ReturnToNexus()`** por:
  1. `_bossDefeated` já está marcado (feito em `OnBossDefeated`); manter.
  2. `MaterializeExtractionPortal(bossRoom)`: novo método privado que amostra um **ponto de NavMesh alcançável dentro da `Boss_Room`** reusando exatamente o mesmo `FindReachableSpot(room)` que posiciona o `RewardTrophy` (ring sampling + `NavMesh.CalculatePath` com fallback ao ponto mais próximo do centro, R2.6/R8.2), instancia o `GameObject` do portal parentado ao diretor, adiciona `ScenePortal`, chama `portal.Configure(_player.transform, _returnScene)` **antes/no momento do spawn** (o `ScenePortal.Start()` inicia sua corrotina de polling ao ser instanciado — como o portal nasce em runtime, `Configure` precisa acontecer no mesmo frame do `AddComponent`).
  3. Definir o objetivo no HUD: `"SETOR PURIFICADO\nEntre no portal de extração para retornar ao Nexus."` (via `_objective`).
  4. Marcar `IsRunComplete = true` (a run está logicamente concluída; o jogador só precisa entrar no portal). **Não** chamar `ReturnToNexus()` aqui — o próprio `ScenePortal` carrega o Nexus quando o jogador entra no raio, com a mesma guarda `CanStreamedLevelBeLoaded` (R8.4/R8.5, delegada ao `ScenePortal`).
- **Guarda `_leaving` e uma-única-vez.** `OnBossDefeated` já protege contra reentrada (`_bossDefeated`/`_leaving`), garantindo que o portal seja materializado **exatamente uma vez** (R8.1: enquanto o chefe não morre, nenhum portal existe). O `_leaving` **não** é setado no caminho do portal (ele pertence a `ReturnToNexus`), preservando sua semântica para o ramo de morte.
- **Ramo de morte inalterado.** `OnPlayerDied` → `ReturnToNexusAfterDeath` (~0.5s) → `ReturnToNexus()` continua carregando o Nexus **diretamente**, sem passar pelo portal (R8.6). O portal só existe no caminho de vitória.

Esboço do método novo (assinatura ilustrativa, mesma linguagem C# do projeto):

```csharp
// Feature: playable-procedural-run. Requirements: 8.1, 8.2, 8.3.
private void MaterializeExtractionPortal(Room bossRoom)
{
    Vector3 spot = FindReachableSpot(bossRoom);          // reusa o placement do troféu (R8.2)
    var portalObject = new GameObject("Extraction_Portal");
    portalObject.transform.SetParent(transform, false);
    portalObject.transform.position = spot;
    var portal = portalObject.AddComponent<ScenePortal>();
    portal.Configure(_player.transform, _returnScene);   // destino explícito = Nexus (R8.3)
    // O ScenePortal.Start() inicia o polling; ao entrar no raio ele faz o load guardado (R8.4/R8.5).
}
```

### 2. ProceduralStageEnvironment — as-built (sem mudança)

Constrói a geometria por sala a partir do `RoomGraph`: piso navegável, painéis, paredes com aberturas de porta de largura `DoorWidth = 4`, rótulos TMP, materiais por tipo e um de **9 `ArenaShape`** (`ResolveArenaShape`: AccessAtrium/OpenDeck/Crossroads/Octagon/SplitLanes/PillarCourt/MemoryVault/HiddenArchive/CoreSanctum). Adiciona um `NavMeshSurface` (`collectObjects = Children`, `useGeometry = PhysicsColliders`) e chama `BuildNavMesh()`. Expõe `Build(RoomGraph)` (retorna o `NavMeshSurface`) e `SetRevealed(roomId, bool)` (liga/desliga `Renderer`s da sala). `Update()` lê `Keyboard.current.eKey.wasPressedThisFrame` e chama `_director.TryDiscoverSecret()` (R7.2). Salas secretas nascem ocultas (`SetRevealed(id, false)`) preservando o piso na navegação (R7.1). Esta feature **não altera** este componente.

### 3. EncounterGates — Boss_Seal as-built (sem mudança)

O modelo de portas direcionais (`ConfigureDirectional`, `AddDoor`, `OpenDoor`, `SealDoor`, `IsDoorSealed`, `SealAll`) coexiste com o par legado entrada/saída. O selo do chefe é `ConfigureBossSeal(Direction, requiredFragments)` (cria a porta + um soquete-esfera "Fragmento de acesso" visível por fragmento), `SetBossSealProgress(int)` (recolore soquetes carregados) e `IsBossSealReady`. `OpenDoor` na direção do selo **é bloqueado** enquanto `_bossSealProgress < _bossSealRequired` (R6.3). Esta feature **não altera** este componente.

### 4. TG_TopDown_Camera — mudança (B) afastamento de câmera

**As-built.** `TechGuy.Cameras.TG_TopDown_Camera` tem `m_Target` (público), `[SerializeField] m_Height = 9`, `m_Distance = 10`, `m_Angle = 0`. `HandleCamera()` posiciona a câmera a partir de altura + distância + ângulo e aplica `LookAt` no alvo. Hoje o `FirstSectorBuilder` já aponta `m_Target = player`, mas **apenas** posiciona o transform em `(0, 14, -12)` e chama `LookAt` — os campos serializados de enquadramento seguem nos padrões 9/10, e `HandleCamera()` sobrescreve a posição fixa no primeiro `Start()`/`Update()`.

**Mudança (B).** O `FirstSectorBuilder` passa a configurar os campos serializados da câmera via `SerializedObject`:

```csharp
var camData = new SerializedObject(camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>());
camData.FindProperty("m_Height").floatValue = 16f;     // ponto de partida sugerido (R4.6)
camData.FindProperty("m_Distance").floatValue = 16f;
camData.FindProperty("m_Angle").floatValue = 0f;
camData.ApplyModifiedPropertiesWithoutUndo();
```

- `m_Target` permanece = `player` por referência serializada (R4.1/R4.5). Os campos continuam expostos e ajustáveis no Inspector (R4.6/R4.7).
- **Trade-off.** Afastar demais encolhe o jogador na tela e prejudica a leitura de telegraph dos inimigos; próximo demais não mostra a horda numa arena de 24x24. Os valores 16/16 são um ponto de partida — o ajuste fino é feito na verificação em Play (D), observando uma `Combat_Room` de 24x24 com a horda ao redor no estilo top-down (Diablo 4 / Lost Ark).
- A posição fixa de transform pode permanecer como estado inicial (será sobrescrita por `HandleCamera()`), mas o enquadramento efetivo passa a vir dos campos serializados (R4.7).

### 5. FirstSectorBuilder — as-built + mudança (B) + garantia de ausência de FirstSectorDirector

**As-built.** `FirstSectorBuilder.Build` autora `FirstSector.unity`: move `PlayerActor` + câmera de `CombatStudy`, cria "First sector procedural flow" com `SwarmAttackCoordinator` + `ProceduralStageEnvironment` + `RunSeedSource` + `ProgressionDirector`, conecta todas as referências serializadas do diretor, define os `StageGenerationParams` (min/max 5, densidade 8–16, variedade 4, `roomSize` 24x24, tesouro 0.5, secreto 0.3, retry 50), gera o grafo representativo (semente 1701), assa o NavMesh e o persiste em `Assets/_Project/Art/FirstSector/Navigation.asset` com **GUID preservado via `EditorUtility.CopySerialized`** (R11.1), adiciona luz, valida caminhos (`ValidatePaths`), captura screenshot e salva; `ConnectLobby()` aponta o portal do Nexus para `"FirstSector"` e adiciona a cena ao Build Settings.

**Mudanças.**
- **(B)** Substituir o bloco `camera.transform.position = new Vector3(0, 14, -12); camera.transform.LookAt(...)` pela configuração dos campos serializados de `TG_TopDown_Camera` descrita acima (mantendo `m_Target`).
- **Garantia R1.2.** A cena é montada em `NewScene(EmptyScene)` e nunca instancia `FirstSectorDirector`, logo a cena não contém esse componente. O design **mantém** essa garantia e a torna verificável pelo teste EditMode existente (que já afirma zero `FirstSectorDirector`).

### 6. Validadores — mudança (C) aposentar / desacoplar

- **`FirstSectorValidation.cs` — RETIRAR (R10.1).** É totalmente acoplado à antiga missão linear: referencia `FirstSectorDirector`, `IsComplete`, `ClearedEncounters`, `Boons`, tamanhos fixos de encontro (3/4/3) e posições de warp fixas que não existem no fluxo procedural. **Decisão:** deletar `FirstSectorValidation.cs` **e** seu `.meta` juntos (R11.1), pois não há verificação salvável — todo o corpo depende de conceitos removidos. Nenhuma outra classe referencia `FirstSectorValidation` (é `[InitializeOnLoad]` estático autônomo), então a remoção é segura.
- **`ArsenalValidation.cs` — DESACOPLAR (R10.2/R10.3).** Este validador **não** referencia `FirstSectorDirector`. Suas fases 0–6 são de arsenal/loadout no `NexusLobby` e independem de cena. As fases 7–8 fazem `SceneManager.LoadScene("FirstSector")` e afirmam que a arma/loadout (lança) **persiste através do carregamento de cena** e que a barra de skills corresponde — isso continua verdadeiro com a `FirstSector` procedural, porque a persistência de arma vem de `WeaponLoadout`/`PlayerActor`, não da direção linear. **Decisão:** manter as fases 7–8 (carregar `"FirstSector"` + assertivas de persistência de arma), **removendo apenas** qualquer texto/suposição de "FirstSector como missão linear" e garantindo que nenhuma assertiva dependa de estado de encontro linear (`ClearedEncounters`, `IsComplete`) — a leitura do código atual mostra que já não dependem, então o desacoplamento é confirmar e, se necessário, ajustar comentários/mensagens. Nenhuma referência a `FirstSectorDirector` permanece (R10.4).
- **Compilação e testes (R10.5).** Após a retirada, o projeto deve compilar sem erros e `FirstSectorProceduralTests` + demais testes EditMode/PlayMode devem passar. Um passo de verificação (D) compila e roda os testes.

## Data Models

Esta feature **adiciona pouquíssimo dado novo**; ela reusa os modelos existentes. Referência (sem duplicar):

- **Extraction_Portal:** uma instância de `ScenePortal` (`_player`, `_destination`, `_radius = 1.25`) com `_destination = _returnScene` (Nexus). Sem novo tipo de dado — é o mesmo componente do portal do Nexus, construído em runtime.
- **Enquadramento de câmera:** os campos serializados já existentes `m_Height`, `m_Distance`, `m_Angle` de `TG_TopDown_Camera`. A mudança (B) apenas define seus valores na autoração; nenhum campo novo.
- **Estado de selo/fragmentos:** já modelado no `ProgressionDirector` (`_bossAccessFragments`, `_requiredBossAccessFragments`, `HasBossAccess`, `BossAccessFragments`, `RequiredBossAccessFragments`) e nos sockets do `EncounterGates`. Sem novo dado.
- **Modelo de grafo (`RoomGraph`/`Room`/`RoomConnection`/`RoomComposition`):** pertence ao núcleo da spec `procedural-stage-room-generation`; inalterado.

## Correctness Properties

*Uma propriedade é uma característica ou comportamento que deve valer para todas as execuções válidas do sistema — uma afirmação formal sobre o que o sistema deve fazer. Propriedades são a ponte entre a especificação legível por humanos e garantias de correção verificáveis por máquina.*

Esta feature é de **integração de cena** (spawn de um portal em runtime, ajuste de câmera, retirada de validadores). Grande parte do comportamento é side-effect de cena/NavMesh/carregamento, melhor verificado por testes de exemplo (EditMode dirigidos por reflection, como os testes irmãos) e por checagem in-Editor (PlayMode/Unity_MCP), não por 100 iterações randômicas. Ainda assim, algumas propriedades merecem ser enunciadas e verificadas:

### Property 1: Portal materializa exatamente uma vez, só na derrota sem próxima Stage

*Para toda* run em que o chefe é derrotado com `_stageCount == 1` (sem próxima Stage), exatamente um `Extraction_Portal` (`ScenePortal`) é materializado dentro da `Boss_Room`, e enquanto o chefe não é derrotado nenhum portal existe.

**Validates: Requirements 8.1, 8.2** — Verificação: **teste EditMode** (dirigir `OnBossDefeated`/`AdvanceToNextStageOrConclude` por reflection e contar `ScenePortal` filhos do diretor).

### Property 2: Destino do portal é o Nexus

*Para todo* `Extraction_Portal` materializado, seu `Destination` é igual a `_returnScene` (o Nexus).

**Validates: Requirements 8.3** — Verificação: **teste EditMode** (afirmar `portal.Destination == "NexusLobby"`).

### Property 3: Entrar no portal carrega o Nexus (guardado)

*Para toda* entrada do jogador no raio do `Extraction_Portal`, quando o Nexus está no Build Settings, o portal inicia `LoadSceneAsync(Nexus)`; quando ausente, registra erro e não carrega.

**Validates: Requirements 8.4, 8.5** — Verificação: **PlayMode / Unity_MCP** (comportamento herdado de `ScenePortal`, já guardado por `CanStreamedLevelBeLoaded`).

### Property 4: Morte retorna ao Nexus independentemente do portal

*Para toda* morte do jogador durante a Stage, a run retorna ao Nexus pelo fluxo de morte (`ReturnToNexus`) sem materializar nem depender do `Extraction_Portal`.

**Validates: Requirements 8.6** — Verificação: **teste EditMode** (dirigir `OnPlayerDied`; afirmar que nenhum `ScenePortal` foi criado) + **PlayMode**.

### Property 5: Selo do chefe nunca abre antes de N fragmentos

*Para todo* número de fragmentos menor que `RequiredBossAccessFragments`, a porta do selo do chefe permanece selada mesmo quando `OpenDoor` é chamado na direção do selo.

**Validates: Requirements 6.3** — Verificação: **teste EditMode** (já coberto por `BossSeal_RequiresAllThreeAccessFragmentsBeforeOpening` em `FirstSectorProceduralTests`).

### Property 6: Enquadramento da câmera afastado do padrão

*Para a* câmera autorada na cena, `m_Height` e `m_Distance` são maiores que os padrões (9 e 10), enquadrando uma `Combat_Room` de 24x24.

**Validates: Requirements 4.6, 4.7** — Verificação: **teste EditMode** (ler os campos serializados na cena autorada) + **checagem visual Unity_MCP** (screenshot com a sala 24x24 e a horda visíveis).

## Error Handling

| Situação | Tratamento | Requisito |
| --- | --- | --- |
| Nexus ausente do Build Settings ao entrar no portal | `ScenePortal` registra erro identificando o destino e **não** inicia o carregamento (`CanStreamedLevelBeLoaded`) | 8.5 |
| `FindReachableSpot` não acha ponto alcançável na `Boss_Room` | Fallback ao ponto de NavMesh mais próximo do centro da sala + `Debug.LogWarning` de diagnóstico (mesmo padrão do `RewardTrophy`) | 8.2 |
| `Boss_Room` sem interior alcançável de NavMesh | Portal materializado no centro da sala como último recurso (mesma cadeia de fallback do troféu); diagnóstico registrado | 8.2 |
| Portal instanciado sem `Configure` chamado no mesmo frame | Evitado por contrato: `Configure(player, _returnScene)` é chamado imediatamente após `AddComponent`, antes de a corrotina `Start` avaliar distância | 8.3 |
| Morte do jogador durante o delay de transição do chefe | `ReleaseStageTransition` aborta se `IsRunComplete`/`_player.IsDead`; ramo de morte assume o retorno (R8.6) | 8.6 |
| `unity-mcp` indisponível no Editor | Reportar a indisponibilidade; **não** marcar como verificada nenhuma autoração/fluxo dependente de inspeção in-Editor | 12.9 |
| Retirada de validador deixando referência pendente | Remover `FirstSectorValidation.cs` + `.meta` juntos; compilar o projeto para confirmar ausência de referências a `FirstSectorDirector` | 10.4, 10.5 |

## Testing Strategy

**Abordagem dupla.** Testes de exemplo (EditMode, dirigidos por reflection como os testes irmãos em `FirstSectorProceduralTests`) para a lógica de spawn do portal e o enquadramento; verificação in-Editor (PlayMode / Unity_MCP) para o fluxo ponta a ponta e checagens visuais. Property-based testing não se aplica aqui (integração de cena, side-effects, carregamento de cena) — usa-se exemplo + integração.

### EditMode (estender `FirstSectorProceduralTests`, manter os existentes verdes)

- **Portal na derrota do chefe (Property 1/2).** Novo teste que monta um grafo com `Boss_Room` alcançável (padrão dos testes existentes), define `_stageCount = 1` por reflection, dirige a derrota do chefe (`OnBossDefeated`/`AdvanceToNextStageOrConclude` via reflection) e afirma que **exatamente um** `ScenePortal` foi materializado como filho do diretor, com `Destination == "NexusLobby"` na `Boss_Room`.
- **Morte não materializa portal (Property 4).** Dirigir `OnPlayerDied`; afirmar que nenhum `ScenePortal` foi criado e que `IsRunComplete` fica verdadeiro.
- **Enquadramento de câmera (Property 6).** Abrir a cena autorada `FirstSector.unity` e afirmar `m_Height > 9` e `m_Distance > 10` na `TG_TopDown_Camera`, com `m_Target` = jogador.
- **Regressões existentes.** Manter `AuthoredFirstSector_...`, `BossSeal_Requires...`, `ThirdCombatReward_...`, `EnteringFromRoomB_...`, `EastWestDoor_...` passando sem alteração.

### PlayMode

- **Ponta a ponta (R12.4).** Entrar em Play, gerar, lutar pelas 5 salas, acumular fragmentos → selo abre → chefe → **portal materializa** → jogador entra → carrega o Nexus. Verificar inimigos sobre o NavMesh.
- **Ramo de morte.** Matar o jogador durante a Stage e confirmar retorno ao Nexus sem portal (R8.6).

### Verificação in-Editor via Unity_MCP (R12)

- **NavMesh + enquadramento (R12.3).** Screenshot do NavMesh assado e do layout; confirmar cobertura das Rooms e o enquadramento afastado (sala 24x24 + horda visíveis); ajustar `m_Height`/`m_Distance` se necessário.
- **Play até o portal (R12.4).** Jogar de ponta a ponta e observar o `Extraction_Portal` aparecer na `Boss_Room` e levar ao Nexus; confirmar morte → Nexus.
- **Console (R12.5).** Ler o console para as mensagens de R1 (seed/geração), R5 (tuning/avisos de composição), R6 (progresso de fragmentos/selo) e R8 (derrota do chefe / portal / retorno).
- **Testes (R12.6).** Rodar EditMode + PlayMode pelas ferramentas de teste do Editor e reportar cada resultado; **confirmar que o projeto compila** após a retirada de `FirstSectorValidation` e o desacoplamento de `ArsenalValidation`.
- **`.meta` e AGENTS.md (R12.7/R12.8).** Preservar `.meta` de qualquer asset removido (o `.cs` + `.meta` de `FirstSectorValidation` juntos); bloquear qualquer edição que introduza `GameObject.Find`/`FindObjectOfType`/strings mágicas na lógica de gameplay.
- **Indisponibilidade (R12.9).** Se `unity-mcp` estiver indisponível, reportar e não marcar como verificada nenhuma autoração/fluxo dependente da inspeção in-Editor.
