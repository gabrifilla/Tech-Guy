# Design Document

## Visão geral

A feature melhora o **feel das armas base** (foco manopla) atacando cinco alvos concretos, todos
sobre sistemas existentes, mais um asset central de tuning:

1. **Hitbox do projétil do arco** (`ArsenalProjectile`): raio de sweep de `0.12` → alvo `~0.35`,
   configurável.
2. **Hitbox do golpe básico da manopla**: reconciliar a cápsula fina (`0.19`) do
   `GauntletHitbox.prefab` com o `attackBoxSize` do `Gauntlet.asset` numa **definição única** e mais
   generosa.
3. **Skills base da manopla** (Avanço/Punhos/Choque/Asura): reduzir custo de mana e tempo parado
   das mais pesadas, elevar dano/utilidade das fracas, preservando identidade e pipeline.
4. **Stats iniciais do player** (`PlayerArpgStats`): `baseDamage`, `armor`, vida base ajustáveis.
5. **Indicador de proteção**: ícone de escudo acima da cabeça + aura no chão (via `CombatGroundRing`),
   dirigido pelo `Shield` e, opcionalmente, pelo `FrontalReflector`.

Camada transversal: **`CombatBalanceConfig`** (ScriptableObject) reúne os knobs, atuando como
override/multiplicador global sobre os assets por-arma/por-skill, com **fallback para os padrões
atuais**. Princípio geral: sem config, ou com campos vazios, o combate roda como hoje (exceto os
padrões de código deliberadamente melhorados de hitbox — R1.4).

## Arquitetura

```
CombatBalanceConfig (ScriptableObject, em Assets/_Project/Resources)
   └─ CombatBalance.Current  (acessor estático, cache + reset em RuntimeInitializeOnLoadMethod)
        ├─► ArsenalProjectile        → projectileSweepRadius
        ├─► GauntletHitbox/Gauntlet  → basicHitbox (definição unificada)
        ├─► BreakerGauntletCombat    → skillManaCostMultiplier / overrides por skill
        ├─► PlayerArpgStats/PlayerActor → startingBaseDamage, startingArmor, startingHealth
        └─► ProtectionIndicator      → protectionIconEnabled, protectionAuraColor

Actor.TakeDamage
   └─ (novo) evento DamageAbsorbed(this) quando IDamageAbsorber consome e dano-à-vida == 0
        └─► ProtectionIndicator: pisca ícone/aura

Shield (IDamageAbsorber)  ──observado por──►  ProtectionIndicator (ícone + CombatGroundRing)
FrontalReflector (Mirror) ──(opcional)─────►  ProtectionIndicator (só ícone; mantém seu arco)
```

### Resolução do config (lacuna fechada)

- **Local do asset:** `Assets/_Project/Resources/CombatBalanceConfig.asset` (pasta `Resources` sob
  `_Project`, conforme o AGENTS.md trata `Assets/_Project/Resources` como área do jogo). O nome do
  asset é fixo para permitir `Resources.Load`.
- **Acessor:** `CombatBalance.Current` faz `Resources.Load<CombatBalanceConfig>("CombatBalanceConfig")`
  uma vez e cacheia em `static`. Se retornar `null`, `Current` devolve `null` e cada consumidor usa
  seu padrão local (R5.3).
- **Reset de cache (lacuna fechada):** um método `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`
  zera o cache no início de cada play, para o cache estático não vazar entre plays no Editor. No
  Editor, o mesmo reset roda em `InitializeOnLoadMethod`/recompilação.
- **Multiplicador, não substituição:** onde há valor por-arma/por-skill, o config multiplica/override
  no ponto de uso, sem reescrever os assets (R5.5).
- **Clamps na leitura:** cada getter aplica clamp (ver `CombatBalanceBounds`, classe pura testável):
  raio de sweep em `[0.05, 1.0]`, dimensões de hitbox `> 0`, multiplicador de custo de mana em
  `[0.1, 2]`, vida base `> 0`, `armor ≥ 0`.

## Componentes e mudanças

### 1. `CombatBalanceConfig` + `CombatBalanceBounds` — `Assets/_Project/Scripts/Core/Balance/`

Campos (padrão neutro salvo onde indicado; R indicado):

| Campo | Tipo | Padrão | Efeito |
|---|---|---|---|
| `projectileSweepRadius` | float | 0.35 | Raio do `SphereCastAll` do `ArsenalProjectile` (R1). Padrão já melhora o feel. |
| `gauntletBasicBoxSize` | Vector3 | (1.6, 1.9, 1.9) | Caixa frontal unificada do básico da manopla (R2). |
| `gauntletBasicReach` | float | 2.2 | Alcance efetivo do básico, alinhado a `attackDistance` (R2.4). |
| `skillManaCostMultiplier` | float | 0.7 | Multiplica o custo de mana das skills da manopla (R3.1). |
| `startingBaseDamage` | float | 5 | Override de `PlayerArpgStats.baseDamage` (R4). 0/omitido = padrão. |
| `startingArmor` | float | 0 | Override de `armor` (R4). |
| `startingHealth` | float | 0 (=usa autorado) | Override da vida base; 0 mantém o valor do prefab (R4.3). |
| `protectionIconEnabled` | bool | true | Liga o ícone de escudo (R6.1). |
| `protectionAuraColor` | Color | ciano-claro | Cor da aura/ícone (R6.7). |

> Nota sobre padrões não-neutros: `projectileSweepRadius` (0.35) e `skillManaCostMultiplier` (0.7)
> já embutem a correção pretendida — são os valores de partida da calibragem (task de tuning). Todos
> os demais são neutros. O R5.3 garante que a ausência do asset cai em padrões de código.

`CombatBalanceBounds` (estática, sem `MonoBehaviour`) concentra os clamps para property tests (R7.3).

### 2. Hitbox do projétil do arco — `ArsenalProjectile`

- Substituir os literais `0.12f` nos `SphereCastAll` (no `Update` e no `HasInlineTarget`) por
  `CombatBalance.Current?.ProjectileSweepRadius ?? DefaultSweepRadius`, com `DefaultSweepRadius = 0.35f`.
- O mesmo raio é usado no sweep de dano e na checagem de pierce in-line, para consistência (R1.3).
- Nada muda no anti-tunneling (continua `SphereCastAll` por frame, ordenado por distância) nem no
  bloqueio por cenário sólido (R1.2/R1.5).

### 3. Hitbox do golpe básico da manopla — `GauntletHitbox.prefab` / `Gauntlet.asset` / `HitboxDamage`

**Problema:** duas definições divergentes — a `CapsuleCollider` de raio `0.19` no prefab (o que
realmente balança via `HitboxDamage.OnTrigger*`) e o `attackBoxSize 1.25×1.8×1.6` do asset (usado por
outro caminho). O básico sofre porque a cápsula fina é a ativa.

**Decisão:** unificar em **uma** definição. Duas opções avaliadas:
- **(A)** Engrossar o `CapsuleCollider` do prefab para um volume frontal maior (raio/altura), mantendo
  o `HitboxDamage` como está.
- **(B)** Trocar o básico para resolver por `PlayerActor.TryApplyAreaDamage` com a caixa unificada
  (`gauntletBasicBoxSize`), aposentando a cápsula como fonte de dano.

**Escolhida (A) como base, com a caixa unificada vinda do config:** ajustar o collider do prefab
para casar com `gauntletBasicBoxSize`/`gauntletBasicReach`, preservando o `HitboxDamage` e seu dedupe
`hitActors` por ativação (R2.3). O `attackBoxSize` do asset é reconciliado para o mesmo volume, para
não haver duas fontes. (B) fica registrada como alternativa caso a cápsula não cubra bem o arco
lateral. Sem config, usa-se o volume autorado reconciliado (R2.5).

> Preservar `.meta` do prefab ao editá-lo (AGENTS.md). A edição do collider é feita no `.prefab` já
> existente, não recriando o asset.

### 4. Skills base da manopla — assets de skill + `BreakerGauntletCombat`

Números atuais (arma base 15 de dano; mult = soma dos `damageMultiplier` dos `AreaHitStep`s):

| Skill | Mana | CD | `activeTime` | Dano aprox. | Diagnóstico |
|---|---|---|---|---|---|
| Avanço (Q) | 80 | 3s | 0.6s | ~1.4× | caro para o retorno |
| Punhos (W) | 150 | 5s | 1.2s | ~3.4× | mana alta, dano difuso |
| Choque (E) | 140 | 6s | 0.8s | ~2.7× | aceitável |
| Asura (R) | 280 | 16s | 2.9s | ~10.5× | 2.9s parado tomando dano |

**Abordagem (dados, não código de lógica):**
- **Custo:** aplicar `skillManaCostMultiplier` (padrão 0.7) globalmente, e/ou ajustar os `_manaCost`
  autorados das mais caras (Punhos, Asura) para o jogador encadear ≥2 skills com a mana inicial (R3.1).
- **Tempo parado:** reduzir o `activeTime` do Asura e/ou aumentar `_animationSpeed`, comprimindo a
  sequência de `AreaHitStep`s, mantendo o número de hits (R3.2).
- **Dano/utilidade das fracas:** elevar `damageMultiplier` dos steps de Avanço/Punhos para o dano por
  mana ficar competitivo, sem ultrapassar o arco (R3.3/R3.6).
- **Preservação:** o pipeline (`BreakerGauntletCombat` executando os `AreaHitStep`s, reações/stance)
  não muda; só os dados dos assets e o multiplicador global (R3.4).

Os números finais são fixados na **task de calibragem** (não no design), para permitir iteração em
playtest. O config expõe o multiplicador global; os `_manaCost`/`activeTime`/`damageMultiplier` por
skill continuam nos assets.

### 5. Stats iniciais do player — `PlayerArpgStats` / `PlayerActor`

- No ponto onde o player inicializa stats/vida, aplicar os overrides do config quando presentes:
  `baseDamage`, `armor`, e vida base (via `SetMaxHealth`/`baseMaxHealth`), respeitando `[Min]` e
  clamps (R4.4). Sem override (campo 0/omitido), usa o autorado (R4.3).
- A fórmula de mitigação (`amount*100/(100+armor)`) e o roll de dano não mudam de forma (R4.2).

### 6. Indicador de proteção — `ProtectionIndicator` (novo) — `Assets/_Project/Scripts/Effects/`

**Elementos visuais:**
- **Aura no chão:** `CombatGroundRing.Create(transform, "Protection aura", protectionAuraColor)` —
  mesmo padrão do `FrontalReflector` (R6.6), anel completo sob o inimigo.
- **Ícone de escudo:** um `SpriteRenderer` num filho world-space em
  `transform.position + Vector3.up * altura`, com billboard para `Camera.main`. A altura vem dos
  `Renderer`/`Collider` bounds do inimigo, acima da barra de vida (R6.1/R6.7). Respeita
  `protectionIconEnabled`.

**Sprite do ícone (lacuna fechada):** não existe sprite de escudo no projeto. Será gerado
proceduralmente por uma ferramenta de Editor (como outros visuais do projeto — ex.
`manage_texture`/builder) e salvo em `Assets/_Project/Art/UI/ProtectionShieldIcon.png` (com `.meta`),
importado como `Sprite`. O `ProtectionIndicator` carrega-o via referência serializada ou
`Resources`/`AssetDatabase` no Editor. Alternativa de fallback: desenhar o escudo com o mesmo
`CombatGroundRing`/LineRenderer erguido, caso não se queira um asset de imagem.

**Fonte de estado (liga/desliga):**
- **Shield:** o indicador aparece enquanto houver `Shield` com `RemainingCapacity > 0`. Como `Shield`
  se autodestrói ao esgotar/expirar, o indicador detecta a ausência do componente e some (R6.2). Para
  robustez, o `Shield` ganha um evento leve (`Depleted`) ou o indicador consulta `GetComponent<Shield>()`
  por frame (barato: um `GetComponent` por inimigo protegido).
- **Feedback de absorção (lacuna fechada):** em `Actor.TakeDamage`, hoje temos `previousHealth` e
  `actualDamage = previousHealth - health`. Quando existe um `IDamageAbsorber` e `actualDamage == 0`
  com `amount > 0` **na entrada do absorber**, o `Actor` levanta um novo evento
  `event Action<Actor> DamageAbsorbed`. A distinção "houve absorção" é feita comparando o `amount`
  antes e depois de `absorber.Absorb(...)` (se o absorber consumiu tudo e nada chegou à vida). O
  `ProtectionIndicator` assina `DamageAbsorbed` no `OnEnable` e desassina no `OnDisable`, e ao receber
  o evento pisca o ícone/aura (R6.3). Isso **não** dispara para reduções por `_damageTakenModifiers`
  (essas alteram `amount` antes do absorber, mas o gatilho do evento é especificamente "absorber
  consumiu e vida não caiu"), atendendo R6.8.
- **Mirror:** o `FrontalReflector` mantém seu arco; opcionalmente o mesmo `ProtectionIndicator` mostra
  só o ícone de escudo enquanto `ShieldActive` (R6.4), sem duplicar a aura.
- **Limpeza:** em `OnDisable`/morte, destrói o `CombatGroundRing` e o ícone, espelhando o
  `HideArcVisual` do `FrontalReflector` (R6.5).

**Anexação:** o `ProtectionIndicator` é garantido no inimigo quando um `Shield` é concedido
(`ShieldSupportBehavior.GrantShieldsToAllies`, no `AddComponent<Shield>`), reutilizando o componente
se já existir.

## Fluxo de dados — hit totalmente absorvido (o "golpe sem dano")

```
Player golpeia → HitboxDamage.TryDamageActor → PlayerActor.DealResolvedAttackDamage
   → enemy.TakeDamage(dano)
      → Actor.TakeDamage: amount *= _damageTakenModifiers   // reduções que NÃO são proteção
      → absorber = Shield; entrada = amount; leftover = absorber.Absorb(amount)
      → health -= leftover ; actualDamage = previousHealth - health
      → [novo] se entrada > 0 && leftover == 0 (absorveu tudo): DamageAbsorbed?.Invoke(this)
   → ProtectionIndicator (já mostrando ícone+aura): pisca para confirmar a absorção
```

## Decisões de design e alternativas

- **Config central vs. só editar assets:** SO central como camada de override para tuning num lugar
  só (seu pedido "expor os valores"), preservando a autoria fina por-arma/por-skill.
- **Hitbox básico (A) engrossar cápsula vs. (B) migrar para área:** (A) preserva o `HitboxDamage` e
  seu dedupe, menor risco de regressão; (B) reservado se o arco lateral não ficar bom.
- **Skills: multiplicador global vs. reautorar cada asset:** as duas — multiplicador para tuning
  rápido, reautoração pontual das mais problemáticas (Punhos/Asura) fixada na calibragem.
- **Ícone world-space (billboard) vs. UI overlay:** world-space acompanha o inimigo e não polui o
  HUD; combina com a aura no chão do mesmo objeto.
- **Reuso de `CombatGroundRing`:** coesão visual com o Mirror; evita novo sistema de desenho.
- **Escopo do indicador restrito a `Shield`/`FrontalReflector`:** os `_damageTakenModifiers` reduzem
  dano mas não são "proteção"; sinalizá-los confundiria o jogador (R6.8).

## Estratégia de testes

- **EditMode (lógica pura):**
  - `CombatBalanceBounds`: todos os clamps (raio de sweep, dimensões, multiplicador de mana, vida,
    armor) — property tests, no estilo de `ShieldHealPropertyTests`/`DisplacementTierBoundsPropertyTests`.
  - Gatilho de `DamageAbsorbed`: uma função/decisão pura "entrada > 0 && leftover == 0 ⇒ absorvido",
    exercitável sem cena.
- **Verificação em cena/Editor:** sem `CombatBalanceConfig`, o combate usa padrões (projétil no novo
  raio, demais neutros); flechas acertam com o raio maior; básico da manopla cobre a frente; ícone+aura
  aparecem num inimigo com `Shield` e somem ao esgotar; arco do Mirror intacto. Como o Unity não roda
  via CLI aqui, validar também por inspeção de referências e `git diff`, preservando `.meta`.
- **Regressão:** rodar a suíte EditMode existente e garantir que segue passando (R7.3).

## Calibragem — valores de partida e roteiro de playtest

> Consolidação da task 7.1. Os números vivem nos assets (não em código); esta seção apenas fixa e
> registra o **estado de partida** verificado e o roteiro de validação em playtest. Requisitos
> tocados: 3.1, 3.2, 3.3, 4.1, 1.1, 2.2.

### (a) Valores de partida (verificados nos assets)

**`Assets/_Project/Resources/CombatBalanceConfig.asset`** (bate com a tabela do design §1):

| Campo | Valor de partida | Requisito |
|---|---|---|
| `projectileSweepRadius` | 0.35 | R1.1 (era 0.12) |
| `gauntletBasicBoxSize` | (1.6, 1.9, 1.9) | R2.2 |
| `gauntletBasicReach` | 2.2 | R2.4 |
| `skillManaCostMultiplier` | 0.7 | R3.1 |
| `startingBaseDamage` | 5 | R4.1 |
| `startingArmor` | 0 | R4.1 |
| `startingHealth` | 0 (= usa autorado do prefab) | R4.3 |
| `protectionIconEnabled` | true | R6.1 |
| `protectionAuraColor` | (0.4, 0.9, 1.0, 1.0) — ciano-claro | R6.7 |

**`Gauntlet.asset`** (hitbox do básico reconciliado — fonte única): `attackBoxSize (1.6, 1.9, 1.9)`,
`attackDistance 2.2`, `attackDamage 15`. Casa exatamente com `gauntletBasicBoxSize`/`gauntletBasicReach`
do config, eliminando a divergência com a cápsula fina antiga (R2.1/R2.2).

**Assets de skill da manopla** (`_manaCost` autorado; custo efetivo = autorado × 0.7):

| Skill | `_manaCost` autorado | Custo efetivo (×0.7) | `activeTime` | Dano (soma dos `damageMultiplier`) |
|---|---|---|---|---|
| Avanço (Q) — `BreakerAdvance` | 80 | 56 | 0.6 | 0.7 + 1.1 = 1.8× |
| Punhos (W) — `BreakerFlurry` | 110 | 77 | 1.2 | 6×0.46 + 1.1 = 3.86× (7 hits) |
| Choque (E) — `BreakerShock` | 140 | 98 | 0.8 | 0.5 + 2.2 = 2.7× (inalterado) |
| Asura (R) — `BreakerAsura` | 210 | 147 | 2.0 | 16×0.42 + 3.8 = 10.52× (17 hits, finisher intacto) |

Player inicial (`FirstSector.unity` / `PlayerArpgStats`): **mana pool 1500**, vida 100, `baseDamage 5`,
`armor 0`.

### Conta de encadeamento (R3.1)

Com pool inicial de **1500** de mana e custos efetivos (autorado × 0.7):

- Par mais caro possível: Asura (147) + Choque (98) = **245** ≪ 1500 → sobra ~1255.
- Duas skills quaisquer cabem com folga enorme; na prática dá para encadear as quatro
  (56 + 77 + 98 + 147 = **378**) e ainda restam ~1122.
- Portanto **R3.1 satisfeito com folga**: o jogador encadeia ≥2 skills base a partir da mana inicial
  sem secar o recurso. (O multiplicador 0.7 é escopado à família manopla via
  `BreakerGauntletAbility.ManaCost`; Arco/Lança mantêm o custo autorado — R3.6/R7.2.)

### Relações-chave verificadas

- **R1.1** — `projectileSweepRadius 0.35 > 0.12` antigo; padrão de código em `ArsenalProjectile`
  também 0.35, então o feel melhora mesmo sem o asset.
- **R2.2** — caixa frontal (1.6, 1.9, 1.9) cobre a frente e leve lateral; alcance 2.2 sem virar
  ataque de longo alcance (R2.4).
- **R3.2** — Asura passou de `activeTime` 2.9 → 2.0 (menos tempo parado), com nº de hits preservado
  e finisher (mult 3.8) intacto.
- **R3.3** — Avanço/Punhos com dano por mana mais competitivo, sem ultrapassar o arco.
- **R4.1** — `startingBaseDamage 5`, `startingArmor 0`, `startingHealth 0` presentes e clampados na
  leitura.

### (b) Roteiro curto de playtest (validação manual em cena)

Rodar em `FirstSector.unity` (ou `CombatStudy.unity`) com a manopla equipada:

1. **Encadear ≥2 skills com a mana inicial:** a partir do pool cheio (1500), usar Asura → Choque em
   sequência; confirmar que ambas ativam e ainda sobra mana visível na orbe. (R3.1)
2. **Flechas acertam confiável:** com o arco, atirar em inimigos que passam raspando pela linha de
   tiro; com o raio 0.35 o acerto deve registrar sem "passar de raspão". Conferir que paredes ainda
   bloqueiam a flecha (sem atravessar cenário sólido). (R1.1/R1.5)
3. **Básico cobre a frente:** com a manopla, golpe básico em inimigo colado e levemente lateral deve
   acertar de forma consistente (sem whiff); um acerto por alvo por ativação (dedupe preservado).
   (R2.2/R2.3)
4. **Manopla vs. arco equilibrados:** comparar tempo-para-matar e conforto entre manopla e arco no
   mesmo grupo de inimigos; a manopla deve parecer jogável sem dominar o arco. (R3.3/R3.6)
5. **Indicador de proteção (sanidade):** contra um inimigo com `Shield`, confirmar ícone+aura
   aparecendo e sumindo ao esgotar, e o piscar ao ter um golpe totalmente absorvido. (R6.1/R6.3)

> Observação: o Unity não roda via CLI neste ambiente; a verificação acima é manual em Editor/Play.
> A não-regressão de lógica pura fica coberta pela suíte EditMode (tasks 8.x).
