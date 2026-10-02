# Boons do Tech-Guy

Este documento cataloga **todos** os boons (bênçãos) do jogo, agrupados por arma e por categoria. É gerado a partir do código-fonte e serve como referência de design e de conteúdo.

## O que é um boon?

Um **boon** é uma melhoria temporária escolhida como recompensa durante uma incursão (run). Ele vale **apenas pela incursão atual** — nenhum boon é persistido entre runs, e todas as armas/habilidades modificadas por eles são cópias de runtime isoladas, nunca os assets originais.

Existem duas naturezas de boon:

- **Boons de arma** — ficam travados pela família da arma equipada (**Manopla / Gauntlet**, **Arco / Bow** ou **Lança / Spear**). Só aparecem como oferta se forem da família da sua arma. Cada boon tem um **rank máximo**: ao escolher o mesmo boon repetidas vezes, o rank sobe e o efeito se intensifica, até o `Rank máx.`.
- **Bênçãos universais** — não dependem da arma. Aplicam-se a qualquer arma e cobrem sinergias elementais, atributos e transformações de habilidade.

Os ranks nunca diminuem e sempre fortalecem a quantidade afetada. O rank máximo da maioria dos boons de arma é **3**; alguns são de efeito único (`Rank máx. = 1`), ou seja, são adquiridos uma única vez.

> Observação sobre ofertas: além dos boons de arma, a composição de recompensas de cada sala sempre tenta incluir uma opção que altera habilidade (uma "reescrita", quando elegível) e opções de upgrade universais. Alguns boons de arma foram **aposentados** da composição de ofertas, mas continuam no catálogo por compatibilidade com runs/saves (veja a seção final).

---

## Manopla (Gauntlet)

| Título | Kind (enum) | Efeito | Rank máx. |
| --- | --- | --- | --- |
| Propulsor de combate | `RocketAdvance` | Q: +70% de avanço por nível; respeita os limites navegáveis. | 3 |
| Mil punhos | `FlurryEcho` | W: repete o último golpe +2 vezes por nível, a 55% do dano e postura. | 3 |
| Epicentro | `ShockRing` | E transforma os impactos em círculos ao redor do jogador. +20% de raio por nível. | 3 |
| Asura reverberante | `AsuraEcho` | R: +1 réplica do golpe final por nível, a 65% do dano e postura. | 3 |
| Dínamo de combate | `Momentum` | Q/W/E originais geram +10 de energia Asura por uso e por nível. | 3 |
| Reserva divina | `AsuraReserve` | Após consumir Asura, conserva 20 de energia por nível. | 3 |
| Terceiro impacto | `ComboNova` | Cada terceiro básico dispara uma nova de 2,5m com 60% do dano da arma por nível. | 3 |
| Golpe de ímpeto | `MomentumStrike` | Cada acerto direto acumula uma pilha (até 10) que aumenta seu dano em 2% por nível por pilha. Sofrer dano zera as pilhas. | 3 |
| Onda de choque | `Shockwave` | O golpe final do combo libera uma onda esférica ao redor do jogador. Raio base de 3m, +20% por nível. | 3 |
| Punho de Asura | `AsuraFist` | Seus básicos carregam o finalizador: cada acerto básico gera +2 de energia Asura por nível. | 3 |
| Guarda partida | `GuardBreaker` | A cada 3 básicos seguidos, o terceiro racha a postura (+50% de dano de postura por nível) e arremessa ao quebrar a guarda. | 3 |
| Combo faminto | `HungryCombo` | Tecer básicos acelera suas skills: cada acerto básico reduz a recarga das habilidades em 0,3s por nível. | 3 |
| Punho sísmico | `SeismicFist` | OPCIONAL: reinstaura o arremesso. Ao quebrar a guarda (básico ou skill), empurra o inimigo, +30% de distância por nível. | 3 |

Total oferecível na Manopla: **13 boons**.

---

## Arco (Bow)

| Título | Kind (enum) | Efeito | Rank máx. |
| --- | --- | --- | --- |
| Corda tripla | `TwinShot` | Todas as flechas: +2 projéteis por nível, com dano individual dividido por 1 + 0,5 × nível. | 3 |
| Agulhas espectrais | `Piercing` | Flechas básicas e de habilidades atravessam inimigos. Cenário continua bloqueando. | 1 |
| Flecha saltadora | `Ricochet` | Cada flecha busca +2 inimigos após acertar, por nível. Cada salto conserva 75% do dano. | 3 |
| Olho caçador | `Homing` | Flechas curvam em direção a inimigos próximos à trajetória, sem atravessar paredes. | 1 |
| Tambor de disparos | `RapidBurst` | Q: +2 disparos e +25% de cadência entre disparos por nível. | 3 |
| Pavão de aço | `WideVolley` | E: +4 flechas por nível, comprimidas num leque de até 100 graus. | 3 |
| Nuvem obediente | `GuidedRain` | R acompanha o cursor a cada pulso, respeitando o alcance. | 1 |
| Flecha estilhaçante | `SplitArrow` | Ao matar com uma flecha, dispara +1 flecha por nível a partir do alvo, em direções distintas, com 50% do dano. | 3 |
| Tiro carregado | `ChargedShot` | Segure o ataque por 0,6s para disparar uma flecha perfurante com +75% de dano por nível. | 3 |
| Disparo em recuo | `KitingStep` | Atirar recuando dá um impulso curto de reposicionamento, +25% de distância por nível. O kiting vira ritmo ativo. | 3 |
| Cadência adaptativa | `AdaptiveCadence` | Manter 6m ou mais acelera seu disparo básico em +15% por nível; aproximar-se do alvo zera o bônus. | 3 |
| Chuva marcadora | `RainMark` | O R marca e lentifica inimigos sob a chuva; acertos em marcados causam +20% de dano por nível enquanto a marca durar. | 3 |

Total oferecível no Arco: **12 boons**.

---

## Lança (Spear)

| Título | Kind (enum) | Efeito | Rank máx. |
| --- | --- | --- | --- |
| Estocada ecoante | `EchoThrust` | Habilidades de estocada: +1 repetição por nível; dano de cada golpe dividido por 1 + 0,2 × nível. | 3 |
| Tridente espiral | `Trident` | E dispara estocadas em três direções. Cada direção causa 60% do dano. | 1 |
| Dragão liberto | `DragonWave` | R também lança uma onda perfurante de alcance duplo e 60% do dano por nível. | 3 |
| Distância perfeita | `SpearTip` | Acertos diretos a 3m ou mais: +45% de dano por nível. | 3 |
| Carrasco | `Execution` | Acertos diretos contra inimigos abaixo de 30% de vida: +60% de dano por nível. | 3 |
| Lua viajante | `Orbit` | W: +25% de raio por nível; as varreduras avançam 1,5m por pulso. | 3 |
| Condutor vital | `Siphon` | Cada acerto direto que causa dano recupera 2 de mana por nível. Descargas secundárias não recuperam. | 3 |
| Lança fantasma | `PhantomSpear` | Estocadas repetem com uma cópia espectral após um instante. | 1 |
| Lua partida | `MoonShard` | Varreduras lançam projéteis a partir das extremidades. | 3 |
| Retorno de pacote | `ReturnWave` | Ondas retornam ao jogador ao atingir o alcance. | 1 |
| Encadeamento | `ChainThrust` | Estocadas encadeiam uma estocada curta a um inimigo próximo. | 1 |
| Espaçamento perfeito | `PerfectSpacing` | Acertos diretos entre 3,5m e 6,5m do jogador: +35% de dano por nível. | 3 |
| Linha empalada | `ImpalingLine` | A estocada atinge todos os inimigos na linha e os puxa para o jogador. Alcance da puxada cresce por nível. | 3 |
| Recuo controlado | `SpacingRecoil` | Uma estocada que conecta recua você até a distância ideal, +20% de recuo por nível, sem sair da banda. | 3 |
| Muralha de hastes | `PikeWall` | O W cria uma zona de hastes que empurra inimigos para fora do seu alcance de perigo, +30% de empurrão por nível. | 3 |
| Ponto cego | `EdgeStrike` | Acertar na ponta do alcance racha a postura (+40% por nível) e abre uma janela de vulnerabilidade ao quebrar a guarda. | 3 |

Total oferecível na Lança: **16 boons**.

---

## Bênçãos universais

Estas recompensas **não dependem da arma** e aparecem no mesmo pool de ofertas de qualquer família. São definidas em `RunBoons.OfferReward`. As sinergias elementais (Bobina encadeada, Reator de sucata, Combustível instável, Ressonância térmica) e a sobrecarga/elementos acumulam a cada escolha (são repetíveis); vitalidade, foco e transformações são de uso único.

| Título | Id | Efeito | Escala |
| --- | --- | --- | --- |
| Bobina encadeada | `conductor` | Acertos saltam com 45% do dano e carregam fogo/gelo. Cada cópia: +1 alvo por salto e +0,5m de alcance (inicial: 3,5m). | Sinergia `RunSynergy.Conductor`, acumula por cópia |
| Reator de sucata | `detonation` | Mortes por impacto explodem: 75% do dano em 3,5m. Cópias: +25 pontos percentuais e +0,5m. Explosões continuam a cadeia. | Sinergia `RunSynergy.Detonation`, acumula por cópia |
| Combustível instável | `reactor` | Com fogo adquirido, sua queimadura ganha por segundo +12% do dano do acerto por cópia. Combine com críticos e descargas. | Sinergia `RunSynergy.Reactor`, acumula por cópia |
| Ressonância térmica | `resonance` | Descargas e explosões: +40% de dano por efeito de fogo/gelo já presente no alvo, por cópia. Prepare a horda com elementos! | Sinergia `RunSynergy.Resonance`, acumula por cópia |
| Sobrecarga elemental | `overflow` | Acertos em alvos com fogo/gelo ativo disparam um burst de 50% do dano do golpe por cópia, sem remover o efeito. | Repetível (sobe o rank de overflow no registro elemental) |
| Coração de ferro | `vitality` | +100 de vida máxima e recuperação completa de vida. | Uso único |
| Foco eficiente | `focus` | Q custa 25% menos mana e recarrega 35% mais rápido. | Uso único |
| Lâmina incandescente | `ignite` | BIZARRO: seus ataques agora causam QUEIMADURA, dano contínuo por 4s. Acumula. | Repetível (cada escolha deixa a queimadura mais forte) |
| Toque glacial | `frost` | BIZARRO: seus ataques agora CONGELAM — chance de imobilizar e lentidão pesada por 2.5s. | Repetível (cada escolha aumenta a chance) |
| Disparo prismático (Arco) / Nova de impacto (demais) | `transform` | Arco: TRANSFORMA Q — troca o disparo duplo por cinco flechas perfurantes em leque. Demais: TRANSFORMA Q — troca o avanço/estocada por uma explosão circular de 4m. Não gera Asura. | Uso único; título/efeito dependem de a arma disparar flechas |

Total de bênçãos universais: **10**.

Notas:
- `Disparo prismático` e `Nova de impacto` são o **mesmo** boon `transform`; o jogo mostra um ou outro conforme a arma dispara flechas (Arco) ou não.
- A oferta de reescrita de habilidade (`transform`) só surge quando há uma habilidade válida no slot Q e passa por uma chance (~20%) por conjunto de ofertas.
- As bênçãos mencionadas em documentos antigos como "Núcleo de força", "Mãos velozes", "Fluxo arcano" e "Foco eficiente" correspondem a modificadores de atributo no código (`power`, `haste`, `recharge`, `focus`). Apenas `focus` (Foco eficiente), `vitality` (Coração de ferro) e os elementais/sinergias acima entram atualmente no pool de ofertas de `OfferReward`; os demais atributos (`power`, `haste`, `recharge`, `crit`, `brutal`, `bulwark`, `swift`) existem como casos aplicáveis em `Choose`, mas **não** são adicionados ao pool de ofertas na versão atual do código.

---

## Legado / não oferecidos (compatibilidade)

Os valores de enum abaixo permanecem no `Catalog` (`WeaponRunModifiers.cs`) e no enum `WeaponBoon`, mas foram **aposentados** da composição de ofertas: estão listados em `RunBoons.RetiredFamilyBoons` e são pulados pelo gate de família em `OfferReward`. Não deixam de existir no catálogo para que runs/saves que já os adquiriram continuem funcionando — eles simplesmente **não aparecem** como recompensa in-game na versão atual.

| Título | Kind (enum) | Família | Efeito (como catalogado) | Rank máx. |
| --- | --- | --- | --- | --- |
| Punhos titânicos | `LongFists` | Manopla | Básicos e habilidades: +25% de alcance, largura e raio por nível. | 3 |
| Demolidor | `StanceCrusher` | Manopla | Habilidades originais: +75% de dano de postura e +30% de empurrão por nível. | 3 |
| Motor em pane | `Berserker` | Manopla | Abaixo de 40% da vida: acertos diretos causam +50% de dano por nível. | 3 |
| Balista portátil | `HeavyBolt` | Arco | W: +60% de dano e +25% de preparação por nível. | 3 |
| Horizonte mortal | `Sniper` | Arco | Acertos diretos: até +60% de dano por nível aos 12m de distância do jogador. | 3 |
| Monção | `LongRain` | Arco | R: +3 pulsos e +20% de raio por nível. | 3 |
| Haste impossível | `LongReach` | Lança | Básicos e habilidades: +30% de alcance e raio por nível. | 3 |
| Órbita das luas | `TripleMoon` | Lança | W: +2 varreduras por nível. | 3 |
| Ponta contaminada | `Affliction` | Lança | Acertos diretos: +25% de dano por fogo/gelo presente no inimigo, por nível. | 3 |

Total de entradas de legado: **9 boons**.

---

## Fontes e totais

Fontes de verdade (código autoritativo):
- `Assets/_Project/Scripts/Weapons/WeaponRunModifiers.cs` — enum `WeaponBoon`, `Catalog` de `Definition(...)`, e a lógica de rank/família. É daqui que vêm os títulos, descrições e ranks máximos de todos os boons de arma.
- `Assets/_Project/Scripts/Core/RunBoons.cs` — `OfferReward` (pool de bênçãos universais e lista `RetiredFamilyBoons` de boons aposentados) e `Choose` (aplicação de cada boon).
- `Assets/_Project/Scripts/Characters/Combat/RunSynergyEffects.cs` — enum `RunSynergy` (Conductor, Detonation, Reactor, Resonance) por trás das sinergias elementais universais.

Contagem (confere com o catálogo):
- Entradas `Definition(...)` no `Catalog`: **50**.
- Boons de arma oferecíveis: **41** — Manopla 13, Arco 12, Lança 16.
- Entradas de legado / aposentadas: **9** (41 + 9 = 50).
- Bênçãos universais: **10** (definidas em `OfferReward`, fora do `Catalog`).

Valores atuais na data desta leitura do código. Caso o código mude, atualize este documento a partir das fontes acima.
