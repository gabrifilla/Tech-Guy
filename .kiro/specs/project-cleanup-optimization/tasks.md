# Implementation Plan

## Visão geral

Plano de implementação para a limpeza e otimização do Tech Guy, derivado diretamente dos componentes
numerados do design (§1–§11) e das 9 propriedades de correção (P1–P9). A ordem respeita as dependências
reais: o **núcleo de pooling puro** (`SimpleObjectPool<T>` + `ComponentPool<T>`, §2) vem antes das três
integrações de pool (§3/§4/§5-moedas); o **baseline** (§1) é capturado cedo para permitir comparação
antes/depois; UI (§6), hot paths (§7), preload de `Resources` (§8) e import (§9) são amplamente
independentes; a **ferramenta de limpeza de Editor** (§10) e a revisão do **FastScriptReload** (§11)
formam a última frente, com a remoção de asset e a mudança de config **sempre gated em confirmação
item-a-item do usuário**.

Todas as mudanças preservam o comportamento observável (R9), preservam `.meta`/GUID e não tocam código
third-party (salvo config do FastScriptReload, R8.3). Linguagem de implementação: **C#** (o design já é
escrito em C#/Unity; nenhum pseudocódigo).

> **Ressalva de execução (R10):** o Unity não roda de forma confiável via CLI neste ambiente (o test
> runner de EditMode já sofre timeout). O piso automatizável são os **property tests de lógica pura**
> (P1–P9, EditMode). Metas de performance/visual são validadas por **Profiler + playtest manual** e
> por **inspeção/`git diff`**, com relato explícito do que não pôde ser validado via CLI. Sub-tarefas
> marcadas com `*` são de teste e podem ser puladas para um MVP mais rápido.

## Tasks

- [ ] 1. Baseline de performance mensurável (§1)
- [ ] 1.1 Capturar e registrar o baseline do Profiler num artefato da spec
  - Capturar no Profiler, em **ocioso** e em **combate**, no mínimo: `GC Reserved`, `GC Used`,
    `GC Allocated In Frame`, tempo de frame CPU (médio/pico) e as contagens
    (objetos/assets/texturas/materiais).
  - Registrar num arquivo versionado sob `.kiro/specs/project-cleanup-optimization/` (ex.:
    `baseline.md`), com data/contexto e valores observados.
  - Escrever as metas como **relativas** ao baseline (alloc/frame ocioso → ~0; menos/menores picos de
    GC em combate; `Texture Memory` menor), sem números absolutos não verificáveis.
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [ ] 2. Núcleo de pooling puro e adaptador Unity (§2)
- [ ] 2.1 Criar `SimpleObjectPool<T>` (classe pura) em `Assets/_Project/Scripts/Core/Pooling/`
  - `ctor(Func<T> factory, int prewarm = 0)`; `Acquire()` (livre ou cria via `factory`, R3.7);
    `Release(item)` (devolve ao conjunto de livres, ignora nulo/duplicado); `CreatedCount`
    (high-water-mark), `FreeCount`/`LiveCount`.
  - Sem qualquer dependência de `MonoBehaviour`/cena — 100% testável em EditMode.
  - _Requirements: 3.7, 3.8, 10.2_
- [ ] 2.2 Criar `ComponentPool<T> where T : Component` (adaptador Unity) na mesma pasta
  - Recebe `factory` (cria `GameObject`+`T` uma vez por instância), `Action<T>` de reset (chamado no
    acquire) e nome do root; ativa/desativa no acquire/release; parenteia sob root lazy `"<Nome>Pool"`
    espelhando `HitboxDamage.EnsureEffectPoolRoot`.
  - Não expõe estado novo ao gameplay — só substitui o par `Instantiate`/`Destroy`.
  - _Requirements: 3.5_
- [ ] 2.3 Adicionar reset de caches/pools estáticos entre plays
  - `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` que zera os pools estáticos no início de cada
    play, para o estado `static` não vazar entre plays no Editor (mesmo padrão de `CombatBalance`).
  - _Requirements: 3.4, 10.2_
- [ ]* 2.4 Property test do núcleo de pool
  - **Feature: project-cleanup-optimization, Property 1: `SimpleObjectPool<T>` — acquire nunca entrega
    instância viva duplicada; expande quando vazio; release permite reuso; `CreatedCount` nunca excede
    o high-water-mark de instâncias simultâneas.** FsCheck, ≥100 iterações, em
    `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 3.1, 3.2, 3.3, 3.7, 3.8, 10.2_

- [ ] 3. Pooling do projétil do player — `ArsenalProjectile` (§3)
- [ ] 3.1 Introduzir o pool estático atrás de `Fire`/`FireSingle`
  - Adicionar `static ComponentPool<ArsenalProjectile>` (root `"ArrowPool"`); `FireSingle` faz
    `Pool.Acquire()` no lugar de `new GameObject`. Nenhum call-site muda (`ArsenalCombat`,
    `SplitArrowCoordinator`, `ArsenalValidation`).
  - A `factory` cria `GameObject` + `ArsenalProjectile` + `LineRenderer` + **um** `Material`
    (`Shader.Find("Universal Render Pipeline/Unlit")` cacheado em `static`) uma vez por instância;
    Material/LineRenderer são reusados (R3.8).
  - _Requirements: 3.1, 3.5, 3.8_
- [ ] 3.2 Implementar `ResetForReuse(...)` e trocar `Destroy` por `ReleaseToPool()`
  - `ResetForReuse` (chamado após `Acquire`): `SetPositionAndRotation`; reatribui
    `_owner/_damage/_multiplier/_remaining/_piercing/_bounces/_homing/_returning/_weapon/_nextSeek`;
    `_returned=false`; `_hit.Clear()`; `_seekTarget=null`; `_material.color=color`.
  - Substituir cada `Destroy(gameObject)` terminal do `Update()` por `ReleaseToPool()`
    (desativa + `Pool.Release`); pierce/ricochete/homing/retorno (ramos `continue`/`return`) ficam
    idênticos. O material vive com a instância pooled.
  - _Requirements: 3.1, 3.4, 3.6, 3.7, 3.8_
- [ ]* 3.3 Property test de reset (sobre estado puro que espelha o projétil)
  - **Feature: project-cleanup-optimization, Property 2: para qualquer "estado sujo", após `Reset`
    todos os campos rastreados voltam ao default (contadores zerados, `_hit` vazio, flags `false`).**
    FsCheck, ≥100 iterações, em `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 3.4_

- [ ] 4. Pooling do projétil de inimigo — `EnemyProjectile` (§4)
- [ ] 4.1 Introduzir o pool estático atrás de `Spawn`
  - `static ComponentPool<EnemyProjectile>` (root `"EnemyProjectilePool"`); `Spawn` faz `Acquire` no
    lugar de `new GameObject` e segue chamando `Configure(...)`. Nenhum call-site muda
    (`EnemyCombatActions`, `HazardCasterBehavior`).
  - _Requirements: 3.2, 3.5, 3.7_
- [ ] 4.2 Completar o reset no acquire e trocar `Destroy` por `ReleaseToPool()`
  - Complementar `Configure(...)` com a limpeza do que persiste entre usos: reidratar/limpar o
    `_impactTelegraph`, `_flightElapsed=0`, re-subscrever o owner (`_ownerSubscribed=false` antes de
    `SubscribeOwner`), reusar `_runtimeMaterial` (não recriar).
  - Trocar os `Destroy(gameObject)` de `StepStraight`/`StepArced`/`OnOwnerDied`/cascade por
    `ReleaseToPool()` (esconde/limpa telegraph como o `OnDestroy` faz hoje, sem destruir o material).
    Cascata de morte do owner e "não aplicar mais dano" ficam idênticos.
  - _Requirements: 3.2, 3.4, 3.6, 3.8_

- [ ] 5. Pooling de moedas — `CoinPickup` + `CoinDrop` (§5)
- [ ] 5.1 Introduzir o pool estático de moedas na fábrica `SpawnCoin`
  - `static ComponentPool<CoinPickup>` cuja `factory` faz `Object.Instantiate(prefab)` do prefab de
    `Resources.Load<CoinPickup>("Items/CoinPickup")` (mantido em `_sharedCoin`) uma vez por instância.
  - `CoinDrop.SpawnCoin` passa a `Acquire` + posicionar + `SetValue` + `SetPlayer` (o prefab segue
    Unity-managed; só paramos de instanciar/destruir por moeda).
  - _Requirements: 3.3, 3.5, 3.7_
- [ ] 5.2 Implementar `ResetForReuse` e trocar `Destroy` por `ReleaseToPool()`
  - `ResetForReuse`: `_collected=false`, `CancelInvoke()` + re-`Invoke(Expire, lifetime)`,
    `_basePosition`/posição/`localScale` e fase de spin reinicializados (o que `Start` faz hoje roda
    no reset).
  - `Collect()` e o fim de `FlourishThenDestroy()` chamam `ReleaseToPool()` no lugar de `Destroy`.
    Valor coletado, magnet, bob e flourish ficam idênticos.
  - _Requirements: 3.3, 3.4, 3.6_

- [ ] 6. Checkpoint — pooling completo
  - Rodar a suíte EditMode (incl. `ProjectileMotion*`, `ArsenalRicochetDecayInvariantTests`) e os
    property tests P1/P2, garantindo verde. Ensure all tests pass, ask the user if questions arise.

- [ ] 7. HUD/UI sem alocação por frame (§6)
- [ ] 7.1 Dirty-tracking no `PlayerHUD`
  - Introduzir um cache plain C# (`UiValueCache`) dos últimos valores renderizados (vida/mana/asura,
    índice de slot, tooltip, `_feedback`). No `Update`, comparar valor atual com o cacheado e **só**
    remontar/reatribuir o TMP quando muda, com o **mesmo** formato atual
    (`$"{Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maxHealth)}"`, etc.).
  - _Requirements: 2.1, 2.2, 2.6_
- [ ] 7.2 Dirty-tracking no `RunRewardUI` (IMGUI) e no `CombatReadabilityUI`
  - `RunRewardUI`: manter o desenho IMGUI, mas cachear rótulos/strings e reconstruí-los só quando os
    dados de recompensa mudam (`_run.Acquired.Count`, conjunto de escolhas, item sob hover);
    layout/informações idênticos.
  - `CombatReadabilityUI.LateUpdate`: só remontar strings e recomputar `WorldToScreenPoint` quando os
    valores de origem mudarem; posicionamento em tela preservado.
  - _Requirements: 2.3, 2.4, 2.6_
- [ ]* 7.3 Property test da decisão de dirty (UI)
  - **Feature: project-cleanup-optimization, Property 3: o cache só (re)emite texto quando o valor de
    origem difere do anterior.** FsCheck, ≥100 iterações, em
    `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 2.1, 2.3, 2.4_
- [ ]* 7.4 Property test da formatação (UI)
  - **Feature: project-cleanup-optimization, Property 4: para o mesmo estado, a string do formatter é
    idêntica à fórmula atual (vida/mana/asura).** FsCheck, ≥100 iterações, em
    `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 2.2, 2.6_

- [ ] 8. Hot paths mais baratos (§7)
- [ ] 8.1 Barra de vida do `Actor` por evento
  - Remover a chamada `UpdateHealthBar()` do `Update()` e, em `Awake`, assinar o próprio evento
    `HealthChanged` para chamar `UpdateHealthBar` (o `Actor` já dispara `HealthChanged` em
    `TakeDamage`/`SetMaxHealth`/`Heal`/`RestoreHealthToMax`). Fill = `clamp01(health/max)` idêntico.
  - _Requirements: 4.2, 4.4_
- [ ] 8.2 `OutlineScript` — cache de câmera/componentes + throttle de raycast
  - Cachear `Camera.main` em `_cam` (refrescar se `null`); criar um `FrameThrottle` puro para limitar o
    `Physics.Raycast` a cada N frames; cachear o `Outline` por `Transform` (dicionário ou
    `TryGetComponent`-uma-vez). Destaque no hover inalterado.
  - _Requirements: 4.1, 4.4_
- [ ] 8.3 Cachear `GetComponent` por frame nos candidatos da investigação
  - Onde de fato existir `GetComponent`/`GetComponentInParent` por frame em `Update`/`LateUpdate`,
    resolver a referência uma vez (`Awake`/`Start`) e reusá-la (candidatos: `EnemyAI`,
    `PreferredDistanceLayer`, `FrontalReflector`, `CombatReactionController`, `ProtectionIndicator`,
    `Shield`, `PriorityTargetMarker`, `StatusEffect`).
  - Onde a referência é obrigatória, validar em `Awake`/`OnValidate` e logar aviso claro em vez de
    gerar `NullReferenceException` silenciosa por frame.
  - _Requirements: 4.3, 4.4, 4.5_
- [ ]* 8.4 Property test da barra de vida
  - **Feature: project-cleanup-optimization, Property 5: para qualquer sequência de mutações de vida,
    a barra recomputa sse o valor mudou, e o valor é `clamp01(health/max)`.** FsCheck, ≥100 iterações,
    em `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 4.2_
- [ ]* 8.5 Property test do throttle
  - **Feature: project-cleanup-optimization, Property 6: `FrameThrottle` dispara no máximo uma vez por
    intervalo e nunca perde o primeiro tick.** FsCheck, ≥100 iterações, em
    `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 4.1_

- [ ] 9. Preload/cache de `Resources` (§8)
- [ ] 9.1 `WeaponCache` estático em `WeaponLoadout`
  - Adicionar `WeaponCache` (índice → `WeaponScript`) preenchido uma vez no boot
    (`[RuntimeInitializeOnLoadMethod]`), carregando os três `ResourcePaths`
    ("Weapons/Melee/Gauntlet/Gauntlet", "Weapons/Ranged/Bow_arrow/Bow", "Weapons/Melee/Spear/Spear").
    `LoadSelected()`/`Select()` leem do cache em vez de `Resources.Load` no caminho de troca; arma
    equipada preservada.
  - Se um preload falhar (path inválido), logar aviso e cair na carga sob demanda atual, sem quebrar o
    combate.
  - _Requirements: 5.1, 5.3, 5.4, 5.5_
- [ ] 9.2 Warm-up do efeito de `HitboxDamage` no boot
  - Aquecer o efeito chamando `GetEffectInstance(path)` uma vez no boot (o pool já existe), para o
    primeiro uso em combate não disparar carga síncrona de disco. Fallback à carga sob demanda com
    aviso, sem quebrar o combate.
  - _Requirements: 5.2, 5.3, 5.5_
- [ ]* 9.3 Property test do cache de armas
  - **Feature: project-cleanup-optimization, Property 7: para todo índice válido, `get` é idempotente
    e não recarrega após preload; índice inválido cai no fallback do índice 0.** FsCheck, ≥100
    iterações, em `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 5.1_

- [ ] 10. Import de texturas e assets (§9)
- [ ] 10.1 Passada de import settings sobre as texturas mais pesadas
  - Ajustar `maxTextureSize`, compressão por plataforma e `mipmaps` conforme o uso (3D vs. UI),
    priorizando as maiores (prováveis sob "STYLIZED MALE CHARACTER"), para reduzir `Texture Memory`.
    Texturas estritamente de UI/sprite recebem settings de UI (sem mipmaps quando desnecessário).
  - **Somente import settings** (edição do bloco do importer no `.meta`, **sem recriar o asset**):
    `.meta`/GUID e todas as referências ficam intactos; aparência preservada (sem artefato/blur/
    banding). Em asset third-party, limitar-se a settings, sem tocar conteúdo/código.
  - Considerar atlas apenas onde o ganho de draw calls for demonstrável, sem perda visual.
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [ ] 11. Ferramenta de Editor: identificação segura de órfãos (§10)
- [ ] 11.1 `AssetReferenceScanner` (lógica pura) em `Assets/_Project/Scripts/Editor/Cleanup/`
  - Dado o GUID de um asset e o conteúdo dos arquivos de referência (`.unity`, `.prefab`, `.asset`,
    `.meta`), decidir se o GUID é **referenciado**; asset é candidato a órfão **somente** se o GUID
    não aparecer em nenhum deles. Reportável: onde a referência foi encontrada.
  - _Requirements: 7.1, 7.5_
- [ ] 11.2 `OrphanClassifier` com guardas de `Resources` e whitelist
  - (a) Asset sob `**/Resources/**` nunca é marcado órfão puro sem confirmar ausência de uso por path
    (marca "requer verificação de path"); (b) whitelist de pacotes usados (TextMesh Pro pela UI,
    QuickOutline pelo `OutlineScript`) nunca é proposta para remoção.
  - _Requirements: 7.2, 7.7_
- [ ] 11.3 `ProjectCleanupReport` (janela/menu de Editor) — só relatório
  - Rodar a varredura e produzir um relatório legível (por candidato: caminho, tamanho, motivo),
    seguindo o formato do design; para third-party (destaque "STYLIZED MALE CHARACTER" ~565 MB),
    distinguir **"remoção total do pacote"** de **"trim parcial"**. Salvar como artefato versionável.
    A ferramenta **só reporta** — não remove nada.
  - _Requirements: 7.3, 7.8_
- [ ]* 11.4 Property test do detector de órfão
  - **Feature: project-cleanup-optimization, Property 8: um asset é candidato sse seu GUID não aparece
    em nenhum arquivo de referência; se aparece em ≥1, não é candidato (e a origem é reportável).**
    FsCheck, ≥100 iterações, em `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 7.1, 7.5, 7.7_
- [ ]* 11.5 Property test da guarda de `Resources`
  - **Feature: project-cleanup-optimization, Property 9: para qualquer asset sob `**/Resources/**`, o
    classificador nunca o marca como órfão puro (marca "requer verificação de path").** FsCheck, ≥100
    iterações, em `Assets/_Project/Scripts/Tests/EditMode/Editor/`.
  - _Requirements: 7.2_
- [ ]* 11.6 Example/edge tests da ferramenta e dos fallbacks
  - Relatório contém os campos esperados e distingue total/trim (R7.3/R7.8); dependência ausente emite
    aviso e não gera NRE por frame (R4.5); preload falho cai no comportamento atual com aviso (R5.5).
  - _Requirements: 7.3, 7.8, 4.5, 5.5_

- [ ] 12. Checkpoint — UI, hot paths, resources, import e ferramenta de limpeza
  - Rodar a suíte EditMode e os property tests P3–P9, garantindo verde. Ensure all tests pass, ask the
    user if questions arise.

- [ ] 13. Remoção gated de assets (identificar → confirmar → remover) (§10)
- [ ] 13.1 Implementar o passo de remoção item-a-item, confirmado
  - Passo de remoção **separado** da varredura: para cada candidato do relatório, exigir **confirmação
    explícita do usuário item-a-item**; nada é removido em lote ou silenciosamente.
  - Se **qualquer** referência for encontrada (cena/prefab/SO ou path de `Resources`), **rejeitar** a
    remoção e relatar a origem. Ao remover após confirmação, remover o asset **junto do `.meta`** (via
    `AssetDatabase`), sem deixar `.meta` órfão nem GUID pendente.
  - _Requirements: 7.4, 7.5, 7.6, 9.2, 9.3_

- [ ] 14. Revisão do FastScriptReload (config-only, gated) (§11)
- [ ] 14.1 Correlacionar o auto-reload com stutter e propor config sob confirmação
  - Correlacionar os logs de auto-reload ("full reload will be triggered") com picos de frame no
    Editor, registrando a evidência.
  - Se o auto-reload durante o Play for identificado como contribuinte, **propor desabilitá-lo via
    configuração da ferramenta** (config-only), com **confirmação do usuário**, **sem** editar o
    código-fonte do pacote third-party. Se inconclusivo, relatar como inconclusivo e manter o estado
    atual, sem mudança às cegas.
  - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [ ] 15. Validação final e resumo (§1/§10 — R10)
- [ ] 15.1 Rodar a suíte EditMode completa e comparar Profiler antes/depois
  - Rodar a suíte EditMode completa (property tests novos P1–P9 + suíte existente, incl.
    `ProjectileMotion*`, `ArsenalRicochetDecayInvariantTests`, `DamageAbsorbedDecisionPropertyTests`)
    e confirmar verde após pooling/hot paths.
  - Capturar a medição **pós-mudança** no Profiler com o mesmo roteiro do baseline (Task 1.1) e
    comparar (`GC Allocated In Frame` ocioso → ~0 após §6/§7; menos/menores picos de GC em combate após
    §3; `Texture Memory` menor após §9).
  - _Requirements: 1.5, 10.3_
- [ ] 15.2 Verificação por inspeção e resumo de validação transparente
  - Confirmar por inspeção/`git diff`: `.meta`/GUID preservados em cada import setting e movimentação;
    nenhuma edição em código third-party salvo config do FastScriptReload; remoção sempre remove o
    `.meta` junto.
  - Entregar um **resumo de validação** distinguindo claramente: o que foi **medido no Profiler**, o
    que foi **verificado por inspeção/`git diff`**, e o que **depende de playtest manual do usuário**
    (8 cenas abrem sem missing script/asset; combate/câmera/UI/coleta idênticos). Declarar
    explicitamente o que não pôde ser validado via CLI.
  - _Requirements: 9.1, 9.4, 10.1, 10.4_

## Notes

- Sub-tarefas marcadas com `*` são de teste (property/example) e podem ser puladas para um MVP mais
  rápido; não são implementadas automaticamente.
- Cada tarefa referencia requisitos específicos para rastreabilidade.
- Os checkpoints garantem validação incremental nas fronteiras de fase (após pooling; após UI/hot
  paths/resources/import/ferramenta de limpeza).
- Os property tests P1–P9 validam a lógica pura (o único piso automatizável via CLI); performance e
  visual são validados por Profiler + playtest manual, conforme a ressalva de execução acima.
- A remoção de assets (Task 13) e a mudança de config do FastScriptReload (Task 14) são **gated em
  confirmação item-a-item do usuário** — nunca deleção/edição automática.
- `.meta`/GUID são preservados em toda movimentação/edição; código third-party não é modificado (salvo
  a config do FastScriptReload, sujeita a confirmação).

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1"] },
    { "id": 1, "tasks": ["2.2", "2.3", "2.4"] },
    { "id": 2, "tasks": ["3.1", "4.1", "5.1", "7.1", "7.2", "8.1", "8.2", "8.3", "9.1", "9.2", "10.1", "11.1"] },
    { "id": 3, "tasks": ["3.2", "4.2", "5.2", "7.3", "7.4", "8.4", "8.5", "9.3", "11.2", "11.3"] },
    { "id": 4, "tasks": ["3.3", "11.4", "11.5", "11.6"] },
    { "id": 5, "tasks": ["13.1", "14.1"] },
    { "id": 6, "tasks": ["15.1"] },
    { "id": 7, "tasks": ["15.2"] }
  ]
}
```
