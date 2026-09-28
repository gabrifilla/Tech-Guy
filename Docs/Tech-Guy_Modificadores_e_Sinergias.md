# Tech-Guy — Sistema de Modificadores, Sinergias e Builds Emergentes

> Documento de design focado em **diversão, identidade de build e combinações absurdas**, sem priorizar balanceamento numérico neste estágio.

---

## 1. Objetivo do sistema

O sistema de modificadores de **Tech-Guy** deve fazer o jogador sentir que cada run pode evoluir para uma combinação inesperada, exagerada e visualmente marcante.

As principais referências de sensação são:

- **The Binding of Isaac** — combinações de itens que transformam completamente ataques simples;
- **Hades** — boons que interagem entre si e criam arquétipos claros durante a run;
- **Megabonk / survivors-like** — sensação de crescimento exponencial e de "quebrar" a run ao encontrar as peças certas;
- **Diablo / Lost Ark** — impacto, clareza de combate e identidade forte entre habilidades e armas.

O objetivo não é impedir combinações muito fortes.

O objetivo é fazer com que o jogador pense:

> "Eu não acredito que isso funciona junto."

ou:

> "Essa run virou outra coisa."

Uma run excepcionalmente forte deve ser uma recompensa por construir uma boa combinação, e não necessariamente um problema.

---

# 2. Filosofia geral

## 2.1 Modificadores devem gerar comportamento, não apenas números

Modificadores puramente numéricos podem existir, mas devem funcionar como suporte.

Exemplos:

- `+25% dano`
- `+15% crítico`
- `+20% velocidade`
- `+40 armadura`

Eles ajudam a build, mas raramente definem uma run.

Os modificadores mais importantes devem alterar o comportamento do combate:

- adicionar projéteis;
- repetir ataques;
- gerar explosões;
- criar ricochetes;
- converter habilidades;
- espalhar efeitos;
- retornar projéteis;
- criar cópias;
- disparar ataques secundários;
- transformar status em reações.

---

## 2.2 O jogador deve perceber imediatamente quando pega algo importante

Uma boa regra:

> Se o modificador é importante, deve ser possível perceber sua existência sem abrir a tela de status.

Exemplos fortes:

- uma flecha vira três;
- inimigos explodem;
- uma chuva acompanha o cursor;
- uma estocada deixa um eco;
- o terceiro soco gera uma nova;
- um ataque retorna;
- gelo + fogo gera uma reação nova.

---

## 2.3 Algumas escolhas devem ser "combustível"

Nem todos os modificadores precisam transformar o jogo.

Existem três grupos úteis:

### A. Modificadores de atributo

Melhoram a eficiência da build.

Exemplos:

- Power;
- Crit;
- Haste;
- Recharge;
- Swift.

### B. Modificadores de comportamento

Mudam como ataques funcionam.

Exemplos:

- TwinShot;
- Piercing;
- Ricochet;
- TripleMoon;
- FlurryEcho;
- GuidedRain.

### C. Modificadores de sinergia

Ficam mais fortes quando interagem com outras escolhas.

Exemplos:

- Conductor;
- Detonation;
- Resonance;
- Affliction;
- ComboNova.

A maioria das runs memoráveis deve surgir da combinação entre **B + C**, com A servindo como combustível.

---

# 3. Direção temática

O sistema de modificadores combina especialmente bem com a narrativa de **Tech-Guy**.

O protagonista está dentro de um sistema digital.

Logo, os modificadores podem ser tratados como:

- patches;
- overrides;
- módulos;
- extensões;
- exploits;
- hotfixes;
- hooks;
- mutações;
- forks;
- corrupção de código.

Quanto mais modificadores interagem, mais o jogo pode transmitir a sensação de que o jogador está ultrapassando os limites esperados do sistema.

Exemplos de feedback visual ou narrativo:

```text
WARNING: Projectile count exceeds expected parameters.
```

```text
STACK OVERFLOW DETECTED.
Continuing anyway.
```

```text
UNAUTHORIZED COMBAT ROUTINE LOADED.
```

A própria arma pode comentar:

> "Isso definitivamente não estava no manual."

Esse tipo de resposta ajuda a transformar builds absurdas em parte da identidade do jogo.

---

# 4. Recompensas gerais atuais

## conductor — Bobina encadeada

**Estado:** excelente.

Descargas em cadeia são um dos melhores efeitos para criar sensação de crescimento exponencial.

Combina naturalmente com:

- TwinShot;
- RapidBurst;
- FlurryEcho;
- TripleMoon;
- ComboNova;
- Crit;
- Ignite;
- Frost;
- Detonation;
- Resonance.

Quanto mais hits a build produz, mais oportunidades existem para iniciar cadeias.

### Possíveis evoluções futuras

#### Supercondutor

Descargas podem saltar novamente para inimigos já atingidos após passarem por pelo menos outro alvo.

#### Curto-circuito

Quando uma descarga atinge um inimigo congelado, ele libera uma pequena explosão elétrica.

---

## detonation — Reator de sucata

**Estado:** excelente.

É exatamente o tipo de efeito que transforma grupos de inimigos em reação em cadeia.

Funciona muito bem como "finalizador" de uma build.

Combina com:

- Conductor;
- Ignite;
- Frost;
- WideVolley;
- LongRain;
- ComboNova;
- Execution.

### Combinação ideal

```text
Muitos hits
    ↓
Primeira morte
    ↓
Detonation
    ↓
Morte secundária
    ↓
Nova Detonation
    ↓
Conductor / Resonance
```

Esse é um dos pilares mais importantes do sistema.

---

## reactor — Combustível instável

**Estado:** bom, mas depende de Ignite.

Sua função como multiplicador de uma build de fogo é clara.

Pode continuar existindo como peça de especialização.

### Sugestão

Permitir que ele também interaja futuramente com outras fontes térmicas ou explosivas.

Exemplo:

> Explosões contra inimigos queimando também amplificam a queimadura atual.

---

## resonance — Ressonância térmica

**Estado:** excelente como payoff de build elemental.

Ela incentiva explicitamente:

```text
Fogo + gelo + efeitos secundários
```

Em vez de obrigar o jogador a escolher somente um elemento.

Isso é muito positivo.

O jogo deve evitar a regra tradicional:

> fogo e gelo são builds separadas.

Em Tech-Guy, misturar os dois pode ser justamente a parte divertida.

---

## power — Núcleo de força

**Estado:** útil como combustível.

Não precisa ser alterado.

É uma escolha simples que melhora qualquer run.

O cuidado principal é apenas não permitir que o pool fique saturado de opções similares.

---

## haste — Mãos velozes

**Estado:** bom.

Velocidade de ataque é especialmente interessante porque indiretamente fortalece:

- ComboNova;
- Ignite;
- Frost;
- crit procs;
- efeitos on-hit futuros.

Ela já possui potencial de sinergia.

---

## recharge — Fluxo arcano

**Estado:** bom como combustível.

Cooldown reduction pode se tornar extremamente interessante quando a build depende de habilidades específicas.

### Sinergias futuras interessantes

#### Loop de execução

Críticos reduzem cooldown.

#### Overflow

Usar uma habilidade imediatamente após sair de cooldown aumenta seu efeito.

#### Cache quente

Habilidades utilizadas repetidamente dentro de poucos segundos recebem bônus progressivo.

---

## vitality — Coração de ferro

**Estado:** útil, mas pouco expressivo.

Pode continuar existindo como opção de sobrevivência.

Caso se queira torná-lo mais interessante futuramente:

### Coração redundante

Ao cair abaixo de determinada vida, recebe um escudo.

### Recovery Protocol

Ao matar um inimigo raro ou elite, recupera parte da vida máxima.

---

## focus — Foco eficiente

**Estado:** muito bom.

É mais interessante que Recharge porque possui identidade específica:

> "essa run está girando ao redor do Q."

Esse tipo de especialização deve aparecer mais vezes.

---

## crit — Olho preciso

**Estado:** excelente combustível.

Crítico fica muito mais interessante quando existirem efeitos que dependam dele.

### Modificadores futuros

#### Fragmentação crítica

Críticos geram pequenos projéteis.

#### Kernel Panic

Críticos contra inimigos congelados explodem o alvo.

#### Overclock crítico

Críticos aumentam temporariamente attack speed.

---

## brutal — Golpe brutal

**Estado:** o modificador atual menos interessante em termos de fantasia.

O problema não é balanceamento.

O problema é:

> o jogador dificilmente percebe que algo novo aconteceu.

### Sugestão

Transformá-lo em um efeito de impacto.

#### Golpe brutal

A cada terceiro acerto direto contra o mesmo inimigo, causa um segundo impacto.

ou:

#### Ruptura

Acertos pesados provocam uma pequena onda atrás do alvo.

Isso mantém a ideia de "dano bruto", mas torna o efeito perceptível.

---

## bulwark — Placa reforçada

**Estado:** funcional, porém passivo demais.

Pode permanecer como opção defensiva simples.

Uma versão mais divertida seria:

### Blindagem reativa

Após receber dano, libera uma onda que empurra inimigos próximos.

ou:

### Placa cinética

Bloquear ou absorver dano carrega o próximo ataque.

Assim defesa passa a participar da ofensiva.

---

## swift — Passo veloz

**Estado:** bom.

Movimento é perceptível e divertido.

Além disso, abre espaço para uma build cinética.

### Modificador futuro

#### Energia cinética

Dano aumenta conforme a velocidade de movimento.

### Sinergia

```text
Swift
+
RocketAdvance
+
Energia cinética
=
personagem-míssil
```

---

## ignite — Lâmina incandescente

**Estado:** conceito excelente.

O nome pode ser mais genérico, porque "Lâmina" não combina igualmente com todas as armas.

Alternativas:

- Código Incandescente;
- Sobrecarga Térmica;
- Núcleo Incandescente;
- Pacote Incendiário.

O importante é que Ignite funcione como uma **chave de sinergia**, permitindo que vários outros modificadores passem a interagir com fogo.

---

## frost — Toque glacial

**Estado:** excelente chave de sinergia.

Idealmente deve permitir combinações com Ignite em vez de competir com ele.

### Reação sugerida

#### Choque térmico

Aplicar fogo em um inimigo congelado provoca uma explosão térmica.

ou:

Aplicar congelamento em um inimigo queimando provoca dano instantâneo baseado na queimadura restante.

Isso cria uma build:

```text
Ignite
+
Frost
+
Resonance
+
Detonation
=
Thermal Chain Reaction
```

---

## transform — Disparo prismático / Nova de impacto

**Estado:** excelente.

É provavelmente um dos modificadores mais interessantes do catálogo.

Ele parece pertencer a uma categoria especial:

# REWRITE

Um Rewrite altera fundamentalmente uma habilidade.

Exemplos de apresentação:

```text
[ REWRITE AVAILABLE ]

Q -> DISPARO PRISMÁTICO
```

ou:

```text
ABILITY OVERRIDE DETECTED
```

Esse tipo de recompensa pode receber uma apresentação mais rara e dramática.

---

# 5. Arco

O arco já é a família com maior potencial de criar builds visualmente absurdas.

Sua fantasia principal pode ser:

> **multiplicar, redirecionar e espalhar projéteis.**

---

## TwinShot — Corda tripla

**Estado:** excelente.

Não reduzir por medo de ficar forte.

Esse modificador é exatamente o tipo de escolha que faz uma run começar a sair do controle.

### Sinergias

#### TwinShot + Homing

Cria um enxame de flechas.

#### TwinShot + Ricochet

Cada projétil cria sua própria cadeia de saltos.

#### TwinShot + Piercing

A tela passa a ser atravessada por múltiplas linhas de projéteis.

#### TwinShot + Conductor

O número de oportunidades para gerar descargas cresce drasticamente.

#### TwinShot + Ignite/Frost

Transforma o arco em um aplicador de status em massa.

---

## Piercing — Agulhas espectrais

**Estado:** excelente.

Piercing é ainda melhor quando outros modificadores transformam o que ocorre **depois** de atravessar um inimigo.

### Nova sinergia sugerida

#### Piercing + Ricochet

Depois de perfurar todos os inimigos possíveis, a flecha começa a ricochetear.

ou:

Cada inimigo perfurado adiciona +1 ricochete potencial.

---

## Ricochet — Flecha saltadora

**Estado:** excelente.

É um dos efeitos mais importantes para construir caos controlado.

### Interação sugerida

Ricochetes podem procurar inimigos que ainda não foram atingidos antes de repetir alvos.

---

## Homing — Olho caçador

**Estado:** excelente.

Homing transforma várias escolhas medianas em combinações muito melhores.

### Homing + WideVolley

As flechas começam abertas, mas curvam para diferentes inimigos.

### Homing + TwinShot

Enxame.

### Homing + Ricochet

Os saltos deixam de parecer aleatórios e começam a "caçar" grupos.

---

## HeavyBolt — Balista portátil

**Estado:** bom.

É um modificador de especialização.

Ele faz W ganhar identidade como golpe pesado.

### Possível evolução

#### Railgun

W atravessa todos os inimigos e ganha dano por inimigo atravessado.

---

## RapidBurst — Tambor de disparos

**Estado:** excelente.

Ataques repetidos criam muitas oportunidades de interação.

Combina particularmente com:

- Ignite;
- Frost;
- Conductor;
- Crit;
- Ricochet.

---

## WideVolley — Pavão de aço

**Estado:** excelente.

É uma das melhores ferramentas visuais da arma.

### Combinação importante

```text
WideVolley
+
TwinShot
+
Homing
=
Swarm
```

O ataque começa parecendo um shotgun e termina parecendo uma chuva de mísseis.

---

## LongRain — Monção

**Estado:** excelente.

Transforma R numa habilidade de controle territorial.

---

## GuidedRain — Nuvem obediente

**Estado:** excelente.

É uma alteração clara de comportamento.

### Sinergia

`GuidedRain + LongRain`

faz a habilidade deixar de ser "área colocada" e virar uma tempestade móvel.

---

## Sniper — Horizonte mortal

**Estado:** bom.

Diferente de Power porque influencia posicionamento.

Isso é importante.

---

# 6. Novos modificadores sugeridos para arco

## ReturningShot — Pacote de retorno

Projéteis retornam ao jogador depois de atingir o limite de alcance.

Eles podem causar dano novamente no retorno.

### Combinações

- Piercing;
- Ricochet;
- Homing;
- TwinShot.

Uma única flecha pode:

```text
sair
→ perfurar
→ ricochetear
→ retornar
→ atravessar novamente
```

---

## SplitShot — Fragmentação

Ao atingir um inimigo, a flecha se divide em pequenos projéteis.

Combina fortemente com:

- Piercing;
- Ricochet;
- Ignite;
- Frost.

---

## DeadTarget — Alvo compilado

Acertar repetidamente o mesmo inimigo marca o alvo.

Projéteis próximos passam a priorizar esse inimigo.

Excelente para bossing e Homing.

---

## OrbitingArrows — Buffer orbital

Projéteis que não encontram alvo ficam orbitando o jogador por alguns segundos.

Se um inimigo entrar no alcance, eles disparam automaticamente.

Potencialmente extremamente divertido com TwinShot.

---

# 7. Lança

A lança atualmente possui boa base, mas menos modificadores transformativos do que arco e manoplas.

A fantasia sugerida:

> **alcance, zona ideal, ecos, ondas e movimentos orbitais.**

A lança deve parecer uma arma capaz de transformar espaço ao redor do jogador.

---

## LongReach — Haste impossível

**Estado:** bom.

É simples, mas extremamente perceptível.

Além disso, amplifica outras modificações espaciais.

---

## EchoThrust — Estocada ecoante

**Estado:** excelente.

Repetir ataques é sempre muito forte em termos de diversão.

Não há necessidade de evitar exagero enquanto o efeito for legível.

---

## TripleMoon — Órbita das luas

**Estado:** excelente.

É exatamente o tipo de modificador que pode transformar W em uma máquina absurda.

Combina com:

- Orbit;
- LongReach;
- Affliction;
- Ignite;
- Frost.

---

## Trident — Tridente espiral

**Estado:** excelente.

Muda claramente o formato da habilidade.

### Possível sinergia

`Trident + EchoThrust`

cada direção ganha seus próprios ecos.

---

## DragonWave — Dragão liberto

**Estado:** excelente.

Adicionar uma onda transforma R em uma habilidade híbrida melee/ranged.

---

## Affliction — Ponta contaminada

**Estado:** bom como payoff.

Ele cria incentivo para builds elementais.

---

## SpearTip — Distância perfeita

**Estado:** conceitualmente muito bom.

É um dos melhores modificadores de identidade de arma porque incentiva spacing.

Idealmente deve premiar uma **zona ideal de contato**, e não apenas distância mínima.

---

## Execution — Carrasco

**Estado:** útil, mas pouco transformativo.

Pode permanecer como payoff.

### Possível upgrade futuro

#### Execução em cadeia

Matar um inimigo com Carrasco projeta uma estocada em direção ao inimigo próximo.

Isso torna Execute parte do caos da build.

---

## Orbit — Lua viajante

**Estado:** excelente.

É um dos principais modificadores da lança.

`TripleMoon + Orbit` já forma um núcleo de build.

---

## Siphon — Condutor vital

**Estado:** bom.

Ele cria economia de recurso e permite builds de alta frequência.

É uma boa peça de infraestrutura.

---

# 8. Novos modificadores sugeridos para lança

## PhantomSpear — Lança fantasma

Toda estocada deixa uma cópia espectral.

Após um pequeno atraso, a cópia repete a estocada.

### Sinergias

- EchoThrust;
- Trident;
- LongReach;
- Affliction.

Em uma build forte, o campo pode ficar cheio de ataques atrasados.

---

## ReturnWave — Retorno de pacote

Ondas lançadas pela lança retornam para o jogador depois de atingir o limite de alcance.

### Sinergias

- DragonWave;
- Trident;
- LongReach.

---

## MoonShard — Lua partida

Cada varredura de W lança projéteis a partir de suas extremidades.

### Sinergia central

```text
TripleMoon
+
Orbit
+
MoonShard
+
LongReach
```

Resultado:

> o jogador vira o centro de um sistema orbital de ataques.

---

## Impale — Ponteiro inválido

Inimigos atingidos pela ponta ficam "marcados".

A próxima habilidade que os atinge gera uma estocada fantasma adicional.

Combina muito bem com SpearTip.

---

## ChainThrust — Encadeamento

Atingir um inimigo com uma estocada cria uma pequena estocada automática em direção a outro inimigo próximo.

É uma versão própria do Conductor para a identidade física da lança.

---

# 9. Manoplas

As manoplas possuem uma identidade muito boa.

Fantasia principal:

> **agressividade, movimento, combo, postura e geração de Asura.**

É a arma que mais deve recompensar ficar constantemente em cima do inimigo.

---

## LongFists — Punhos titânicos

**Estado:** bom.

Mais alcance em uma arma corpo a corpo é muito perceptível.

---

## RocketAdvance — Propulsor de combate

**Estado:** excelente.

Não é necessário ter medo de a habilidade ficar exagerada.

Na filosofia atual, atravessar metade da arena pode ser justamente a graça.

### Sinergia futura

Movimento pode causar dano ou gerar efeitos.

---

## FlurryEcho — Mil punhos

**Estado:** excelente.

É um modificador perfeito para a fantasia das manoplas.

---

## ShockRing — Epicentro

**Estado:** excelente.

Transforma o formato espacial de uma habilidade.

---

## AsuraEcho — Asura reverberante

**Estado:** excelente.

É o payoff natural da build de Asura.

---

## Momentum — Dínamo de combate

**Estado:** excelente infraestrutura.

Ajuda a fechar o loop:

```text
habilidades
→ Asura
→ R
→ AsuraEcho
→ mais impacto
```

---

## AsuraReserve — Reserva divina

**Estado:** muito bom.

Evita que a build "desligue" depois do ultimate.

---

## StanceCrusher — Demolidor

**Estado:** muito bom.

Ajuda a dar identidade específica para manoplas:

> esta arma destrói postura.

---

## ComboNova — Terceiro impacto

**Estado:** excelente.

Um dos melhores modificadores das manoplas.

Especialmente porque escala naturalmente com:

- Haste;
- Flurry;
- Crit;
- Ignite;
- Frost;
- Conductor.

---

## Berserker — Motor em pane

**Estado:** bom.

É mecanicamente interessante porque muda o comportamento do jogador.

Pode gerar runs em que o jogador deliberadamente aceita jogar com pouca vida.

---

# 10. Novos modificadores sugeridos para manoplas

## Afterimage — Processo fantasma

Movimentos rápidos deixam uma cópia do jogador.

Após um pequeno atraso, a cópia repete o último golpe.

Combina fortemente com:

- RocketAdvance;
- FlurryEcho;
- ComboNova.

---

## ImpactDrive — Driver de impacto

Mover-se rapidamente por certa distância carrega o próximo ataque.

### Build

```text
Swift
+
RocketAdvance
+
ImpactDrive
```

O jogador vira literalmente um projétil.

---

## ComboOverflow — Overflow de combo

Continuar acertando inimigos sem ficar muito tempo sem atacar aumenta progressivamente o tamanho dos impactos.

Quando o combo termina, o bônus desaparece.

Cria uma fantasia agressiva sem precisar de contador complexo.

---

## AsuraLeak — Vazamento de Asura

Enquanto estiver acima de determinada quantidade de Asura, pequenos pulsos de energia são liberados ao redor do jogador.

Isso transforma recurso acumulado em presença visual.

---

## GroundLoop — Loop de impacto

Golpes que lançam inimigos ao ar deixam uma área no chão.

Quando o inimigo cai nessa área, recebe uma segunda explosão.

Conversa diretamente com o sistema de air juggle.

---

# 11. Sinergias elementais

Elementos não devem obrigatoriamente representar builds separadas.

Uma direção mais interessante é:

> elementos se combinam para criar reações.

---

## Fogo + Gelo — Choque térmico

Aplicar fogo em inimigo congelado:

- causa uma explosão;
- remove ou reduz o congelamento;
- espalha parte dos efeitos para inimigos próximos.

---

## Gelo + Conductor — Supercondutor

Descargas contra inimigos congelados:

- ganham alcance;
- podem atingir mais alvos;
- ou criam estilhaços.

---

## Fogo + Detonation — Combustão

Inimigos queimando produzem explosões maiores quando morrem.

---

## Frost + Detonation — Shatter

Inimigos congelados explodem em fragmentos quando morrem.

---

## Ignite + Frost + Resonance

Essa combinação pode funcionar como um dos primeiros arquétipos deliberadamente "quebrados":

```text
Acerto
 ↓
Ignite
 ↓
Frost
 ↓
Thermal Shock
 ↓
Resonance
 ↓
Morte
 ↓
Detonation
 ↓
Nova aplicação
```

---

# 12. Sinergias emergentes recomendadas

O sistema deve permitir que efeitos existentes interajam mesmo quando não existe uma recompensa explicitamente chamada "sinergia".

---

## Arco — Swarm Protocol

```text
TwinShot
+
WideVolley
+
Homing
+
Ricochet
```

Resultado:

dezenas de projéteis procurando alvos diferentes.

---

## Arco — Packet Storm

```text
RapidBurst
+
TwinShot
+
Conductor
+
Frost
```

Resultado:

grande quantidade de hits disparando descargas e congelamentos.

---

## Arco — Recursive Projectile

```text
Piercing
+
Ricochet
+
ReturningShot
+
SplitShot
```

Uma flecha pode atravessar, ricochetear, dividir e voltar.

Esse tipo de comportamento é extremamente alinhado com Binding of Isaac.

---

## Lança — Lunar Engine

```text
TripleMoon
+
Orbit
+
LongReach
+
MoonShard
```

O jogador cria uma grande zona orbital ao redor de si.

---

## Lança — Ghost Compiler

```text
EchoThrust
+
PhantomSpear
+
Trident
+
DragonWave
```

Cada ataque começa a gerar múltiplas versões temporais ou espaciais dele mesmo.

---

## Lança — Perfect Distance

```text
SpearTip
+
LongReach
+
Impale
+
Affliction
```

Build baseada em manter constantemente a distância ideal.

---

## Manoplas — Infinite Combo

```text
Haste
+
FlurryEcho
+
ComboNova
+
Momentum
+
AsuraReserve
```

Loop:

```text
básicos
→ ComboNova
→ habilidade
→ Asura
→ R
→ AsuraEcho
→ continuar combo
```

---

## Manoplas — Human Missile

```text
Swift
+
RocketAdvance
+
ImpactDrive
+
ShockRing
```

Mover-se passa a ser parte do dano.

---

## Manoplas — Stand Rush

```text
FlurryEcho
+
Afterimage
+
ComboNova
+
Haste
```

Ataques começam a produzir ataques atrasados e novas simultaneamente.

---

# 13. Categorias especiais de recompensa

Além das recompensas normais, pode ser interessante adicionar categorias diferentes.

---

## Rewrite

Muda fundamentalmente uma habilidade.

Exemplos:

- Q vira nova;
- flecha vira laser;
- W passa a orbitar;
- R muda de golpe único para sequência.

Deve ser raro e visualmente destacado.

---

## Exploit

Modificador poderoso com comportamento estranho.

Exemplo:

> Projéteis podem atingir o mesmo inimigo novamente após ricochetear em outro alvo.

---

## Overflow

Modificadores que melhoram conforme o jogador ultrapassa valores esperados.

Exemplo:

> Attack Speed acima de determinado valor começa a gerar golpes fantasma.

---

## Fork

Duplica uma ação.

Exemplo:

> Após usar Q, uma cópia da habilidade é disparada novamente em direção diferente.

---

## Hook

Responde a um evento.

Exemplos:

- OnCrit;
- OnKill;
- OnFreeze;
- OnDash;
- OnExplosion;
- OnStanceBreak.

Essa categoria pode gerar enorme quantidade de builds futuras.

---

# 14. Modificadores genéricos futuros

## Fragmentação crítica

Críticos geram três fragmentos.

---

## KillChain

Cada inimigo morto em sequência aumenta temporariamente attack speed.

---

## Recursion

Efeitos secundários possuem pequena chance de gerar outro efeito secundário.

Pode ser limitado internamente, mas deve parecer caótico.

---

## Garbage Collector

Cadáveres ou inimigos mortos são convertidos em pequenos projéteis digitais.

---

## Memory Leak

Cada habilidade usada aumenta gradualmente seu custo, mas também seu dano.

O efeito reseta após alguns segundos sem usar a habilidade.

---

## Stack Overflow

Ao acumular uma quantidade alta de um mesmo efeito, ocorre uma descarga massiva e a pilha é parcialmente consumida.

---

## Race Condition

Usar duas habilidades quase simultaneamente produz um efeito adicional.

---

## Multithreading

Habilidades possuem chance de gerar uma segunda execução paralela com atraso mínimo.

---

## Hot Reload

Ao transformar uma habilidade com Rewrite, ela fica temporariamente supercarregada.

---

## Segmentation Fault

Golpes muito fortes possuem chance de "quebrar" o inimigo, criando fragmentos que causam dano ao redor.

---

# 15. Princípio importante: evitar antissinergias acidentais

Sempre que possível, dois modificadores interessantes devem:

1. funcionar juntos;
2. produzir uma interação especial;
3. ou pelo menos não invalidar um ao outro.

Exemplo ruim:

```text
Piercing impede Ricochet de acontecer.
```

Melhor:

```text
Piercing acontece primeiro.
Após terminar as perfurações, começa o Ricochet.
```

Outro exemplo:

```text
Frost remove Ignite.
```

Melhor:

```text
Frost + Ignite = Thermal Shock.
```

O jogador deve ser recompensado por experimentar.

---

# 16. O objetivo final de uma run

No começo da run:

```text
Q lança uma flecha.
```

No final de uma run absurda:

```text
Q lança sete flechas.

Elas saem em leque.

Cada flecha procura um alvo.

Atravessa inimigos.

Depois ricocheteia.

Aplica fogo.

Aplica gelo.

Fogo + gelo provoca Thermal Shock.

Críticos soltam fragmentos.

Mortes explodem.

Explosões ativam Resonance.

Descargas saltam entre inimigos.

Projéteis retornam.

A arma reclama que isso não deveria funcionar.
```

Esse deve ser o ideal de sensação do sistema.

Não necessariamente toda run chegará nesse ponto.

Mas o jogador deve saber que:

> **é possível.**

---

# 17. Prioridades sugeridas de implementação

Sem considerar balanceamento, a ordem mais valiosa em termos de diversão seria:

## Prioridade 1 — Interações entre modificadores existentes

Implementar regras claras para:

- Piercing + Ricochet;
- Ignite + Frost;
- Detonation + elementos;
- Homing + WideVolley;
- TripleMoon + Orbit;
- Haste + ComboNova.

Essas combinações já usam conteúdo existente.

---

## Prioridade 2 — Mais comportamento para lança

Adicionar 2–4 opções como:

- PhantomSpear;
- MoonShard;
- ReturnWave;
- ChainThrust.

Hoje o arco possui mais possibilidades de ficar visualmente absurdo.

---

## Prioridade 3 — Hooks genéricos

Criar infraestrutura para:

- OnCrit;
- OnKill;
- OnFreeze;
- OnBurn;
- OnStanceBreak;
- OnDash;
- OnExplosion.

Isso permitirá criar muitos modificadores novos com pouco código específico.

---

## Prioridade 4 — Categoria Rewrite

Separar modificadores que realmente reescrevem uma habilidade.

Isso ajuda a criar escolhas raras, memoráveis e visualmente distintas.

---

## Prioridade 5 — Feedback de "sistema quebrando"

Adicionar gradualmente:

- glitch visual;
- comentários das armas;
- mensagens de sistema;
- efeitos mais exagerados;
- avisos falsos;
- pequenos bugs visuais deliberados.

Quanto mais absurda a build fica, mais o próprio universo deve parecer reagir ao jogador.

---

# 18. Resumo

A direção recomendada não é reduzir os modificadores absurdos.

É fazer o contrário:

> **aumentar o número de maneiras pelas quais eles podem conversar entre si.**

O catálogo atual já possui uma base forte.

Os melhores elementos atualmente são:

- TwinShot;
- Ricochet;
- Homing;
- Conductor;
- Detonation;
- Resonance;
- TripleMoon;
- Orbit;
- FlurryEcho;
- ComboNova;
- AsuraEcho;
- Transform.

Eles devem servir como referência para futuros modificadores.

Os modificadores puramente numéricos continuam úteis, mas devem funcionar principalmente como combustível para sistemas mais expressivos.

O objetivo do sistema deve ser permitir que o jogador transforme:

```text
um ataque simples
```

em:

```text
uma cadeia completamente absurda de eventos
que só existe porque aquela combinação específica
de modificadores apareceu naquela run.
```

Esse é o momento em que **Tech-Guy** deixa de parecer apenas um ARPG com upgrades e passa a ter sua própria identidade de roguelike.
