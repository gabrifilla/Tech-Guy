# Catálogo de modificadores

Estado conferido no código e nos assets em 28/09/2026. Há **46 opções de recompensa**: 16 gerais e 30 específicas de arma (10 por família). As duas versões de transformação de Q contam como uma opção, escolhida conforme a arma.

As recompensas duram somente a incursão, acumulam por escolha e não alteram os assets originais. Q/W/E/R abaixo identificam os **slots 1/2/3/4**; remapear o teclado não muda os efeitos. “Acerto direto” inclui básicos, áreas e projéteis; descargas e explosões são impactos secundários.

## Recompensas gerais

| ID | Nome | Efeito implementado | Limite por run |
| --- | --- | --- | --- |
| conductor | Bobina encadeada | Descargas herdam 45% do dano efetivamente causado e transportam fogo/gelo. Cada cópia acrescenta 1 alvo por salto; alcance 3,5 m na primeira, +0,5 m nas seguintes. | Repetível |
| detonation | Reator de sucata | Mortes por impacto explodem com 75% do dano causado, em 3,5 m; cópias acrescentam 25 pontos percentuais de dano e 0,5 m. Pode continuar a cadeia. | Repetível |
| reactor | Combustível instável | Com queimadura adquirida, acrescenta ao DPS 12% do dano do acerto por cópia. | Repetível |
| resonance | Ressonância térmica | Descargas e explosões recebem +40% de dano por efeito de fogo/gelo já presente, por cópia. | Repetível |
| power | Núcleo de força | +25 pontos percentuais de dano aumentado, para básicos e habilidades. | Repetível |
| haste | Mãos velozes | +0,25 ao multiplicador de velocidade de ataque básico. | Repetível |
| recharge | Fluxo arcano | +15 pontos percentuais de redução de recarga. | 1 |
| vitality | Coração de ferro | +100 de vida máxima e restaura toda a vida. | 1 |
| focus | Foco eficiente | Slot 1: custo de mana ×0,75 e recarga ×0,65. Permanece após transformar a habilidade. | 1 |
| crit | Olho preciso | +15 pontos percentuais de chance crítica e +0,4 ao multiplicador de dano crítico. | Repetível |
| brutal | Golpe brutal | +8 de dano fixo antes dos multiplicadores. | Repetível |
| bulwark | Placa reforçada | +40 de armadura. | Repetível |
| swift | Passo veloz | +20% de velocidade de movimento por cópia, aditivo com outros aumentos percentuais. | Repetível |
| ignite | Lâmina incandescente | Habilita queimadura; cada escolha soma 6 DPS. Duração de 4 s. Acertos renovam a duração e conservam o maior DPS; não criam um DOT adicional por acerto. | Repetível |
| frost | Toque glacial | +35 pontos percentuais de chance de aplicar gelo, até 100%. Efeito por 2,5 s com intensidade 90%, suficiente para congelar. | Repetível |
| transform | Disparo prismático / Nova de impacto | Substitui o slot 1: arco recebe leque de 5 flechas perfurantes; lança/manoplas recebem explosão circular de raio 4 m, sem geração de Asura. | 1 |

Os aumentos de chance crítica e redução de recarga respeitam os limites de `PlayerArpgStats`. Cascatas limitam-se a quatro gerações e 32 impactos secundários; cada alvo recebe no máximo um impacto secundário por acerto original. Ticks isolados de queimadura não detonam.

## Modificadores específicos de arma

Só aparecem para a família equipada. Cada nível corresponde a uma escolha. IDs completos usam o prefixo `weapon_`.

### Arco

| ID | Nome | Efeito | Nível máximo |
| --- | --- | --- | --- |
| weapon_TwinShot | Corda tripla | Todas as flechas: +2 projéteis por nível, com dano individual dividido por 1 + 0,5 × nível. | 3 |
| weapon_Piercing | Agulhas espectrais | Flechas básicas e de habilidades atravessam inimigos. Cenário continua bloqueando. | 1 |
| weapon_Ricochet | Flecha saltadora | Cada flecha busca +2 inimigos após acertar, por nível. Cada salto conserva 75% do dano. | 3 |
| weapon_Homing | Olho caçador | Flechas curvam em direção a inimigos próximos à trajetória, sem atravessar paredes. | 1 |
| weapon_HeavyBolt | Balista portátil | W: +60% de dano e +25% de preparação por nível. | 3 |
| weapon_RapidBurst | Tambor de disparos | Q: +2 disparos e +25% de cadência entre disparos por nível. | 3 |
| weapon_WideVolley | Pavão de aço | E: +4 flechas por nível, comprimidas num leque de até 100 graus. | 3 |
| weapon_LongRain | Monção | R: +3 pulsos e +20% de raio por nível. | 3 |
| weapon_GuidedRain | Nuvem obediente | R acompanha o cursor a cada pulso, respeitando o alcance. | 1 |
| weapon_Sniper | Horizonte mortal | Acertos diretos: até +60% de dano por nível aos 12m de distância do jogador. | 3 |

### Lança

| ID | Nome | Efeito | Nível máximo |
| --- | --- | --- | --- |
| weapon_LongReach | Haste impossível | Básicos e habilidades: +30% de alcance e raio por nível. | 3 |
| weapon_EchoThrust | Estocada ecoante | Habilidades de estocada: +1 repetição por nível; dano de cada golpe dividido por 1 + 0,2 × nível. | 3 |
| weapon_TripleMoon | Órbita das luas | W: +2 varreduras por nível. | 3 |
| weapon_Trident | Tridente espiral | E dispara estocadas em três direções. Cada direção causa 60% do dano. | 1 |
| weapon_DragonWave | Dragão liberto | R também lança uma onda perfurante de alcance duplo e 60% do dano por nível. | 3 |
| weapon_Affliction | Ponta contaminada | Acertos diretos: +25% de dano por fogo/gelo presente no inimigo, por nível. | 3 |
| weapon_SpearTip | Distância perfeita | Acertos diretos a 3m ou mais: +45% de dano por nível. | 3 |
| weapon_Execution | Carrasco | Acertos diretos contra inimigos abaixo de 30% de vida: +60% de dano por nível. | 3 |
| weapon_Orbit | Lua viajante | W: +25% de raio por nível; as varreduras avançam 1,5m por pulso. | 3 |
| weapon_Siphon | Condutor vital | Cada acerto direto que causa dano recupera 2 de mana por nível. Descargas secundárias não recuperam. | 3 |

### Manoplas

| ID | Nome | Efeito | Nível máximo |
| --- | --- | --- | --- |
| weapon_LongFists | Punhos titânicos | Básicos e habilidades: +25% de alcance, largura e raio por nível. | 3 |
| weapon_RocketAdvance | Propulsor de combate | Q: +70% de avanço por nível; respeita os limites navegáveis. | 3 |
| weapon_FlurryEcho | Mil punhos | W: repete o último golpe +2 vezes por nível, a 55% do dano e postura. | 3 |
| weapon_ShockRing | Epicentro | E transforma os impactos em círculos ao redor do jogador. +20% de raio por nível. | 3 |
| weapon_AsuraEcho | Asura reverberante | R: +1 réplica do golpe final por nível, a 65% do dano e postura. | 3 |
| weapon_Momentum | Dínamo de combate | Q/W/E originais geram +10 de energia Asura por uso e por nível. | 3 |
| weapon_AsuraReserve | Reserva divina | Após consumir Asura, conserva 20 de energia por nível. | 3 |
| weapon_StanceCrusher | Demolidor | Habilidades originais: +75% de dano de postura e +30% de empurrão por nível. | 3 |
| weapon_ComboNova | Terceiro impacto | Cada terceiro básico dispara uma nova de 2,5m com 60% do dano da arma por nível. | 3 |
| weapon_Berserker | Motor em pane | Abaixo de 40% da vida: acertos diretos causam +50% de dano por nível. | 3 |

## Passivas e infraestrutura de atributos

O asset `DamageMultiplierExample` é uma passiva de exemplo: `DamageMultiplier`, modo `MoreMultiplier`, valor 1,5 (×1,5 no dano). Não pertence às 46 recompensas. `StatModifierPassiveAbility` permite compor passivas com os atributos abaixo; `AttackPassiveAbility` oferece o gancho `OnAfterAttackHits` para efeitos após acertos.

| Atributo | Uso |
| --- | --- |
| BaseDamage | Dano base do personagem |
| FlatDamageBonus | Dano fixo adicional |
| IncreasedDamagePercent | Aumento percentual de dano |
| DamageMultiplier | Multiplicador de dano |
| CriticalChance | Chance crítica em pontos percentuais |
| CriticalDamageMultiplier | Multiplicador de dano crítico |
| AttackSpeedMultiplier | Velocidade dos ataques básicos |
| MovementSpeedMultiplier | Velocidade de movimento |
| CooldownReductionPercent | Redução percentual de recarga |
| Armor | Redução de dano recebido por armadura |
| MaxHealthBonus | Vida máxima adicional |
| MaxManaBonus | Mana máxima adicional |

Cálculo genérico: `(base + soma Flat) × (1 + soma IncreasedPercent / 100) × produto MoreMultiplier`. `IncreasedPercent` usa 20 para 20%, e não 0,2. A correção de Passo veloz acompanha este documento: o código anterior concedia apenas 0,2% apesar de anunciar 20%.

## Modificadores de inimigos

| Modificador / perfil | Efeito dos assets atuais |
| --- | --- |
| Normal | Vida, dano, movimento e velocidade de ataque ×1 |
| Magic | Vida ×2, dano ×1,3, movimento ×1,1, velocidade de ataque ×1,1 |
| Rare | Vida ×4, dano ×1,6, movimento ×1,15, velocidade de ataque ×1,2 |
| Acelerado (`Affix_Haste`) | Movimento ×1,35 e velocidade de ataque ×1,2 |
| Resistente (`Affix_Guard`) | Dano recebido ×0,7 |
| Aura de gelo (`EnemyFrostAura`) | Raio de 5 m, redução de movimento do jogador de 30%; consulta a cada 0,15 s. Auras multiplicam seus efeitos: duas deixam 49% da velocidade. |

Raridade e resistência a controle são sistemas separados. Empurrão, interrupção, stun, lançamento e knockback são reações/postura; não são novas recompensas. Asura também concede proteção temporária durante a execução (dano recebido ×0,15).

## Fontes

- `Assets/_Project/Scripts/Core/RunBoons.cs`: ofertas, aplicação e limites gerais.
- `Assets/_Project/Scripts/Weapons/WeaponRunModifiers.cs`: catálogo de arma e planos de execução.
- `Assets/_Project/Scripts/Characters/Combat/PlayerOnHitEffects.cs`, `RunSynergyEffects.cs`, `BurnStatus.cs` e `ChillStatus.cs`: elementos e cascatas.
- `Assets/_Project/Scripts/Characters/Player/Stats/PlayerArpgStats.cs`: cálculo dos atributos.
- `Assets/_Project/ScriptableObjects/Abilities/Passives`, `ScriptableObjects/Enemies` e `Prefabs/EnemyVariants`: passivas e afixos configurados.

Itens apenas propostos em `RunSynergies.md` não estão incluídos como funcionalidades existentes.
