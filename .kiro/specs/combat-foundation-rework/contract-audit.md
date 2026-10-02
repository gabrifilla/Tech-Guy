# Auditoria de Contrato — Combat Foundation Rework (Tarefa 11)

> **Objetivo (R8.1–R8.4):** registrar os emissores e assinantes reais dos canais de evento/APIs que
> o refactor (tarefas 13/15/16/18) deve preservar **por contrato**, e as relações de ordem que os
> boons realmente dependem, para que a semântica observável seja preservada ou explicitamente adaptada.
>
> **Escopo:** somente leitura. Nenhuma mudança de código de produção foi feita.
>
> **Como ler:** cada membro traz assinatura exata, emissor(es) real(is) com arquivo + método, e
> assinantes/consumidores com a semântica observável de que dependem. A seção final consolida os
> **invariantes de ordem a preservar**.

## Arquivos âncora inspecionados

| Arquivo | Caminho |
| --- | --- |
| `AbilityHolder` | `Assets/_Project/Scripts/Abilities/AbilityHolder.cs` |
| `CharControlScript` | `Assets/_Project/Scripts/Characters/Player/Actions/CharControlScript.cs` |
| `HookBus` | `Assets/_Project/Scripts/Core/HookBus.cs` |
| `BreakerGauntletCombat` | `Assets/_Project/Scripts/Abilities/Weapon/BreakerGauntletCombat.cs` |
| `PlayerActor` (pipeline de dano/hit) | `Assets/_Project/Scripts/Characters/Player/PlayerActor.cs` |
| `HitboxDamage` (único emissor de `OnBasicHit`) | `Assets/_Project/Scripts/Weapons/HitboxDamage.cs` |

---

## 1. `CharControlScript.BasicAttackPerformed`

- **Assinatura:** `public event System.Action BasicAttackPerformed;` (`CharControlScript.cs`, l. 99).
- **Emitido por:** `CharControlScript.PerformAttack(Actor victim)` — `BasicAttackPerformed?.Invoke();`
  é a **última** linha do método (`CharControlScript.cs`, l. 874), disparada **uma vez por ataque
  básico executado**, depois de:
  1. tocar a animação de ataque;
  2. resolver dano (projétil de Arco via `ArsenalProjectile.Fire`, ou área melee via
     `playerActor.TryApplyAreaDamage(...)`);
  3. incrementar `_runBasicCount` + `TryProcComboNova()` (apenas no caminho melee);
  4. marcar `playerBusy` e agendar `ClearBusyAfter`.
- **Condição de emissão:** só é alcançada após `CanStartBasicAttack()` (não ocupado, fora de cooldown,
  não em dash, não castando) e um alvo/direção válidos. Portanto representa um ataque **efetivamente
  iniciado**, não apenas uma tentativa.
- **Assinantes reais:**
  - `KitingStepCoordinator` (`Assets/_Project/Scripts/Core/KitingStepCoordinator.cs`): `Subscribe()`
    liga `_controls.BasicAttackPerformed += OnBasicAttack`; em `OnBasicAttack` reposiciona o jogador
    via `NavMeshAgent` na direção corrente de movimento. Observa:
    - **cardinalidade:** um impulso **candidato** por disparo do evento (um por ataque básico);
    - um **gate temporal próprio** (`_nextImpulseAt`) impede encadear impulso a cada frame — ou seja,
      ele tolera N disparos mas limita a frequência de efeito;
    - só existe **enquanto a família de arma é Arco** (criado/reconfigurado por `RunBoons`), logo na
      prática só o básico de Arco chega a ele.
  - `KitingStepPlayModeTests` dispara o evento real por reflexão para validar o reposicionamento.
- **Semântica observável a preservar:** o evento deve continuar disparando **exatamente uma vez por
  ataque básico que foi executado** (tap => 1; cada repetição de hold => 1), e **não** disparar quando
  o ataque é recusado/no-op (R1.8/R1.11). Nada no assinante depende de ordem entre `BasicAttackPerformed`
  e a resolução de dano — mas, de fato, hoje o dano é resolvido **antes** do `Invoke()`.

---

## 2. `CharControlScript.CancelCombo()`

- **Assinatura:** `public void CancelCombo()` (`CharControlScript.cs`, l. 765).
- **Efeito observável:** zera `currentComboCount`, limpa `playerBusy`, zera `lastAttackTime`, para as
  coroutines `attackBusyCoroutine`/`hitboxCoroutine` e chama `playerActor.DeactivateHitbox()`. É a
  forma canônica de **interromper o estado de combo/ocupação do ataque básico** sem matar um golpe que
  já resolveu dano (ele apenas limpa estado de encadeamento).
- **Chamado por (emissores/consumidores):**
  - `AbilityHolder.CheckUse(...)` (`AbilityHolder.cs`, l. 240) — ao autorizar uma habilidade,
    `_characterControl.CancelCombo()` é chamado **antes** de `ability.TryActivate` (via `TryUseAbility`),
    garantindo que o básico não siga ocupado durante um cast.
  - `BreakerGauntletCombat.Execute(...)` (`BreakerGauntletCombat.cs`, l. 106) — no início do cast da
    Manopla: `if (TryGetComponent(out CharControlScript control)) control.CancelCombo();`.
  - `PlayerActor.EquipWeapon` (`PlayerActor.cs`, l. 198) — ao trocar de arma.
  - `WeaponLoadout.cs` (l. 101), `LobbyInteraction.cs` (l. 94), `RunBoons.cs` (l. 202) — ao abrir
    painéis/lobby/escolha de boons (bloqueio de mundo).
  - `MoveToPosition(...)` (`CharControlScript.cs`, l. 609) — uma **Ordem_de_Movimento** cancela o combo
    e limpa o alvo (R1.9).
  - `CharControlScript.OnDisable()` (l. 148).
- **Semântica observável a preservar:** continuar existindo e, ao ser chamado, deixar o jogador apto a
  iniciar outra ação sem resíduo de `playerBusy`/combo. O refactor que troca o bloqueio total por
  `CancelResolver` (tarefa 13.4) deve manter `CancelCombo()` como o ponto que encerra o encadeamento do
  básico quando uma habilidade/dash cancela o estado atual.

---

## 3. `CharControlScript.RequireAttackRelease()`

- **Assinatura:** `public void RequireAttackRelease() => _waitForAttackRelease = true;`
  (`CharControlScript.cs`, l. 100).
- **Efeito observável:** arma uma trava (`_waitForAttackRelease`) que faz `HandlePointerInput()` ignorar
  o input primário **até que o botão seja solto** (`if (!primary) _waitForAttackRelease = false;` seguido
  de `if (_waitForAttackRelease) return;`, l. ~175). Evita que o clique usado para **retomar do pause**
  ou **confirmar uma escolha de boon** seja interpretado como ataque/movimento.
- **Chamado por:**
  - `PauseMenuUI` ao retomar o jogo (`PauseMenuUI.cs`, l. 50).
  - `RunBoons` ao fechar a escolha de recompensa (`RunBoons.cs`, l. 411).
- **Semântica observável a preservar:** o primeiro input primário **após** armar a trava não deve gerar
  ataque nem Ordem_de_Movimento; a trava limpa quando o botão é solto. Nenhum boon depende disso, mas é
  parte do contrato de UX de retomada.

---

## 4. `AbilityHolder.AbilityUsed`

- **Assinatura:** `public event Action<int> AbilityUsed;` (`AbilityHolder.cs`, l. 43). O índice `int` é o
  slot: `0..3` = Q/W/E/R; **`4` = dash**.
- **Emitido por:**
  - `AbilityHolder.TryUseAbility(int index)` (`AbilityHolder.cs`, l. 228): `AbilityUsed?.Invoke(index);`
    é a **última** ação de um uso bem-sucedido, **após** `ability.TryActivate` (que deduz mana) e **após**
    a transição de estado `Ready -> Active/Cooldown`. Dispara **uma vez por uso bem-sucedido** do slot.
  - `AbilityHolder.TryUseDash(DashScript dash)` (`AbilityHolder.cs`, l. 254): `AbilityUsed?.Invoke(4);`
    após `dash.TryActivate(...)` bem-sucedido e **imediatamente antes** de `_runBoons.Hooks?.RaiseDash()`.
- **Assinantes reais:**
  - `PlayerHUD` (`Assets/_Project/Scripts/UI/PlayerHUD.cs`, l. 56): `_holder.AbilityUsed += OnUsed`
    (feedback cosmético de cooldown na HUD).
  - `TutorialDirector` (`Assets/_Project/Scripts/Core/TutorialDirector.cs`, l. 89): avança estágios do
    tutorial a cada habilidade usada.
  - `ImpactGuardTracker` (Guard Breaker, `Assets/_Project/Scripts/Core/ImpactGuardTracker.cs`, l. 71):
    `_holder.AbilityUsed += OnAbilityUsed` e em `OnAbilityUsed(int slot)` **reseta a streak** de básicos
    consecutivos (`_counter.Reset()`), independentemente do slot. **R5.3 do boon depende de que qualquer
    uso de habilidade dispare `AbilityUsed`** para interromper a streak de golpes básicos.
- **Semântica observável a preservar:**
  - disparo **uma vez por uso bem-sucedido**, nunca em rejeição;
  - o valor `4` continua significando **dash** (o Guard Breaker reseta em qualquer slot, incluindo dash);
  - para o dash, a ordem atual é `AbilityUsed(4)` **antes** de `OnDash` (ver §10, invariante O-5).

---

## 5. `AbilityHolder.AbilityRejected`

- **Assinatura:** `public event Action<int, AbilityUseFailure> AbilityRejected;` (`AbilityHolder.cs`, l. 44).
  Mesma convenção de slot (`4` = dash).
- **Emitido por:** o helper central `Reject(int index, AbilityUseFailure reason)` (`AbilityHolder.cs`,
  l. 262): `AbilityRejected?.Invoke(index, reason); return false;`. Os pontos de chamada:
  - `TryUseAbility`: slot não-`Ready` ⇒ `Cooldown`; falha de `TryActivate` ⇒ `Requirement`.
  - `CheckUse(...)`: `BlocksWorldInput` ⇒ `Interaction`; morto/inativo ⇒ `Unavailable`; lobby ⇒
    `Interaction`; `IsCasting || isDashing` ⇒ **`Busy`**; `!CanActivate` ⇒ `Requirement`; sem mana ⇒
    `NotEnoughMana`.
  - `TryUseDash`: cooldown do dash > 0 ⇒ `Reject(4, Cooldown)`; `CheckUse(4,...)` falho ⇒ a razão
    correspondente; `dash.TryActivate` falho ⇒ `Reject(4, Requirement)`.
- **Assinantes reais:** `PlayerHUD.OnRejected` (`PlayerHUD.cs`, l. 57) — feedback cosmético (ex.: shake/
  cue de "não pode usar"). Nenhum boon assina.
- **Semântica observável a preservar (R5.12/R6.5/R6.6):**
  - o refactor **não pode** emitir `AbilityRejected` a cada reavaliação interna de uma intenção
    bufferizada; **no máximo uma** notificação terminal por tentativa recusada;
  - dash em cooldown deve continuar emitindo `AbilityRejected(4, Cooldown)` **antes** de cancelar a ação
    atual, com o cooldown preservado (R6.6);
  - a razão `Busy` hoje vem do bloqueio total `IsCasting || isDashing`; ao trocar pelo `CancelResolver`
    (tarefa 13.4), a rejeição por "janela de cancel fechada / destino indisponível" deve continuar
    chegando como **uma** rejeição observável no mesmo canal.

---

## 6. `AbilityHolder.AttackHitsResolved`

- **Assinatura:** `public event Action<PlayerActor, IReadOnlyList<Actor>> AttackHitsResolved;`
  (`AbilityHolder.cs`, l. 49).
- **Emitido por:** `AbilityHolder.NotifyAttackHits(PlayerActor owner, IReadOnlyList<Actor> damagedActors)`
  (`AbilityHolder.cs`, l. 138): guarda contra `owner` nulo / lista vazia; despacha primeiro para cada
  `AttackPassiveAbility.OnAfterAttackHits(owner, damagedActors)` e **depois** invoca
  `AttackHitsResolved?.Invoke(owner, damagedActors)`.
- **Quem chama `NotifyAttackHits` (origem real dos acertos):**
  - `PlayerActor.TryApplyAreaDamage(...)` (`PlayerActor.cs`, l. 431): após o loop de colisões, se
    `damageCount > 0`, chama `abilityHolder.NotifyAttackHits(this, damagedActors)` **uma vez por
    chamada de área**, com a lista deduplicada de atores que **efetivamente** sofreram dano. Este é o
    caminho do **básico melee** (via `CharControlScript.PerformAttack`), do **ComboNova** e das áreas de
    habilidade que passam por `TryApplyAreaDamage`.
  - `ArsenalProjectile` (`Assets/_Project/Scripts/Weapons/ArsenalProjectile.cs`, l. 175): por acerto de
    projétil de Arco, `holder.NotifyAttackHits(_owner, new[] { actor })` — **um ator por chamada**.
- **Assinantes reais:**
  - `PerfectSpacingFeedback` (`Assets/_Project/Scripts/Effects/PerfectSpacingFeedback.cs`): feedback
    cosmético de streak de "espaçamento perfeito"; em `OnAttackHitsResolved` filtra `owner == _player` e
    escala uma streak conforme os atores atingidos estão na faixa de distância.
  - `EdgeVulnerabilityRegistry` (Edge Strike / Lança, `Assets/_Project/Scripts/Core/EdgeVulnerabilityRegistry.cs`):
    em `OnResolved` abre janelas de vulnerabilidade para acertos "de ponta" (lido depois por
    `DealResolvedAttackDamage` via o amplificador). Instanciado/assinado por `RunBoons` (R13).
- **Semântica observável a preservar (R8.5):**
  - uma emissão **por resolução de ataque** carregando a **mesma** lista de atores danificados;
  - a ordem interna **passivas primeiro, depois o evento** deve ser mantida (os passivos podem alterar
    estado que o assinante de evento observa);
  - uma ação **cancelada antes de resolver dano** não deve gerar `AttackHitsResolved` daquele evento;
  - **não duplicar**: uma reavaliação de buffer/transição por cancelamento não pode fazer o mesmo acerto
    notificar duas vezes (tarefa 18.2 via ledger/consumo).

---

## 7. `AbilityHolder.IsCasting`

- **Assinatura:** `public bool IsCasting =>` (`AbilityHolder.cs`, l. 52) — verdadeiro quando
  `(_breakerCombat && _breakerCombat.IsExecuting) || (_arsenalCombat && _arsenalCombat.IsExecuting)`.
- **Consumidores reais (leitores do estado observável):**
  - `AbilityHolder.CheckUse` (l. 239): `if (IsCasting || isDashing) return Reject(..., Busy)` — é o
    **bloqueio total** que a tarefa 13.4 vai substituir pelo `CancelResolver`, preservando o valor
    observável de `IsCasting`.
  - `CharControlScript.Update` (l. ~190): `if (_abilityHolder && _abilityHolder.IsCasting && !_abilityHolder.MovementAllowedWhileCasting) return;` — pino de locomoção durante cast melee.
  - `CharControlScript.CanStartBasicAttack` (l. ~440): inclui `!_abilityHolder.IsCasting`.
  - `CharControlScript.RequestMove` / `ClickToMove` (l. ~555 / l. ~618): bloqueiam Ordem_de_Movimento
    durante cast melee.
  - `WeaponLoadout.cs` (l. 101): impede troca de arma durante cast.
- **Semântica observável a preservar (R4.7):** `IsCasting` continua `true` durante Startup+Active (e
  Recovery enquanto o executor reporta `IsExecuting`) de um cast, para que o restante do código continue
  tratando o estado como "castando" para fins de gating de ataque/ação. O `CancelResolver` muda a
  **decisão de cancelamento**, não o **valor** de `IsCasting`.

---

## 8. `AbilityHolder.MovementAllowedWhileCasting`

- **Assinatura:** `public bool MovementAllowedWhileCasting =>` (`AbilityHolder.cs`, l. 61) — verdadeiro
  **somente** quando `_arsenalCombat && _arsenalCombat.IsExecuting && _arsenalCombat.AllowsMovementWhileFiring`
  (o Arco atira em movimento a velocidade reduzida). Casts melee (Manopla/Lança) deixam `false`.
- **Consumidores reais:** os mesmos pontos de locomoção de `CharControlScript`:
  - `Update` (l. ~190), `RequestMove` (l. ~557), `ClickToMove` (l. ~620) — a condição
    `IsCasting && !MovementAllowedWhileCasting` é o que **pina** o jogador em cast melee e **libera** a
    locomoção em cast de Arco.
- **Semântica observável a preservar (R4.7):** o par `(IsCasting, MovementAllowedWhileCasting)` continua
  determinando a mesma política de locomoção. O refactor da Manopla (melee) **não deve** passar a
  liberar movimento onde hoje não libera, salvo configuração deliberada por fase (fração de movimento),
  e **não deve** alterar o comportamento observável do Arco.

---

## 9. `AbilityHolder.ReduceCooldowns(float)`

- **Assinatura:** `public void ReduceCooldowns(float seconds)` (`AbilityHolder.cs`, l. 275).
- **Efeito observável (R6.1–R6.3 do boon):** valor `<= 0` é no-op (l. 278); percorre apenas os slots em
  **estado `Cooldown`** e aplica `cooldownTimers[i] = Mathf.Max(0f, cooldownTimers[i] - seconds)`.
  **Nunca** lê/escreve o `cooldownTime` autorado da ability (opera só nos timers vivos por run); slots
  `Ready`/`Active` ficam intactos.
- **Chamadores reais:** API pública consumida por boons/efeitos de redução de recarga (ex.: efeitos que
  concedem redução ao acertar/matar). Não há assinatura de evento; é chamada direta.
- **Semântica observável a preservar:** a assinatura e o escopo (apenas slots em `Cooldown`, clamp em 0,
  no-op para `<= 0`) devem permanecer idênticos. O refactor dos coordenadores **não deve** mudar a
  indexação dos slots (`0..3`), da qual a correspondência Q/W/E/R depende.

---

## 10. `HookBus.OnBasicHit`

- **Assinatura:** `public event Action<Actor, float> OnBasicHit;` (`HookBus.cs`, l. 38) — ator atingido +
  dano efetivamente aplicado. Canal **adicional** ao `OnHit` genérico (que ainda dispara para o mesmo
  acerto).
- **Emitido por:** `HookBus.RaiseBasicHit(actor, damage)` (`HookBus.cs`, l. 88). **Único caminho de
  produção que o chama:** `HitboxDamage.TryDamageActor(...)` (`HitboxDamage.cs`, l. 121–128), no ramo
  `owner is PlayerActor attacker`:
  ```
  float before = actor.health;
  attacker.DealResolvedAttackDamage(actor, finalDamage); // raises OnHit/OnCrit/OnKill
  attacker.RaiseBasicAttackHit(actor, before - actor.health); // raises OnBasicHit then OnBasicKill
  ```
  `PlayerActor.RaiseBasicAttackHit` (`PlayerActor.cs`, l. 337) guarda contra nulo/dano `<= 0` e dispara
  `OnBasicHit` e, se o inimigo morreu, `OnBasicKill` (deduplicado por ator no bus).
- **Granularidade / ordem (CRÍTICO):** o `OnBasicHit` é por **acerto direto de básico** resolvido via
  `HitboxDamage` (trigger do hitbox animado, ativado por `PlayerActor.ActivateHitbox` → animação).
  A sequência por acerto é sempre: **`OnHit` (genérico) → `OnBasicHit` (básico) → `OnBasicKill` (se
  morte)**, porque `DealResolvedAttackDamage` roda antes de `RaiseBasicAttackHit`.
  > **Observação importante para o refactor:** o **básico melee atual da Manopla** em
  > `CharControlScript.PerformAttack` resolve dano por `PlayerActor.TryApplyAreaDamage(...)`, que dispara
  > `OnHit` + `AttackHitsResolved`, **mas NÃO `OnBasicHit`** (o canal básico só sai do caminho
  > `HitboxDamage`/`ActivateHitbox`). Portanto hoje há **dois caminhos de básico** com granularidades
  > diferentes. Qualquer migração do básico da Manopla para `ImpactEvent` deve decidir **conscientemente**
  > por qual canal o acerto básico é surfado, para não (a) parar de alimentar os boons que escutam
  > `OnBasicHit`, nem (b) passar a disparar `OnBasicHit` em acertos de área que antes não disparavam.
- **Assinantes reais (todos boons da Manopla, criados/reconfigurados por `RunBoons`):**
  - `AsuraSurge` (Asura Fist, `Assets/_Project/Scripts/Core/AsuraSurge.cs`): `+2*rank` de energia Asura
    **por `OnBasicHit`** (via `BreakerGauntletCombat.AddAsuraEnergy`, que clampa no máximo). **Ignora
    `OnHit`** de propósito — só o canal básico carrega energia (R4.3 do boon).
  - `AdaptiveCadenceTracker` (`Assets/_Project/Scripts/Core/AdaptiveCadenceTracker.cs`): ativa/remove um
    modificador de cadência conforme a **distância** do básico (acerto na/além da faixa ativa; mais perto
    remove). Depende de **um disparo por acerto básico** com o ator correto para medir distância.
  - `ImpactGuardTracker` (Guard Breaker, `Assets/_Project/Scripts/Core/ImpactGuardTracker.cs`): conta
    **somente** básicos no canal `OnBasicHit`; a cada 3 consecutivos quebra a guarda. Reseta a streak em
    `AbilityHolder.AbilityUsed` (uso de skill). Depende de que **skill não dispare `OnBasicHit`** e de
    que **cada básico** dispare exatamente uma vez.
  - `HungryComboTracker` (Hungry Combo, referenciado por `RunBoons.cs` R6): também escuta `OnBasicHit`.
- **Lifecycle a preservar:** todas as assinaturas são derrubadas por `HookBus.Clear()` no fim da run e
  pelos `OnDestroy` dos coordenadores; o canal básico nunca deve carregar acertos de uma run anterior.

### Canais vizinhos relevantes do `HookBus` (contexto de ordem)

- `OnHit(Actor,float)` / `OnCrit` / `OnKill(Actor)` — emitidos em `PlayerActor.DealResolvedAttackDamage`
  (`PlayerActor.cs`, l. 318–325), **nesta ordem**: `RaiseHit` → `RaiseCrit` (se crítico) → `RaiseKill`
  (se morto). `OnKill` é deduplicado por ator no bus.
- `OnDash()` — emitido por `AbilityHolder.TryUseDash` via `_runBoons.Hooks?.RaiseDash()`
  (`AbilityHolder.cs`, l. 257), **uma vez por dash bem-sucedido**, **após** `AbilityUsed(4)`.
- `OnBasicKill(Actor)` — emitido logo após `OnBasicHit` quando o básico mata (dedup próprio).

---

## Resumo — Invariantes de ordem/semântica a preservar (R8.3)

Os identificadores `O-n` abaixo são referência local para as tarefas 13/15/16/18.

- **O-1 (básico resolvido uma vez):** `BasicAttackPerformed` dispara **exatamente uma vez por ataque
  básico executado** (tap ⇒ 1; cada repetição de hold ⇒ 1), e nunca em no-op/recusa. O dano do básico é
  resolvido **antes** do `Invoke()` no fluxo atual.
- **O-2 (ordem do pipeline de dano direto):** por acerto direto, a ordem é
  `OnHit → OnCrit? → OnKill?` (genérico, em `DealResolvedAttackDamage`). Preservar esta ordem e a
  deduplicação de `OnKill`/`OnBasicKill` por ator.
- **O-3 (canal básico só pelo caminho de básico):** `OnBasicHit`/`OnBasicKill` disparam **se e somente
  se** o acerto veio do caminho `HitboxDamage` (básico), **nunca** por skill/área; e sempre **depois** do
  `OnHit` genérico do mesmo acerto. Boons de Asura/cadência/guarda dependem disto. **Risco do refactor:**
  o básico da Manopla hoje usa `TryApplyAreaDamage` (sem `OnBasicHit`) — a migração para `ImpactEvent`
  deve escolher o canal de básico de forma explícita (adaptar, não quebrar).
- **O-4 (resolução de acerto única e com lista):** `AttackHitsResolved` dispara **uma vez por resolução**
  de ataque, com a lista de atores danificados; internamente **passivas primeiro, depois o evento**.
  Cancelamento antes de resolver dano ⇒ sem emissão; reavaliação de buffer/transição ⇒ **sem
  duplicação** (ledger/consumo — tarefa 18.2).
- **O-5 (dash: ordem e preservação de cooldown):**
  - sucesso ⇒ `AbilityUsed(4)` **antes** de `OnDash`, cada um **uma vez**;
  - cooldown/indisponível ⇒ `AbilityRejected(4, Cooldown)` **antes** de qualquer cancelamento, com o
    cooldown restante **inalterado**;
  - fora de janela de cancel e fora do buffer ⇒ `AbilityRejected(4, ...)` **uma vez**, sem mudar a fase
    da ação atual.
- **O-6 (rejeição terminal única):** `AbilityRejected` emite **no máximo uma** notificação terminal por
  tentativa; **nunca** a cada reavaliação interna de uma intenção bufferizada (R5.12).
- **O-7 (uso de habilidade único e com índice de slot):** `AbilityUsed(index)` dispara **uma vez por uso
  bem-sucedido**; `index 0..3` = Q/W/E/R, `index 4` = dash. Guard Breaker reseta a streak em **qualquer**
  slot; `ReduceCooldowns` depende da mesma indexação de slots.
- **O-8 (`CancelCombo` encerra o encadeamento do básico):** chamado por habilidade/dash/troca de arma/
  Ordem_de_Movimento/pause para limpar `playerBusy`/combo sem matar um golpe já resolvido. O
  `CancelResolver` deve continuar usando `CancelCombo()` (e `ArsenalCombat.Cancel()` /
  `BreakerGauntletCombat.FinishForDodge()`) como o ponto de encerramento ao cancelar a ação atual.
- **O-9 (gating de cast observável):** o par `(IsCasting, MovementAllowedWhileCasting)` continua
  determinando a locomoção e o gating de ataque/ação. Trocar o bloqueio total por `CancelResolver` muda a
  **decisão de cancelamento**, não os **valores observáveis** desses dois membros (R4.7); Arco segue
  liberando movimento, melee (Manopla/Lança) segue pinando, salvo fração de movimento por fase
  deliberada.
- **O-10 (`RequireAttackRelease` consome o próximo input primário):** após retomar do pause ou fechar a
  escolha de boons, o primeiro input primário não gera ataque/movimento até o botão ser solto.
- **O-11 (isolamento por run):** todas as assinaturas de `HookBus` são limpas por `HookBus.Clear()` no
  fim da run; os coordenadores de boon derrubam suas assinaturas no `OnDestroy`. Nenhum canal pode
  carregar efeito de uma run anterior.

## Tabela síntese emissor → assinantes

| Membro | Emissor real (arquivo · método) | Assinantes / consumidores (dependência observável) |
| --- | --- | --- |
| `BasicAttackPerformed` | `CharControlScript.PerformAttack` (l. 874) | `KitingStepCoordinator` (impulso por básico, Arco), testes PlayMode |
| `CancelCombo()` | — (API) | `AbilityHolder.CheckUse`, `BreakerGauntletCombat.Execute`, `PlayerActor.EquipWeapon`, `MoveToPosition`, lobby/pause/boons |
| `RequireAttackRelease()` | — (API) | `PauseMenuUI.Resume`, `RunBoons` (fim da escolha) |
| `AbilityUsed(int)` | `AbilityHolder.TryUseAbility` (l. 228), `TryUseDash` (l. 254, slot 4) | `PlayerHUD`, `TutorialDirector`, `ImpactGuardTracker` (reset de streak) |
| `AbilityRejected(int,reason)` | `AbilityHolder.Reject` (l. 262) | `PlayerHUD.OnRejected` (cosmético) |
| `AttackHitsResolved(owner,list)` | `AbilityHolder.NotifyAttackHits` (l. 150) ← `PlayerActor.TryApplyAreaDamage` (l. 431), `ArsenalProjectile` (l. 175) | `PerfectSpacingFeedback`, `EdgeVulnerabilityRegistry` (Edge Strike), `AttackPassiveAbility` (via `OnAfterAttackHits`, antes do evento) |
| `IsCasting` | `AbilityHolder` (getter, l. 52) | `CheckUse`, `CharControlScript` (locomoção/ataque/move), `WeaponLoadout` |
| `MovementAllowedWhileCasting` | `AbilityHolder` (getter, l. 61) | `CharControlScript.Update/RequestMove/ClickToMove` |
| `ReduceCooldowns(float)` | — (API, l. 275) | boons de redução de recarga (chamada direta; só slots em `Cooldown`) |
| `HookBus.OnBasicHit(Actor,float)` | `HookBus.RaiseBasicHit` (l. 88) ← `HitboxDamage.TryDamageActor` (l. 127) ← `PlayerActor.RaiseBasicAttackHit` (l. 337) | `AsuraSurge`, `AdaptiveCadenceTracker`, `ImpactGuardTracker`, `HungryComboTracker` |

---

_Auditoria concluída sem alterações de código de produção. Base: leitura direta dos arquivos citados
(linhas aproximadas, podem variar com edições futuras). Conforme AGENTS.md, nenhuma GUID/.meta foi
tocada._
