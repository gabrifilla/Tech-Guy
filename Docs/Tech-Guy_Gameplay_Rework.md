# Tech-Guy — Gameplay Rework

> Documento de reestruturação de gameplay, combate, progressão de run e sistema de boons.
>
> Baseado no estado atual descrito em `README.md` e no catálogo atual de `Boons.md`.

---

## 1. Objetivo deste documento

Este documento define uma proposta de reestruturação da gameplay de **Tech-Guy** sem descartar os sistemas já implementados. O objetivo é transformar o protótipo atual em uma base de combate mais clara, responsiva e escalável para um action roguelite inspirado em **Lost Ark**, **Diablo**, **Hades**, **Binding of Isaac** e **Dead Cells**.

O problema central não é falta de conteúdo. O projeto já possui:

- três armas jogáveis;
- quatro habilidades por arma;
- sistema de dash;
- mana;
- energia Asura;
- postura/stagger;
- inimigos normais, raros e boss;
- afixos;
- 41 boons de arma oferecíveis;
- 10 bênçãos universais;
- sinergias elementais;
- transformações de habilidades;
- sistema de salas e recompensas.

O que falta é uma **hierarquia de design** que determine claramente:

1. o que o jogador faz a cada segundo;
2. por que cada arma existe;
3. que decisão cada boon cria;
4. como uma run evolui;
5. quais sistemas são fundamentais e quais são apenas multiplicadores.

A principal meta desta reestruturação é:

> **O combate deve ser divertido antes dos boons. Os boons devem transformar um combate já divertido em algo progressivamente mais poderoso, estranho e expressivo.**

---

# 2. Nova identidade de gameplay

Tech-Guy não deve tentar reproduzir igualmente todos os jogos que o inspiram. Cada referência deve resolver uma camada diferente do design.

| Referência | Papel dentro de Tech-Guy |
| --- | --- |
| Lost Ark | Feeling do combate, habilidades direcionais, stagger, compromisso de animação e controle de grupos |
| Diablo | Densidade de inimigos, elites, afixos, power fantasy e leitura de hordas |
| Hades | Estrutura da run, escolhas de recompensa, rotas e construção gradual da build |
| Binding of Isaac | Combinações multiplicativas, interações emergentes e builds absurdas |
| Dead Cells | Armas que mudam a forma de pensar, posicionar e jogar |

A nova definição mecânica sugerida para o jogo é:

> **Tech-Guy é um action roguelite de combate contra hordas em que cada arma possui uma linguagem de combate própria e cada run distorce essa linguagem até criar uma versão exagerada dela.**

Isso gera três níveis de identidade:

```text
ARMA
↓
COMO EU JOGO
↓
BOONS DE ARMA
↓
QUE BUILD EU ESTOU CONSTRUINDO
↓
PROTOCOLOS / SINERGIAS
↓
COMO ESSA BUILD QUEBRA AS REGRAS
```

---

# 3. Prioridade zero — corrigir o núcleo do combate

Antes de criar novas armas, novos boons ou novas áreas, o núcleo do combate deve ser revisado.

Atualmente existem dois comportamentos opostos:

- ataques básicos possuem bastante automação;
- habilidades retiram quase todo o controle durante sua execução.

Isso pode gerar uma sensação de combate ao mesmo tempo automático e rígido.

A primeira grande mudança deve atacar exatamente essa diferença.

---

## 3.1. Remover o comportamento de auto-combate

### Estado atual

Ao clicar em um inimigo, o personagem:

1. seleciona o alvo;
2. aproxima-se automaticamente;
3. começa a atacar quando entra no alcance.

Esse comportamento aproxima o sistema de um ARPG tradicional baseado em target acquisition.

### Problema

Para o tipo de combate que Tech-Guy busca, isso reduz a participação do jogador no momento-a-momento.

O ataque básico deveria ser uma **ação**, não uma ordem persistente.

### Novo comportamento recomendado

#### Clique rápido no inimigo

```text
Clique
↓
Mover até alcance
↓
Executar UM ataque
↓
Fim da ordem
```

#### Segurar ataque

```text
Segurar
↓
Mover até alcance
↓
Ataque
↓
Ataque
↓
Ataque
↓
Soltar botão
↓
Parar
```

O sistema ainda pode manter um alvo internamente para:

- orientação;
- tracking;
- pequenos ajustes de direção;
- assistência de mira;
- escolha de animação.

Porém o personagem não deve continuar lutando sem input ativo.

### Resultado esperado

O jogador passa a sentir que:

- está executando cada ataque;
- controla quando começa e termina uma sequência;
- pode reagir rapidamente a ameaças;
- o básico participa do ritmo do combate.

### Prioridade

**CRÍTICA**

---

# 4. Reestruturar o sistema de animação e compromisso

## 4.1. Problema do hard lock atual

Durante uma habilidade, atualmente podem ficar bloqueados:

- movimento;
- ataque básico;
- dash;
- outras habilidades.

Esse modelo simplifica a implementação, mas produz uma sensação excessivamente rígida.

O objetivo não deve ser remover commitment. Commitment é importante para dar peso aos ataques.

O objetivo é torná-lo **intencional e específico por habilidade**.

---

## 4.2. Todas as ações ofensivas devem possuir fases

Cada ataque ou habilidade deve ser dividido conceitualmente em:

```text
STARTUP
↓
ACTIVE
↓
RECOVERY
```

### Startup

Preparação do ataque.

Pode incluir:

- antecipação;
- wind-up;
- deslocamento inicial;
- telegraph visual do próprio jogador.

### Active

Momento em que o ataque possui hitbox/projétil/efeito.

É normalmente a parte de maior compromisso.

### Recovery

Animação posterior ao impacto.

É aqui que o jogo deve permitir parte dos cancels.

---

# 5. Criar categorias de compromisso de habilidade

Cada habilidade deve possuir uma categoria explícita.

## 5.1. Fluid

Habilidades rápidas e responsivas.

Características:

- recovery curto;
- movimento parcial permitido;
- cancel relativamente cedo;
- baixo risco individual.

Exemplo possível:

- pequenos disparos do arco;
- ataques básicos rápidos;
- golpes rápidos de combo.

---

## 5.2. Committed

Ataques fortes em que o jogador aceita ficar vulnerável.

Características:

- startup legível;
- active forte;
- janela de cancel limitada;
- recompensa proporcional ao risco.

Exemplo possível:

- golpe pesado da lança;
- segundo impacto de Impacto de Choque;
- tiro carregado.

---

## 5.3. Channel

Ações sustentadas.

Características:

- continuam enquanto a sequência está ativa;
- o jogador perde parte da mobilidade;
- dash pode cancelar;
- cancel deve possuir custo ou oportunidade perdida.

Exemplo possível:

- Rajada Asura;
- futuros lasers;
- habilidades contínuas.

---

# 6. Implementar Cancel Windows

Não permitir cancel a qualquer momento.

Cada habilidade deve definir explicitamente:

```text
canCancelIntoDash
canCancelIntoBasic
canCancelIntoSkill
cancelWindowStart
cancelWindowEnd
```

Exemplo conceitual:

```text
Punhos Relâmpago

0% ------------------------------ 100%
     STARTUP   ACTIVE     RECOVERY

Dash:                    [======]
Skill:                       [===]
Basic:                    [======]
```

Isso mantém o peso da animação sem transformar habilidades em longos períodos de falta de controle.

---

# 7. Implementar Input Buffer

O combate precisa aceitar comandos realizados pouco antes da ação atual terminar.

Sugestão inicial:

```text
Input Buffer: 100–180 ms
```

O valor final deve ser validado em playtest.

O sistema deve armazenar temporariamente:

- ataque básico;
- dash;
- Q;
- W;
- E;
- R.

Exemplo:

```text
W ainda está terminando
↓
jogador aperta Q
↓
Q entra no buffer
↓
abre a janela de cancel
↓
Q executa imediatamente
```

Isso ajuda a produzir um combate muito mais fluido sem necessariamente acelerar todas as animações.

### Prioridade

**CRÍTICA**

---

# 8. Revisar Mana como sistema universal

## 8.1. Estado atual

O jogador possui mana máxima, regeneração e custos para habilidades.

Na manopla ainda existe um segundo recurso: **Energia Asura**.

Isso cria múltiplas limitações simultâneas:

```text
Cooldown
+
Mana
+
Energia especial
```

---

## 8.2. Proposta para o protótipo

Remover temporariamente mana como recurso universal.

Usar:

```text
Q / W / E
↓
Cooldown

R
↓
Recurso / regra específica da arma
```

Isso deixa cada arma livre para possuir sua própria economia.

### Manopla

```text
Pressão ofensiva
↓
Energia Asura
↓
Rajada Asura
```

### Arco

Pode inicialmente não possuir recurso secundário.

### Lança

Pode inicialmente não possuir recurso secundário.

### Futuras armas

Uma arma específica pode usar:

- mana;
- heat;
- ammunition;
- charge;
- corruption;
- combo meter;
- energia;
- cooldown interno.

O recurso passa a ser parte da identidade da arma e não um imposto universal.

### Observação

A remoção inicial de mana não precisa ser definitiva.

Ela deve ser tratada como um **experimento de design**.

Se os playtests mostrarem que habilidades ficam excessivamente disponíveis, primeiro testar:

1. cooldowns;
2. cargas;
3. geração de recurso específica da arma;
4. resource gating contextual.

Somente depois reavaliar uma mana universal.

### Prioridade

**ALTA**

---

# 9. Transformar Posture/Stagger em pilar do combate

O sistema de postura já existe, mas deve deixar de ser uma propriedade secundária e virar uma das regras fundamentais de combate.

---

## 9.1. Modelo recomendado

```text
HIT
↓
STAGGER
↓
POSTURE DAMAGE
↓
STANCE BREAK
↓
REAÇÃO DEPENDENTE DO TIPO DE INIMIGO
```

---

## 9.2. Reações por categoria

| Categoria | Hit comum | Golpe forte | Stance Break |
| --- | --- | --- | --- |
| Mob pequeno | Stagger frequente | Interrupção | Launch / Knockdown |
| Mob médio | Stagger parcial | Interrupção situacional | Knockdown |
| Elite | Pouco stagger | Resistência elevada | Vulnerable / Stagger window |
| Boss | Sem interrupção comum | Sem interrupção | Stagger window |

---

## 9.3. Stagger não deve significar Knockback

Separar claramente:

```text
Stagger
Knockback
Knockdown
Launch
Pull
Stance Break
```

Cada um deve ser um estado diferente.

### Stagger

Interrompe brevemente a ação.

### Knockback

Move o inimigo horizontalmente.

### Knockdown

Derruba o inimigo.

### Launch

Joga o inimigo para o ar.

### Pull

Reposiciona o inimigo em direção a uma origem.

### Stance Break

Evento de quebra da resistência do inimigo e não um movimento por si só.

A reação decorrente do stance break depende da categoria do alvo.

---

# 10. Dar feedback forte ao Stance Break

Quando a postura quebra, utilizar múltiplos canais de feedback:

- flash;
- som específico;
- hit-stop curto;
- alteração de VFX;
- animação distinta;
- alteração da barra de postura;
- ícone de vulnerabilidade em elites/bosses;
- reação física em mobs comuns.

O jogador deve perceber a quebra sem precisar olhar diretamente para uma barra.

### Prioridade

**CRÍTICA**

---

# 11. Redefinir as três armas por comportamento

Cada arma deve responder à pergunta:

> **O que um jogador bom desta arma faz durante o combate?**

Se a resposta for apenas “usa skills diferentes”, a arma ainda não possui identidade suficiente.

---

# 12. Manopla — identidade: PRESSÃO

## 12.1. Fantasia

A manopla representa combate agressivo e de curta distância.

O jogador deve querer:

- entrar no grupo;
- manter contato;
- alternar golpes;
- interromper inimigos;
- quebrar postura;
- continuar atacando;
- converter pressão em Asura.

Loop:

```text
ENTRAR
↓
PRESSIONAR
↓
STAGGER
↓
STANCE BREAK
↓
GERAR ASURA
↓
R
↓
REINICIAR PRESSÃO
```

---

## 12.2. Regra principal

**Manter ofensiva deve ser recompensado.**

Sugestões sistêmicas:

- ataques consecutivos aumentam momentum;
- sofrer um golpe pode reduzir momentum;
- alternar categorias de golpe aumenta Asura;
- quebrar postura concede grande quantidade de Asura;
- perfect dodge agressivo pode manter/completar combo;
- alguns ataques podem avançar levemente para manter contato.

---

## 12.3. Papel dos boons atuais

Os boons atuais que mais reforçam essa identidade incluem:

- Momentum;
- Asura Reserve;
- Asura Fist;
- Guard Breaker;
- Hungry Combo;
- Momentum Strike;
- Flurry Echo;
- Shock Ring;
- Asura Echo.

Esses efeitos devem virar o centro da família de boons da arma.

---

# 13. Arco — identidade: REPOSICIONAMENTO

## 13.1. Fantasia

O arco não deve ser simplesmente a arma que ataca de longe.

Sua regra deve ser:

> **Posicionamento gera eficiência.**

Loop:

```text
ATIRAR
↓
REPOSICIONAR
↓
CRIAR DISTÂNCIA
↓
ENCONTRAR LINHA / ÂNGULO
↓
ATIRAR NOVAMENTE
```

---

## 13.2. Sistemas que reforçam essa ideia

- bônus por distância;
- ataques que empurram/recuam o jogador;
- piercing;
- ricochet;
- ataques carregados;
- habilidades que marcam zonas;
- maior recompensa por alinhar grupos;
- mobilidade após ataques.

Boons atuais já alinhados:

- Kiting Step;
- Adaptive Cadence;
- Charged Shot;
- Rain Mark;
- Piercing;
- Ricochet;
- Split Arrow;
- Guided Rain.

---

## 13.3. Objetivo

Um jogador experiente de arco deve olhar para uma horda e pensar:

```text
"Onde eu preciso ficar para esse disparo atingir o maior número possível de inimigos?"
```

Não apenas:

```text
"Qual habilidade está fora do cooldown?"
```

---

# 14. Lança — identidade: ESPAÇAMENTO

## 14.1. Fantasia

A lança deve possuir uma faixa de distância ideal.

O jogador não quer ficar:

- colado demais;
- distante demais.

Ele quer manter o inimigo no alcance da ponta da lança.

```text
PLAYER -------- SWEET SPOT -- ENEMY
```

---

## 14.2. Sweet Spot como regra nativa

A distância ideal não deve existir apenas através de boons.

Ela deve fazer parte da arma base.

Exemplo:

```text
Hit normal = 100%
Sweet Spot = 115% + posture bonus
```

Valores exatos devem ser definidos posteriormente.

---

## 14.3. Feedback visual

Quando o alvo estiver na faixa ideal:

- ponta da lança pode brilhar;
- retícula pode mudar;
- trail pode se intensificar;
- hit effect pode ser diferente;
- som pode possuir um layer adicional.

O jogador deve aprender a distância através da sensação e não de um número na HUD.

---

## 14.4. Boons alinhados

- SpearTip;
- PerfectSpacing;
- SpacingRecoil;
- PikeWall;
- EdgeStrike;
- ImpalingLine;
- EchoThrust;
- PhantomSpear.

Esses boons devem compor o núcleo da arma.

### Prioridade das identidades de arma

**CRÍTICA**

---

# 15. Reestruturar completamente a taxonomia dos boons

Atualmente diferentes tipos de melhoria aparecem conceitualmente juntos.

Isso dificulta:

- leitura;
- balanceamento;
- geração de ofertas;
- construção consciente de build;
- criação de conteúdo futuro.

A nova taxonomia deve possuir quatro grupos principais.

---

# 16. Categoria 1 — Mutation

## Função

Modificar profundamente uma habilidade ou ação.

Equivalente ao momento em que uma habilidade deixa de ser apenas “a mesma coisa com números maiores”.

Exemplos:

```text
Avanço Relâmpago
↓
Avanço Demolidor

Q atravessa inimigos e termina em uppercut.
```

```text
Estocada do Dragão
↓
Dragão Perfurante

Q atravessa toda a linha e arrasta inimigos para a trajetória.
```

```text
Disparo Duplo
↓
Disparo Prismático

Q dispara um leque de projéteis perfurantes.
```

### Regras recomendadas

- relativamente raras;
- preferencialmente específicas da arma;
- idealmente mudam decisões de combate;
- não devem ser simples bônus numéricos;
- podem ser mutuamente exclusivas quando modificarem o mesmo comportamento.

---

# 17. Categoria 2 — Technique

## Função

Especializar a linguagem natural da arma.

### Manopla

Exemplos:

- Guard Breaker;
- Hungry Combo;
- Momentum;
- Asura Fist;
- Flurry Echo.

### Arco

Exemplos:

- Ricochet;
- Piercing;
- Adaptive Cadence;
- Kiting Step;
- Rain Mark.

### Lança

Exemplos:

- PerfectSpacing;
- EdgeStrike;
- SpacingRecoil;
- PhantomSpear;
- ImpalingLine.

### Regra

Technique deve responder:

> **Que versão desta arma estou construindo nesta run?**

---

# 18. Categoria 3 — Protocol

## Função

Criar interações sistêmicas entre diferentes partes da build.

Os universais atuais são bons candidatos:

- Conductor;
- Detonation;
- Reactor;
- Resonance;
- Overflow;
- Ignite;
- Frost.

Esses efeitos são responsáveis pelo lado **Binding of Isaac** da build.

Exemplo:

```text
FLECHA
↓
PIERCING
↓
KILL
↓
SPLIT ARROW
↓
IGNITE
↓
DETONATION
↓
CONDUCTOR
↓
REAÇÃO EM CADEIA
```

Protocol deve criar a sensação de que dois sistemas independentes começaram a conversar entre si.

---

# 19. Categoria 4 — Utility

## Função

Resolver sobrevivência, economia e qualidade de vida.

Possíveis exemplos:

- aumento de vida;
- cura;
- escudo;
- mobilidade;
- reroll;
- recuperação;
- economia de recursos;
- redução de cooldown contextual.

Utility não deve competir em quantidade com Techniques e Mutations.

Caso contrário o jogador frequentemente recebe três escolhas pouco interessantes.

---

# 20. Remover transformações genéricas que apagam a identidade da arma

A transformação atual que substitui Q por uma explosão circular na Manopla/Lança deve ser revista.

Problema:

```text
Manopla única
↓
Q vira AoE genérica
```

ou

```text
Lança única
↓
Q vira AoE genérica
```

Isso reduz identidade.

Transformações devem preservar a fantasia original.

### Manopla

Transformações devem falar sobre:

- avanço;
- uppercut;
- barrage;
- combo;
- stance break;
- Asura.

### Arco

Transformações devem falar sobre:

- projéteis;
- trajetória;
- distância;
- marks;
- volley;
- ricochet;
- charge.

### Lança

Transformações devem falar sobre:

- linha;
- sweet spot;
- thrust;
- sweep;
- pull;
- spacing;
- spear echo.

### Prioridade

**ALTA**

---

# 21. Construir arquétipos emergentes, não classes fixas

O jogo não precisa mostrar ao jogador algo como:

```text
BUILD ASURA
BUILD PINBALL
BUILD SWEET SPOT
```

Esses nomes são ferramentas internas de design.

O jogador deve descobrir essas combinações naturalmente.

---

## 21.1. Exemplos — Manopla

### Asura Engine

```text
Momentum
+
Asura Reserve
+
Asura Fist
+
Asura Echo
```

Objetivo:

- carregar Asura rapidamente;
- usar R frequentemente;
- manter fluxo ofensivo.

### Stance Breaker

```text
Guard Breaker
+
Shock Ring
+
Seismic Fist
```

Objetivo:

- quebrar postura;
- controlar grupos;
- criar janelas de vulnerabilidade.

### Infinite Pressure

```text
Hungry Combo
+
Momentum Strike
+
Flurry Echo
```

Objetivo:

- atacar sem parar;
- reduzir cooldown através do básico;
- aumentar valor de sequências longas.

---

# 22. Exemplos — Arco

### Machine Gun

```text
Twin Shot
+
Rapid Burst
+
Wide Volley
```

### Pinball

```text
Ricochet
+
Piercing
+
Split Arrow
```

### Hunter

```text
Charged Shot
+
Adaptive Cadence
+
Rain Mark
```

### Kiting

```text
Kiting Step
+
Homing
+
Guided Rain
```

---

# 23. Exemplos — Lança

### Sweet Spot

```text
PerfectSpacing
+
EdgeStrike
+
SpacingRecoil
```

### Impaler

```text
EchoThrust
+
PhantomSpear
+
ImpalingLine
```

### Battlefield Control

```text
PikeWall
+
Orbit
+
ReturnWave
```

### Spectral Dragon

```text
PhantomSpear
+
DragonWave
+
MoonShard
```

---

# 24. Reestruturar o sistema de recompensa da run

Hoje a recompensa principal acontece depois de eliminar uma sala.

A principal mudança recomendada é permitir que o jogador saiba **qual categoria de recompensa está perseguindo antes de entrar no próximo encontro**.

---

## 24.1. Modelo de rota

```text
                    TECHNIQUE
                  /
COMBAT ----------
                  \
                    REPAIR

                         ELITE
                       /
PRÓXIMA ESCOLHA ------
                       \
                         PROTOCOL
```

O jogador decide:

- quero melhorar minha arma;
- quero procurar uma Mutation;
- preciso recuperar vida;
- quero enfrentar um elite;
- quero investir em sinergia universal.

Isso adiciona uma nova camada de decisão:

> **Não apenas qual recompensa escolher, mas qual recompensa perseguir.**

---

# 25. Não implementar procedural generation imediatamente

Não é necessário começar com salas procedurais.

Primeiro utilizar um grafo manual.

Exemplo:

```text
                  [Technique]
                 /
[Start] -- [Combat]
                 \
                  [Repair]
                     \
                     [Elite]
                        \
                       [Protocol]
                           \
                           [Boss]
```

O sistema pode ser produzido com salas fixas e conexões pré-definidas.

Procedural generation deve entrar apenas quando o loop provar ser divertido.

### Prioridade

**ALTA**

---

# 26. Transformar HP em recurso da run

A recuperação total ou quase garantida após cada encontro reduz bastante o peso das decisões.

HP deve funcionar como uma moeda invisível.

Exemplo:

```text
25% HP restante
↓
PRÓXIMA ESCOLHA
↓
Repair
OU
Elite com Mutation rara
```

Essa escolha só existe quando dano sofrido possui consequência persistente durante a run.

---

## 26.1. Alteração recomendada

Remover cura automática completa entre encontros normais.

Alternativas possíveis:

- pequenas fontes de cura ocasionais;
- recompensa Repair;
- cura após boss/intermediário;
- drops limitados;
- execução de inimigos específicos;
- perks temporários;
- fountain/evento.

Durante o tutorial inicial, cura generosa ainda pode existir.

### Prioridade

**ALTA**

---

# 27. Reestruturar o papel das salas

As salas devem ser classificadas por função.

Tipos iniciais sugeridos:

| Sala | Função |
| --- | --- |
| Combat | encontro padrão |
| Technique | oferece boon de arma |
| Protocol | oferece sinergia/universal |
| Mutation | oferece transformação importante |
| Elite | combate difícil + recompensa melhor |
| Repair | recuperação de vida/recursos |
| Event | escolha narrativa/sistêmica |
| Boss | final de setor |

Nem todas precisam estar implementadas inicialmente.

MVP recomendado:

- Combat;
- Technique;
- Protocol;
- Repair;
- Elite;
- Boss.

---

# 28. Reestruturar inimigos em função da arma do jogador

A função de um inimigo não deve ser apenas:

- melee;
- ranged;
- fast;
- tank.

Cada inimigo deve impor uma pergunta de combate.

Exemplos:

### Swarm

```text
Problema: quantidade
Resposta: AoE / stagger / posicionamento
```

### Charger

```text
Problema: invade distância
Resposta: dodge / stance break / spacing
```

### Artillery

```text
Problema: força movimentação
Resposta: fechar distância / encontrar cobertura / burst
```

### Protector

```text
Problema: protege outros inimigos
Resposta: prioridade de alvo
```

### Controller

```text
Problema: reduz espaço seguro
Resposta: reposicionamento
```

### Bruiser

```text
Problema: resistência alta a stagger
Resposta: postura / ataques comprometidos
```

Os encontros devem combinar papéis.

Exemplo:

```text
Swarm
+
Artillery
+
Bruiser
```

Isso gera um problema de combate mais interessante do que simplesmente aumentar HP.

---

# 29. Elites devem alterar decisões, não apenas atributos

Afixos puramente numéricos podem existir, mas não devem dominar o sistema.

Exemplo fraco:

```text
+50% HP
+25% velocidade
```

Exemplo mais interessante:

```text
Elite cria zonas perigosas após receber X golpes.
```

```text
Elite teleporta quando sofre stance break.
```

```text
Elite protege inimigos próximos enquanto não for interrompido.
```

```text
Elite deixa uma cópia digital ao morrer.
```

Objetivo:

> O jogador deve identificar o afixo e adaptar a forma de lutar.

---

# 30. Bosses devem testar sistemas aprendidos

Boss não deve ser apenas uma versão maior de um inimigo comum.

Cada boss precisa testar pelo menos três fundamentos.

Exemplo do Guardião do Núcleo:

1. **movimentação** — investidas;
2. **leitura de área** — shockwave;
3. **posture** — interromper sequências perigosas;
4. **burst window** — aproveitar stance break.

A fase abaixo de 50% de vida pode modificar padrões, mas deve continuar legível.

---

# 31. Meta progressão do Nexus

A progressão persistente deve priorizar **novas possibilidades** em vez de aumentos permanentes de poder.

Evitar que a principal progressão seja:

```text
+2% dano
+5 HP
+1% crítico
```

Preferir:

- nova arma;
- nova Mutation;
- novo Technique boon;
- novo Protocol;
- novo evento;
- novo tipo de sala;
- novo NPC;
- novo modificador de run;
- nova opção inicial;
- reroll;
- banish;
- escolha adicional;
- loadout cosmético;
- nova rota/setor.

Isso preserva a importância da habilidade do jogador e evita transformar runs iniciais em versões objetivamente piores do jogo.

---

# 32. Progressão narrativa pode ser integrada ao sistema

A história de Tech-Guy permite transformar desbloqueios em partes do próprio universo.

Conceitos possíveis:

```text
PATCH
HOTFIX
OVERCLOCK
ROOT ACCESS
MEMORY FRAGMENT
CORRUPTED PACKAGE
DRIVER
PROTOCOL
PROCESS
KERNEL
```

Exemplo:

```text
Encontrar um Memory Fragment
↓
libera nova conversa no Nexus
↓
libera Mutation
↓
Mutation passa a aparecer nas próximas runs
```

Meta progressão e narrativa passam a reforçar a mesma fantasia.

---

# 33. HUD — alterações necessárias

A HUD atual pode ser simplificada se mana deixar de ser universal.

## Elementos essenciais

- HP;
- Q/W/E/R;
- dash;
- recurso específico da arma;
- cooldown;
- status importantes;
- feedback de boon relevante.

---

## 33.1. Informação contextual por arma

### Manopla

Mostrar:

- Energia Asura;
- possível momentum/combo;
- estado do R.

### Arco

Evitar uma barra extra sem função clara.

Feedback deve acontecer principalmente no mundo:

- distância;
- marca;
- charge;
- trajetória.

### Lança

O sweet spot deve ser comunicado preferencialmente:

- na arma;
- no alvo;
- no cursor;
- no VFX.

Não através de mais uma barra.

---

# 34. Filosofia de feedback de combate

Cada acerto deve responder quatro perguntas:

1. Acertei?
2. Foi forte?
3. O inimigo reagiu?
4. Minha build ativou alguma coisa?

Utilizar combinação de:

- hit-stop;
- câmera;
- som;
- VFX;
- flash;
- animação;
- números de dano;
- partículas;
- decal;
- reação física;
- UI contextual.

Evitar depender somente de damage numbers.

---

# 35. Damage Numbers

Os números devem ajudar a comunicar impacto, não ser a única fonte de informação.

Sugestão de hierarquia:

```text
Hit comum
Critical
Weak point / sweet spot
Stance break
Protocol proc
Execution
```

Cada um pode possuir diferenciação visual sem transformar a tela em excesso de texto.

---

# 36. Priorizar legibilidade em hordas

Com grande número de inimigos, o maior risco é excesso de ruído visual.

A prioridade visual deve ser:

```text
1. Ameaça inimiga
2. Personagem
3. Ataque do personagem
4. Proc importante
5. Dano secundário
```

O jogador nunca deve deixar de enxergar um telegraph perigoso porque sua própria build gerou partículas demais.

Isso será especialmente importante quando Protocols criarem chains, explosions e elementos simultaneamente.

---

# 37. Regras para criar futuros boons

Antes de aprovar um novo boon, responder:

### Pergunta 1

Ele muda alguma decisão do jogador?

### Pergunta 2

Ele reforça a identidade da arma?

### Pergunta 3

Ele combina com pelo menos outro boon de maneira interessante?

### Pergunta 4

Ele é visualmente perceptível?

### Pergunta 5

Ele cria apenas DPS ou cria gameplay?

Se o boon for apenas:

```text
+20% damage
```

provavelmente deve ser:

- fundido com outro efeito;
- transformado;
- movido para Utility;
- removido.

---

# 38. Regras para futuros Protocols

Protocol deve conectar sistemas.

Boas estruturas:

```text
ON HIT → efeito
ON KILL → efeito
ON STANCE BREAK → efeito
ON DODGE → efeito
ON ELEMENT → efeito
ON CRIT → efeito
ON CLOSE RANGE → efeito
ON SWEET SPOT → efeito
```

Combinações devem permitir cadeias emergentes.

---

# 39. Regras para futuras Mutations

Mutation deve alterar verbo ou comportamento.

Exemplos válidos:

```text
Q agora atravessa inimigos.
```

```text
W deixa uma zona persistente.
```

```text
E pode ser carregado.
```

```text
R vira uma sequência manual de golpes.
```

```text
Ataque básico finaliza com uppercut.
```

Exemplo fraco:

```text
Q causa +40% de dano.
```

---

# 40. Estrutura recomendada de uma run curta

Para o próximo vertical slice:

```text
NEXUS
↓
Escolha da arma
↓
START
↓
Combat
↓
Escolha de rota
├── Technique
└── Repair
↓
Combat
↓
Protocol
↓
Elite
↓
Mutation
↓
Combat
↓
Boss
↓
NEXUS
```

Duração desejada inicial:

```text
10–15 minutos
```

A meta não é conteúdo infinito.

A meta é validar a estrutura inteira.

---

# 41. Vertical Slice recomendado

O próximo grande milestone deve usar apenas uma arma.

## Arma escolhida

**Manopla**

Motivos:

- já possui identidade mais forte;
- Asura cria um loop natural;
- melee expõe rapidamente problemas de hit feedback;
- stagger/posture é facilmente testável;
- pressão ofensiva combina com hordas.

---

# 42. Escopo do Vertical Slice

Implementar somente:

### Jogador

- manopla;
- ataque básico revisado;
- Q/W/E/R;
- dash;
- input buffer;
- cancel windows;
- Energia Asura.

### Inimigos

- Swarm;
- Bruiser;
- Ranged;
- Charger;
- Elite;
- Boss.

### Boons

Aproximadamente:

```text
8–12 Techniques
3–4 Mutations
5–6 Protocols
3–4 Utilities
```

Não é necessário usar todos os boons atuais imediatamente.

### Run

- 6–8 encontros;
- 1 bifurcação real;
- 1 Repair;
- 1 Elite;
- 1 Mutation garantida;
- 1 Boss.

---

# 43. Critério de sucesso do Vertical Slice

Pergunta principal:

> **É divertido lutar por 10 minutos usando somente a Manopla?**

Mais especificamente:

### Combate base

- ataques possuem impacto?
- movimento é responsivo?
- skills parecem diferentes?
- dash funciona como ferramenta defensiva?
- inimigos reagem aos golpes?

### Build

- duas runs geram estilos diferentes?
- boons alteram decisões?
- existe momento de power spike?
- sinergias ficam perceptíveis?

### Run

- escolher caminho importa?
- perder HP altera decisões?
- Elite parece arriscado?
- Mutation parece recompensa especial?

Se a maioria dessas respostas for negativa, não expandir conteúdo ainda.

---

# 44. Ordem de implementação recomendada

## Fase 1 — Combat Foundation

### Objetivo

Resolver feeling do personagem.

### Implementar

- remover auto-combate persistente;
- input buffer;
- startup/active/recovery;
- cancel windows;
- categorias Fluid/Committed/Channel;
- dash cancellation;
- hit-stop;
- reação básica dos inimigos.

### Não implementar ainda

- novos mapas;
- procedural;
- dezenas de novos boons;
- nova arma.

### Critério de conclusão

Um pequeno sandbox com 3–5 inimigos já deve ser divertido.

---

# 45. Fase 2 — Posture & Enemy Interaction

### Implementar

- stagger;
- stance break;
- knockdown;
- launch;
- resistência por categoria;
- feedback audiovisual;
- janela de stagger do boss.

### Critério de conclusão

O jogador deve conseguir descrever visualmente a diferença entre:

- mob pequeno;
- elite;
- boss.

---

# 46. Fase 3 — Manopla 2.0

### Implementar

- identidade de pressão;
- geração de Asura revisada;
- loop de R;
- momentum se necessário;
- 8–12 Techniques;
- 3–4 Mutations.

### Critério de conclusão

Manopla deve funcionar sem Protocols universais.

---

# 47. Fase 4 — Run Structure

### Implementar

- mapa simples de rota;
- tipos de recompensa;
- HP persistente durante a run;
- Repair;
- Elite;
- Mutation reward;
- boss.

### Critério de conclusão

Duas runs consecutivas devem produzir decisões diferentes mesmo usando a mesma arma.

---

# 48. Fase 5 — Protocol System

### Implementar

- Ignite;
- Frost;
- Conductor;
- Detonation;
- Reactor;
- Resonance;
- Overflow.

Revisar interações recursivas e limites de performance.

### Critério de conclusão

Combinações devem ser capazes de gerar comportamentos inesperados sem destruir legibilidade ou performance.

---

# 49. Fase 6 — Arco 2.0

### Implementar

- identidade de reposicionamento;
- distância como vantagem;
- trajectory gameplay;
- Kiting Step;
- charged attack;
- marks;
- Mutations específicas.

### Critério de conclusão

O arco deve parecer uma arma completamente diferente da Manopla sem depender apenas de ser ranged.

---

# 50. Fase 7 — Lança 2.0

### Implementar

- sweet spot nativo;
- feedback de distância;
- spacing tools;
- pull/push;
- stance damage na ponta;
- Mutations específicas.

### Critério de conclusão

O jogador deve naturalmente tentar manter a distância correta.

---

# 51. Fase 8 — Meta Progression

Somente após o loop de run funcionar.

### Implementar

- desbloqueios;
- novos boons;
- novas Mutations;
- novos Protocols;
- opções de reroll;
- eventos;
- progressão narrativa;
- novos setores.

---

# 52. Fase 9 — Procedural / Expansão

Somente depois do vertical slice validado.

Possíveis expansões:

- procedural generation;
- mais salas;
- biome modifiers;
- eventos raros;
- challenge rooms;
- minibosses;
- armas adicionais;
- novas famílias de Protocol;
- mutadores de dificuldade.

---

# 53. Sistemas que devem ser adiados

Para evitar scope creep, não priorizar agora:

- crafting complexo;
- loot tradicional estilo Diablo;
- dezenas de stats;
- equipamentos com affixes;
- procedural avançado;
- árvore de talentos grande;
- várias moedas permanentes;
- múltiplas novas armas;
- conteúdo endgame;
- dezenas de bosses.

Esses sistemas podem ser bons futuramente, mas não resolvem o problema atual de gameplay.

---

# 54. Backlog por prioridade

## P0 — obrigatório

- [ ] Remover auto-combate persistente
- [ ] Criar input buffer
- [ ] Implementar startup/active/recovery
- [ ] Criar cancel windows
- [ ] Revisar dash
- [ ] Separar stagger / knockback / knockdown / launch / stance break
- [ ] Melhorar hit feedback
- [ ] Validar Manopla sem boons
- [ ] Definir identidade oficial das três armas

## P1 — próximo milestone

- [ ] Remover/testar gameplay sem mana universal
- [ ] Manopla 2.0
- [ ] Reorganizar boons em Mutation / Technique / Protocol / Utility
- [ ] Criar rota de run
- [ ] Tornar HP persistente na run
- [ ] Criar Repair room
- [ ] Reestruturar Elite rewards
- [ ] Criar Mutations específicas da Manopla

## P2 — expansão do vertical slice

- [ ] Protocol interactions
- [ ] Elite affixes comportamentais
- [ ] Boss revisado
- [ ] Arco 2.0
- [ ] Lança 2.0

## P3 — após validação

- [ ] Meta progressão
- [ ] Eventos
- [ ] Novos setores
- [ ] Procedural generation
- [ ] Novas armas

---

# 55. Métricas para playtest

Não medir apenas win rate.

Observar:

### Combate

- quantas vezes o jogador tenta cancelar uma animação e não consegue;
- frequência de uso do básico;
- frequência de dash;
- quantidade de dano recebido durante recovery;
- tempo entre inputs ofensivos;
- tempo sem executar ações.

### Posture

- quantas stance breaks acontecem por encontro;
- quantas são percebidas pelo jogador;
- quantas geram aproveitamento da janela.

### Boons

- pick rate;
- abandono de opções;
- boons que nunca mudam comportamento;
- combinações recorrentes;
- combinações que o jogador comenta espontaneamente.

### Run

- escolha de rotas;
- uso de Repair;
- risco assumido para Elite;
- HP médio antes do boss;
- duração da run.

---

# 56. Perguntas obrigatórias após cada playtest

Perguntar ao jogador:

1. Qual arma você sentiu que estava usando?
2. O que você tentava fazer durante o combate?
3. Qual boon mais mudou sua forma de jogar?
4. Você percebeu quando quebrava postura?
5. Em algum momento o personagem não fez o que você esperava?
6. Você sentiu que alguma animação te prendeu demais?
7. Qual foi a decisão mais difícil da run?
8. Em que momento você se sentiu mais forte?
9. Alguma recompensa pareceu inútil?
10. Você começaria outra run imediatamente?

A pergunta 10 é uma das métricas qualitativas mais importantes para um roguelite.

---

# 57. Definition of Done — combate

O combate base estará pronto para expansão quando:

- inputs forem previsíveis;
- ataque básico não jogar sozinho;
- jogador entender quando está comprometido;
- dash cancelar apenas onde deve;
- ações encadearem de forma natural;
- hits possuírem peso;
- inimigos reagirem de acordo com sua categoria;
- postura possuir impacto real;
- a Manopla for divertida sem nenhum boon.

---

# 58. Definition of Done — sistema de boons

O sistema estará pronto quando:

- cada boon possuir categoria clara;
- Mutations alterarem comportamento;
- Techniques criarem estilos de arma;
- Protocols criarem interações sistêmicas;
- Utilities resolverem necessidades específicas;
- ofertas tiverem pelo menos duas escolhas atraentes com frequência;
- builds diferentes surgirem naturalmente;
- nenhuma arma perder sua identidade devido a um boon genérico.

---

# 59. Definition of Done — run

A run estará pronta para expansão quando:

- o jogador escolher caminhos;
- HP influenciar decisões;
- Elite representar risco/recompensa;
- Mutation parecer um evento importante;
- duas runs produzirem sequências diferentes;
- o jogador reconhecer power spikes;
- chegar ao boss representar consequência das decisões anteriores.

---

# 60. Resultado final esperado

Após essa reestruturação, Tech-Guy deve possuir uma leitura muito mais clara.

## Manopla

```text
PRESSÃO
↓
POSTURA
↓
ASURA
```

## Arco

```text
POSICIONAMENTO
↓
ÂNGULO
↓
MULTI-HIT / MARK
```

## Lança

```text
DISTÂNCIA
↓
SWEET SPOT
↓
CONTROLE
```

Sobre essas três identidades entram:

```text
TECHNIQUES
↓
definem a build

MUTATIONS
↓
reescrevem habilidades

PROTOCOLS
↓
criam combinações absurdas

UTILITY
↓
mantém a run viva
```

E a run passa a perguntar constantemente:

```text
O QUE EU PRECISO AGORA?

mais poder?
mais sinergia?
uma transformação?
cura?
arriscar um elite?
```

Esse deve ser o núcleo da experiência.

---

# 61. Próximo passo recomendado

Depois deste documento, o trabalho mais útil é produzir um **Boons v2**, revisando individualmente todos os boons existentes e classificando cada um como:

```text
KEEP
REWORK
MERGE
RETIRE
MUTATION
TECHNIQUE
PROTOCOL
UTILITY
```

Para cada boon também devem ser definidos:

- arma;
- gatilho;
- efeito;
- interação com outros boons;
- feedback visual;
- possibilidade de rank;
- conflitos;
- Mutations relacionadas;
- arquétipos emergentes.

Isso permitirá reorganizar o catálogo atual sem jogar fora o trabalho já implementado.

---

# 62. Resumo executivo

A prioridade do projeto não deve ser adicionar mais conteúdo neste momento.

A ordem correta é:

```text
COMBAT FEEL
↓
POSTURE / REAÇÕES
↓
IDENTIDADE DAS ARMAS
↓
BOONS
↓
ESTRUTURA DA RUN
↓
SINERGIAS
↓
META PROGRESSÃO
↓
CONTEÚDO
```

O maior risco atual seria continuar adicionando boons, inimigos e armas sobre uma fundação de combate ainda indefinida.

O maior ganho possível agora é fazer um vertical slice pequeno e extremamente polido.

> **Se lutar durante dez minutos com apenas a Manopla já for divertido, todo o restante do jogo passa a multiplicar uma fundação boa.**

