# Design Document

## Visão geral

A feature ataca os **stutters do Editor** e a **pressão de GC** do Tech Guy com uma passada de
limpeza e otimização **sem alterar gameplay nem o visual observável**. O trabalho se organiza em sete
frentes, todas sobre sistemas já existentes (mais ferramentas de Editor novas para a parte de
limpeza):

1. **Baseline mensurável** (R1/R10): capturar Profiler antes/depois e registrar num artefato da spec,
   com metas **relativas**.
2. **Zero alocação por frame no HUD/UI** (R2): `PlayerHUD`, `RunRewardUI` e `CombatReadabilityUI`
   passam a reconstruir strings **só quando o valor de origem muda** (dirty-tracking), preservando o
   texto/layout atual.
3. **Object pooling** (R3): projéteis do player (`ArsenalProjectile`), de inimigo (`EnemyProjectile`)
   e moedas (`CoinPickup`) deixam de fazer `Instantiate`/`Destroy` por uso, reusando instâncias de um
   pool que espelha o precedente `HitboxDamage.effectPools`.
4. **Hot paths mais baratos** (R4): `OutlineScript` cacheia `Camera.main`/`Outline` e faz throttle do
   raycast; `Actor` passa a atualizar a barra de vida **por evento** (`HealthChanged`) em vez de todo
   frame; `GetComponent` por frame vira referência cacheada onde existir.
5. **Preload/cache de `Resources`** (R5): `WeaponLoadout` pré-carrega as três armas uma vez; o efeito
   de `HitboxDamage` é aquecido no boot para o primeiro combate não travar.
6. **Import de texturas/assets** (R6): passada de Editor sobre `maxTextureSize`/compressão/mipmaps,
   **só settings**, `.meta`/GUID preservados, aparência intacta.
7. **Limpeza segura** (R7/R8): uma ferramenta de Editor **identifica** órfãos por GUID e gera um
   **relatório**; a remoção é um passo separado, **item-a-item, confirmado pelo usuário**. O
   FastScriptReload é avaliado como possível causa de stutter e, se for, sua config (não o código) é
   ajustada com confirmação.

**Camada transversal:** um helper de pooling puro e testável — **`SimpleObjectPool<T>`** — em
`Assets/_Project/Scripts/Core/Pooling/`, reutilizado pelos três novos pools e coberto por
property tests (o único núcleo automatizável via CLI, R10.2). O restante das garantias de performance
é validado por **Profiler + playtest manual**, com transparência explícita sobre o que não roda via
CLI (R10.1/R10.4).

**Princípio de não-regressão:** todas as mudanças preservam o comportamento observável para o mesmo
estado de jogo (R9). Onde a otimização introduz caching/pool, o caminho de fallback mantém o
comportamento atual quando algo falta (R4.5/R5.5/R3.7).

## Arquitetura

```
Assets/_Project/Scripts/Core/Pooling/
   ├─ SimpleObjectPool<T>        (classe pura, sem MonoBehaviour — acquire/release/expand/count)
   └─ ComponentPool<T>           (adaptador MonoBehaviour: fábrica via factory delegate + root lazy,
                                   espelha o padrão de HitboxDamage: instância inativa reusada,
                                   root "…Pool" criado sob demanda)

RUNTIME — pooling behind existing factories (sem mudar call-sites)
   ArsenalProjectile.Fire → FireSingle ──► ArsenalProjectile.Pool.Acquire()  (era new GameObject)
        Update()/expiry/hit  ─────────────► arrow.ReleaseToPool()             (era Destroy)
        OnAcquire(): reset completo (pos/rot, _remaining, _hit.Clear, _bounces, _homing,
                     _returning/_returned, _seekTarget, material.color) — R3.4
        Material/LineRenderer criados 1x por instância, reusados (R3.8)

   EnemyProjectile.Spawn ───────────────► EnemyProjectile.Pool.Acquire()      (era new GameObject)
        Update()/land/owner-gone ────────► projectile.ReleaseToPool()         (era Destroy)

   CoinDrop.SpawnCoin ──────────────────► CoinPickup.Pool.Acquire()           (era Instantiate)
        CoinPickup.Collect()/Expire()  ──► ReleaseToPool()                    (era Destroy)
        OnAcquire(): _collected=false, CancelInvoke + re-Invoke(Expire), scale/pos/spin reset

UI — dirty-tracking (recompute só na mudança)
   PlayerHUD.Update ──► UiValueCache (último int/estado) ──► TMP.text só quando muda (R2.1/R2.2)
   RunRewardUI.OnGUI ─► RewardTextCache (Acquired.Count/choices/hovered) ──► rebuild só na mudança
   CombatReadabilityUI.LateUpdate ─► dirty-check dos valores de origem

HOT PATHS
   OutlineScript.Update ─► _cam (cache de Camera.main) + FrameThrottle (raycast a cada N) +
                            cache de Outline por Transform (dicionário/TryGetComponent-once)
   Actor: (remove UpdateHealthBar de Update) ─► assina o próprio HealthChanged ──► UpdateHealthBar

RESOURCES
   WeaponLoadout ─► WeaponCache (static, índice→WeaponScript) preload no boot; troca sem IO
   HitboxDamage  ─► warm-up do efeito no boot (GetEffectInstance uma vez)

EDITOR (limpeza — só reporta; remoção é passo confirmado)
   Assets/_Project/Scripts/Editor/Cleanup/
      ├─ AssetReferenceScanner  (lógica pura: GUID × arquivos-texto → referenciado?)   R7.1/R7.5
      ├─ OrphanClassifier        (Resources-guard, whitelist TMP/QuickOutline)          R7.2/R7.7
      └─ ProjectCleanupReport    (janela/menu: gera relatório; remoção item-a-item)     R7.3/R7.4/R7.8
```

### Resolução do helper de pooling (lacuna fechada)

- **Núcleo puro (`SimpleObjectPool<T>`):** um `T` genérico com uma `Func<T> factory`, uma pilha/lista
  de livres e uma contagem de "criadas". `Acquire()` tira um livre ou chama `factory` (expansão,
  R3.7); `Release(item)` devolve para reuso. Não conhece Unity — é 100% testável em EditMode
  (R10.2). Mantém um `CreatedCount` (high-water-mark) para a garantia de não-vazamento (R3.8).
- **Adaptador (`ComponentPool<T> where T : Component`):** embrulha o núcleo com a parte Unity —
  a `factory` cria o `GameObject` + componente (uma vez por instância, incluindo Material/LineRenderer
  do projétil), parenteia sob um root lazy `"<Nome>Pool"` (exatamente como
  `HitboxDamage.EnsureEffectPoolRoot`), desativa no release e ativa no acquire. **Espelha o padrão
  existente** (R3.5), então a coesão de implementação é preservada.
- **Reset no acquire:** cada tipo poolável expõe um método de reset chamado no acquire, que devolve a
  instância ao estado "recém-criada" (R3.4). O reset é o ponto crítico de correção: sem ele, um
  projétil reusado carregaria `_hit`/`_bounces`/`_returned` do disparo anterior.
- **Reset de cache estático entre plays (lacuna fechada):** como os pools e caches são `static`, um
  método `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` zera pools/caches no início de cada play
  para o estado estático não vazar entre plays no Editor — mesmo padrão que o projeto já usa em
  `CombatBalance`.

## Componentes e mudanças

### 1. Baseline e validação — artefato da spec (R1/R10)

- Capturar no Profiler, em **ocioso** e **combate**, no mínimo: `GC Reserved`, `GC Used`,
  `GC Allocated In Frame`, tempo de frame CPU (médio/pico) e as contagens
  (objetos/assets/texturas/materiais), registrando data/contexto num arquivo versionado sob o
  diretório da spec (R1.1/R1.2).
- Metas escritas como **relativas** ao baseline (ex.: alloc/frame ocioso → ~0; menos/menores picos de
  GC em combate), evitando números absolutos não verificáveis aqui (R1.3).
- Toda métrica não medível via CLI é declarada "validada por Profiler + playtest" no resumo final
  (R1.4/R10.1/R10.4). Uma medição pós-mudança com o mesmo roteiro evidencia o ganho (R1.5).

### 2. `SimpleObjectPool<T>` + `ComponentPool<T>` — `Assets/_Project/Scripts/Core/Pooling/` (R3.5, R10.2)

`SimpleObjectPool<T>` (classe pura):

| Membro | Assinatura | Efeito |
|---|---|---|
| ctor | `SimpleObjectPool(Func<T> factory, int prewarm = 0)` | Guarda a fábrica; opcionalmente pré-cria. |
| `Acquire` | `T Acquire()` | Devolve um livre ou cria via `factory` (R3.7). Nunca `null` se `factory` não for nula. |
| `Release` | `void Release(T item)` | Devolve ao conjunto de livres para reuso; ignora nulos/duplicados. |
| `CreatedCount` | `int` | Total já criado (high-water-mark) — base da garantia anti-vazamento (R3.8). |
| `FreeCount`/`LiveCount` | `int` | Livres e "em uso" (derivado). |

`ComponentPool<T>` (adaptador Unity): recebe uma `factory` que cria o `GameObject`+`T`, um `Action<T>`
de reset (chamado no acquire) e o nome do root; ativa/desativa a instância no acquire/release e a
parenteia sob o root lazy. Não expõe estado novo ao gameplay — só substitui o par
`Instantiate`/`Destroy`.

### 3. Projétil do player — `ArsenalProjectile` (R3.1, R3.4, R3.6, R3.7, R3.8)

**Situação atual (verificada):** `Fire(...)` → `FireSingle(...)` faz `new GameObject("Energy arrow")`
+ `AddComponent<ArsenalProjectile>()` + `AddComponent<LineRenderer>()` + `new Material(Shader.Find(...))`
**por flecha** (e TwinShot dispara várias); `Update()` faz `Destroy(gameObject)` em expiração/impacto;
`OnDestroy()` destrói o material. Chamadores: `ArsenalCombat`, `SplitArrowCoordinator` e a validação de
Editor `ArsenalValidation`.

**Decisão — centralizar o pool ATRÁS da API estática, sem mudar call-sites:**
- Adicionar um `static ComponentPool<ArsenalProjectile>` (com seu root `"ArrowPool"`). `FireSingle`
  passa a fazer `Pool.Acquire()` em vez de `new GameObject`; nenhum chamador muda (`Fire` continua com
  a mesma assinatura, e `Fire` continua expandindo TwinShot em N `FireSingle`). ✔ R3.1
- A **fábrica** cria o `GameObject`, o `ArsenalProjectile`, o `LineRenderer` e **um** `Material`
  (`Shader.Find("Universal Render Pipeline/Unlit")`) **uma vez por instância**. O Material e o
  `LineRenderer` são reusados; nada de `new Material`/`Destroy(material)` por disparo (R3.8). O
  `Shader.Find` é cacheado num `static` para não repetir a busca.
- **Reset no acquire** (`ResetForReuse(...)`, chamado por `FireSingle` após `Acquire`): reposiciona
  (`SetPositionAndRotation`), reatribui `_owner/_damage/_multiplier/_remaining/_piercing/_bounces/
  _homing/_returning/_weapon/_nextSeek`, faz `_returned = false`, `_hit.Clear()`, `_seekTarget = null`
  e `_material.color = color`. Isso garante que uma reutilização se comporte como recém-criada
  (R3.4) e preserva **exatamente** pierce/homing/ricochete/retorno/dano (R3.6).
- **Release em vez de Destroy:** substituir cada `Destroy(gameObject)` do `Update()` por
  `ReleaseToPool()` (desativa e devolve ao pool). O `OnDestroy` que destruía o material deixa de ser o
  caminho normal (o material vive com a instância pooled); o descarte real do material só ocorreria no
  teardown do play. ✔ R3.8
- **Pierce/ricochete/retorno intactos:** os ramos que hoje fazem `continue`/`return` sem destruir
  seguem iguais; só os pontos terminais (que faziam `Destroy`) passam a `ReleaseToPool()`. ✔ R3.6

> `ArsenalValidation` (Editor) apenas exercita `Fire`; como o pool fica atrás de `Fire`, a validação
> segue funcionando sem alteração de call-site.

### 4. Projétil de inimigo — `EnemyProjectile` (R3.2, R3.4, R3.6, R3.7)

**Situação atual (verificada):** `EnemyProjectile.Spawn(...)` é a fábrica única
(`EnemyCombatActions`, `HazardCasterBehavior` a usam) — faz `new GameObject("Enemy projectile")` +
`AddComponent` + `Configure(...)`; `Update()`/`StepStraight`/`StepArced`/`OnOwnerDied` fazem
`Destroy(gameObject)`; `OnDestroy` libera material + telegraph. A lógica de viagem/hit já vive na
classe pura `ProjectileMotion` (já property-testada).

**Decisão — mesmo padrão, atrás de `Spawn`:**
- `static ComponentPool<EnemyProjectile>` (root `"EnemyProjectilePool"`); `Spawn` faz `Acquire` em vez
  de `new GameObject` e chama `Configure(...)` normalmente. Nenhum chamador muda. ✔ R3.2
- **Reset no acquire:** `Configure(...)` já reinicializa `_motion` (novo `ProjectileMotion`), direção,
  arco e telegraph; complementar com limpeza do que persiste entre usos — reidratar/limpar o
  `_impactTelegraph` anterior, `_flightElapsed = 0`, re-subscrever o owner (`_ownerSubscribed=false`
  antes de `SubscribeOwner`), reusar o `_runtimeMaterial` já criado (não recriar). ✔ R3.4/R3.8
- **Release:** trocar os `Destroy(gameObject)` de `StepStraight`/`StepArced`/`OnOwnerDied`/cascade por
  `ReleaseToPool()`, que primeiro esconde/limpa o telegraph (como o `OnDestroy` faz hoje) sem destruir
  o material reusável. A cascata de morte do owner (`Died`/`MarkOwnerGone`) e o "não aplicar mais dano"
  seguem idênticos (R3.6/R19.6 preservado).

### 5. Moedas — `CoinPickup` + `CoinDrop` (R3.3, R3.4, R3.6, R3.7)

**Situação atual (verificada):** `CoinDrop.SpawnCoin` faz `Instantiate(prefab, …)` a partir de
`Resources.Load<CoinPickup>("Items/CoinPickup")` (cacheado em `_sharedCoin`). `CoinPickup.Collect()`/
`Expire()` fazem `Destroy(gameObject)` (ou via `FlourishThenDestroy`). Há `_collected`,
`Invoke(nameof(Expire), lifetime)` e `CancelInvoke`.

**Decisão:**
- `static ComponentPool<CoinPickup>` cuja **fábrica** faz `Object.Instantiate(prefab)` do prefab de
  `Resources` **uma vez por instância** (o prefab continua sendo o asset Unity-managed; só paramos de
  instanciar/destruir por moeda). `CoinDrop.SpawnCoin` passa a `Acquire` + posicionar + `SetValue` +
  `SetPlayer`. ✔ R3.3
- **Reset no acquire** (`ResetForReuse`): `_collected = false`, `CancelInvoke()` e re-`Invoke(Expire,
  lifetime)`, `_basePosition`/posição/`localScale` e fase de spin reinicializados (o que `Start` faz
  hoje passa a rodar no reset, para reuso). ✔ R3.4
- **Release em vez de Destroy:** `Collect()` e o **fim** de `FlourishThenDestroy()` chamam
  `ReleaseToPool()` em vez de `Destroy`. Valor coletado, magnet, bob e flourish permanecem idênticos
  (R3.6).

### 6. HUD/UI sem alocação por frame (R2)

- **`PlayerHUD`:** introduzir um pequeno cache de valores exibidos (`UiValueCache`, plain C#): guarda
  os últimos inteiros renderizados (vida/mana/asura, índice de slot, etc.). No `Update`, comparar o
  valor atual com o cacheado; **só** quando muda, remontar a string com o **mesmo** formato
  (`$"{Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maxHealth)}"`, etc.) e atribuir ao TMP. Tooltip e
  `_feedback` recebem o mesmo tratamento (guardar o último texto e só reatribuir quando difere), em vez
  de `clear/set` todo frame. ✔ R2.1/R2.2/R2.6
- **`RunRewardUI`:** manter o desenho IMGUI, mas cachear os rótulos/strings construídos e reconstruí-los
  **só** quando os dados de recompensa mudam (`_run.Acquired.Count`, conjunto de escolhas, item sob
  hover). O layout e as informações mostradas ficam idênticos. ✔ R2.3
- **`CombatReadabilityUI.LateUpdate`:** só remontar strings e recomputar `WorldToScreenPoint` quando os
  valores de origem mudarem; posicionamento em tela preservado. ✔ R2.4
- O ganho de alloc/frame atribuível à UI é confirmado no Profiler (R2.5).

> A parte pura e testável é a **decisão de dirty** ("o valor mudou?") e a **formatação** ("mesmo estado
> ⇒ mesma string"). Ambas viram propriedades (P3/P4), sem depender de cena.

### 7. Hot paths (R4)

- **`OutlineScript`:** cachear `Camera.main` num campo (`_cam`, refrescado se `null`); throttlar o
  raycast com um contador de frames/orçamento de tempo (`FrameThrottle`, pura) para não fazer
  `Physics.Raycast` todo frame; cachear o `Outline` por `Transform` (dicionário ou
  `TryGetComponent`-uma-vez) em vez de vários `GetComponent<Outline>()` por frame. O destaque
  percebido no hover não muda. ✔ R4.1
- **`Actor`:** remover a chamada `UpdateHealthBar()` do `Update()` (que hoje roda para **cada** actor
  todo frame) e, em `Awake`, assinar o próprio evento `HealthChanged` para chamar `UpdateHealthBar` —
  o `Actor` já dispara `HealthChanged` em `TakeDamage`/`SetMaxHealth`/`Heal`/`RestoreHealthToMax`, e
  `UpdateHealthBar` já é chamado direto nesses pontos, então a barra continua idêntica (fill =
  `clamp01(health/max)`), apenas sem o custo por frame. ✔ R4.2/R4.4
- **`GetComponent` por frame → cache em `Awake`/`Start`:** aplicar aos casos onde de fato existir um
  `GetComponent`/`GetComponentInParent` por frame em `Update`/`LateUpdate` (candidatos da investigação:
  `EnemyAI`, `PreferredDistanceLayer`, `FrontalReflector`, `CombatReactionController`,
  `ProtectionIndicator`, `Shield`, `PriorityTargetMarker`, `StatusEffect`). A referência é resolvida
  uma vez e reusada; onde ela é obrigatória, validar em `Awake`/`OnValidate` e logar um aviso claro em
  vez de gerar `NullReferenceException` silenciosa por frame. ✔ R4.3/R4.5
- Lista tratada como **candidatos**: aplica-se onde um `GetComponent` por frame realmente existe; não
  se promete refatorar arquivos que já cacheiam.

### 8. Preload/cache de `Resources` (R5)

- **`WeaponLoadout`:** adicionar um `WeaponCache` estático (índice → `WeaponScript`) preenchido uma
  vez no boot (`[RuntimeInitializeOnLoadMethod]`), carregando os três `ResourcePaths`
  ("Weapons/Melee/Gauntlet/Gauntlet", "Weapons/Ranged/Bow_arrow/Bow", "Weapons/Melee/Spear/Spear").
  `LoadSelected()`/`Select()` passam a ler do cache em vez de `Resources.Load` no caminho de troca; a
  arma equipada permanece a mesma. ✔ R5.1
- **`HitboxDamage`:** aquecer o efeito no boot chamando `GetEffectInstance(path)` uma vez (o pool já
  existe), de modo que o primeiro uso em combate não dispare carga síncrona de disco. ✔ R5.2
- Assets sob `Resources` continuam Unity-managed: **não** são movidos/removidos sem confirmar que
  nenhum `Resources.Load` depende do caminho (R5.4). Se um preload falhar (path inválido), logar aviso
  e cair na carga sob demanda atual, sem quebrar o combate (R5.5).

### 9. Import de texturas e assets (R6)

- Uma passada de Editor revisa `maxTextureSize`, compressão por plataforma e `mipmaps` conforme o uso
  (3D vs. UI), priorizando as maiores (provavelmente sob "STYLIZED MALE CHARACTER"), para reduzir
  `Texture Memory` no Profiler (R6.1). Texturas estritamente de UI/sprite recebem settings de UI
  (ex.: sem mipmaps quando desnecessário), preservando a nitidez na resolução de uso (R6.2).
- Atlas só onde o ganho de draw calls for demonstrável, sem perda visual (R6.3).
- **Somente import settings**: edição do bloco do importer no `.meta`, **sem recriar o asset**, logo
  `.meta`/GUID e todas as referências ficam intactos (R6.5). Aparência preservada, sem
  artefato/blur/banding (R6.4). Em asset third-party, limita-se a settings, sem tocar
  conteúdo/código (R6.6).

### 10. Limpeza segura de órfãos e third-party — Editor (R7)

Ferramentas novas em `Assets/_Project/Scripts/Editor/Cleanup/` (assembly de Editor; não entra no
build):

- **`AssetReferenceScanner` (lógica pura):** dado o GUID de um asset e o conteúdo dos arquivos de
  referência (`.unity`, `.prefab`, `.asset`, `.meta`), decide se o GUID é **referenciado**. Um asset
  é candidato a órfão **somente** se seu GUID não aparecer em nenhum desses arquivos (R7.1). Como é
  pura, entra em property tests (P8) e é o coração da garantia de segurança.
- **`OrphanClassifier`:** aplica duas guardas antes de marcar candidato — (a) asset sob `**/Resources/**`
  nunca é classificado como órfão puro sem confirmar ausência de uso por caminho (R7.2, P9); (b)
  whitelist de pacotes comprovadamente usados (TextMesh Pro pela UI, QuickOutline pelo `OutlineScript`)
  nunca é proposta para remoção (R7.7).
- **`ProjectCleanupReport` (janela/menu de Editor):** roda a varredura e produz um **relatório
  legível** — por candidato: caminho, tamanho, motivo (ausência de referências), e para pacotes
  third-party suspeitos (destaque para "STYLIZED MALE CHARACTER" ~565 MB) distingue **"remoção total
  do pacote"** de **"trim parcial"** (só sub-assets não usados) (R7.3/R7.8). O relatório é salvo como
  artefato versionável.
- **Portão "identificar → confirmar → remover":** a ferramenta **só reporta**. A remoção é um passo
  separado, **item-a-item, com confirmação explícita do usuário**; nada é removido em lote ou de forma
  silenciosa (R7.4). Se **qualquer** referência for encontrada (cena/prefab/SO ou path de
  `Resources`), a remoção é **rejeitada** e a origem da referência é relatada (R7.5). Ao remover após
  confirmação, o asset é removido **junto do seu `.meta`** (via `AssetDatabase`), sem deixar `.meta`
  órfão nem GUID pendente (R7.6).

#### Formato do relatório (R7.3/R7.8)

```
# Relatório de candidatos a remoção — <data>
## Órfãos (sem referência de GUID encontrada)
- <caminho>  | <tamanho> | motivo: nenhum GUID em .unity/.prefab/.asset/.meta
## Requer verificação de path (sob Resources)
- <caminho>  | <tamanho> | motivo: pode ser alvo de Resources.Load — confirmar antes
## Pacotes third-party
- STYLIZED MALE CHARACTER | ~565 MB | classificação: [remoção total | trim parcial] | sub-assets usados: <lista>
- FastScriptReload | 22 MB | ver Requisito 8
## Mantidos (whitelist / referenciados)
- TextMesh Pro | usado pela UI    - QuickOutline | usado por OutlineScript
```

### 11. FastScriptReload (R8)

- Correlacionar os logs de auto-reload ("full reload will be triggered") com picos de frame no Editor,
  registrando a evidência (R8.1). Se o auto-reload durante o Play for identificado como contribuinte,
  **propor desabilitá-lo via configuração da ferramenta** (config-only), com confirmação do usuário,
  **sem** editar o código-fonte do pacote (R8.2/R8.3). Se inconclusivo, relatar como inconclusivo e
  manter o estado atual, sem mudança às cegas (R8.4).

## Fluxo de dados — disparo de flecha com pooling (o caminho que era `new`/`Destroy`)

```
ArsenalCombat/SplitArrowCoordinator → ArsenalProjectile.Fire(...)
   └─ N× FireSingle(...)   (N = 1 + 2·TwinShot)
        ├─ arrow = Pool.Acquire()               // reusa instância inativa OU cria (R3.7)
        ├─ arrow.ResetForReuse(owner, dmg, ..., color)
        │     └─ SetPosRot; _hit.Clear(); _returned=false; _seekTarget=null; _material.color=color  (R3.4)
        └─ arrow.gameObject.SetActive(true)
   Update() por frame: sweep/pierce/ricochete/homing/return  (idênticos — R3.6)
        expira/atinge/cenário sólido → arrow.ReleaseToPool()  // era Destroy (R3.1)
             └─ SetActive(false); Pool.Release(arrow)          // Material/LineRenderer preservados (R3.8)
```

## Decisões de design e alternativas

- **Pool genérico compartilhado vs. pool por sistema:** escolhido um **helper genérico**
  (`SimpleObjectPool<T>` puro + `ComponentPool<T>`), porque concentra o núcleo testável num só lugar
  (R10.2) e reduz duplicação entre flecha/inimigo/moeda. A alternativa (espelhar o dicionário estático
  de `HitboxDamage` em cada sistema) evitaria uma abstração nova, mas repetiria a lógica de
  acquire/expand três vezes e dificultaria o property test. O adaptador **espelha** o padrão existente
  (root lazy, reuso por instância inativa), então a coesão pedida por R3.5 é mantida.
- **Pool atrás da API estática (`Fire`/`Spawn`) vs. mudar chamadores:** escolhido **atrás da API** —
  zero mudança em `ArsenalCombat`/`SplitArrowCoordinator`/`ArsenalValidation`/`EnemyCombatActions`/
  `HazardCasterBehavior`, menor superfície de regressão, e o pooling fica invisível ao gameplay (R9.6).
- **Barra de vida por evento vs. dirty-flag no `Update`:** escolhido **por evento** (`HealthChanged`),
  pois o `Actor` já dispara o evento em todos os pontos de mutação de vida e já chama `UpdateHealthBar`
  ali; assinar o evento elimina o custo por frame sem risco de "esquecer" um caminho. A alternativa
  dirty-flag ainda rodaria uma comparação por frame por actor — pior e desnecessário.
- **UI: dirty-tracking vs. reescrever para eventos:** escolhido **dirty-tracking** (comparar valor
  exibido) por ser mínimo, local e de baixo risco, preservando o texto/format atual (R2.6). Reescrever
  o HUD para um modelo orientado a eventos seria uma refatoração grande, fora do escopo (R9.6).
- **Import: atlas vs. settings por textura:** priorizar **settings por textura** (ganho garantido de
  memória, risco visual baixo) e usar **atlas só onde o ganho de draw calls for demonstrável** — atlas
  mal aplicado pode introduzir sangramento de borda/mudança visual, o que violaria R6.4.
- **Third-party: remoção total vs. trim parcial:** o relatório **distingue** os dois (R7.8) e nunca
  decide sozinho; a "STYLIZED MALE CHARACTER" (565 MB) é o candidato primário, mas só sai após
  confirmação item-a-item — se algum sub-asset estiver referenciado, vira "trim parcial" e o resto é
  mantido (R7.5).
- **Limpeza: ferramenta que remove vs. ferramenta que só reporta:** escolhido **só reporta** + remoção
  manual confirmada. Automatizar a remoção violaria o portão de segurança e arriscaria quebrar
  referências (R7.4).

## Estratégia de testes

O único núcleo automatizável de forma confiável via CLI é **lógica pura** (R10.1/R10.2). O restante é
validado por **Profiler + playtest manual + inspeção/`git diff`**, com transparência explícita.

**EditMode (property/example tests, no estilo de `ProjectileMotionPropertyTests`/
`CombatBalanceBoundsPropertyTests`, em `Assets/_Project/Scripts/Tests/EditMode/Editor/`, FsCheck,
mínimo 100 iterações, cada teste com a tag `Feature: project-cleanup-optimization, Property N: <texto>`):**

- **P1 (pool):** `SimpleObjectPool<T>` — acquire nunca entrega instância viva duplicada; expande
  quando vazio; release permite reuso; `CreatedCount` nunca excede o high-water-mark de instâncias
  simultâneas (sem criação por-acquire quando há livres). Cobre R3.1/R3.2/R3.3/R3.7/R3.8/R10.2.
- **P2 (reset):** para qualquer "estado sujo", após `Reset` todos os campos rastreados voltam ao
  default (contadores zerados, coleção `_hit` vazia, flags `false`). Modelado sobre um objeto de
  estado puro que espelha os campos do projétil/moeda. Cobre R3.4.
- **P3 (UI dirty):** o cache só (re)emite texto quando o valor de origem difere do anterior. Cobre
  R2.1/R2.3/R2.4.
- **P4 (UI formatação):** para o mesmo estado, a string do formatter é idêntica à fórmula atual
  (vida/mana/asura). Cobre R2.2/R2.6.
- **P5 (barra de vida):** para qualquer sequência de mutações de vida, a barra recomputa sse o valor
  mudou, e o valor é `clamp01(health/max)` — idêntico ao atual. Cobre R4.2.
- **P6 (throttle):** `FrameThrottle` dispara no máximo uma vez por intervalo e nunca perde o primeiro
  tick. Cobre R4.1 (parte lógica).
- **P7 (cache de armas):** para todo índice válido, `get` é idempotente e não recarrega após preload;
  índice inválido cai no fallback do índice 0. Cobre R5.1.
- **P8 (detector de órfão):** um asset é candidato **sse** seu GUID não aparece em nenhum arquivo de
  referência; se aparece em ≥1, não é candidato (e a origem é reportável). Cobre R7.1/R7.5/R7.7.
- **P9 (guarda de Resources):** para qualquer asset sob `**/Resources/**`, o classificador nunca o
  marca como órfão puro (marca "requer verificação de path"). Cobre R7.2.

**Example/edge tests:** validação de dependência ausente emite aviso e não gera NRE por frame (R4.5);
preload falho cai no comportamento atual com aviso (R5.5); relatório contém os campos esperados e
distingue total/trim (R7.3/R7.8).

**Não-regressão de lógica pura:** rodar a suíte EditMode existente (incl. `ProjectileMotion*`,
`ArsenalRicochetDecayInvariantTests`, `DamageAbsorbedDecisionPropertyTests`) e garantir que segue
verde após as mudanças de pooling/hot path (R3.6/R9.1).

**Profiler (antes/depois) — não via CLI:** `GC Allocated In Frame` em ocioso deve cair perto de zero
após R2/R4; frequência/tamanho dos picos de GC em combate deve reduzir após R3; `Texture Memory` deve
cair após R6 (R2.5/R6.1/R1.5).

**Inspeção/`git diff`:** `.meta`/GUID preservados em toda edição de import setting e movimentação de
asset (R6.5/R9.2/R9.3); nenhuma edição em código third-party salvo config do FastScriptReload
(R8.3/R9.5); remoção sempre remove o `.meta` junto (R7.6).

**Playtest manual (Editor/Play):** as 8 cenas abrem sem missing script/asset (R9.4); combate, câmera,
UI e coleta de moedas idênticos ao atual (R3.6/R4.4/R9.1); flechas/ricochete/homing/retorno e valor de
moeda sem diferença perceptível; auto-reload do FastScriptReload correlacionado (ou não) com spikes
(R8.1).

**Resumo de validação (R10.4):** ao final, entregar um resumo distinguindo claramente **o que foi
medido no Profiler**, **o que foi verificado por inspeção/`git diff`** e **o que depende de playtest
manual do usuário**, declarando explicitamente o que não pôde ser validado via CLI (R10.1).

> Observação de ambiente: o Unity não roda de forma confiável via CLI aqui (o test runner de EditMode
> já sofre timeout), então as garantias em cena/Play mode são validadas manualmente e reportadas como
> tal — os property tests de lógica pura são o piso automatizável.
