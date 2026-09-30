# Implementation Plan

- [x] 1. Camada central de balanceamento
- [x] 1.1 Criar `CombatBalanceBounds` (classe estática pura) com os clamps
  - Clamps: `projectileSweepRadius` [0.05, 1.0], dimensões de hitbox > 0, `skillManaCostMultiplier` [0.1, 2], vida base > 0, `armor` ≥ 0.
  - Sem dependência de `MonoBehaviour`/cena (property-testável).
  - _Requirements: 5.4_

- [x] 1.2 Criar o `CombatBalanceConfig` (ScriptableObject) e o acessor `CombatBalance.Current`
  - Campos conforme a tabela do design (`projectileSweepRadius`, `gauntletBasicBoxSize`, `gauntletBasicReach`, `skillManaCostMultiplier`, `startingBaseDamage`, `startingArmor`, `startingHealth`, `protectionIconEnabled`, `protectionAuraColor`). Getters aplicam `CombatBalanceBounds`.
  - Acessor estático: `Resources.Load<CombatBalanceConfig>("CombatBalanceConfig")` cacheado; retorna `null` sem quebrar consumidores.
  - Reset do cache em `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` (e no Editor) para não vazar entre plays.
  - Criar o asset em `Assets/_Project/Resources/CombatBalanceConfig.asset` (preservar `.meta`).
  - _Requirements: 5.1, 5.2, 5.3_

- [x] 2. Hitbox do projétil do arco
- [x] 2.1 Substituir o raio de sweep fixo por valor do config
  - Trocar os literais `0.12f` do `ArsenalProjectile` (sweep de dano e `HasInlineTarget`) por `CombatBalance.Current?.ProjectileSweepRadius ?? 0.35f`.
  - Preservar anti-tunneling, ordenação por distância, pierce/ricochete/homing e bloqueio por cenário sólido.
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 3. Hitbox do golpe básico da manopla
- [x] 3.1 Unificar a definição de acerto do básico
  - Ajustar o `CapsuleCollider` do `GauntletHitbox.prefab` para um volume frontal coerente com `gauntletBasicBoxSize`/`gauntletBasicReach`, preservando o `HitboxDamage` e o dedupe `hitActors` (preservar `.meta`).
  - Reconciliar o `attackBoxSize` do `Gauntlet.asset` para o mesmo volume, eliminando a divergência.
  - Fallback para o volume autorado quando o config estiver ausente.
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [x] 4. Skills base da manopla
- [x] 4.1 Aplicar o multiplicador global de custo de mana
  - Em `BreakerGauntletCombat`, ao debitar mana da skill, multiplicar o `_manaCost` por `skillManaCostMultiplier` (fallback 1). Não alterar o pipeline de `AreaHitStep`s.
  - _Requirements: 3.1, 3.4, 3.5_

- [x] 4.2 Reautorar as skills mais problemáticas (dados)
  - Ajustar `_manaCost`/`activeTime`/`_animationSpeed`/`damageMultiplier` de Punhos (W) e Asura (R) para reduzir mana e tempo parado; elevar dano/utilidade de Avanço (Q) e Punhos (W). Preservar identidade e nº de hits.
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.6_

- [x] 5. Stats iniciais do player
- [x] 5.1 Aplicar overrides de stats iniciais do config
  - Na inicialização de stats/vida do player, aplicar `startingBaseDamage`/`startingArmor`/`startingHealth` quando presentes (0/omitido = autorado), respeitando `[Min]` e clamps. Sem alterar a fórmula de mitigação nem a forma do roll de dano.
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 6. Indicador de proteção
- [x] 6.1 Evento `DamageAbsorbed` em `Actor.TakeDamage`
  - Levantar `event Action<Actor> DamageAbsorbed` quando a entrada do `IDamageAbsorber` for > 0 e o leftover for 0 (absorveu tudo, vida não caiu). Não disparar para reduções por `_damageTakenModifiers`. Sem alterar ordem/donos do fluxo de dano.
  - _Requirements: 6.3, 6.8, 7.1_

- [x] 6.2 Gerar o sprite do ícone de escudo
  - Gerar proceduralmente um sprite de escudo e salvar em `Assets/_Project/Art/UI/ProtectionShieldIcon.png` (importado como `Sprite`, com `.meta`). Fallback documentado: desenho por `LineRenderer` se o asset não for desejado.
  - _Requirements: 6.1, 6.7_

- [x] 6.3 Criar o componente `ProtectionIndicator`
  - Aura no chão via `CombatGroundRing.Create(...)` com `protectionAuraColor`; ícone de escudo world-space (billboard) acima da cabeça, altura pelos bounds; respeitar `protectionIconEnabled`.
  - Mostrar enquanto houver `Shield` com `RemainingCapacity > 0`; assinar `DamageAbsorbed` em `OnEnable`/desassinar em `OnDisable` e piscar ao receber.
  - Limpeza total em `OnDisable`/morte (destruir ring e ícone), espelhando `HideArcVisual`.
  - _Requirements: 6.1, 6.2, 6.3, 6.5, 6.6, 6.7_

- [x] 6.4 Anexar o indicador aos inimigos protegidos
  - Garantir o `ProtectionIndicator` quando um `Shield` é concedido (`ShieldSupportBehavior.GrantShieldsToAllies`), reutilizando o componente existente.
  - _Requirements: 6.1, 6.2_

- [x] 6.5 Integração opcional com o Mirror
  - `FrontalReflector` exibe o mesmo ícone enquanto `ShieldActive`, mantendo o arco frontal atual sem duplicar a aura.
  - _Requirements: 6.4_

- [x] 7. Calibragem dos números-alvo
- [x] 7.1 Definir e registrar os valores de partida
  - Fixar no `CombatBalanceConfig.asset` e nos assets de skill os valores-alvo iniciais (raio de sweep, caixa do básico, multiplicador de mana, reautoração de Punhos/Asura, stats iniciais).
  - Registrar um roteiro curto de validação em playtest (encadear ≥2 skills com a mana inicial; flechas acertam confiável; básico cobre a frente; manopla vs. arco equilibrados).
  - _Requirements: 3.1, 3.2, 3.3, 4.1, 1.1, 2.2_

- [x] 8. Testes e verificação
- [x] 8.1 Testes EditMode
  - Property tests para `CombatBalanceBounds` (todos os clamps) e para a decisão pura de `DamageAbsorbed` (entrada > 0 && leftover == 0 ⇒ absorvido).
  - _Requirements: 7.3_

- [x] 8.2 Não-regressão e defaults
  - Rodar a suíte EditMode existente e confirmar que segue passando.
  - Confirmar (cena/Editor ou inspeção + `git diff`, preservando `.meta`): flechas acertam com o raio maior; básico da manopla cobre a frente; skills encadeáveis; ícone+aura aparecem/somem com o `Shield`; arco do Mirror intacto; arco (arma) sem regressão. Relatar o que não pôde ser validado via CLI.
  - _Requirements: 7.1, 7.2, 7.3_
