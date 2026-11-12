# Tech Guy — STATUS

**Atualizado:** 11/11/2026 · **Branch:** `gameplayOverhaul` · **Fase atual:** inimigos — passe de ameaça 2 implementado em 11/11/2026 (salas-pacote como padrão, avisos mais longos, anti-loop do Hooker, dash com cooldown, vida 700), aguardando playtest. Salas-pacote aprovadas em playtest em 11/11/2026. 1b.1 aprovada em playtest em 10/11/2026. 1a aprovada em playtest em 09/11/2026. ([plano](plano/TECH_GUY_PLANO_RESET_DE_LOOP.md))

> Ponto de entrada único. `02_ESTADO_E_LACUNAS.md` e `03_PRIORIDADES.md` ficam como histórico e não são mais atualizados.

## Onde estamos
- **Combate base:** divertido, segundo o desenvolvedor. A Manopla é a única arma em foco.
- **Run:** `NexusBlockout → FirstSector → NexusBlockout`. Uma run é um setor. Boss morto → hit-stop curto + inimigos e perigos restantes da sala removidos → faixa "UNIVERSO 1 CONCLUÍDO" (~2,5 s) → portal (o jogador entra quando quiser) → resultado "VITÓRIA" no Nexus.
- **Meta-progressão:** `MetaProgress` v2 (JSON em `Application.persistentDataPath`, migra o v1 sozinho). Fragmentos de Dados: drops de inimigos (mesmas taxas das moedas), +1 por sala de combate vencida, +2 por elite, +10 na morte do boss (revisado em 10/11); ficam na morte e no abandono. Vitória e universo concluído contam na morte do boss. NPC "Desconhecido" (placeholder) resgatável no Universo 1 abre o terminal do Nexus com 6 melhorias: Vida reforçada 20, Reroll 40, Sala limpa 50, Reparo reforçado 60, Rolagem extra 75, Quarta opção 90 (total 335). "Oferta inicial" foi retirada; quem a possuía recebe 65 de volta ao carregar o perfil. Arco e Lança bloqueados até a Fase 1d. O saldo antigo de moedas (`TechGuy.Currency.Coins`) é legado e não é usado.

## Jogável agora (o que testar)
| O quê | Como |
|---|---|
| Run completa com a Manopla | MainMenu → Nexus → portal First Sector → boss → faixa → portal → resultado "VITÓRIA" |
| Fragmentos, NPC e terminal | Run 1: NPC preso (âmbar) na 2ª sala de combate desde o começo da luta, falando; destrava (ciano) ao vencer a sala; libertar cura 30%. Depois, na 1ª sala. No Nexus, a primeira conversa toca uma vez; depois o G abre o terminal direto |
| Atalho do boss (Editor/Development) | Na escolha de rota do FirstSector, **F9** limpa as salas pela política real e entra na sala do boss |
| Recompensas C05 | Qualquer sala de combate |
| Combo protegido | Titanic Fists → Conductor → Detonation |
| Escolha de passagem pelas portas (10/11) | Sem menu: ao concluir a sala, cada porta mostra o ícone/recompensa da sala seguinte (número 1/2, cadeado no Boss sem Mutation); **encostar** na porta confirma. A faixa de percurso aparece no canto superior esquerdo só nesse momento (você → 3 salas; bifurcação com os dois ramos até reencontrarem) com a dica "Encoste em uma porta para seguir". O toque só vale depois de se afastar das portas uma vez |

## Congelado / pausado
Lança, Arco, arte 2.5D/pictórica e prólogo. Os protótipos G1/G2/G3 (RUN-EXP-10) estão só no branch `proto/run-exp-10-g123`, fora do `gameplayOverhaul`.
Preservar: Titanic → Conductor → Detonation, salas 09/09B, C05 e os controles WASD/mouse/Q/E/R.

## Saúde técnica
- EditMode: 289/289 (10/11/2026, 1b.1: suíte completa 288/289 no job `1109d941`; a falha, um custo antigo fixo no teste, passou após correção no job `e31f058b`, 13/13). PlayMode da 1b: 5/5 (job `d31dd00b`).
- PlayMode completo: último 828/879 (09/11/2026, job `861b5b32`, antes da 1a). Suítes focais da 1a: 43 testes passando (job `da07be45` 26/28; as 2 falhas, um erro do teste novo e um reload do Fast Script Reload, passaram após correção no job `17fa570c`; resultados 15/15 no `07e7e185`).
- Atenção: a suíte completa grava em `Docs/validation/artifacts/` (caminhos fixos). Confira o `git status` antes de commitar depois de rodá-la.

### Quarentena F0 — 44 testes PlayMode
`[Explicit("QUARENTENA-F0: motivo")]`: ficam fora da execução padrão e rodam quando selecionados pelo nome. `grep -r QUARENTENA-F0 Assets` lista todos, com o motivo. Fase 1a triou o grupo de rota/salas: 6 saíram da quarentena (reescritos pela regra 09B ou ambiente corrigido) e 6 seguem com motivo atualizado. Suíte padrão após a quarentena: **não rodada** (meta: verde).

| Motivo | Testes |
|---|---|
| Navegam por clique (removido no C01) — 9 | NexusBlockout/NexusSpatialJourney `Journey720`/`Journey1080`; NexusUsability `CombatHudIsReserved…`, `PedestalKeyEquips…`, `PointerWalkAndInteraction…`, `ResultsBlockPedestalKey…`; ManualRunPlayerFeedback `PublishedFloorClickActuallyMoves…` |
| Esperam efeito síncrono (triagem 10.2B) — 9 | BasicAttackR1 `GauntletPhase3{BasicWithoutRun, CadencedWait, PublicEventOrder, RConsumesOnce, ReentrantRequest}…`, `ManualLastBasicBatch…`, `ManualPlayerDeathInLastLethalBatch…`, `MigratedGauntletBasicInputs…`; ManualRunPlayerFeedback `FirstTwoRooms…` |
| Causa não determinada, Manopla — 7 | BasicAttackR1 `GauntletPhase3{AuthoredZeroChildDelays, DemolishingAdvance, LegacyShockwave, RewardsOfferExecutable, SharedHudGroups}…`, `ManualLastEcho…`, `ManualLastSkillCancels…` |
| Anteriores ao catálogo atual — 5 | BasicAttackR1 `FourPathsAcquireMutationAtC4…`, `RunFocusDiscounts…`, `ManualImpossibleFirstMutation…`, `PauseConfirmedAbortWaits…`; ProtocolPublishedComposition `FiveCompositions…` |
| Salas retangulares fixas / modo fora da produção (triagem 1a) — 5 | RunExp03FirstSector `Archive05Comparison…`, `ConsolidatedRoles…`, `RefinedRoomComparisons…`; ManualAuthoredNavigation `FourAuthoredPaths…`; BoonPublishedEncounter `Gauntlet…` (seed aleatória, causa não determinada) |
| Ambiente de teste vazando / Lança pausada — 4 | BasicAttackR1 `SpearBoon{ContactStake, PathStake, Horizon}…`; ManualAuthoredNavigation `AuthoredClosedGate…` |
| Dependem da ordem (passam em seleção menor) — 5 | `TornadoKeepsCommittedDirection…`, BreakerKitEmission `ConsecutiveNativeSkills…`, Gpr015 `GameViewTriple…`, SpacingRecoil `Recoil_NeverCrossesSolidScenery`, TitanicRoomClear `RoomClearElementalLastKill…` |

Saíram na 1a: `{Gauntlet,Spear,Bow}NaturalSeed…`, `GeneratedPaths…`, BoonPublishedEncounter `Bow…`/`Spear…`.

## Playtest da 1b (10/11/2026)
| Tema | O que o desenvolvedor sentiu | Consequência |
|---|---|---|
| Vontade de jogar de novo | **Não veio** da tela de resultado. Veio da curiosidade pelo NPC (vontade de voltar ao Nexus e falar com ele). | NPC/conversas pesam mais que números. |
| Progressão | O sistema ficou legal. | Manter a estrutura. |
| Melhorias do terminal | Pouco empolgantes; só o **Reroll** empolga. "Oferta inicial" (boon extra) é **quebrada**; uma opção a mais numa oferta é aceitável. | Rever o catálogo: menos números, mais escolha. Trocar "Oferta inicial". |
| Economia | Fragmentos chegam **rápido demais**. | Reduzir ganhos e/ou subir custos. |
| Resgate do NPC | Fácil, mas passa despercebido e sem significado: só aparece depois da sala limpa, sem urgência. | Resgate precisa acontecer **durante** o combate, com risco. |
| Terminal | Bem localizado, junto das armas. Repetir o diálogo toda vez cansa. | Sem diálogo novo, G abre a loja direto. |
| Inimigos | **Entediantes**: só avançam quando você entra no alcance, não dão sensação de perigo; a maioria é ranged e foge quando você se aproxima, o que vira uma perseguição chata com a Manopla. | Problema de combate fora do escopo da 1b; afeta toda run. |

### Ajustes 1b.1 (10/11/2026)
- Economia mais lenta (sala +1, boss +10; custos 20→90, total 335).
- Catálogo: "Oferta inicial" retirada (boon grátis vetado); entra "Rolagem extra" (+1 reroll, soma com o Reroll).
- Resgate: a cápsula não é destruída. Ela já está na sala quando a luta começa, trancada, com o NPC chamando o jogador ("Ei! Aqui!"), reagindo à luta e à vida baixa, e um marcador que fica na borda da tela quando ela está fora de vista. Vencer destrava; libertar cura 30% da vida máxima.
- Terminal: o diálogo do NPC no Nexus fica salvo como visto (`seenDialogues` no MetaProgress); depois o G abre a loja direto ("[G] Terminal de melhorias").

### Passe de inimigos — etapa 1: pressão no encontro (10/11/2026)
Causa medida: todo inimigo do pool do FirstSector tem `sightRange` 7 m em salas de 22–30 × 18–24 m (a maioria patrulhava até o jogador chegar perto), e os ranged recuavam sem limite até a distância preferida (6–14 m), com o kiting do Shooter voltando a cada 2 s.
- Em sala de encontro (`EnemyAI.ConfigureEncounterBounds`), o alcance de visão cobre a arena inteira (1,5 × o lado maior): todos entram na luta desde o começo.
- Recuo com orçamento, só para ranged/controle: 1,2 s por encontro para se afastar do jogador (kiting do Shooter ou distância preferida). Gasto o orçamento, ele fica e luta. O espaçamento curto dos melee (2–2,5 m) não entra no orçamento: na 1ª versão entrava, e os melee se espremiam contra o jogador.
- Linha de tiro: em encontro, um inimigo de tiro direto (Shooter, SpreadShooter, Sniper, Hooker) com a linha bloqueada por geometria **avança** até o jogador em vez de esperar atrás da cobertura. Defeito achado no caminho: o Emissor recuava para 9–11 m, ficava atrás da cobertura sem linha e sem rota, e travava sem atirar.
- Testes 09B: o roteiro `SpatialGameplay09B` assume "centro quieto" (jogador imune; ranged a 6–9 m). Agora ele devolve os outros papéis às posições de spawn, com IA pausada e projéteis em voo recolhidos, só durante as checagens isoladas de golpe básico, skills e dash, e os reativa antes das mortes forçadas. Geometria das salas e asserções intactas. A mensagem de "Ranged reacquisition" ganhou o estado da IA para diagnóstico.
- **Validação (10/11/2026):** compila sem erros. Final: 09B L/Courtyard 24/24 + `RangedFluidityPlayModeTests` 16/16 (novo: Emissor sem orçamento ainda atira) + 1b 5/5, job `c2aa2f5a`. A suíte PlayMode completa foi rodada **antes** das correções finais (job `f8ca1906`: 768/45/45 quarentena); as falhas de input/pausa/mapa passam isoladas com e sem a mudança (dependem da ordem). Suíte completa após as correções: **não rodada**. Não jogado no Editor.
- Não mexido: mix de papéis das salas (sorteio do `ManualRunRandomizer`, preso a seeds/distribuições testadas), limite de 3 atacantes melee simultâneos, dano e cadência.

### Revisão de design dos inimigos (10/11/2026, só análise)
Tabela completa, camadas, mix e boss: plano, seção "Inimigos: papéis e ameaça em camadas".

**Feito:** levantamento por leitura de código, prefabs e assets dos 16 arquétipos, de Emissor/Fragmento e do `SectorBoss`. Nenhum código de gameplay foi alterado. Nada foi jogado nem rodado no Unity.
- Base igual para todos (dano 12, cadência 1,5 s): nenhum inimigo se destaca como o perigoso.
- No máximo 3 atacando em melee, e o Swarm disputa esses lugares. Salas são sorteio uniforme num pool de 18, sem regra de combinação. A elite é o variante legado `Magic_Haste`.
- **Esquisitos achados:**
  - **Charger:** só ataca colado (≤1,8 m), então a investida nunca vem de longe, apesar da distância preferida de 6 m.
  - **Hooker:** puxa sem dano e sem castigo; para a Manopla, isso ajuda.
  - **Healer:** foge e tenta socar ao mesmo tempo.
  - **Mirror:** reflete só projéteis, então com a Manopla é um Grunt mais tanque.
  - **Fragile:** repete Swarm/Rush.
  - **Pool:** Emissor e Fragmento dobram o peso de Sniper e Rush.
  - **Bomber:** escala 0,5, difícil de ler.
- **Boss:** só usa golpes do Heavy e do Charger, fica 1,4 s parado entre ciclos, luta sozinho e não tem fase nova.

**Proposta:** papéis em 6 camadas: bucha → linha → âncora → combinador → prioridade → elite/boss. Cada sala vira um "pacote": 1 âncora + bucha em ondas + 0–1 combinador + 0–1 prioridade. A bucha sai do limite de 3. Elite vira "Comandante", que piora os comuns ao redor. Boss: núcleo exposto + enxame a cada terço, combos sem pausa morta.

**Decisões para o desenvolvedor:**
1. Trocar o sorteio uniforme por pacotes de papéis? (Mexe no `ManualRunRandomizer`; as salas 09/09B ficam intactas.)
2. Fragile: fundir no Swarm ou virar Volátil (explode ao morrer)?
3. Mirror: cortar até o Arco ou virar Guarda (frente imune a golpe, abre com quebra de postura)?
4. Hooker: puxar para zona/aliados ou trocar o puxão por enraizar?
5. SpreadShooter: manter ou virar modificador do Shooter?
6. Emissor/Fragmento: só skins (peso único no pool) ou sair do pool?
7. Boss: direção A (núcleo + enxame), B (arena que encolhe), C (combos) ou A+C (recomendado)?

**Decisões tomadas (10/11/2026):** todas as recomendações aprovadas, com estes ajustes:
1. Pacotes de papéis no lugar do sorteio uniforme (salas 09/09B intactas).
2. Fragile → **Volátil** (explode ao morrer).
3. Mirror → **Guarda** (frente imune, abre com quebra de postura).
4. Hooker → **gancho rápido em linha**, no estilo do hook de inimigos puxadores de MOBA/ARPG (Pudge, Diablo IV): projétil de skillshot com telegraph legível; se acertar, arrasta o jogador rápido até o Hooker, de preferência para perto dos aliados ou de zonas de perigo. Substitui o puxão lento atual.
5. SpreadShooter → modificador do Shooter.
6. Emissor e Fragmento → skins, com peso único no pool.
7. Bosses: **frente separada, depois**. Cada boss terá estilo único e épico, com referências de God of War, Phantom Blade Zero e Wukong adaptadas ao isométrico, e de Lost Ark e Diablo, sem copiar. A direção A+C do boss do Universo 1 vira insumo dessa frente.
- **Novo inimigo (do desenvolvedor):** ranged com metralhadora na cauda que atira **laser em rajadas**. Subcategoria dos Shooters ("Rajada"). O prefab está em desenvolvimento; caminho a confirmar.

**O que prototipar primeiro (uma frente):** sala-pacote com inimigos existentes: *Fornalha* (Heavy + 6 Swarm + HazardCaster) e *Anzol* (Charger corrigido + Bomber + Rush), com Swarm/Rush fora do limite de 3. É o teste mais barato de "o problema é papel, não número". Para validar: jogar 3 salas de cada pacote e responder se algum momento obrigou a sair do lugar ou trocar de alvo. Depois: boss A+C.

### Protótipo: salas-pacote Fornalha e Anzol (10/11/2026, descartável)
- **Como ligar:** menu do Editor **Tech Guy ▸ Protótipo ▸ Salas-pacote (Fornalha · Anzol)**. Fica desligado por padrão, vale a partir da próxima run e só existe no Editor. Com ele ligado, as salas de combate normais trocam o sorteio por pacotes: id ímpar = **Fornalha** (Heavy + 6 Swarm + HazardCaster), id par = **Anzol** (Charger + Bomber + 4 Rush). Elite, boss e as demais salas não mudam. Um rótulo laranja "PROTÓTIPO · sala-pacote …" aparece durante o encontro.
- Nas salas-pacote, Swarm e Rush atacam **fora do limite de 3 atacantes melee**; Heavy, Charger e os outros continuam no limite.
- **Charger corrigido (vale sempre, não só no protótipo):** abre a investida a até 6,5 m (antes, só colado) e investe até 7,5 m.
- Posições: salas espaciais usam os slots seguros do próprio layout; salas retangulares usam uma grade 3×4 afastada das coberturas. O gerador de salas, as seeds e as salas 09/09B não mudam (o pacote é aplicado sobre a cópia da rota da run).
- **Validação:** compila sem erros. Testes focados: `RoomPacksPrototypePlayModeTests` 3/3 (novo), `RangedFluidityPlayModeTests` 17/17 (inclui o Charger investindo de >3 m), `EnemyPhysicalAttackTests` + `ArchetypeIntegrationTests` + 1b 14/14. Não jogado no Editor.
- **Defeito de input no Editor (corrigido em 10/11/2026, sem relação com o protótipo):** execuções de teste abortadas ou com falha deixavam teclados/mouses virtuais registrados no Editor (havia 32), e o último continuava como `Keyboard.current`. O jogo lia o teclado fantasma e o jogador não andava após o Play. Correção: `Core/EditorInputDeviceGuard.cs` (só no Editor) devolve o teclado e o mouse reais como "atuais" ao entrar no Play. Os testes que criam dispositivos virtuais continuam passando (3/3 de input direto). Segundo defeito, a causa do "só anda pelo mouse": o foco do input (`CharControlScript._directHasFocus`) só era atualizado pelo evento `OnApplicationFocus`, que no Editor às vezes não chega com "focado" depois do Play (Play iniciado sem o Game view focado, modo Play Unfocused). Isso deixava WASD, ataques e skills descartados a sessão inteira. Correção: o foco se recupera quando `Application.isFocused` é verdadeiro (só liga, nunca desliga). Testes de input direto 5/5. **Causa raiz:** com a configuração "ResetAndDisableNonBackgroundDevices", o Input System desativa o teclado e o mouse quando o Editor perde o foco e deveria reativá-los ao voltar; nesse Editor a reativação falhava, e os dispositivos ficavam desativados a sessão inteira (só o input antigo, o clicar-para-mover, funcionava). Correção em `Core/EditorInputDeviceGuard.cs` (só no Editor): ao entrar no Play, remove teclados/mouses virtuais que vazaram de testes; durante o Play, com o Editor focado, reativa o teclado e o mouse reais; enquanto um teste usa dispositivos virtuais, desliga os reais para o teste não sofrer interferência. Testes de input direto 5/5 (job `6e8695d2`). A configuração do projeto não mudou (uma tentativa de trocar o "editorInputBehaviorInPlayMode" foi revertida por não ser a causa).
- O bloqueio por corpo **continua** (decisão do desenvolvedor): se o jogador se deixa cercar, o problema é dele.
- **O que jogar para validar:** ligar o protótipo, jogar 3 salas de cada pacote (F9 pula para o boss quando quiser) e responder: algum momento te obrigou a sair do lugar ou a trocar de alvo? A Fornalha passa sensação de poder com risco? O Anzol pune quem fica parado?
- **Playtest (11/11/2026):** aprovado como direção ("mais frenético"). Ficar parado não mata, mas os ranged obrigam a se aproximar. O Anzol pune dash no timing ou direção errada. O único medo real veio de um **loop de puxões do Hooker** (não conseguia sair até o cooldown). Alguns ataques de longe são **rápidos demais** (medido: Shooter e SpreadShooter têm 0,25 s de preparação e nenhum aviso visível; Bomber 0,35 s; HazardCaster 0,25 s). Pedidos do desenvolvedor: **menos vida inicial** (hoje 1000) para o jogador ter cuidado, e **cooldown no dash** (hoje 0 s e custo 0: dash infinito).

### Passe de ameaça 2 (11/11/2026)
- **Salas-pacote viram o padrão:** ligadas em builds e no Editor (o menu Tech Guy ▸ Protótipo ▸ Salas-pacote desliga, para comparar). Rótulo "PROTÓTIPO" removido.
- **Ataques de longe com aviso justo:** Shooter e SpreadShooter preparam em 0,6 s (antes 0,25 s, sem aviso), com linha/cone visível; a mira do Shooter fica travada no início do aviso, então dá para desviar andando de lado. Bomber 0,35 → 0,6 s; HazardCaster 0,25 → 0,6 s.
- **Hooker sem loop:** a janela pós-puxão em que o jogador não pode ser puxado de novo subiu de 0,6 s para 2,5 s.
- **Dash:** cooldown 0 → 1,2 s (`Dash.asset`), mostrado no indicador de dash.
- **Vida inicial:** 1000 → 700 (`Resources/CombatBalanceConfig.asset`); com Vida reforçada, 900.
- **Validação:** compila sem erros. Testes focados, 101 + 1: ranged, projéteis, apresentação inimiga, puxão (RUN00), input direto, dash/cancel, 1b, salas-pacote, arquétipos, ataque físico, 24 variantes 09B e bordas côncavas/gancho. Ajustes de teste: janela de puxão usa a constante nova; o teste de bordas côncavas espera o cooldown do dash e declara foco explícito, como o restante da fixture. Suíte completa não rodada (é do desenvolvedor). Não jogado.
- **O que jogar para validar:** 2–3 runs. Os tiros de longe agora dão para ler e desviar? O Hooker ainda prende? O dash com cooldown obriga a escolher o momento? Com 700 de vida, dá medo sem ficar injusto?

### Frente de legibilidade e ameaça (11/11/2026)
- **Melee atacam de verdade:** confirmado nos prefabs que a escala 0,5 dá alcance de ~1,1 m, enquanto a distância preferida era 1,8–2,5 m. A regra de "aproximar até o próprio alcance" (`EnemyAI.PositioningAttackRange`), antes só do Grunt normal, vale agora para Rush, Grunt, Heavy, Swarm, Fragile e Mirror, em qualquer rank exceto Boss. O teste de escopo foi atualizado para o contrato novo, e um teste novo exige que Rush, Heavy e Swarm acertem um alvo parado.
- **Nomes dos inimigos:** cada inimigo mostra o nome do arquétipo acima dele, colorido por papel (branco = melee, laranja = tiro, roxo = controle, verde = suporte; " · Elite" quando aplicável). Só no Editor, ligado por padrão; menu **Tech Guy ▸ Protótipo ▸ Nomes dos inimigos**.
- **HazardCaster:** a IA genérica disparava um "ataque" dele a cada 1,5 s (aviso sem chão) e podia cancelar a criação real do chão. Agora só o ciclo próprio dele (a cada 6 s) cria o chão. O dano continua vindo de pisar no chão, não da criação.
- **Tiros diretos:** aviso de 0,6 → 0,8 s e linha de mira mais grossa (1 m; o projétil não mudou).
- **Validação:** compila sem erros; testes focados 53/53 (melee, ranged, arquétipos, ataque físico, apresentação, projéteis, salas-pacote, 1b) e 09B + bordas 24/25. A falha, `Distribution09BL270Bow` (o alvo leva 18 de dano antes da checagem de tiro bloqueado), é intermitente: passa isolada e já tinha aparecido antes em outra variante do Arco (arma pausada). Não jogado.

- **Playtest (11/11/2026):** os nomes facilitaram muito o feedback. Os melee atacam, mas **ainda não ameaçam**; 700 de vida ainda parece muito. HazardCaster bem melhor. Tiros diretos não apareceram: os pacotes Fornalha e Anzol não têm Shooter nem SpreadShooter, que só aparecem nas salas de elite.

### Passe de ameaça 3 (11/11/2026)
Decisões do desenvolvedor: dano por papel, vida 500, mais atacantes juntos, terceiro pacote "Mira".
- **Dano por golpe** (base 12 × multiplicador do perfil em `ScriptableObjects/Enemies/Archetypes/*.asset`): Rush, Grunt e Mirror 24; Heavy e Charger 45; Swarm 10; Fragile 18; Shooter 22; Sniper 36; Bomber 30. Hooker sem mudança (o gancho não causa dano).
- **Vida inicial:** 700 → 500 (Vida reforçada: 700).
- **Atacantes melee simultâneos:** 3 → 5 (`_maxConcurrentMeleePerRoom` no FirstSector). Swarm e Rush continuam fora do limite nas salas-pacote.
- **Pacote Mira:** 3 Grunt + 2 Shooter + 1 SpreadShooter. As salas de combate revezam Fornalha, Anzol e Mira pelo id da sala.
- **Validação:** compila sem erros; testes focados 46/46 (melee com dano real por papel, ranged, arquétipos, ataque físico, salas-pacote com os três pacotes, 1b, projéteis). Não jogado.
- **O que jogar:** os melee agora ameaçam? 500 de vida dá medo sem ficar injusto? Na Mira, o aviso dos tiros diretos (0,8 s, linha grossa) dá para ler e desviar?

- **Playtest do passe 3 (11/11/2026):** 500 de vida ficou mais justo; o tiro direto ficou legível. Problemas: andando, o jogador escapa de quase todos (jogador 5,0 contra 2,1–4,2 dos inimigos; só o Rush, 5,25, acompanhava) e 2 socos fracos matam a maioria (vida base 60; Swarm 12, Rush 36).

### Passe de ameaça 4 (11/11/2026)
Decisões: vida por papel e velocidade dos comuns (investida curta e jogador mais lento ficaram de fora).
- **Vida:** Grunt 150, Rush 90, Charger 165, Heavy 330, Mirror 180, atiradores e controle 120 (Sniper 108), Fragile 60. Swarm continua 12 (bucha).
- **Velocidade (jogador 5,0):** Grunt, Swarm, Charger, Fragile, Shooter e SpreadShooter 4,55; Mirror, Bomber, HazardCaster e Hooker 4,2; Sniper 3,85. Rush 5,25 e Heavy 2,1, sem mudança.
- **Testes:** compila sem erros; 41/41 focados. O teste dos melee agora confere vida e velocidade reais. Dois testes que fixavam a vida antiga do Grunt (60) passaram a usar a vida máxima. O teste da animação de recuo do Shooter teve a faixa ajustada para "caminhada, longe de corrida" (0,3–0,65): com o Shooter mais rápido, o valor amortecido ainda está subindo quando é medido.
- **O que jogar:** fugir andando ainda funciona, mas devagar o bastante para o dash ser a saída? Os inimigos aguentam o suficiente para os golpes deles importarem?

- **Playtest do passe 4 (11/11/2026):** ainda dá para fugir andando, sem dash (rever depois). O melee para ao lado do jogador e só então começa o golpe, o que dá tempo de sair. Swarm frágil demais; deve aguentar mais e bater pouco, ser bucha. Mais Swarm. Heavy ainda pouco tanque. HazardCaster perfeito.

### Passe de ameaça 5 (11/11/2026)
- **Golpe quase na chegada:** preparação dos comuns encurtada (`EnemyAttackPatterns.MeleeWindup`): Rush 0,3 → 0,25 s (piso de aviso R2.1); Grunt 0,4 → 0,3 s; Swarm e Fragile 0,7 (padrão) → 0,3 s; Mirror 0,7 → 0,35 s. Heavy e Charger mantêm os avisos longos.
- **Swarm bucha:** vida 12 → 45, dano 10 → 6. **Fornalha:** 6 → 9 Swarm.
- **Heavy:** vida 330 → 600.
- **Testes:** compila sem erros; 41/41 focados (com vida/dano reais de Swarm e Heavy e a Fornalha de 9 Swarm). Não jogado.
- **Playtest (11/11/2026): aprovado.** "Ficou muito melhor": medo de morrer no meio da run; depois de 3 salas, com 282 de 500 de vida, mesmo sem ficar parado batendo. É a run em que o desenvolvedor mais apanhou.
- **Pendente para rever:** fugir andando ainda é fácil (possíveis caminhos: investida curta nos comuns, jogador mais lento, dash com cooldown maior já decidido para a frente de mana/HUD/dash).

### Frente de mana, HUD e dash (11/11/2026)
- **Mana desligada para o jogador:** chave `playerManaEnabled` no `CombatBalanceConfig`, desligada. As skills não exigem nem gastam mana (`PlayerActor.HasMana`/`TrySpendMana`). O HUD esconde o orbe de mana, o valor, a legenda e o ornamento do lado do orbe; os textos das skills mostram só o cooldown. O sistema de mana continua intacto, para ser revisitado.
- **Testes:** `ManaSystemTestDefaults` (assembly PlayMode inteira) religa a mana, para os testes existentes continuarem exercitando o sistema; `PlayerManaSwitchPlayModeTests` (novo) garante o estado do jogo: mana desligada no config, skills sem gasto e orbe escondido.
- **Dash:** cooldown 1,2 → 2,0 s.
- **Validação:** compila sem erros; 45/46 focados (mana, Asura, responsividade, cancelamentos, dash, input direto, 1b, salas-pacote, mutações). A falha, `Spatial09ReinforcementsConcaveEdgesAndHookRecovery`, é "Failed to capture screen shot" na 4ª sala, depois das checagens de jogo: ambiente (a aba Game provavelmente não está visível); o mesmo teste passou mais cedo hoje. Não jogado.
- **Playtest (11/11/2026): aprovado** ("ficou tudo correto").
- **O que jogar:** o HUD sem o orbe de mana ficou bem resolvido ou deixou um vazio estranho? O dash com 2,0 s obriga a escolher o momento? Ainda dá para fugir andando com facilidade?

### Caminhos mais curtos (11/11/2026)
- **Compactação da rota** (`ManualRunRandomizer.Compact`): depois do sorteio, cada nível de profundidade fica a meia-profundidade das maiores salas vizinhas mais **14 m** de corredor; cada ramo de bifurcação fica a meia-largura da sua maior sala mais **16 m**. Considera a rotação das salas espaciais. Sorteio, seeds e conteúdo das salas não mudam; só os centros das salas.
- **Medido em 200 rotas:** corredores com média de 58 → **36 m**, mediana 68 → **41 m**, 90% até 84 → **50 m**.
- **Testes:** EditMode 45/45 (corredores não atravessam outras salas, não dividem chão, aberturas únicas, 09/09B). Um contrato mudou de propósito: a variedade de salas do 09B pode mover a sala, porque agora a posição acompanha o tamanho real; o teste passou a exigir posições iguais para a mesma seed. PlayMode: testes de mundo da run e caminhos gerados passaram.
- **Bloqueio de ambiente:** os testes visuais das salas 09/09B falham com "Failed to capture screen shot", **inclusive no código do último commit, sem estas mudanças** (comparação feita guardando as alterações). Provável causa: o Game view está num segundo monitor. Falhas antigas, sem relação: testes que pedem Arco ou Lança, bloqueados por progressão desde a 1b.
- **O que jogar:** a caminhada entre salas ficou curta o bastante? Alguma sala ou corredor ficou sobreposto, apertado ou estranho, principalmente nas bifurcações?

### Próximo, depois dos caminhos curtos (pedido do desenvolvedor, 11/11/2026)
- **Hooker como projétil visível, no estilo do hook do Thresh (LoL):** o gancho sai como um projétil que dá para ver vindo e desviar; lançamento mais rápido e puxão mais rápido até o Hooker. Substitui o puxão atual (decisão 4 da revisão de inimigos). Manter a janela anti-loop de 2,5 s.
- **Hooker útil fora do gancho:** enquanto o gancho recarrega, ataques de média distância usando a corrente/o gancho (golpes, não puxão). O gancho não volta a ser usado assim que o cooldown termina: uso com atraso aleatório, para ficar imprevisível.

## Roteiro de validação antes da próxima etapa (11/11/2026)
Preparação: com o jogo fechado, apagar `MetaProgress.json` (e `MetaProgress.v1.json`, se existir) em `%USERPROFILE%\AppData\LocalLow\DefaultCompany\T.G - Tech Guy\`. Salas-pacote ligadas (padrão). Jogar a partir do MainMenu. F9 pula para o boss.
1. **Input no Editor:** WASD, ataque, especial (botão direito), Q/E/R, dash, Esc. Sair do Unity (Alt+Tab) e voltar: tudo continua respondendo.
2. **Run 1 (morrer de propósito):** vida 700. Salas alternam Fornalha / Anzol. Na 2ª sala de combate, o NPC aparece preso (âmbar) desde o início, falando; vencer destrava (ciano); libertar cura 30%. Anotar os Fragmentos ganhos.
3. **Ameaça:** Shooter e SpreadShooter mostram linha/cone por ~0,6 s e dá para desviar andando de lado; Bomber e HazardCaster têm aviso legível; o Charger investe de longe; depois de um puxão do Hooker, há ~2,5 s sem novo puxão; o dash espera 1,2 s (indicador no HUD); Swarm cerca (bloqueio por corpo mantido).
4. **Nexus:** tela de resultado com Fragmentos e próxima meta; terminal com custos 20/40/50/60/75/90; a conversa toca uma vez e depois o terminal abre direto. Comprar Vida reforçada quando der.
5. **Run 2:** vida 900 com a Vida reforçada; sem cápsula (NPC já resgatado); vencer o boss (+10).
6. **Ao final:** em `MetaProgress.json`, conferir `fragments`, `upgrades`, `rescuedAllies`, `seenDialogues` e `runHistory`.
Perguntas para responder depois: a vida de 700 dá cuidado sem ficar injusta? O cooldown de 1,2 s no dash está certo? Os ataques de longe ficaram justos? O Hooker ainda prende? A economia está no ritmo (1ª compra em 1–2 runs)?

### Playtest do passe de ameaça 2 (11/11/2026)
- **Aprovado:** Vida reforçada funciona; Hooker sem loop, como combinado; dash com cooldown ficou melhor (pode subir mais); 1ª compra sai na 1ª run completa, ou em 2.
- **Melee quase não atacam.** O dano vem só do Bomber. Causa provável, por leitura de código, ainda não confirmada no Unity: a regra que limita a distância de espera ao alcance do golpe (`EnemyAI.PositioningAttackRange`) só vale para o Grunt; Rush (para a 2,0 m), Heavy (2,5 m) e Swarm (1,8 m) param fora do próprio alcance (~1,8 m ou menos) e só acertam se o jogador entrar.
- **Vida 700 ≈ infinita** com os inimigos como estão (consequência do item acima).
- **HazardCaster:** não causa dano no momento em que cria o chão, só quando o chão já existe; ataca rápido demais.
- **Avisos de longe ainda fracos**, exceto Sniper, Hooker e Charger. Bomber e HazardCaster não foram identificados.
- **Identificação:** todos usam o mesmo modelo; o desenvolvedor não consegue dizer qual inimigo é qual. Pedido: identificação visível para dar feedback.
- **Caminhos entre salas longos demais:** encurtar ou dar relevância. Ideias do desenvolvedor para recompensas de dash: cargas extras, golpes básicos que reduzem o cooldown.
- **Mana sem função:** as skills são usadas livremente. O desenvolvedor considera remover a mana e ajustar o HUD; quer opiniões antes.
- **Decisões (11/11/2026):**
  1. **Mana:** sai do jogador (custos 0, orbe fora do HUD; sistema interno mantido desligado). **Revisitar depois**, por exemplo quando Lança e Arco voltarem com recursos próprios.
  2. **Caminhos:** encurtar já; dar relevância depois (Fase 2: Fragmentos, emboscadas ou eventos no caminho).
  3. **Dash:** cooldown 2,0 s. Boons de dash (cargas extras, básico que reduz o cooldown) entram na frente de boons.
  4. **Ordem:** (a) legibilidade e ameaça: rótulo com o nome de cada inimigo, melee atacando dentro do alcance, avisos de longe mais fortes, HazardCaster ajustado; (b) mana, HUD e dash 2,0 s; (c) caminhos mais curtos. Uma frente por vez, cada uma jogável.

## Próxima etapa
Passe de inimigos jogado em 10/11/2026: aggro e contorno de cobertura aprovados; ranged ficaram alcançáveis (às vezes ainda recuam). **Ainda não há perigo, nem no boss**; segundo o desenvolvedor, a causa é o papel de cada inimigo, não valores ou quantidade. Decisões da revisão aprovadas em 10/11/2026. Salas-pacote aprovadas em 11/11/2026; passe de ameaça 2 jogado em 11/11/2026. Frente de ameaça (passes 1–5) aprovada em 11/11/2026. Mana, HUD e dash aprovados em 11/11/2026. **Agora: playtest dos caminhos mais curtos** → Hooker (gancho visível e ataques de média distância) → mana/HUD/dash → caminhos curtos → (se aprovado) Volátil, Guarda, gancho do Hooker, Shooter/Rajada e pool → Fase 1c (rapport da Ico). Bosses: frente própria, depois.

## Backlog
- Boss do Universo 1 aprovado tecnicamente, mas sem graça no playtest; redesign pendente (diagnóstico e direções A/B/C na revisão de inimigos de 10/11).
- Revive por run → artefato na Fase 1c.
- Inimigos passivos e ranged que fogem (playtest 1b): etapa 1 feita (aggro e orçamento de recuo); mix de papéis em aberto.
- **Depois da Fase 1d:** tela "Escolha sua próxima jornada" ao fim de um universo: 2–3 percursos lado a lado, cada um com sua rota em miniatura (reaproveitar `RouteStripModel` e o desenho da faixa de percurso). Referência: print do desenvolvedor de 10/11/2026.

## Decisões em aberto (do desenvolvedor)
- Nome do Desconhecido, se ele ganhar um ao ser "terminado". *(Decidido em 10/11/2026: o Desconhecido é um NPC inacabado do jogo do protagonista; ver `Docs/00_VISAO.md` → Narrativa.)*
- Terminal fechado até o 1º resgate ou versão mínima desde a 1ª run.

## Regras de processo
Uma frente por vez · cada etapa termina jogável · relatório de 1 página (Feito / Não feito / Não testado / O que jogar) · sem novos arquivos em `Docs/decisions/` por etapa · boon que não aparece em 10 s de vídeo não entra.
