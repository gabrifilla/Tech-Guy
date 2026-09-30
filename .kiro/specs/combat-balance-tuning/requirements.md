# Requirements Document

## Introduction

Esta feature trata do **feel de combate das armas base do Tech Guy**, com foco na **manopla**, que
hoje é sentida como muito mais desconfortável de jogar que o **arco**. A investigação do código
apontou causas concretas:

- **Hitbox dos projéteis do arco é minúsculo.** O `ArsenalProjectile` faz o sweep com raio de
  `0.12` m (12 cm). As flechas passam raspando por inimigos que visualmente deveriam ser acertados.
- **Hitbox do golpe básico da manopla é fino e divergente.** O `GauntletHitbox.prefab` usa uma
  `CapsuleCollider` de raio `0.19` m, enquanto o `Gauntlet.asset` também define um `attackBoxSize`
  de `1.25 × 1.8 × 1.6`. São **duas definições de acerto** para o mesmo golpe, e a cápsula fina é a
  que efetivamente balança — por isso o básico "erra" alvos próximos.
- **As skills base da manopla pesam demais no recurso e no tempo parado.** Custos de mana altos
  (Avanço 80, Punhos 150, Choque 140, Asura 280) esvaziam a mana em um ou dois usos; e janelas
  ativas longas (Asura `activeTime` 2.9 s) deixam o jogador parado recebendo dano. A manopla ainda
  obriga a aproximação (`attackDistance` 2.2, `attackRadius` 0), enquanto o arco ataca de longe —
  daí o desconforto relativo.
- **Stats iniciais do player deixam a manopla frágil.** `PlayerArpgStats` inicia com
  `baseDamage` 5 e `armor` 0; a arma soma 15 de dano. É a base a calibrar.
- **Proteção invisível em inimigos.** O componente `Shield` (concedido pelo `ShieldSupportBehavior`)
  absorve dano e **não tem indicação visual**; o jogador acerta, o dano é absorvido, a vida não cai,
  sem nenhuma pista. O Mirror (`FrontalReflector`) reduz projéteis no arco frontal e já mostra um
  arco no chão, mas o `Shield` é invisível.

O objetivo, **sem introduzir progressão, loja ou novos arquétipos**, é: melhorar o hitbox do
projétil do arco e do golpe básico da manopla; rebalancear as skills base da manopla (custo, tempo
e/ou dano) para ela não secar a mana nem expor demais o jogador; expor/ajustar os stats iniciais do
player; e dar uma indicação visual clara de proteção de dano em inimigos (ícone de escudo na cabeça
+ aura no chão).

A feature reaproveita os sistemas existentes: `ArsenalProjectile`, `HitboxDamage` /
`GauntletHitbox.prefab` / `Gauntlet.asset`, `BreakerGauntletAbility` / `BreakerGauntletCombat` /
`AreaHitStep`, `PlayerArpgStats` / `PlayerActor`, `Shield` / `ShieldSupportBehavior`,
`FrontalReflector` / `CombatGroundRing`. **Não** cria novo sistema de combate, dash, nem CC; ajusta
dados e adiciona apenas o visual do indicador de proteção.

## Glossary

- **Hitbox de sweep do projétil**: o raio usado no `SphereCastAll` do `ArsenalProjectile` que decide
  se a flecha acerta um alvo ao longo do trajeto do frame (hoje `0.12` m).
- **Golpe básico**: o ataque comum (não-skill) da arma. Na manopla, resolvido pelo
  `GauntletHitbox.prefab` (`HitboxDamage`) e pelos campos de `Gauntlet.asset`.
- **Skill base da manopla**: cada uma das quatro habilidades do `Gauntlet.asset` — Avanço (Q),
  Punhos (W), Choque (E), Asura (R) — descritas por `AreaHitStep`s e custo/`activeTime`/`cooldown`.
- **Knob de balanceamento**: valor de tuning (raio de hitbox, custo, dano base, mitigação, etc.)
  exposto de forma editável, idealmente num asset central.
- **Config central**: o `CombatBalanceConfig` (ScriptableObject) que reúne os knobs desta feature.
- **Proteção de dano**: mecânica de inimigo que absorve/reduz dano recebido — hoje o `Shield`
  (absorção depletável, `IDamageAbsorber`) e o `FrontalReflector` (redução no arco frontal).
- **Indicador de proteção**: par de elementos visuais (ícone de escudo acima da cabeça + aura no
  chão) que sinaliza que um inimigo está protegido contra dano.

## Requirements

### Requisito 1 — Hitbox do projétil do arco mais generoso e ajustável

**User Story:** Como jogador de arco, quero que minhas flechas acertem alvos que claramente estão no
caminho, sem passar raspando, para o combate à distância parecer confiável.

#### Acceptance Criteria

1. WHEN uma flecha (`ArsenalProjectile`) viaja em direção a um inimigo cujo corpo intersecta o
   trajeto do sweep THEN o sistema SHALL registrar o acerto com um raio de sweep maior que o atual
   `0.12` m (alvo de design ~`0.30`–`0.40` m), configurável.
2. WHEN o raio de sweep é aumentado THEN o sistema SHALL preservar o comportamento anti-tunneling
   existente (o sweep contínuo por `SphereCastAll`, não teleporte por frame) e a resolução de
   múltiplos alvos por ordem de distância.
3. WHERE existem regras de pierce, ricochete e homing THEN o sistema SHALL manter essas regras
   funcionando com o novo raio, sem alterar quem é atingido além do ganho de tolerância do raio.
4. IF o raio não é configurado no config central THEN o sistema SHALL usar um padrão de código
   (o novo valor-alvo), e não o `0.12` antigo.
5. WHEN uma flecha passa por cenário sólido (não-trigger) THEN o sistema SHALL continuar sendo
   bloqueada pelo cenário como hoje, sem atravessar paredes por causa do raio maior.

### Requisito 2 — Hitbox do golpe básico da manopla unificado e mais generoso

**User Story:** Como jogador de manopla, quero que meus golpes básicos acertem inimigos à minha
frente de forma consistente, sem sensação de whiff em alvos colados.

#### Acceptance Criteria

1. WHEN o golpe básico da manopla é executado THEN o sistema SHALL usar **uma única** definição de
   área de acerto coerente, eliminando a divergência atual entre a cápsula de raio `0.19` m do
   `GauntletHitbox.prefab` e o `attackBoxSize` `1.25 × 1.8 × 1.6` do `Gauntlet.asset`.
2. WHEN a definição unificada é aplicada THEN o sistema SHALL cobrir um volume frontal
   perceptivelmente maior que a cápsula fina atual, de modo que inimigos imediatamente à frente e
   levemente laterais sejam atingidos.
3. WHERE o golpe básico usa o `HitboxDamage` (dedupe `hitActors` por ativação) THEN o sistema SHALL
   preservar o comportamento de um acerto por alvo por ativação, sem duplicar dano.
4. WHEN a área de acerto do básico é definida THEN o sistema SHALL manter o alcance efetivo coerente
   com `attackDistance` (2.2) sem transformar o básico num ataque de longo alcance.
5. IF os valores do básico são expostos no config central THEN o sistema SHALL permitir ajustá-los
   por lá, com fallback para os valores autorados no `Gauntlet.asset`/prefab.

### Requisito 3 — Skills base da manopla rebalanceadas para uso sustentável

**User Story:** Como jogador de manopla, quero conseguir usar minhas skills em sequência sem secar a
mana num único uso e sem ficar parado tomando dano de graça, para a manopla parecer tão jogável
quanto o arco.

#### Acceptance Criteria

1. WHEN os custos de mana das skills da manopla são revisados THEN o sistema SHALL reduzir o custo
   das skills mais caras de modo que o jogador consiga encadear pelo menos duas skills base a
   partir da mana inicial, com valores-alvo definidos na calibragem (design).
2. WHERE uma skill tem janela ativa longa (ex.: Asura, `activeTime` 2.9 s) THEN o sistema SHALL
   reduzir o tempo parado e/ou compensar com dano/utilidade, de forma que a exposição a dano durante
   a skill seja proporcional ao retorno.
3. WHEN o dano por mana e por segundo das skills fracas (Avanço, Punhos) é revisado THEN o sistema
   SHALL elevá-lo para ficar competitivo com o custo, sem tornar a manopla dominante sobre o arco.
4. WHEN qualquer skill é ajustada THEN o sistema SHALL preservar sua identidade e o pipeline
   existente (`AreaHitStep`s executados por `BreakerGauntletCombat`, reações/stance atuais).
5. WHERE os valores de custo/tempo/dano são expostos no config central THEN o sistema SHALL permitir
   overrides globais (ex.: multiplicador de custo de mana de skill), com fallback para os valores
   autorados em cada asset de skill.
6. WHEN o rebalanceamento é aplicado THEN o sistema SHALL manter o arco jogável como hoje, sem
   regressão no seu custo/efetividade.

### Requisito 4 — Stats iniciais do player ajustáveis

**User Story:** Como designer, quero ajustar os stats iniciais do player (dano base, mitigação,
vida) num lugar central, para calibrar a fragilidade inicial sem editar lógica.

#### Acceptance Criteria

1. WHEN o projeto roda THEN o sistema SHALL permitir ajustar, via config central, ao menos:
   `baseDamage`, `armor` e a vida base do player, aplicados sobre os valores de `PlayerArpgStats`.
2. WHEN um stat inicial é alterado no config THEN o sistema SHALL refletir o novo valor em runtime
   sem alterar a fórmula existente (`ReduceIncomingDamage` = `amount*100/(100+armor)`; roll de dano
   inalterado na forma).
3. IF um stat não é configurado THEN o sistema SHALL usar o valor atual de `PlayerArpgStats` como
   padrão (`baseDamage` 5, `armor` 0).
4. WHEN os stats são aplicados THEN o sistema SHALL respeitar os `[Min]` já existentes e clamps
   coerentes, sem permitir valores degenerados (ex.: vida ≤ 0).

### Requisito 5 — Valores de balanceamento centralizados

**User Story:** Como designer, quero os knobs desta feature num asset central editável, para tunar
o feel de arma de forma iterativa.

#### Acceptance Criteria

1. WHEN o projeto é aberto THEN o sistema SHALL expor um `CombatBalanceConfig` (ScriptableObject)
   contendo, no mínimo: raio de sweep do projétil, dimensões do hitbox básico da manopla,
   multiplicador de custo de mana de skill (e/ou overrides por skill), e os stats iniciais do
   player (R4) e parâmetros do indicador (R6).
2. WHEN um valor é alterado no config THEN o sistema SHALL usá-lo em runtime sem exigir mudança em
   código de lógica.
3. IF o config está ausente ou um campo está vazio THEN o sistema SHALL cair no padrão de código,
   sem quebrar o combate.
4. WHEN um valor é lido THEN o sistema SHALL aplicar clamps coerentes para impedir valores
   degenerados.
5. WHERE valores já são autorados em `WeaponScript`/assets de skill/prefabs THEN o sistema SHALL
   tratá-los como fonte por-arma/por-skill e o config central SHALL atuar como override/multiplicador
   global, sem duplicar nem sobrescrever silenciosamente os assets.

### Requisito 6 — Indicação visual de proteção de dano

**User Story:** Como jogador, quando meu golpe acerta mas não causa dano por causa de uma proteção,
quero uma indicação visual clara de que o inimigo está protegido.

#### Acceptance Criteria

1. WHILE um inimigo tem proteção de dano ativa (`Shield` com capacidade restante) THEN o sistema
   SHALL exibir um **ícone de escudo acima da cabeça** do inimigo e uma **aura/anel no chão** sob ele.
2. WHEN a proteção se esgota ou expira (Shield destruído) THEN o sistema SHALL remover o ícone e a
   aura desse inimigo.
3. WHEN um golpe do jogador é totalmente absorvido pela proteção (dano à vida = 0 apesar do hit)
   THEN o sistema SHALL dar um feedback de impacto na proteção (ex.: piscar ícone/aura),
   distinguível de um hit normal que causa dano.
4. WHERE o inimigo é um Mirror (`FrontalReflector`) THEN o sistema SHALL manter o arco frontal
   existente e SHALL poder exibir o ícone de escudo enquanto a redução do Mirror está ativa, de
   forma consistente com o indicador do `Shield`.
5. WHEN o inimigo protegido morre ou é desabilitado THEN o sistema SHALL remover todos os elementos
   do indicador, sem deixar ícones ou auras órfãos na cena.
6. WHERE já existe o padrão de anel no chão (`CombatGroundRing`, usado pelo `FrontalReflector`)
   THEN o sistema SHALL reaproveitar esse padrão para a aura, mantendo coesão visual.
7. WHEN o indicador está visível THEN o sistema SHALL posicionar o ícone de escudo de forma legível
   acima da cabeça do inimigo, acompanhando sua posição, com cor/estilo que comunique "protegido"
   sem se confundir com a barra de vida ou telegrafos de ataque.
8. WHERE um hit é apenas reduzido por outro caminho que não é proteção de dano (ex.: os
   `_damageTakenModifiers` do `Actor`, clamp 0.1–10) THEN o sistema SHALL **não** acionar o
   indicador de proteção, para não sinalizar proteção onde ela não existe.

### Requisito 7 — Sem regressão de combate

**User Story:** Como desenvolvedor, quero que o rebalanceamento não quebre o fluxo de combate
existente nem os sistemas dependentes.

#### Acceptance Criteria

1. WHEN a feature está ativa com os knobs nos padrões escolhidos THEN o sistema SHALL preservar os
   caminhos de dano existentes (`Actor.TakeDamage`, `PlayerActor.DealResolvedAttackDamage`,
   `IDamageAbsorber`, `FrontalReflector`), sem alterar quem recebe dano nem a ordem de aplicação.
2. WHEN o arco é usado THEN o sistema SHALL manter seu comportamento atual, exceto pelo ganho de
   raio de hitbox (R1).
3. WHEN testes EditMode existentes rodam THEN o sistema SHALL mantê-los passando, e a feature SHALL
   adicionar cobertura para os clamps dos knobs e para a lógica pura onde aplicável (sem cena).
