# Tech Guy — Plano "Reset de Loop"

**Data:** 09/11/2026 · **Status:** direção recomendada; arma, artefatos, duração, sala de descanso e NPCs decididos (seção 7).

## 1. Diagnóstico (do desenvolvedor)
- Jogar é divertido, mas as runs ficam repetitivas.
- Não há progressão: falta motivo para jogar a próxima run.
- Não há identidade visual estabelecida, embora a direção esteja clara na cabeça do autor.
- A visão do jogo continua intacta.

Conclusão: o núcleo (combate) funciona. O que falta é o **loop ao redor**: meta-progressão, variedade entre runs e identidade visual.

## 2. Estrutura de progressão proposta pelo desenvolvedor

1. **Universos = fases.** Cada universo tem estilo de arte próprio. Termina-se o Universo 1 para liberar o 2; a run seguinte passa por 1 → 2 e, ao vencer o 2, libera o 3, e assim por diante (modelo Dead Cells/Isaac).
2. **Cada arma pertence a um universo** e tem personalidade própria.
3. **Rapport com as armas** (inspirado em Lost Ark): a afinidade sobe ao longo das runs; cada nível libera algo, e no nível máximo a arma entrega um **artefato**.
4. **Itens e NPCs ao estilo Hades:** itens obtidos nas runs, interações que liberam conteúdo novo.
5. **Universos planejados:** (1) tecnológico, (2) cartoonizado, (3) noir em preto e branco. O **protagonista mantém o mesmo estilo** em todos.

## 3. Avaliação

| Ideia | Ponto forte | Risco | Recomendação |
|---|---|---|---|
| Armas como NPCs com rapport | Une personalidade das armas, meta e motivo para trocar de arma. | Escrita e diálogos custam; risco de virar só +X% | **Adotar.** Diálogo em texto curto no início, sem voz. Haverá também NPCs resgatados nas runs (seção 7). |
| Artefatos do rapport | Recompensa de longo prazo clara | "Melhorar dano" contraria o pilar anti +X% | **DECIDIDO:** coleção estilo Keepsakes (seção 7). Efeitos direcionam a build, não só números. |
| Arma ligada ao universo | Lore forte, e a arma vira recompensa de progressão | Prender a arma ao universo mata a variedade | **DECIDIDO:** a arma vem do seu universo, mas é escolhida no Nexus e usada em todos os universos até o fim da run. |
| Universos sequenciais | Meta clara, sensação de avanço | Repetir o Universo 1 em toda run é justamente a queixa atual | **DECIDIDO:** até ~10 min por universo, variando com a build. Variações de rota/sala por universo e, mais tarde, modificadores de dificuldade opcionais. |
| Arte diferente por universo | Identidade fortíssima (conceito tipo Spider-Verse, sem copiar) | Custo de 3 conjuntos de arte + inimigos | Comportamentos de inimigo **reaproveitados** e reskinados por universo. Arte de cada universo feita só quando ele for jogável. |
| Player constante | Âncora visual e de legibilidade | Precisa se destacar em fundos muito diferentes | Contorno/rim light e paleta do player fixos e testados contra os três estilos. No noir, o player em cor é um gancho visual forte. |

## 4. Modelo de meta-progressão recomendado (MVP)

Começar com **duas** moedas, não mais:
- **Fragmentos de Dados** (comum): ganhos em toda run, inclusive na morte. Gastos num terminal do Nexus.
- **Rapport por arma:** sobe ao usar a arma (salas vencidas, boss, tempo de uso) e com **presentes** raros achados nas runs.

**Rapport — níveis por arma:** ver o esboço na seção 7.

**Terminal do Nexus (Fragmentos) — 6 a 8 itens iniciais:** +1 reroll de oferta; começar a run com 1 boon; sala-loja passa a aparecer; +1 cura entre universos; revelar o tipo de recompensa nas portas; +1 chance de oferta rara. Nada que seja só "+10% dano" em sequência.

**Liberação de armas:** vencer o boss do Universo 1 libera o Universo 2 **e** a Lança. Vencer o boss do 2 libera o 3 **e** o Arco.

**Fim de run:** mostra Fragmentos ganhos, rapport ganho e o próximo desbloqueio mais próximo ("faltam 40 para…"). Isso é o que cria o "só mais uma run".

## 5. Fases de execução

**Fase 0 — Arrumar a casa.** Commit/estado do git; isolar os testes falhos; pausar Lança/Arco/2.5D/prólogo; relatórios de uma página; **auditoria do que já existe** de save, Nexus, desbloqueios, loadout, boss e resultado de run. *(Prompt pronto.)*

**Fase 1 — Motivo para jogar.**
- 1a: Universo 1 com fim claro (boss vencível → tela de vitória → retorno ao Nexus).
- 1b: Fragmentos + primeiro NPC resgatável no Universo 1, que abre o terminal do Nexus + tela de fim de run com progresso. *Playtest 10/11/2026: estrutura aprovada; economia rápida demais, catálogo pouco empolgante (só o reroll), boon extra grátis vetado, resgate sem urgência. O gancho veio do NPC, não da tela de resultado. Detalhes no STATUS.*
- 1c: Rapport da Manopla (níveis 1–5, artefatos nos níveis 2, 4 e 5) + presentes + slot de artefato no Nexus.
- 1d: Universo 2 em **gray-box** (salas atuais, inimigos reaproveitados com outra cor) + **sala de descanso** entre universos (troca de artefato, cura) + Lança liberada ao vencer o Universo 1.

**Fase 2 — Runs diferentes entre si.** ~10 boons "barulhentos" para a Manopla; portas com tipo de recompensa; elite opcional, sala-desafio e sala-loja; elites com modificadores.

**Fase 3 — Identidade visual (em paralelo).** Bíblia de arte escrita para os três universos + regra do player. Depois, **uma sala de referência do Universo 1** (sala + player + 1 inimigo) no padrão final, aprovada pelo autor como régua.

## 6. Regras do processo novo
- Toda etapa termina com um **build jogável** e o autor joga antes da próxima.
- Relatório de **uma página**: feito / não feito / não testado / o que jogar.
- Protótipo de design pode ser descartável; não preservar por padrão o que não foi aprovado.
- Veto de boon: se a mudança não aparece numa gravação de 10 s, não entra.
- Uma frente por vez (a arte da Fase 3 é a única exceção paralela).

## 7. Decisões tomadas (09/11/2026)
1. **Arma:** escolhida no Nexus e usada **em todos os universos até o fim da run**. Troca no meio da run fica para avaliação futura.
2. **Artefatos:** **coleção ao estilo dos Keepsakes de Hades**. Transformar a arma fica como possibilidade futura.
3. **Duração:** média de **até ~10 min por universo**, variando com a força da build.

### Especificação de artefatos (proposta)
- **Fonte:** cada arma entrega artefatos em mais de um nível de rapport; o nível máximo entrega o **artefato assinatura** da arma. Presentes/eventos raros nas runs entregam outros. Assim a coleção tem tamanho suficiente para ser uma escolha real.
- **Uso:** equipar **1 artefato** no Nexus antes da run. **DECIDIDO:** pode ser trocado na **sala de descanso entre universos**, como em Hades.
- **Efeito:** artefatos **direcionam a build** em vez de dar só +X%. Exemplos: começar a run com um boon de certo tipo; a primeira oferta de cada universo sempre traz um Protocol; elites largam presente de rapport; uma revivida por run; ofertas de elite com uma opção a mais; Fragmentos extras por sala sem levar dano. Bônus numérico simples só como item comum e minoritário.
- **MVP:** 1 slot, sem rank de artefato, 4–5 artefatos (os 3 da Manopla + 1–2 de eventos).

### Rapport da Manopla — esboço revisado
| Nível | Libera |
|---|---|
| 1 | Primeira conversa (texto) |
| 2 | Artefato comum + 2–3 boons da Manopla no pool |
| 3 | Conversa + mais boons no pool |
| 4 | Segundo artefato |
| 5 (máx.) | Artefato assinatura da Manopla |

### Duração — implicações de design
- Dimensionar cada universo para ~10 min com build **média**; build forte termina antes (recompensa natural), build fraca passa um pouco.
- Boss de universo: ~1,5–2,5 min desse total.
- Registrar o tempo por universo na tela de fim de run, para calibrar com dados reais.

### Sala de descanso entre universos (DECIDIDO)
Sala curta, sem combate, entre o boss de um universo e o início do próximo. MVP: trocar artefato + cura parcial. Depois: encontro com NPC, conversa com a arma, loja ocasional. Também marca a transição de estilo de arte entre universos.

### NPCs (DECIDIDO: haverá NPCs além das armas)
- Personagens **encontrados ou resgatados durante as runs** passam a morar no Nexus, e cada um traz algo novo: um serviço, uma loja, desbloqueios ou conversas. Referência de princípio: resgates que povoam o hub (Enter the Gungeon) e conversas que avançam a cada retorno (Hades).
- **Cada resgate é uma recompensa de progressão por si só:** o Nexus cresce porque você jogou, sem virar burocracia (cada NPC = uma função clara).
- **MVP recomendado:** **um** NPC resgatado no Universo 1, garantido numa das primeiras runs. Ao chegar ao Nexus, ele **abre o terminal de Fragmentos**. Assim o primeiro serviço do hub nasce de um evento da run, não de um menu que já estava lá.
- Diálogo em texto curto, sem voz no MVP. Rapport fica para as armas; NPCs podem ter progressão mais simples (conversas que avançam por visitas/eventos).

### Inimigos: papéis e ameaça em camadas (APROVADA em 10/11/2026, com ajustes)
> **Ajustes do desenvolvedor:** o Hooker vira **gancho rápido em skillshot** (arrasta o jogador rápido, como os puxadores de Dota/Diablo IV), não puxão lento. Entra um novo **Shooter "Rajada"** (metralhadora na cauda, laser em rajadas; prefab do desenvolvedor em desenvolvimento). **Bosses viram frente própria, depois**: cada um com estilo único e épico (referências: God of War, Phantom Blade Zero, Wukong, Lost Ark, Diablo, adaptadas ao isométrico, sem copiar). A direção A+C abaixo fica como insumo dessa frente.
Origem: playtest de 10/11 ("IA melhor, mas nada é perigoso, nem o boss"; causa provável = papel de cada inimigo, não números). Levantamento só por leitura de código/prefabs; nada jogado nem alterado. Relatório e decisões no `Docs/STATUS.md`.

**Por que hoje nada é perigoso (leitura do código):**
- Os 16 prefabs têm a mesma base (dano 12, cadência 1,5 s); só os multiplicadores do perfil mudam. Ninguém é "o perigoso da sala".
- Melee: no máximo 3 atacando ao mesmo tempo (`SwarmAttackCoordinator`), e o Swarm disputa esses 3 lugares com Grunt e Heavy. Enxame não consegue cercar.
- As salas são um sorteio uniforme: 4–10 inimigos, os 3 primeiros lutadores distintos e o resto aleatório num pool de 18 (16 arquétipos + Fragmento = Rush + Emissor = Sniper). Não há regra de combinação, então as combinações perigosas só aparecem por acaso.
- A elite é o variante legado `Magic_Haste` (não é um arquétipo; muda só reação e postura). Não há elite que torne os comuns mais perigosos.

**Tabela de papéis**

| Inimigo | Hoje (código) | Esquisito hoje | Papel proposto: o que pede do jogador | Veredito |
|---|---|---|---|---|
| Swarm | 0,2× HP, soco 0,7 s; Spawner gera até 10 | ocupa slot melee: o enxame fica parado esperando a vez | **Bucha**: cerca, morre num golpe, enche Asura. Perigo só se ignorado | Manter; tirar do limite de 3 (pool próprio) |
| Fragile | 0,7× HP, postura 30, soco padrão | igual ao Swarm/Rush, sem papel próprio | **Volátil**: explode 0,8 s após morrer (área pequena). Pune AoE descuidado no meio do grupo | Fundir no Swarm **ou** virar Volátil |
| Rush (+ Fragmento) | 0,6× HP, 1,5× velocidade, soco 0,3 s | Fragmento é Rush com outro corpo | **Bucha rápida**: chega primeiro e interrompe | Manter; Fragmento vira só skin |
| Grunt | soco 0,4 s, 2,2 m | já documentado: para fora do alcance do próprio soco | **Linha**: pressão melee básica | Manter |
| Heavy | 2,2× HP, slam 0,95 s → shockwave 1,3 s | ok | **Âncora**: zona proibida na frente; pune quem fica colado sem ler | Manter |
| Charger | perfil 6 m, mas só ataca a ≤1,8 m (gate melee) e o "charge" sai colado | a investida nunca vem de longe | **Investida**: de 5–8 m, faixa travada; obriga a sair da linha | Corrigir o gate (prioridade) |
| Shooter | tiro reto ≥0,25 s, kite curto | ok | **Linha à distância**: pune ficar parado longe | Manter |
| SpreadShooter | 5 tiros em 40°, 0,8× dano | quase igual ao Shooter de perto | **Parede**: nega o ângulo de aproximação, obriga a flanquear | Manter **ou** virar modificador do Shooter |
| Sniper (+ Emissor) | linha 1,0 s, 1,8× dano, 14 m | peso dobrado no pool; aviso e disparo podem divergir (GI1.8) | **Pune parar**: no máximo 1 por sala | Manter; limitar a 1 por sala |
| Bomber | lob 0,35 s + área 2 m na queda; escala 0,5 | pequeno demais para ser lido; o "ataque de cima" do playtest | **Expulsa do lugar**: área onde você está | Manter; corpo maior |
| HazardCaster | zona a cada 6 s, 6 dano/0,5 s, 35% lento, 6 s | dev ficou dentro sem perder HP (não reproduzido) | **Encolhe a arena**: combina com melee | Manter; verificar o dano |
| Hooker | linha 0,6 s, puxa 1,5 s, dano 0 | puxar para perto **ajuda** a Manopla; ninguém pune o puxão | **Desloca para o perigo**: puxa para dentro de zona/aliados, ou enraíza | Refazer |
| Healer | cura 15 a cada 2 s, foge a 3,5 m, usa soco padrão | foge e tenta socar (dois comandos brigando) | **Prioridade**: caçar primeiro | Manter; tirar o soco |
| ShieldSupport | escudo 40 a cada 4 s | sobrepõe o Healer | **Prioridade**: protege a âncora | Manter, nunca junto do Healer |
| Spawner | Swarm a cada 3 s, teto 10, 0,4× vel. | aprovado no playtest | **Prioridade/relógio**: a luta piora se você enrolar | Manter |
| Mirror | reflete só projéteis, 90–180° | com a Manopla só, é um Grunt mais tanque | **Guarda**: frente imune a golpe, abre com quebra de postura ou pelas costas | Cortar até o Arco **ou** virar Guarda |
| Elite (`Magic_Haste`) | variante legado (Charge + DoublePunch) | não é elite de verdade | **Comandante**: arquétipo + modificador + aura que acelera os comuns por perto | Refazer (encaixa na Fase 2) |

**Sobreposições:** 4 corpos de melee fraco (Swarm, Fragile, Rush, Fragmento); 4 de tiro direto (Shooter, Spread, Sniper, Emissor); Bomber e HazardCaster põem área no jogador (ok: um é instantâneo, o outro persiste); Healer e Shield protegem. **Faltam:** um inimigo que pune bater no grupo sem olhar (Volátil), um que exige quebrar guarda (Guarda), elites que tornam os comuns perigosos (Comandante) e ataques de grupo sincronizados.

**Ameaça em camadas (estilo Lost Ark):**
1. **Bucha** (Swarm, Rush, Volátil): muitos, frágeis e rápidos. Dão sensação de poder. Fora do limite de 3.
2. **Linha** (Grunt, Shooter, Spread): pressão de fundo. Sozinhos, são previsíveis.
3. **Âncora** (Heavy, Charger): 1–2 por sala; telegraphs grandes que fecham espaço.
4. **Combinador** (Hooker, HazardCaster, Bomber, Sniper): 0–1 por sala, 2 no fundo da rota. É ele que transforma a bucha em perigo.
5. **Prioridade** (Healer, Shield, Spawner): 0–1 por sala; marcada e protegida pelos outros.
6. **Elite/Boss**: ameaça real sozinha, e piora os comuns ao redor.

**Mix proposto (troca o sorteio por "pacotes"):** cada sala = 1 âncora + bucha (4–8, em 1–2 ondas) + 0–1 combinador + 0–1 prioridade. A profundidade da rota aumenta combinadores, prioridade e ondas, não só o número de inimigos. Pacotes de exemplo: *Fornalha* (Heavy + Swarm + HazardCaster), *Anzol* (Hooker + Bomber + Rush), *Fortaleza* (Shield + Heavy + Spread), *Colmeia* (Spawner + Grunt + Sniper). Risco: o `ManualRunRandomizer` está preso a seeds e distribuições testadas (09/09B). Os pacotes precisam entrar sem mudar a geometria nem as asserções dessas salas.

**Boss do Universo 1 (Guardião do Núcleo), diagnóstico:**
- Usa o mesmo vocabulário do Heavy e do Charger (Charge, HeavySlam, DoublePunch, Shockwave): nada novo para aprender.
- Fica 1,4 s parado entre ciclos (0,9 s com fúria), com telegraphs de 0,85–1,3 s: a Manopla bate de graça.
- Luta sozinho, sem enxame, sem controle de arena e sem fase nova; a fúria só acrescenta um golpe de continuação.
- A quebra de postura dá atordoamento: mais janelas grátis.

**Direções para o boss:**
- (A) **Núcleo + enxame**: a 66% e 33%, abre o núcleo e chama ondas de bucha. O perigo vem dos telegraphs dele sobrepostos ao cerco.
- (B) **Arena que encolhe**: o slam deixa zona eletrificada, setores do chão ligam.
- (C) **Combos de ritmo**: sequências de 3 com tempo variado, slam atrasado, agarrão (puxa e bate), sem pausa morta.
- Recomendação: A + C enxuto. Fase 1: brigão sem pausa morta. A cada terço: núcleo exposto, enxame e anel de zona. Quebrar a postura com o núcleo aberto = janela de dano grande. Sem stun-lock (regra Boss mantida).

### Ainda em aberto
*(Atualizado em 11/11/2026: as 7 decisões de inimigos foram aprovadas; o Desconhecido é um NPC inacabado do jogo do protagonista; o terminal fica escuro até o 1º resgate, como implementado e aprovado na 1b.)* A ordem das próximas etapas e o que falta decidir estão em `Docs/plano/ROTEIRO_PROXIMAS_ETAPAS.md`.
