# Implementation Plan: Run Procedural Jogável (FirstSector)

## Overview

Esta feature **não** reimplementa a geração procedural — quase tudo já está entregue e testado (ver o "Mapa de requisitos → estado" do `design.md`: **R1, R2, R3, R5, R6, R7, R9, R11 já implementados**). O plano cobre **apenas as 4 mudanças "a implementar"** do design e sua verificação, sem nenhuma tarefa de prefab/arte/asset — portal e geometria são construídos por código, e o polimento visual fica para depois. A prioridade explícita é **deixar a run jogável e divertida o quanto antes**: primeiro o portal de extração (que fecha o laço da run), depois o afastamento de câmera (leitura de combate), depois a limpeza dos validadores (compilação/testes) e, por fim, a verificação in-Editor e o tuning de diversão.

As 4 mudanças (do `design.md`):

- **(A) Extraction_Portal** — materializar um `ScenePortal` na `Boss_Room` ao derrotar o chefe, substituindo o retorno instantâneo ao Nexus por uma saída física (`ProgressionDirector.cs`, R8).
- **(B) Afastamento de câmera** — o `FirstSectorBuilder` configura `TG_TopDown_Camera` (`m_Height`/`m_Distance` ~16) via campos serializados (`FirstSectorBuilder.cs`, R4.6/R4.7).
- **(C) Aposentar/desacoplar validadores** — deletar `FirstSectorValidation.cs` (+`.meta`) e desacoplar `ArsenalValidation.cs` da missão linear (R10).
- **(D) Verificação in-Editor** — via Unity_MCP: NavMesh, enquadramento, Play ponta a ponta até o portal, console, testes, compilação (R12).

Cada tarefa de código é pequena, incremental e cita os requisitos. Segue o AGENTS.md: PascalCase para tipos/métodos, `_camelCase` para campos privados serializados, **sem** `GameObject.Find`/`FindObjectOfType`/strings mágicas na lógica de gameplay, validação de dependências em `Awake`/`OnValidate`, e **preservação de `.meta`** (o `.cs` do validador removido é deletado junto com seu `.meta`). Linguagem: **C#** (o design usa C# explicitamente — nenhuma seleção de linguagem é necessária).

Os testes EditMode estendem `Assets/_Project/Scripts/Tests/EditMode/Editor/FirstSectorProceduralTests.cs`, dirigidos por reflection como os testes irmãos existentes; property-based testing **não** se aplica (integração de cena, side-effects, carregamento) — usa-se exemplo + verificação in-Editor.

## Tasks

- [x] 1. Materializar o Extraction_Portal no ProgressionDirector (A)
  - [x] 1.1 Implementar `MaterializeExtractionPortal(Room bossRoom)`
    - Em `Assets/_Project/Scripts/Stages/ProgressionDirector.cs`, adicionar o método privado que amostra um ponto de NavMesh alcançável na `Boss_Room` reusando o **mesmo** `FindReachableSpot(room)` que posiciona o `RewardTrophy` (ring sampling + `NavMesh.CalculatePath`, com fallback ao ponto mais próximo do centro).
    - `new GameObject("Extraction_Portal")` parentado ao diretor (`transform.SetParent(transform, false)`), posição = ponto amostrado, `AddComponent<ScenePortal>()` e, **no mesmo frame**, `portal.Configure(_player.transform, _returnScene)` (o `ScenePortal.Start()` inicia o polling de distância ao ser instanciado).
    - Sem strings mágicas: o destino vem de `_returnScene` (referência serializada), não literal.
    - _Requisitos: 8.2, 8.3._

  - [x] 1.2 Alterar o ramo `!hasNextStage` de `AdvanceToNextStageOrConclude` para materializar o portal
    - Substituir a chamada direta a `ReturnToNexus()` (no ramo sem próxima Stage, pois `_stageCount == 1`) por: manter `_bossDefeated` (já setado em `OnBossDefeated`), chamar `MaterializeExtractionPortal(bossRoom)`, e **não** chamar `ReturnToNexus()` neste caminho — o `ScenePortal` carrega o Nexus por conta própria quando o jogador entra no raio (guarda `Application.CanStreamedLevelBeLoaded`, R8.4/R8.5).
    - Definir `IsRunComplete = true` (a run está logicamente concluída; falta só entrar no portal).
    - Preservar a semântica do guard de sentido único `_leaving` (pertence a `ReturnToNexus`, **não** ao caminho do portal), garantindo materialização exatamente uma vez.
    - _Requisitos: 8.1, 8.2._

  - [x] 1.3 Definir o objetivo do HUD ao materializar o portal
    - Ao materializar, escrever no `_objective` (TMP): `"SETOR PURIFICADO\nEntre no portal de extração para retornar ao Nexus."`.
    - _Requisitos: 8.2._

  - [x] 1.4 Garantir que o ramo de morte permanece inalterado
    - Confirmar que `OnPlayerDied` → `ReturnToNexusAfterDeath` → `ReturnToNexus()` continua carregando o Nexus **diretamente**, sem materializar nem depender do `Extraction_Portal` (R8.6). Nenhuma alteração de comportamento neste caminho; apenas verificar que a mudança (1.2) não o afeta.
    - _Requisitos: 8.6._

- [x] 2. Testes EditMode do portal de extração (estender `FirstSectorProceduralTests`)
  - [x] 2.1 Teste: portal materializa exatamente uma vez, na Boss_Room, com destino Nexus
    - **Property 1: Portal materializa exatamente uma vez, só na derrota sem próxima Stage**
    - **Property 2: Destino do portal é o Nexus**
    - Montar grafo com `Boss_Room` alcançável (padrão dos testes existentes), `_stageCount = 1` via reflection, dirigir `OnBossDefeated`/`AdvanceToNextStageOrConclude` por reflection; afirmar **exatamente um** `ScenePortal` filho do diretor, na `Boss_Room`, com `Destination == "NexusLobby"`; e que antes da derrota nenhum `ScenePortal` existe.
    - _Requisitos: 8.1, 8.2, 8.3._

  - [x] 2.2 Teste: morte não materializa portal, mas ainda retorna
    - **Property 4: Morte retorna ao Nexus independentemente do portal**
    - Dirigir `OnPlayerDied` por reflection; afirmar que **nenhum** `ScenePortal` foi criado e que `IsRunComplete` fica verdadeiro pelo fluxo de morte.
    - _Requisitos: 8.6._

- [x] 3. Afastamento de câmera no FirstSectorBuilder (B)
  - [x] 3.1 Configurar os campos serializados de `TG_TopDown_Camera` na autoração
    - Em `Assets/_Project/Scripts/Editor/FirstSectorBuilder.cs`, substituir o bloco `camera.transform.position = new Vector3(0, 14, -12); camera.transform.LookAt(...)` por configuração via `SerializedObject` da `TechGuy.Cameras.TG_TopDown_Camera`: `m_Height` ~16, `m_Distance` ~16, `m_Angle` mantido; `ApplyModifiedPropertiesWithoutUndo()`.
    - Manter `m_Target = player` por referência serializada (sem `GameObject.Find`/`FindObjectOfType`). Valores são ponto de partida, ajustados em Play na tarefa 6.
    - _Requisitos: 4.6, 4.7._

  - [x] 3.2 Teste EditMode: enquadramento afastado na cena autorada
    - **Property 6: Enquadramento da câmera afastado do padrão**
    - Abrir `FirstSector.unity` e afirmar `m_Height > 9` e `m_Distance > 10` na `TG_TopDown_Camera`, com `m_Target` = jogador.
    - _Requisitos: 4.6, 4.7._

- [x] 4. Aposentar/desacoplar os validadores da missão linear (C)
  - [x] 4.1 Deletar `FirstSectorValidation.cs` e seu `.meta`
    - Remover `Assets/_Project/Scripts/Editor/FirstSectorValidation.cs` **e** `Assets/_Project/Scripts/Editor/FirstSectorValidation.cs.meta` juntos (AGENTS.md: preservar/limpar `.meta` em par). Todo o corpo depende de `FirstSectorDirector`/`ClearedEncounters`/`IsComplete`/`Boons`/tamanhos fixos que não existem no fluxo procedural — não há verificação salvável; é `[InitializeOnLoad]` estático autônomo, então a remoção é segura.
    - _Requisitos: 10.1, 10.4._

  - [x] 4.2 Desacoplar `ArsenalValidation.cs` da moldura de missão linear
    - Em `Assets/_Project/Scripts/Editor/ArsenalValidation.cs`, manter as fases que fazem `SceneManager.LoadScene("FirstSector")` + assertivas de persistência de arma/loadout através do carregamento (persistência vem de `WeaponLoadout`/`PlayerActor`, não da direção linear). Remover **apenas** suposições/comentários de "FirstSector como missão linear"; garantir que **nenhuma** referência a `FirstSectorDirector` permaneça e que nenhuma assertiva dependa de `ClearedEncounters`/`IsComplete`.
    - _Requisitos: 10.2, 10.3, 10.4._

  - [x] 4.3 Confirmar compilação e testes verdes após a retirada
    - Garantir que o projeto compila sem erros e que `FirstSectorProceduralTests` + demais EditMode/PlayMode passam (validado in-Editor na tarefa 6; aqui, confirmar ausência de referências pendentes a `FirstSectorDirector`).
    - _Requisitos: 10.5._

- [x] 5. Checkpoint — reautorar a cena e validar
  - Rodar o menu "Create First Sector" do `FirstSectorBuilder` para **reautorar** `FirstSector.unity` com a câmera afastada e o wiring atual (etapa de autoração/regeneração de cena; assa o NavMesh preservando o GUID de `Navigation.asset` via `CopySerialized`). Garantir que EditMode/PlayMode passam e o projeto compila; perguntar ao usuário se surgirem dúvidas.

- [x] 6. Verificação in-Editor via Unity_MCP (D)
  - [x] 6.1 Screenshot de NavMesh + enquadramento afastado
    - Capturar screenshot do NavMesh assado e do layout; confirmar cobertura das Rooms e o enquadramento afastado (uma `Combat_Room` de 24x24 + a horda ao redor visíveis). Se `unity-mcp` estiver indisponível, reportar e **não** marcar como verificado.
    - _Requisitos: 12.3, 12.9._

  - [x] 6.2 Play ponta a ponta até o portal e morte → Nexus
    - Entrar em Play e confirmar a run jogável de ponta a ponta: seed → geração → `environment.Build` → portas + selo do chefe → salas de combate → fragmentos → selo abre → chefe → `Extraction_Portal` materializa na `Boss_Room` → jogador entra → carrega o Nexus; e que a morte do jogador retorna ao Nexus sem portal.
    - _Requisitos: 12.4, 12.9._

  - [x] 6.3 Ler o console e rodar EditMode + PlayMode
    - Ler o console para as mensagens de R1 (seed/geração), R5 (tuning/avisos de composição), R6 (progresso de fragmentos/selo) e R8 (derrota do chefe / portal / retorno); executar os testes EditMode e PlayMode pelas ferramentas de teste do Editor e reportar cada resultado; **confirmar que o projeto compila** após a retirada de `FirstSectorValidation` e o desacoplamento de `ArsenalValidation`.
    - _Requisitos: 12.5, 12.6, 12.9._

  - [x] 6.4 Ajuste fino de diversão (tuning em Play)
    - Sentindo o jogo, ajustar `m_Height`/`m_Distance` da câmera e a densidade/variedade de `StageGenerationParams` (dentro das faixas existentes, via Inspector — sem valores mágicos) para leitura de combate e cascatas de `RunBoons` boas antes do chefe.
    - _Requisitos: 4.6, 5.4._

- [x] 7. Checkpoint final — run jogável, divertida, testes verdes
  - Garantir a run de ponta a ponta jogável e divertida (fragmentos → selo → chefe → Extraction_Portal → Nexus; morte → Nexus), todos os testes EditMode/PlayMode verdes, projeto compilando e verificação Unity_MCP concluída; perguntar ao usuário se surgirem dúvidas.

## Notes

- **Escopo restrito às lacunas reais.** Os requisitos **R1, R2, R3, R5, R6, R7, R9 e R11 já estão satisfeitos** pelo código atual (ver o "Mapa de requisitos → estado" do design) e **não** geram tarefas. Este plano cobre só as 4 mudanças a implementar (A–D) + verificação.
- **Sem prefab/arte/asset.** Portal e geometria são construídos por código; nenhum prefab/material/modelo é autorado aqui. O polimento visual é diferido.
- Tarefas marcadas com `*` são de teste (opcionais para um MVP mais rápido); as demais são implementação/verificação central. As sub-tarefas de verificação Unity_MCP (6.1–6.4) dependem do Editor: se `unity-mcp` estiver indisponível, reportar e não marcar como verificado (R12.9).
- Cada tarefa referencia requisitos específicos; tarefas de teste referenciam a Property correspondente do design.
- **AGENTS.md:** o `.cs` + `.meta` de `FirstSectorValidation` são removidos **juntos**; nenhuma lógica de gameplay usa `GameObject.Find`/`FindObjectOfType`/strings mágicas (o destino do portal vem de `_returnScene`, e o alvo da câmera de `m_Target` serializado).
- **Paralelismo:** (A) e (B) tocam arquivos diferentes (`ProgressionDirector.cs` vs `FirstSectorBuilder.cs`) e são paralelizáveis; (C) é independente de ambos. A reautoração da cena (tarefa 5) só depois de (A)+(B)+(C).

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "3.1", "4.1", "4.2"] },
    { "id": 1, "tasks": ["1.2", "3.2", "4.3"] },
    { "id": 2, "tasks": ["1.3", "1.4", "2.1", "2.2"] },
    { "id": 3, "tasks": ["6.1", "6.2", "6.3"] },
    { "id": 4, "tasks": ["6.4"] }
  ]
}
```
