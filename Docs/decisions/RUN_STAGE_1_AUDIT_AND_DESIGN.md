# RUN-EXP-01 — Etapa 1: auditoria e desenho da incursão

**30/10/2026. Auditoria concluída no recorte descrito; melhorias abaixo são propostas para revisão.** Não constitui aceite humano, mudança de economia ou autorização da implementação seguinte. A etapa0 entregou correções localizadas com regressões abertas, documentadas no relatório próprio. Esta etapa não tenta fechar GPR-01.

## Visão e método

Tech Guy é um desenvolvedor preso no computador buscando a Terra por mundos digitais; armas companheiras e kits distintos sustentam incursões com decisões. Direção desta frente: controle recuperável → ameaça identificável → escolha com consequência → variedade espacial → conteúdo adicional. Não transformar a run em compra garantida de A+B+C.

Fontes: AGENTS, Docs/README, visão/pilares/estado/prioridades/arquitetura, canônicos systems01–06, art01–04, matriz/captura/telemetria/playtest e requisitos/desenho/tarefas GPR vigentes. Histórico repetido foi consultado conforme relevância, não revalidado em bloco. Evidência atual: código, assets e consultas Unity MCP no projeto gameplayOverhaul/653db8b com alterações locais; [inventário de runtime](../validation/artifacts/run-exp-01/runtime-inventory.json) e [prefabs](../validation/artifacts/run-exp-01/enemy-prefabs.json). Comparadores e fixtures têm assistência descrita na etapa0. Não houve nova partida humana cronometrada.

Skills: locais game-vision, combat-unity, boon-design, unity-acceptance; router, unity-mcp-orchestrator, unity-csharp-scripting, game-feel; tech-guy-world-identity/world-card, tech-guy-level-layout/level-brief, tech-guy-enemy-encounters/encounter-spec, create-game-assets e performance-optimization. Não usar geração procedural como pressuposto para resolver identidade.

## Recompensas e descoberta de builds

`ManualRunRandomizer` conserva grafo e separa streams de layout/inimigos/recompensas. Primeiro combate comum: Technique+Protocol; sala de Mutation: uma aquisição; demais combates comuns:1–2, Elite:2–3, alternando Technique/Protocol; boss sem seleção dessas famílias. A amostra GPR anterior teve uma Mutation e2–7 oportunidades Protocol, mas é **evidência histórica**, não taxa-alvo desta frente. A run não garante sobrevivência, oportunidade útil ou tempo de experimentação por ter muitos tickets.

`RunRewardPolicy` identifica sessão/nó/ordinal, guarda revisão e estados Pending/Committing/Resolved/Faulted; reabertura com inventário idêntico conserva oferta. Sorteio `BoonOfferDraw` deduplica por ID, escolhe até3 sem reposição, ordena por ID antes do RNG e respeita SelectionWeight existente. Pesos complementares2:1 existentes não foram modificados. Falha no commit não é rollback universal. Mutation inicial sem elegível bloqueia boss e permite abandono; alternativas Protocol não podem fingir que concederam Mutation. Preservar esse contrato.

**Achado crítico de contexto:** `TitanicConductorPlaytestRewards.Enabled` foi encontrado **true** no Editor após restauração dos testes. Esse modo temporário transforma as recompensas de combate em Mutation+Protocol e prioriza A/B com oferta unitária. O build não Editor retorna false, mas um playtest físico nesse Editor pode parecer uma build garantida. A preferência preexistente foi preservada, não desativada silenciosamente. Para avaliar descoberta natural, desligar explicitamente o menu `Tools/Tech Guy/Testar Tornado + Conductor` e registrar a condição da sessão. Não atribuir resultados desse modo à economia normal.

Pools base elegíveis consultados no Unity, inventário vazio e ProtocolInventory nulo (não é simulação de run):

| Arma | Technique | Mutation | Protocol |
|---|---:|---:|---:|
| Manopla |4: EchoingFist, TemporalEcho, AsuraEcho, FlurryEcho |3: CadencedAsura, EclipseUppercut, TitanicFists |4: Ignite/Frost/Conductor/Detonation |
| Arco |1: Ricochet |4: HuntingBond, JudgmentArrow, MineGarden, RapidBurst |4 |
| Lança |1: EchoThrust |5: DragonOrbit, HorizonCut, StakeSiege, ChainThrust, PhantomSpear |4 |

IDs completos no JSON; quantidades mudam com ranks, capacidades e inventário. Um pool de uma Technique não oferece três decisões equivalentes: a UI pode exibir menos opções e o fallback alterar a família. Deduplicação por ID resolve duplicatas exatas, não escolhas funcionalmente redundantes. Não aumentar count artificialmente nem habilitar catálogo aposentado para completar três cards.

| Problema / evidência | Proposta | Alternativa e custo | Gate |
|---|---|---|---|
| Uma oportunidade de transformação estrutural no nó5; três opções da Manopla podem caber juntas numa oferta | Mapear habilitadores por estilo e momento; manter kit base viável; testar uma oportunidade estrutural anterior **somente após aprovação da economia** | Conservar timing e melhorar primeiro a comunicação: menor risco, não aumenta tempo com build | Medir encontros restantes e usos reais da transformação, sem alvo de completarABC |
| Arco/Lança têm só1 Technique elegível na condição base | Primeiro auditar saturação/alternativas durante a run e mostrar família alternativa honestamente | Criar suporte comportamental compatível depois, custa executor/arte/testes; novos boons não estão autorizados aqui | Nenhuma escolha morta; variedade de decisões, não só cardcount |
| Muitas aquisições sequenciais podem interromper ritmo | Agrupar apresentação por ticket pendente, sem agrupar commits nem conceder em lote | Manter telas atuais, melhorar hierarquia/ordinais: implementação menor | Uma confirmação por aquisição; reabertura/pausa/morte preservadas |
| Potential de sinergia depende de texto/compatibilidade | Card com ação alterada, efeito imediato, interação possível e limitação; separar “funciona agora” de “pode combinar” | Ícone/tag resumido mais compacto, porém exige glossário | Jogador descreve a decisão sem abrir logs; texto corresponde ao executor |
| Viabilidade entre estilos não medida | Registrar trajetória de escolhas por arma/seed, dano, usos, sobrevivência e motivos de recusa | Só simulação é barata, mas não mede diversão nem execução | Contrastar pressão/postura, precisão/marca, trajetórias/retorno; nenhum vira caminho obrigatório |

Exemplo de conceito de card, **não UI implementada**: “Punhos Titânicos — Básico da Manopla. O golpe terminal emite tornado móvel, inclusive no vazio. Agora: alcançar uma linha além do soco. Potencial: Conductor acrescenta descarga; Detonation acrescenta explosão elegível em morte. Restrição: depende do terminal e do pacote aceito; não aplica queimadura.” Não prometer que os Protocols serão ofertados. “Força agora” deve competir com “potencial depois” sem ocultar custo/limites.

Medição proposta: seeds iguais por arma e políticas de escolha distintas; ofertas e famílias elegíveis, repetição semântica, aquisições por terço de run, oportunidade após saturação e encontros/usos restantes. Tempo real por terço continua **não medido**; não inventar duração a partir de distância. Q/E/R modular permanece atividade separada; metadados devem apontar ação/capacidade e input, não congelar uma quarta skill ou slot antigo.

## Nexus, grafo e salas atuais

Fluxo publicado: MainMenu→PrologueTutorial (conforme save)→NexusBlockout→FirstSector→NexusBlockout. NexusLobby é legado/fixture; não substituir a entrada publicada. FirstSector usa ManualSlice com randomização e materialização em runtime. Arquivo autorado e layout gerado são estados diferentes.

Grafo verificado no asset: `0→1→2→(3 reparo OU4 combate)→5 Mutation→(6 combate OU7 Elite→8 combate)→9→10 boss`. Quatro caminhos,11 nós,12 conexões. Escolhas de ramos são exclusivas. Reparo3 tem fração0,35 autorada. Elite7 usa Magic_Haste; boss10 parte de Heavy e SectorBoss. Extração depois de boss; abandono não é vitória. Ganhar Mutation é gate, não apenas recomendação.

Autoria: arenas principalmente24×20, nó5:32×20. Geração: larguras22–30, profundidades18–24, passos longitudinais38–50 e offsets laterais48–60; mantém topologia. `ManualStageEnvironment.Procedural` gera piso, paredes periféricas, conexões, barreiras e NavMesh da sessão. Não há obstáculo interno/objetivo espacial exclusivo nesse gerador. `PlanEnemies`:4–10 por encontro, variação por profundidade, Elite autorado e até2 suportes após atacantes; posições em grade3×4 com jitter e inset3m. A seleção diversifica tipos, mas não prova composição tática justa.

Consequência: muitas arenas retangulares vazias têm a mesma decisão (“eliminar todos”) e corredores existem para ligar nós afastados, não para um objetivo próprio. Paredes/portas altas e câmera precisam de inspeção em movimento, inclusive VFX simultâneos. O relato humano anterior de travessias extensas/mesma aparência é consistente com a autoria; nenhuma duração, falha de câmera universal ou taxa de repetição foi fabricada.

### Taxonomia proposta — não publicar automaticamente

Tempos abaixo são **categorias de hipótese**, não tuning aprovado: curto≈30–45s, médio≈45–75s, longo≈75–120s; devem ser calibrados por playtest. Recompensas usam tickets existentes até nova decisão.

| Tipo / propósito | Princípio espacial / composição | Spawn e câmera/navegação | Duração / recompensa / variação / custo |
|---|---|---|---|
| Arena aberta: aprender prioridade e esquiva | Área central livre, duas linhas de acesso; swarm+um ranged | Grupos visíveis de lados diferentes, sem cercar ponto de entrada; circulação em anel | Curto; ticket comum; alternar direção/papel; baseline de menor custo |
| Arena com obstáculos: quebrar LOS e alinhar trajetórias | Dois volumes baixos descontínuos, ao menos dois contornos; ranged+heavy | Spawns em bolsões alcançáveis; nenhum bloqueio da única saída; NavMeshPath e dash testados | Médio; ticket comum; alternar volumes/linhas de tiro; custo de path/oclusão medido |
| Pressão em ondas: decidir gastar ou guardar recurso | Área central e duas zonas de entrada; swarm, depois ranged/controlador | Aviso antes de reforço; teto global inclui invocados/pendentes; onda seguinte por condição explícita | Longo; recompensa única no fim; alternar ordem, não duplicar concessão; exige scheduler/limites, ainda proposto |
| Elite: aprender compromisso e punir recovery | Arena lateral ampla, sem funil obrigatório; elite+apoio limitado | Elite visível na entrada, ataques coordenados; telegraph nunca oculto por coluna | Médio/longo; tickets Elite existentes; afixo/mecânica por revisão; custo de VFX/coordenação |
| Desafio opcional: risco/recompensa anunciado | Uma restrição local legível, rota segura alternativa; roster pequeno especializado | Consentimento antes de ativar; falha não trava caminho crítico; limites claros | Curto/médio; prêmio só após economia aprovada; variar condição; exige novo objetivo, não implementado |
| Boss: identidade mecânica dedicada | Espaço para desviar padrão, landmarks periféricos; boss atual como baseline | Nada de surpresa fora da tela; reservar regiões para avisos, sem paredes escondendo ativo | Longo; extração vigente; variar fase/padrão só após aprovação; maior orçamento de animação/teste |
| Respiro/reward/transição | Chão seguro, portal e saída distinguíveis; sem inimigos | Limpar projéteis/causalidade antes da UI; não receber dano enquanto escolhe | Curto; aquisição já devida, sem prêmio extra; variar enquadramento/vestígio; custo mínimo |

Alternativa de baixo risco: conservar grafo e comparar apenas aberta versus dois obstáculos em cena isolada. Alternativa maior: módulos de sala com autoria e conectores, exigindo validação de bounds/NavMesh/gates/replay. Priorizar a primeira antes de ampliar o gerador.

## Inimigos: implementação e problema geométrico

Unity inspecionou **16 prefabs** em EnemyArchetypes. Todos referenciam exatamente as mesmas sete malhas (`MESH_FACEHAIR_03`, GLASSES, HANDS, HAT_02, HEAD_02, PANTS_02, TORSO_01),16.236 vértices somados por conjunto, provenientes do mesmo FBX RA_STYLIZED_MALE,67 bones por renderer e mesmo AnimatorController. Perfis/escala/cor não produzem uma anatomia nova. Isso confirma a crítica de leitura, sem supor que os16 apareçam numa seed.

Nenhum desses prefabs tem CombatReactionController serializado na consulta de asset. **ProgressionDirector.Manual.cs:164 adiciona o controller na raiz e configura o rank no spawn real**; portanto isso não demonstra ausência de reação na run. O Hooker instanciado diretamente pela fixture não passava por esse adaptador; o teste de interrupção anexou explicitamente a mesma autoridade. Ao integrar modelos, garantir reação/visualRoot e perfil resolvidos antes do primeiro impacto, sem resetar postura atingida. Não mass-corrigir prefabs nesta frente.

IA existente: EnemyAI percebe/navega, aproxima, kiting/standoff/retreat e cadência; EnemyCombatActions/EnemyAttackExecution possuem tell, direção comprometida quando aplicável, contato/recovery/cancel. SwarmAttackCoordinator limita melee quando vinculado; ranged não usa seus slots. Healer/ShieldSupport/Spawner/HazardCaster/Mirror têm comportamentos dedicados. Spawns comuns são Instantiate e teardown Destroy; ComponentPool reutiliza projéteis/efeitos, não comprova pool de inimigos. Não confundir infraestrutura de projétil com pooling completo de roster.

## Bíblia visual proposta: agentes de manutenção de uma memória em conflito

Nome de trabalho do primeiro mundo: **Malha de Memória**; não renomeia FirstSector. Tese: infraestrutura onde processos preservam uma rota de retorno que o sistema insiste em isolar. Regra expressiva candidata: linhas de dados indicam caminhos/ameaças, **sem mudar controles ou criar plataforma nova**. Arquitetura de placas fraturadas, encaixes e canais; metal cerâmico fosco, núcleos luminosos pequenos; chão limpo e aviso hostil prioritário. A busca pela Terra aparece futuramente em vestígios de conexão interrompida, não num sistema narrativo inventado nesta entrega.

Alternativa: organismos de papel/código dobrado, maior contraste de mundo e maior custo de deformação/rig; adiada. Evitar só neon genérico e humanoide com acessórios. Cor reforça função, mas reconhecimento deve sobreviver em cinza e por postura. Cinco candidatos, não designs finais:

| Candidato | Forma, proporção, equipamento e movimento | Papel / distância / targeting / ataque | Tell / recovery / contrajogo |
|---|---|---|---|
| **Fragmento corredor** | Baixo quadrúpede, cunha facetada, quatro patas angulares; sem cabeça/torso humano | Swarm melee; aproxima em arco e investida curta comprometida; sem especial gratuito | Abaixa corpo antes do bote; desliza e recompõe patas. Melee interrompe startup elegível; ranged reposiciona em perpendicular. Não corrigir apenas com velocidade |
| **Emissor bifurcado** | Dois blocos suspensos separados por vão, núcleo central e cano horizontal; flutua/abre metades | Ranged médio; busca LOS, reposiciona só entre tiros, projétil anunciado; sem tiro instantâneo fora da tela | Metades abrem/cano alinha e trava mira; recolhe após tiro. Melee encurta durante recovery; ranged quebra LOS/desvia lateral |
| **Carretel de contenção** | Disco horizontal baixo, seis apoios, braço gancho assimétrico e bobina grande | Controlador médio; precisa linha e espaço; um gancho primário, não dano/CC adicional automático | Arma braço, linha comprometida, recolhe cabo; pull limitado e janela da etapa0. Melee entra pelo flanco; ranged sai da faixa; dois controladores não monopolizam controle |
| **Bastião de páginas** | Massa larga, placas sobrepostas, pernas curtas e antebraço martelo desproporcional | Heavy curto; bloqueia região, vira lentamente durante preparação e compromete slam; onda secundária só se justificada | Levanta placa/martelo, bate, fica exposto lateralmente. Melee flanqueia/pune; ranged desloca para ângulo. Não imunizar tudo nem transformar HP em identidade |
| **Árbitro de ponteiros** | Elite alto, tripé, dois braços suspensos longos e arco incompleto acima do núcleo | Elite controlador de linhas; escolhe uma faixa e sequência curta, sem implementar novo boss | Abre braços antes do feixe/varredura candidata; recovery longo legível e postura com janela aprovada. Ambos os kits cruzam a faixa pelo espaço livre e punem o núcleo |

Reações propostas: Fragmento e Emissor permitem stagger/interrupt elegível e push/pull/launch conforme perfil atual; Carretel também cancela seu controle ao ser quebrado, sem projetar deslocamento por Transform. Bastião usa resistência/fallback Heavy; Árbitro mantém janela Elite e proteção de retomada, sem perma-stun. Boss mantém imunidade apropriada a deslocamentos e stagger aprovado. Novo rig não redefine ReactionCategory automaticamente.

Coordenação proposta: limite de melee existente preservado; ao testar composição, no máximo um início de controle duro por oportunidade do alvo, alternância de linhas ranged e rejeição de ataque sem aviso visível. Isso exige validação do coordenador/adaptador, não arquitetura global nova. Limites iniciais de autoria sugeridos: swarm6, ranged2, puller1, heavy1, elite1 **por estudo pequeno**, não números de produção aprovados. Crescer para32/64 somente com budgets e simultaneidade medidos, sem multiplicar avisos ilegíveis. Supports não devem criar ciclo de invocação/controle sem saída.

## Elite e boss

Base atual: Elite é rank/perfil/afixo sobre prefab, não modelo ou mecânica exclusiva garantida. SectorBoss atual Guardião do Núcleo usa aproximação, Charge, HeavySlam, DoublePunch, Shockwave; abaixo de50% abre follow-ups condicionais e reduz ciclo. Preservar essa implementação e seus gates. Direção futura: boss representa a regra do mundo com uma decisão espacial central (abrir caminho entre duas linhas), tell→commit→ativo→recovery observáveis. Primeiro produzir storyboard/arena/pose e contrato; criar boss novo requer autorização. Não adicionar fase ou imunidade só porque há conceito.

## Pipeline 3D e limites técnicos

Reutilizar Actor/EnemyVariant/EnemyAI/EnemyCombatActions, IDs/gerações, PlayerActor damage receipts, ProtocolBudget e contexto de encontro. Conteúdo por arquétipo: visualRoot, mesh/material, rig/Avatar ou rig não humanoide, motion library/poses, hurtbox e anchors de origem. Colisões, NavMeshAgent e identidade ficam na raiz estável; apresentação sob visualRoot. Attachments não podem deslocar hitbox sem contrato.

Pipeline proposto: concept turnaround frontal/lateral/traseiro → blockout em metros/Y-up → malha limpa/UV/material → rig apropriado à anatomia → idle/locomoção/tell/ativo/recovery/gethit/break/death → export FBX ou glTF conforme importador disponível → prefab candidato isolado → QA câmera/colisão/reações/pooling → revisão. Humanoide pode aproveitar retarget; quadrúpede/disco/tripé não deve ser deformado no Avatar bípede existente. Root motion e agent não podem dirigir simultaneamente.

**Blender MCP consultado, desconectado** (“Make sure the Blender addon is running”). Não há produção DCC/model generation realizada. Há SourceArt/Characters/TechGuy e FBX/animator existentes; isso não significa animações próprias novas para os cinco candidatos. Para arte final: artista ou Blender com addon ativo, turnaround aprovado, topologia/UV/rig/clip authored e licença/proveniência. Protótipos da etapa2 serão primitivas próprias, sem aquisição de assets ou promessa de arte pronta.

Budgets iniciais a validar, não metas aprovadas: poucas malhas/materiais por comum, LOD e instancing quando compatíveis; efeito/aviso com teto por encontro; nenhuma alocação permanente por tiro/reação; cleanup de cada listener/lease. Primeiro medir antes de converter todos os inimigos para pool: reutilizar objetos exige reset de Actor.CombatGeneration, HP, postura/resume, nav, target, Animator, afixos, inscrições e summons. Mesh distinta não pode criar outro Actor nem duplicar dano.

8/32/64 da etapa0 são fixtures de alvos/Protocols:2 tornados/pool2; a64 o cap32 limita vítimas e houve64 impactos descartados. CPU p95 do marcador0,2984/0,4065/1,0757ms, frame p95 Editor3,6293/3,2941/5,8779ms; GC agregado≈1,73/1,71/1,06MB. **Não é benchmark de64 agentes reais, GPU ou build final**, e marcador não isola todo custo visual. Para escala real: repetir com Animator/NavMesh/colisão/telegraphs ativos e captura de CPU/GPU/draw calls em player, incluindo spawn/despawn e pior overlap.

## Prioridades, dependências e aceite

1. **P0 controle/leitura:** conservar fix de pull e revisar fogo no playtest; fechar risco de input preso sob teclas seguradas conforme contrato atual e regredir pausa. Entrega técnica localizada, aceite humano pendente.
2. **P1 autoria/verdade de testes:** alinhar fixtures antigas a contratos já aprovados em atividade própria, sem enfraquecer checks corretos; observar assistência true e autoria de reação dos prefabs. Não mascarar322/353.
3. **P1 estudo comparável:** cinco silhuetas/poses e duas geometrias de arena isoladas. Gate: reconhecer papel sem cor/nome, tell visível à escala normal e circulação conceitual clara. Etapa2 não substitui integração de combate.
4. **P2 integração mínima após revisão:** um par de arquétipos e uma sala candidata, preservando grafo/tickets; testar comum/Heavy/Elite/Boss, paredes, múltiplas fontes e teardown. Complexidade média/alta pelo rig/ação/hitbox.
5. **P2 descoberta:** revisar comunicação de escolhas primeiro (baixa/média); proposta de timing de habilitadores/pools depois (média/alta, economia e compatibilidade). Nenhuma taxa-alvoABC.
6. **P3 expansão:** ondas, desafio e boss próprios exigem scheduler/conteúdo/arte/autorização e performance real. QER modular é trilha separada.

Para aceitar: uma pessoa reconhece ameaças e chama as mudanças da build pelo comportamento; consegue recuperar, mover/esquivar e punir; duas partidas não exigem o mesmo trio; ticket/persistência/HP reconciliados; regressões conhecidas explicitadas. Permanecem por medir duração natural, taxa de escolha/saturação e viabilidade por arma. Sem esses dados, não declarar diversão ou balanceamento aprovado.
