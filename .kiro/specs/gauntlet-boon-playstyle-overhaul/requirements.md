# Requirements Document

## Introduction

Esta feature **reformula os conjuntos de boons das três armas** (Manopla, Arco e Lança) no sistema de recompensas por run do Tech-Guy. O objetivo **não** é inflar o catálogo com boons novos: é **aposentar os boons fracos** — principalmente os "só-número" (aumentos planos de atributo) e os de família que ninguém escolhe por serem "sem graça" — e **colocar boons impactantes no lugar**, mantendo o catálogo **enxuto**. A régua é diversão e comportamento, não valores: cada boon novo deve mudar visivelmente *como* a arma é jogada, e cada aposentadoria deve remover uma escolha que não mudava nada.

> **Nota de escopo e nome:** apesar do título/pasta `gauntlet-boon-playstyle-overhaul`, o escopo desta feature agora abrange **as três famílias de arma** (`Gauntlet`, `Bow`, `Spear`), não apenas a Manopla. O nome da spec e da pasta **permanecem inalterados**; somente o conteúdo reflete as três armas. Cada arma mantém sua identidade: **Arco** = kiting/projétil; **Lança** = espaçamento/alcance/controle; **Manopla** = agressão/combo. A Manopla recebe atenção especial porque seus **ataques básicos são hoje inúteis** (ofuscados pelas skills), então os substitutos da Manopla priorizam valorizar o básico.

O trabalho é ancorado nos sistemas que já existem no código e **reutiliza seus pontos de extensão (seams), sem inventar arquitetura nova**:

- `Assets/_Project/Scripts/Weapons/WeaponRunModifiers.cs` — o enum `WeaponBoon`, as famílias `RunWeaponFamily { Gauntlet, Bow, Spear }`, o `Catalog` de `Definition` (Kind, Family, Title, Description, MaxRank), `Rank(kind)`, o gate único `Add(definition)`, `MeleeScale`, `DirectDamageMultiplier(...)`, o snapshot por-cast `Plan(ArsenalAbility, slot)` (`ArsenalCastPlan`) e o `GauntletSteps(BreakerGauntletAbility, slot)` que clona os `AreaHitStep` por-cast. Todo comportamento de boon é expresso mutando esses snapshots por-cast, **nunca** o asset fonte. Os contratos de imutabilidade, monotonicidade por rank e independência de ordem estão documentados nos comentários do próprio arquivo.
- `Assets/_Project/Scripts/Core/RunBoons.cs` — o pool de ofertas, `OfferReward(room)`, a aplicação em `Choose(index)`, as regras de uso único/repetível e o teardown por-run (`OnDestroy`/`ClearRunState`). Os boons de família entram automaticamente pelo gate `definition.Family == WeaponModifiers.Family && Rank < MaxRank` e `OfferReward` já garante **uma** oferta `weapon_` da família equipada por conjunto de recompensas. O pool inline ainda contém boons só-número (`power` +25% dano, `haste` +25% velocidade de ataque, `recharge` +15 de recarga, `crit`, `brutal` +8 dano fixo, `bulwark` +40 armadura, `swift` +20% movimento) e boons que mudam gameplay a preservar (`vitality`, `ignite`, `frost`, `transform`, `focus`, `conductor`, `detonation`, `reactor`, `resonance`, `overflow`). O padrão de coordenador por-run (`SplitArrowCoordinator`, `MomentumStacks`, `PerfectSpacingFeedback`) é a forma estabelecida de anexar estado reativo, derrubado no fim do run.
- `Assets/_Project/Scripts/Core/HookBus.cs` — o barramento de eventos de combate por-run (`OnHit`, `OnCrit`, `OnKill` deduplicado, `OnFreeze`, `OnBurn`, `OnStanceBreak`, `OnDash`, `OnExplosion`), de propriedade de `RunBoons.Hooks`, isolado de exceções e limpo em `Clear()` no fim do run. É o seam para boons reativos. **Importante:** `OnHit`/`OnKill` **não distinguem** se o acerto veio de um Basic_Attack ou de uma skill — ambos passam identicamente por `PlayerActor.DealResolvedAttackDamage`. Boons que recompensam o **básico** precisam de um seam dedicado de acerto básico (um canal `OnBasicHit`/`OnBasicKill`), disparado **somente** pelo caminho de `HitboxDamage`.
- `Assets/_Project/Scripts/Characters/Player/PlayerActor.cs` — resolve dano direto em `DealResolvedAttackDamage`, aplica `DirectDamageMultiplier` e dispara `Hooks.RaiseHit`/`RaiseCrit`/`RaiseKill`. **O ataque básico da Manopla passa por aqui** (via `hitbox`/`HitboxDamage` e `TryApplyDamage`), e **não** por `BreakerGauntletCombat`.
- `Assets/_Project/Scripts/Abilities/AbilityHolder.cs` — o driver de slots Q/W/E/R, os `cooldownTimers` **vivos** por-run, `RefreshLoadout`, o dash (que dispara `Hooks.RaiseDash`) e o canal de acerto resolvido `NotifyAttackHits`/`AttackHitsResolved` (usado por `PerfectSpacingFeedback`). Uma redução de recarga por-run seria um método novo `ReduceCooldowns`, agindo só sobre os timers vivos, nunca sobre `ability.cooldownTime`.
- `Assets/_Project/Scripts/Abilities/Weapon/BreakerGauntletCombat.cs` — executa as skills da Manopla (Q/W/E/R = Avanço/Punhos/Choque/Asura), consome os `AreaHitStep` de `GauntletSteps`, gerencia a energia Asura (`AsuraMomentum`) e, por decisão de design já aplicada, **zera o `pushDistance`** de todos os golpes (básico e skills) da Manopla. O knockback forte continua disponível **apenas** como um `AreaHitStep.breakEffect = StanceBreakEffect.Knockback` deliberado — o canal natural para o boon de knockback opt-in.
- `Assets/_Project/Scripts/UI/RunModifierPresentation.cs` — fornece metadados de exibição (`For`/`ScopeFor`/`Hint`) e o formato de texto de rank `"Nível X/Y"` usado nos cartões de recompensa.

Uma spec anterior, `impactful-weapon-boons`, já adicionou boons de playstyle por família (Arco: `SplitArrow`, `ChargedShot`; Lança: `PerfectSpacing`, `ImpalingLine`; Manopla: `MomentumStrike`, `Shockwave`), além do pré-existente `ComboNova`. Esta feature **não** redefine esses boons; ela **aposenta os fracos e adiciona novos substitutos impactantes**, mantendo o catálogo enxuto.

Balanceamento numérico não é o objetivo; o objetivo é comportamento, feel e feedback claro na tela. Todos os valores nos critérios de aceitação são **pontos de partida ajustáveis** e podem ser afinados durante a implementação.

## Glossary

- **Run_Modifier_System**: O sistema de recompensas por-run de propriedade de `RunBoons`. Boons duram apenas a incursão atual, empilham por escolha e nunca escrevem em assets originais de arma/habilidade/status.
- **Weapon_Boon**: Um boon específico de família definido por um valor do enum `WeaponBoon` e uma `WeaponRunModifiers.Definition` no `Catalog`, rastreado como um rank por-boon em `[1, MaxRank]`.
- **Numeric_Boon**: Um boon cujo efeito é **apenas** um aumento plano de atributo (dano, velocidade de ataque, redução de recarga, vida, armadura, velocidade de movimento), sem mecânica nova.
- **Impactful_Boon**: Um boon que **muda o que uma habilidade faz** ou **como a arma joga**, em oposição a um aumento plano de atributo.
- **Retired_Boon**: Um boon **removido do pool de ofertas** desta feature — não mais oferecido como recompensa — por ser um Numeric_Boon ou um boon de família fraco/"sem graça". Boons aposentados não precisam ser apagados do código em si, mas o Run_Modifier_System deixa de oferecê-los.
- **Preserved_Boon**: Um boon existente que **permanece** ofertável e cujo comportamento esta feature **não** altera.
- **Cast_Plan**: O snapshot mutável e fresco por-cast (`ArsenalCastPlan`, ou a lista clonada de `AreaHitStep` de `GauntletSteps`) produzido por `WeaponRunModifiers`. Todo comportamento de boon é expresso mutando apenas esse snapshot.
- **Run_Family**: A família de arma equipada (`Bow`, `Spear` ou `Gauntlet`) resolvida por `WeaponRunModifiers.Identify`.
- **Direct_Hit**: Um acerto de ataque básico, área ou projétil resolvido pelo pipeline de dano do jogador (`PlayerActor.DealResolvedAttackDamage`). Descargas, explosões, ricochetes e acertos secundários de cascata não são Direct_Hits.
- **Basic_Attack**: O ataque básico (não-skill) de uma arma, resolvido pelo caminho de hitbox animado (`PlayerActor.TryApplyDamage`/`HitboxDamage`), distinto das habilidades Q/W/E/R.
- **Basic_Hit_Channel**: O canal dedicado de acerto básico (`HookBus.OnBasicHit`/`OnBasicKill`) disparado **somente** pelo caminho de `HitboxDamage`, necessário porque `OnHit`/`OnKill` não distinguem básico de skill.
- **Hook_System**: O barramento de eventos de combate por-run `HookBus`, de propriedade de `RunBoons.Hooks`.
- **Resolved_Hit_Channel**: O canal `AbilityHolder.NotifyAttackHits`/`AttackHitsResolved` de acerto resolvido, usado por assinantes cosméticos por-run sem lookup de cena.
- **Asura_Energy**: A energia da Manopla gerenciada por `AsuraMomentum` em `BreakerGauntletCombat`, que habilita o burst Asura (R) quando cheia.
- **Element_System**: O registro on-hit de queimadura/gelo em `PlayerOnHitEffects`, mais os componentes `BurnStatus`/`ChillStatus` nos inimigos.
- **Cascade_System**: O motor de acertos secundários limitado em `RunSynergyEffects`, restrito a `MaxDepth = 4` e `MaxSecondaryHits = 32`, no máximo um acerto secundário por alvo por acerto original.
- **Stance_Break_Knockback**: O efeito de deslocamento forte deliberado `StanceBreakEffect.Knockback`, único canal de empurrão forte da Manopla depois que o `pushDistance` por-golpe foi zerado.
- **Player**: O `PlayerActor` dono do run.
- **Enemy**: Qualquer `Actor` não-jogador que pode sofrer dano.

## Requirements

### Requirement 1: Diretrizes de design para os boons substitutos (cross-cutting)

**User Story:** Como designer, quero que todo boon novo desta feature mude o estilo de jogo e reutilize os sistemas por-run e isolados de asset já existentes, para que as recompensas sejam divertidas e nunca vazem entre runs nem corrompam assets compartilhados.

*Grounding:* `WeaponRunModifiers.Plan`/`GauntletSteps` documentam os contratos de imutabilidade, monotonicidade e independência de ordem; `WeaponRunModifiers.Add` é o gate único que valida família/catálogo/rank; `RunBoons.OnDestroy`/`ClearRunState` derrubam o estado por-run; `HookBus.Clear` descarta assinantes por run.

#### Acceptance Criteria

1. THE Run_Modifier_System SHALL express every new Weapon_Boon's behavior only within the per-cast Cast_Plan snapshot, the per-run Element_System/Cascade_System registries, the Hook_System subscriptions, or run-scoped stat modifiers tagged with `RunBoons` as their source, and SHALL NOT write any new value into a source `WeaponScript`, `Ability`, or status `ScriptableObject` asset.
2. WHEN a new Weapon_Boon's rank increases from 1 toward its `MaxRank`, THE Run_Modifier_System SHALL make the boon's primary named quantity (added count, multiplier, radius, duration, or stack contribution) non-decreasing, so that a higher rank is never weaker than a lower rank on that quantity.
3. THE Run_Modifier_System SHALL produce byte-identical Cast_Plan output for any two acquisition orderings that result in the same set of boon ranks, so that boon behavior depends only on the final rank multiset and not on pick order.
4. WHEN a run ends, THE Run_Modifier_System SHALL clear every new Weapon_Boon's run-scoped state (ranks, Hook_System subscriptions, Element_System/Cascade_System contributions, coordinator components, and stat modifiers) so that no effect from this feature persists into a later run.
5. THE Run_Modifier_System SHALL register each new Weapon_Boon in the `WeaponRunModifiers.Catalog` for exactly one Run_Family, so that it is only ever offered WHILE that Run_Family is equipped.
6. THE Run_Modifier_System SHALL route every new Weapon_Boon's acquisition through the single `WeaponRunModifiers.Add` gate, so that family, catalog, and rank-range validation remain the single point of enforcement.
7. THE Run_Modifier_System SHALL make every new Weapon_Boon in this feature qualify as an Impactful_Boon per the glossary, and SHALL NOT introduce any new Numeric_Boon to satisfy this feature.
8. THE Run_Modifier_System SHALL NOT redefine or duplicate the behavior of any existing gameplay Weapon_Boon it preserves (including `MomentumStrike`, `Shockwave`, `ComboNova`, `SplitArrow`, `ChargedShot`, `PerfectSpacing`, and `ImpalingLine`), so that this feature replaces weak boons rather than restating existing ones.

### Requirement 2: Aposentadoria de boons fracos (manter o catálogo enxuto)

**User Story:** Como jogador, quero deixar de receber ofertas de boons que são só aumento de número ou que não mudam nada no jogo, para que minhas escolhas de recompensa sejam sempre significativas e o catálogo fique enxuto.

*Grounding:* O pool inline de `RunBoons.OfferReward` contém os genéricos só-número (`power`, `haste`, `recharge`, `crit`, `brutal`, `bulwark`, `swift`); o `WeaponRunModifiers.Catalog` contém os boons de família fracos a aposentar — Manopla: `LongFists`, `StanceCrusher`, `Berserker`; Arco: `HeavyBolt`, `Sniper`, `LongRain`; Lança: `LongReach`, `TripleMoon`, `Affliction`. `OfferReward` compõe cada conjunto a partir do pool inline mais uma oferta `weapon_` de família via o gate `definition.Family == WeaponModifiers.Family && Rank < MaxRank`.

#### Acceptance Criteria

1. THE Run_Modifier_System SHALL treat the following inline Numeric_Boons as Retired_Boons and SHALL NOT include them in any reward set: `power`, `haste`, `recharge`, `crit`, `brutal`, `bulwark`, and `swift`.
2. THE Run_Modifier_System SHALL treat the following family Weapon_Boons as Retired_Boons and SHALL NOT offer them in any reward set: `LongFists`, `StanceCrusher`, and `Berserker` (Gauntlet); `HeavyBolt`, `Sniper`, and `LongRain` (Bow); `LongReach`, `TripleMoon`, and `Affliction` (Spear).
3. WHEN composing a reward set, IF a Retired_Boon would otherwise have been drawn into an offer slot, THEN THE Run_Modifier_System SHALL fill that slot from the remaining non-retired offers without raising an error and without reducing the number of choices presented.
4. THE Run_Modifier_System SHALL keep every Preserved_Boon offerable, including the inline gameplay boons (`vitality`, `ignite`, `frost`, `transform`, `focus`, `conductor`, `detonation`, `reactor`, `resonance`, `overflow`) and the preserved family boons for each weapon (Bow: `TwinShot`, `Piercing`, `Ricochet`, `Homing`, `WideVolley`, `GuidedRain`, `SplitArrow`, `ChargedShot`, `RapidBurst`; Spear: `Trident`, `PhantomSpear`, `MoonShard`, `ReturnWave`, `ChainThrust`, `ImpalingLine`, `Orbit`, `EchoThrust`, `DragonWave`, `SpearTip`, `Execution`, `PerfectSpacing`, `Siphon`; Gauntlet: `ComboNova`, `ShockRing`, `MomentumStrike`, `Shockwave`, `Momentum`, `AsuraReserve`, `RocketAdvance`, `FlurryEcho`, `AsuraEcho`).
5. WHILE a run or saved loadout references a Retired_Boon acquired before this feature, THE Run_Modifier_System SHALL continue that run without error, applying no further offers of that Retired_Boon.
6. THE Run_Modifier_System SHALL retire a boon only by removing it from the offer composition, and SHALL NOT alter the behavior, rank data, or asset of any Preserved_Boon as a side effect of retirement.

### Requirement 3: Seam de acerto básico (habilita os substitutos focados no básico)

**User Story:** Como designer, quero um canal de evento que distinga acertos de ataque básico de acertos de skill, para que os boons que valorizam o básico da Manopla reajam só ao básico, já que hoje o Hook_System não faz essa distinção.

*Grounding:* `HookBus.OnHit`/`OnKill` disparam em todo Direct_Hit por `PlayerActor.DealResolvedAttackDamage` sem conhecer a origem; o caminho do básico é `HitboxDamage.TryDamageActor` → `PlayerActor.DealResolvedAttackDamage`, enquanto o caminho das skills é `TryApplyAreaDamage`/`BreakerGauntletCombat` → `DealResolvedAttackDamage`. `HookBus.RaiseKill` já deduplica abates por inimigo com `_killed`.

#### Acceptance Criteria

1. WHEN a Basic_Attack lands a Direct_Hit resolved through the `HitboxDamage` path, THE Run_Modifier_System SHALL raise a Basic_Hit_Channel hit event carrying the struck Enemy and the damage dealt.
2. WHEN a Basic_Attack Direct_Hit kills an Enemy, THE Run_Modifier_System SHALL raise a Basic_Hit_Channel kill event for that Enemy at most once per Enemy death.
3. IF a Direct_Hit originates from a skill rather than a Basic_Attack, THEN THE Run_Modifier_System SHALL NOT raise any Basic_Hit_Channel event for that hit.
4. WHEN a Basic_Attack Direct_Hit is resolved, THE Run_Modifier_System SHALL continue to raise the existing generic `OnHit`/`OnKill` events, so that Preserved_Boons that react to any Direct_Hit remain unaffected.
5. WHEN a run ends, THE Run_Modifier_System SHALL clear the Basic_Hit_Channel subscriptions and its per-run kill-dedup state through `HookBus.Clear`.

### Requirement 4: Manopla — Punho de Asura (básicos geram energia Asura)

**User Story:** Como jogador de Manopla, quero que meus ataques básicos gerem energia Asura, para que o básico vire a forma de carregar meu finalizador e deixe de ser inútil.

*Grounding:* `AsuraMomentum` em `BreakerGauntletCombat` acumula Asura_Energy via skills e pelos boons `Momentum`/`AsuraReserve`, clampando a `[0, Maximum]`; hoje o Basic_Attack não contribui nada. Este boon substitui parte do espaço aberto pela aposentadoria dos fracos da Manopla.

#### Acceptance Criteria

1. WHILE this Gauntlet boon is active at rank R, WHEN a Basic_Attack lands a Direct_Hit on an Enemy, THE Run_Modifier_System SHALL add Asura_Energy equal to (2 × R) to the player's Asura_Energy meter through the Basic_Hit_Channel.
2. WHEN adding Asura_Energy from a Basic_Attack, THE Run_Modifier_System SHALL clamp the resulting Asura_Energy to the meter's existing full threshold, so that the basic path never exceeds the authored maximum.
3. IF a Direct_Hit originates from a Gauntlet skill rather than a Basic_Attack, THEN THE Run_Modifier_System SHALL NOT grant this boon's Asura_Energy for that hit.
4. WHEN a run ends, THE Run_Modifier_System SHALL remove this boon's Basic_Hit_Channel subscription so that Basic_Attack Asura generation does not persist into a later run.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Gauntlet`, with a `MaxRank` of 3.

### Requirement 5: Manopla — Guarda partida (3 básicos quebram postura + knockback)

**User Story:** Como jogador de Manopla, quero que uma sequência de três ataques básicos acumule pressão e quebre a guarda do inimigo com um arremesso, para que o básico vire uma ferramenta de controle de postura e não só um tapinha de dano.

*Grounding:* O Basic_Hit_Channel conta básicos consecutivos; `PlayerActor.ApplyHitReactionTo` aplica `stanceDamage` e reações fora do pipeline de área (caminho do básico); `Stance_Break_Knockback` é o canal de deslocamento forte disponível, resolvido por `CombatReactionController.TriggerStanceBreak`.

#### Acceptance Criteria

1. WHILE this Gauntlet boon is active at rank R, WHEN the Player lands a run of 3 consecutive Basic_Attack Direct_Hits on enemies, THE Run_Modifier_System SHALL apply bonus stance damage scaled by (1 + 0.5 × R) to the third hit of that run.
2. WHEN the third consecutive Basic_Attack of a run breaks an Enemy's stance, THE Run_Modifier_System SHALL request a Stance_Break_Knockback on that Enemy through the deliberate stance-break displacement channel.
3. IF a non-Basic_Attack action or a frame without a Basic_Attack Direct_Hit interrupts the run before the third hit, THEN THE Run_Modifier_System SHALL reset the consecutive-hit counter to zero.
4. WHILE a Basic_Attack run is in progress, THE Run_Modifier_System SHALL present on-screen feedback that communicates the building pressure and clears when the counter resets.
5. THE Run_Modifier_System SHALL drive this behavior through the Basic_Hit_Channel, the Resolved_Hit_Channel, and run-scoped reaction requests only, and SHALL NOT write stance values back to the source weapon or ability assets.
6. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Gauntlet`, with a `MaxRank` of 3.

### Requirement 6: Manopla — Combo faminto (básicos reduzem a recarga das skills)

**User Story:** Como jogador de Manopla, quero que acertar com o básico acelere a volta das minhas habilidades, para que eu seja recompensado por tecer básicos entre as skills em vez de só esperar o cooldown.

*Grounding:* `AbilityHolder` possui os `cooldownTimers` **vivos** dos slots Q/W/E/R; a redução por-run age sobre esses timers via um método novo `ReduceCooldowns`, nunca sobre `ability.cooldownTime` do asset. O Basic_Hit_Channel dispara a cada acerto básico.

#### Acceptance Criteria

1. WHILE this Gauntlet boon is active at rank R, WHEN a Basic_Attack lands a Direct_Hit on an Enemy, THE Run_Modifier_System SHALL reduce the remaining cooldown of the player's Gauntlet abilities by (0.3 × R) seconds.
2. WHEN reducing ability cooldowns from a Basic_Attack hit, THE Run_Modifier_System SHALL keep every remaining cooldown at or above zero and SHALL NOT shorten an ability's authored base `cooldownTime` on its source asset.
3. IF an ability is not currently on cooldown, THEN THE Run_Modifier_System SHALL apply no reduction to that ability for that hit.
4. WHEN a run ends, THE Run_Modifier_System SHALL remove this boon's Basic_Hit_Channel subscription so that cooldown acceleration does not persist into a later run.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Gauntlet`, with a `MaxRank` of 3.

### Requirement 7: Manopla — Punho sísmico (knockback deliberado opt-in, básico + skills)

**User Story:** Como jogador de Manopla, quero poder escolher reinstaurar um empurrão forte nos meus golpes de forma deliberada, para que eu troque o controle "em pé" por um estilo de arremesso quando quiser dispersar a horda.

*Grounding:* `BreakerGauntletCombat` zera o `pushDistance` por-golpe por padrão (básico e skills); `Stance_Break_Knockback` (`StanceBreakEffect.Knockback`) é o único canal de deslocamento forte remanescente; `GauntletSteps`/`AreaHitStep` carregam o `breakEffect` e a distância de knockback, e o hitbox do básico também pode declarar `breakEffect = Knockback` na quebra.

#### Acceptance Criteria

1. WHILE this Gauntlet boon is active at rank R, WHEN a Gauntlet hit from either a Basic_Attack or a skill breaks an Enemy's stance, THE Run_Modifier_System SHALL apply a Stance_Break_Knockback whose distance scales by (1 + 0.3 × R).
2. WHEN applying the Stance_Break_Knockback, THE Run_Modifier_System SHALL move the Enemy only through its existing locomotion/displacement channel and SHALL NOT teleport an Enemy through solid scenery.
3. WHILE this boon is inactive, THE Run_Modifier_System SHALL leave the Gauntlet's default no-push behavior unchanged for both Basic_Attack and skills, so that enabling the knockback is an explicit opt-in reward.
4. THE Run_Modifier_System SHALL express this behavior through the deliberate stance-break displacement path and the per-cast Cast_Plan snapshot (plus the Basic_Attack hitbox break effect) only, and SHALL NOT write push values back to the source weapon or ability assets.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Gauntlet`, with a `MaxRank` of 3.

### Requirement 8: Arco — Disparo em recuo (kiting com reposicionamento)

**User Story:** Como jogador de Arco, quero que disparar enquanto me afasto me dê um impulso curto de reposicionamento, para que o kiting vire uma decisão ativa de ritmo de disparo e mobilidade em vez de só mais dano por distância.

*Grounding:* Os ataques básicos do Arco fluem por `PlayerActor`/caminho de projétil; o `AbilityHolder` possui o dash e dispara `Hooks.RaiseDash`; o `NavMeshAgent` move o jogador respeitando limites navegáveis. Este boon substitui o aposentado `Sniper` (+dano por distância) por uma mudança de ritmo de kiting, preservando a identidade de projétil/kiting do Arco.

#### Acceptance Criteria

1. WHILE this Bow boon is active, WHEN the Player fires a Basic_Attack while moving away from the nearest Enemy, THE Run_Modifier_System SHALL grant the Player a short backward reposition impulse along the current movement direction.
2. WHEN granting the reposition impulse at rank R, THE Run_Modifier_System SHALL scale the impulse distance by (1 + 0.25 × R) and SHALL enforce a per-impulse cooldown so that the reposition cannot be chained every frame.
3. WHILE repositioning the Player, THE Run_Modifier_System SHALL route displacement through the existing `NavMeshAgent` navigation so that the Player never moves through solid scenery.
4. IF the Player is moving toward the nearest Enemy or standing still when firing, THEN THE Run_Modifier_System SHALL NOT grant the reposition impulse for that shot.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Bow`, with a `MaxRank` of 3.

### Requirement 9: Arco — Cadência adaptativa (ritmo de disparo muda com a distância)

**User Story:** Como jogador de Arco, quero que meu ritmo de disparo mude conforme eu mantenho distância ideal de kiting, para que escolher a distância certa vire uma expressão de habilidade em vez de um atributo passivo.

*Grounding:* O caminho de projétil carrega um valor de dano por disparo e a cadência do básico é governada por `AttackSpeedMultiplier`; `DirectDamageMultiplier(distance, ...)` já lê a distância ao jogador. Este boon substitui o aposentado `HeavyBolt` (+dano/windup do W) por uma mecânica de ritmo baseada em distância, mantendo a identidade de kiting.

#### Acceptance Criteria

1. WHILE this Bow boon is active at rank R, WHEN the Player lands consecutive Basic_Attack Direct_Hits on enemies within the kiting band of 6m or more, THE Run_Modifier_System SHALL increase the Player's Basic_Attack firing cadence by a factor scaled by (1 + 0.15 × R), applied through the run-scoped stat pipeline.
2. WHEN a Basic_Attack Direct_Hit lands on an Enemy closer than 6m, THE Run_Modifier_System SHALL reset the adaptive cadence bonus to zero.
3. WHILE the adaptive cadence bonus is active, THE Run_Modifier_System SHALL present on-screen feedback that reflects the current cadence state and clears when the bonus resets.
4. WHEN a run ends, THE Run_Modifier_System SHALL remove the adaptive cadence stat modifier so that no cadence bonus persists into a later run.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Bow`, with a `MaxRank` of 3.

### Requirement 10: Arco — Chuva marcadora (o ultimate controla a área em vez de só pulsar mais)

**User Story:** Como jogador de Arco, quero que meu ultimate marque e prenda os inimigos sob a chuva de flechas, para que o R vire uma ferramenta de controle de área e preparação de dano em vez de só mais pulsos.

*Grounding:* O R do Arco é a chuva de flechas pulsante; `GuidedRain` já faz a nuvem acompanhar o cursor. Este boon substitui o aposentado `LongRain` (+pulsos/raio do R) por uma mecânica de marcação/controle no `Cast_Plan` por-cast, preservando a identidade de projétil/área à distância do Arco.

#### Acceptance Criteria

1. WHILE this Bow boon is active, WHEN the Bow rain ultimate (slot R) resolves a pulse on an Enemy, THE Run_Modifier_System SHALL apply a mark status to that Enemy for a limited duration.
2. WHEN a marked Enemy takes a subsequent Direct_Hit at rank R, THE Run_Modifier_System SHALL amplify that hit's damage by (1 + 0.2 × R) for as long as the mark remains active.
3. WHILE an Enemy is marked, THE Run_Modifier_System SHALL slow that Enemy's movement so that the rain becomes a zoning tool, and SHALL restore normal movement when the mark expires.
4. THE Run_Modifier_System SHALL apply this behavior only to the per-cast rain Cast_Plan snapshot and run-scoped mark state, and SHALL NOT modify the source rain ability asset.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Bow`, with a `MaxRank` of 3.

### Requirement 11: Lança — Recuo controlado (criar espaçamento ideal vira decisão ativa)

**User Story:** Como jogador de Lança, quero que uma estocada bem posicionada me recue até a distância ideal, para que criar o espaçamento perfeito vire uma decisão ativa em vez de um bônus passivo de alcance.

*Grounding:* `WeaponRunModifiers.DirectDamageMultiplier(distance, ...)` já escala por distância (banda de `PerfectSpacing`); `ArsenalCastPlan` carrega os campos de estocada; o `NavMeshAgent` move o jogador respeitando limites navegáveis. Este boon substitui o aposentado `LongReach` (+% alcance plano) por uma mecânica de reposicionamento que muda a decisão, preservando a identidade de espaçamento/alcance da Lança.

#### Acceptance Criteria

1. WHILE this Spear boon is active, WHEN the spear thrust connects with at least one Enemy, THE Run_Modifier_System SHALL step the Player back to settle within the ideal spacing band after the thrust resolves.
2. WHEN repositioning the Player at rank R, THE Run_Modifier_System SHALL scale the step-back distance by (1 + 0.2 × R) while keeping the resulting distance inside the ideal spacing band.
3. WHILE repositioning the Player, THE Run_Modifier_System SHALL route displacement through the existing `NavMeshAgent` navigation so that the Player never moves through solid scenery.
4. IF the thrust connects with no Enemy, THEN THE Run_Modifier_System SHALL apply no step-back for that cast.
5. THE Run_Modifier_System SHALL apply this behavior only to the per-cast thrust Cast_Plan snapshot and run-scoped state, and SHALL NOT modify the source thrust ability asset.
6. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Spear`, with a `MaxRank` of 3.

### Requirement 12: Lança — Muralha de hastes (zona de controle de alcance)

**User Story:** Como jogador de Lança, quero que meu W crie uma zona de hastes que empurra inimigos para fora do meu alcance de perigo, para que controlar o espaço à minha frente vire uma mecânica ativa em vez de só mais varreduras.

*Grounding:* O W da Lança é a varredura (orbita/varre a frente); `AreaHitStep.pushDistance` e o canal de deslocamento usado por `StanceCrusher` demonstram o precedente de empurrão. Este boon substitui o aposentado `TripleMoon` (+varreduras do W) por uma zona de controle no `Cast_Plan` por-cast, preservando a identidade de espaçamento/controle da Lança.

#### Acceptance Criteria

1. WHILE this Spear boon is active, WHEN the spear sweep ability (slot W) is cast, THE Run_Modifier_System SHALL create a control zone in front of the Player for the duration of the sweep.
2. WHEN an Enemy enters or is caught in the control zone at rank R, THE Run_Modifier_System SHALL push that Enemy outward by a displacement scaled by (1 + 0.3 × R).
3. WHILE pushing enemies out of the control zone, THE Run_Modifier_System SHALL move enemies only through their existing locomotion/displacement channel and SHALL NOT teleport an Enemy through solid scenery.
4. THE Run_Modifier_System SHALL apply this behavior only to the per-cast sweep Cast_Plan snapshot and SHALL NOT modify the source sweep ability asset.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Spear`, with a `MaxRank` of 3.

### Requirement 13: Lança — Ponto cego (recompensa por acertar na borda do alcance)

**User Story:** Como jogador de Lança, quero que acertar inimigos exatamente na ponta da minha arma crie uma abertura, para que mirar na borda do meu alcance vire uma decisão de risco/recompensa em vez de um bônus de dano por elemento.

*Grounding:* `DirectDamageMultiplier(distance, ...)` já conhece a distância ao alvo; `PlayerActor.ApplyHitReactionTo` aplica reações de postura/stagger. Este boon substitui o aposentado `Affliction` (+dano por elemento presente) por uma mecânica de controle baseada em alcance, preservando a identidade de espaçamento/alcance da Lança.

#### Acceptance Criteria

1. WHILE this Spear boon is active at rank R, WHEN a Direct_Hit lands on an Enemy at the outer edge of the spear's reach (within the farthest 20% of the thrust range), THE Run_Modifier_System SHALL apply bonus stance damage scaled by (1 + 0.4 × R) to that hit.
2. WHEN an edge-of-reach Direct_Hit breaks an Enemy's stance, THE Run_Modifier_System SHALL open a brief vulnerability window on that Enemy during which it takes amplified Direct_Hit damage.
3. IF a Direct_Hit lands on an Enemy closer than the outer edge of reach, THEN THE Run_Modifier_System SHALL apply no edge-of-reach bonus or vulnerability for that hit.
4. THE Run_Modifier_System SHALL derive this behavior within the direct-hit damage path and run-scoped reaction/vulnerability state only, and SHALL NOT write values back to the source spear ability or weapon assets.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Spear`, with a `MaxRank` of 3.

### Requirement 14: Oferta e variedade — priorizar boons impactantes em cada arma

**User Story:** Como jogador escolhendo recompensas, quero ver boons que mudam meu estilo de jogo com frequência, para que minhas escolhas não sejam dominadas por aumentos planos de número agora que os fracos foram aposentados.

*Grounding:* `RunBoons.OfferReward` monta cada conjunto de ofertas e já inclui exatamente uma oferta `weapon_` da família equipada por conjunto, além de preencher os demais slots a partir do pool inline. A quantidade de escolhas por conjunto não muda. Após a aposentadoria (Requirement 2), o pool restante é composto por Preserved_Boons e pelos novos Impactful_Boons.

#### Acceptance Criteria

1. WHILE a Run_Family is equipped and at least one of that family's Impactful_Boons is below its `MaxRank`, WHEN the Run_Modifier_System builds a reward set, THE Run_Modifier_System SHALL include at least one Impactful_Boon offer for the equipped Run_Family in that set.
2. WHEN selecting the family Weapon_Boon offer for a reward set, THE Run_Modifier_System SHALL draw it from the equipped family's non-retired entries in the `WeaponRunModifiers.Catalog` through the existing family/rank offer gate.
3. THE Run_Modifier_System SHALL keep the number of choices presented per reward set unchanged as a result of this feature.
4. THE Run_Modifier_System SHALL keep every Preserved_Boon available in the pool, so that this feature changes the pool composition only by retiring weak boons and adding impactful substitutes.
5. IF every non-retired Weapon_Boon for the equipped Run_Family has reached its `MaxRank`, THEN THE Run_Modifier_System SHALL fall back to the remaining non-retired offer composition without error.

### Requirement 15: Apresentação e integração da oferta

**User Story:** Como jogador escolhendo recompensas, quero que esses boons impactantes apareçam nos cartões de recompensa com descrições claras e evocativas e dicas de combinação, para que eu entenda como cada um muda meu estilo antes de escolher.

*Grounding:* `RunBoons.OfferReward` monta ofertas a partir do `Catalog` via o gate família/rank e formata o texto de rank como `"Nível X/Y\n<descrição>"`; `RunModifierPresentation` fornece metadados de exibição (`For`/`ScopeFor`/`Hint`) e dicas de combinação.

#### Acceptance Criteria

1. WHEN the Run_Modifier_System offers a reward set WHILE a Run_Family is equipped, THE Run_Modifier_System SHALL make the new boons for that family eligible to appear through the existing family/rank offer gate.
2. WHERE a new Weapon_Boon has a current rank below its `MaxRank`, THE Run_Modifier_System SHALL present its reward card with a Portuguese title and description, a `ScopeFor` family scope, and a rank indicator in the existing `"Nível X/Y"` format.
3. WHEN a new boon has a meaningful synergy with an already-owned or co-offered boon, THE Run_Modifier_System SHALL surface a combination `Hint` consistent with the existing hint presentation.
4. THE Run_Modifier_System SHALL present each new boon's description in terms of the playstyle it changes (basic-attack role, resource flow, mobility, spacing, or control), so that its impact is legible before it is picked.
